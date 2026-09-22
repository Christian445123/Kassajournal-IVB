using Dapper;
using Kassajournal.Core.Models;

namespace Kassajournal.Data.Remote;

/// <summary>
/// Implementierung von <see cref="IRemoteKassaGateway"/> für alle drei unterstützten Engines
/// (MySQL, PostgreSQL, SQL Server). Eine einzige Tabelle "kassa_entries" reicht für den Zweck
/// dieser App; die Upsert-Syntax unterscheidet sich leicht je Engine und wird darum je Fall gebaut.
/// </summary>
public class SqlRemoteKassaGateway(DatabaseSettings settings) : IRemoteKassaGateway
{
    public async Task<(bool Success, string? ErrorMessage)> TestConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            await using var connection = RemoteConnectionFactory.Create(settings);
            await connection.OpenAsync(ct);
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task EnsureSchemaAsync(CancellationToken ct = default)
    {
        await using var connection = RemoteConnectionFactory.Create(settings);
        await connection.OpenAsync(ct);

        var sql = settings.Engine switch
        {
            DatabaseEngine.MySql => """
                CREATE TABLE IF NOT EXISTS kassa_entries (
                    id CHAR(36) PRIMARY KEY,
                    entry_date DATE NOT NULL,
                    category INT NOT NULL,
                    amount DECIMAL(14,2) NOT NULL,
                    beleg_nr VARCHAR(64) NULL,
                    notiz VARCHAR(500) NULL,
                    is_feiertag TINYINT(1) NOT NULL DEFAULT 0,
                    created_at_utc DATETIME(3) NOT NULL,
                    updated_at_utc DATETIME(3) NOT NULL,
                    is_deleted TINYINT(1) NOT NULL DEFAULT 0,
                    INDEX idx_kassa_entries_date (entry_date),
                    INDEX idx_kassa_entries_updated (updated_at_utc)
                ) CHARACTER SET utf8mb4;
                """,
            DatabaseEngine.PostgreSql => """
                CREATE TABLE IF NOT EXISTS kassa_entries (
                    id UUID PRIMARY KEY,
                    entry_date DATE NOT NULL,
                    category INT NOT NULL,
                    amount NUMERIC(14,2) NOT NULL,
                    beleg_nr VARCHAR(64) NULL,
                    notiz VARCHAR(500) NULL,
                    is_feiertag BOOLEAN NOT NULL DEFAULT FALSE,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    is_deleted BOOLEAN NOT NULL DEFAULT FALSE
                );
                CREATE INDEX IF NOT EXISTS idx_kassa_entries_date ON kassa_entries(entry_date);
                CREATE INDEX IF NOT EXISTS idx_kassa_entries_updated ON kassa_entries(updated_at_utc);
                """,
            DatabaseEngine.SqlServer => """
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'kassa_entries')
                BEGIN
                    CREATE TABLE kassa_entries (
                        id UNIQUEIDENTIFIER PRIMARY KEY,
                        entry_date DATE NOT NULL,
                        category INT NOT NULL,
                        amount DECIMAL(14,2) NOT NULL,
                        beleg_nr NVARCHAR(64) NULL,
                        notiz NVARCHAR(500) NULL,
                        is_feiertag BIT NOT NULL DEFAULT 0,
                        created_at_utc DATETIMEOFFSET NOT NULL,
                        updated_at_utc DATETIMEOFFSET NOT NULL,
                        is_deleted BIT NOT NULL DEFAULT 0
                    );
                    CREATE INDEX idx_kassa_entries_date ON kassa_entries(entry_date);
                    CREATE INDEX idx_kassa_entries_updated ON kassa_entries(updated_at_utc);
                END
                """,
            _ => throw new NotSupportedException(),
        };

        await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: ct));
    }

    public async Task PushEntriesAsync(IReadOnlyList<KassaEntry> entries, CancellationToken ct = default)
    {
        if (entries.Count == 0)
        {
            return;
        }

        await using var connection = RemoteConnectionFactory.Create(settings);
        await connection.OpenAsync(ct);

        var sql = settings.Engine switch
        {
            DatabaseEngine.MySql => """
                INSERT INTO kassa_entries (id, entry_date, category, amount, beleg_nr, notiz, is_feiertag, created_at_utc, updated_at_utc, is_deleted)
                VALUES (@Id, @Date, @Category, @Amount, @BelegNr, @Notiz, @IsFeiertag, @CreatedAtUtc, @UpdatedAtUtc, @IsDeleted)
                ON DUPLICATE KEY UPDATE
                    entry_date = VALUES(entry_date), category = VALUES(category), amount = VALUES(amount),
                    beleg_nr = VALUES(beleg_nr), notiz = VALUES(notiz), is_feiertag = VALUES(is_feiertag),
                    updated_at_utc = VALUES(updated_at_utc), is_deleted = VALUES(is_deleted);
                """,
            DatabaseEngine.PostgreSql => """
                INSERT INTO kassa_entries (id, entry_date, category, amount, beleg_nr, notiz, is_feiertag, created_at_utc, updated_at_utc, is_deleted)
                VALUES (@Id, @Date, @Category, @Amount, @BelegNr, @Notiz, @IsFeiertag, @CreatedAtUtc, @UpdatedAtUtc, @IsDeleted)
                ON CONFLICT (id) DO UPDATE SET
                    entry_date = EXCLUDED.entry_date, category = EXCLUDED.category, amount = EXCLUDED.amount,
                    beleg_nr = EXCLUDED.beleg_nr, notiz = EXCLUDED.notiz, is_feiertag = EXCLUDED.is_feiertag,
                    updated_at_utc = EXCLUDED.updated_at_utc, is_deleted = EXCLUDED.is_deleted;
                """,
            DatabaseEngine.SqlServer => """
                MERGE INTO kassa_entries AS target
                USING (SELECT @Id AS id) AS src ON target.id = src.id
                WHEN MATCHED THEN UPDATE SET
                    entry_date = @Date, category = @Category, amount = @Amount, beleg_nr = @BelegNr,
                    notiz = @Notiz, is_feiertag = @IsFeiertag, updated_at_utc = @UpdatedAtUtc, is_deleted = @IsDeleted
                WHEN NOT MATCHED THEN INSERT (id, entry_date, category, amount, beleg_nr, notiz, is_feiertag, created_at_utc, updated_at_utc, is_deleted)
                    VALUES (@Id, @Date, @Category, @Amount, @BelegNr, @Notiz, @IsFeiertag, @CreatedAtUtc, @UpdatedAtUtc, @IsDeleted);
                """,
            _ => throw new NotSupportedException(),
        };

        var parameters = entries.Select(e => new
        {
            e.Id,
            Date = e.Date.ToDateTime(TimeOnly.MinValue),
            Category = (int)e.Category,
            e.Amount,
            e.BelegNr,
            e.Notiz,
            e.IsFeiertag,
            e.CreatedAtUtc,
            e.UpdatedAtUtc,
            e.IsDeleted,
        });

        await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<KassaEntry>> PullChangedSinceAsync(DateTimeOffset since, CancellationToken ct = default)
    {
        await using var connection = RemoteConnectionFactory.Create(settings);
        await connection.OpenAsync(ct);

        const string sql = """
            SELECT id AS Id, entry_date AS Date, category AS Category, amount AS Amount,
                   beleg_nr AS BelegNr, notiz AS Notiz, is_feiertag AS IsFeiertag, created_at_utc AS CreatedAtUtc,
                   updated_at_utc AS UpdatedAtUtc, is_deleted AS IsDeleted
            FROM kassa_entries
            WHERE updated_at_utc > @Since
            """;

        var rows = await connection.QueryAsync(new CommandDefinition(sql, new { Since = since }, cancellationToken: ct));

        return rows.Select(row => new KassaEntry
        {
            Id = (Guid)row.Id,
            Date = ToDateOnly(row.Date),
            Category = (KassaCategory)Convert.ToInt32(row.Category),
            Amount = Convert.ToDecimal(row.Amount),
            BelegNr = row.BelegNr,
            Notiz = row.Notiz,
            IsFeiertag = Convert.ToBoolean(row.IsFeiertag),
            CreatedAtUtc = ToUtcOffset(row.CreatedAtUtc),
            UpdatedAtUtc = ToUtcOffset(row.UpdatedAtUtc),
            IsDeleted = Convert.ToBoolean(row.IsDeleted),
            IsSynced = true,
        }).ToList();
    }

    /// <summary>
    /// Die drei Engines liefern den "updated_at_utc"-Wert unterschiedlich typisiert zurück
    /// (DateTime bei MySQL/PostgreSQL, DateTimeOffset bei SQL Server) - hier vereinheitlicht.
    /// </summary>
    private static DateTimeOffset ToUtcOffset(object value) => value switch
    {
        DateTimeOffset dto => dto.ToUniversalTime(),
        DateTime dt => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc)),
        _ => throw new InvalidOperationException($"Unerwarteter Zeitstempel-Typ: {value.GetType()}"),
    };

    /// <summary>Npgsql liefert für "date" standardmäßig DateOnly, MySqlConnector/SqlClient liefern DateTime.</summary>
    private static DateOnly ToDateOnly(object value) => value switch
    {
        DateOnly d => d,
        DateTime dt => DateOnly.FromDateTime(dt),
        _ => throw new InvalidOperationException($"Unerwarteter Datums-Typ: {value.GetType()}"),
    };
}
