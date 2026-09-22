using Dapper;
using Kassajournal.Core.Models;

namespace Kassajournal.Data.Remote;

/// <summary>
/// Implementierung von <see cref="IRemoteIvbGateway"/> für alle drei unterstützten Engines.
/// Eigene Tabelle "ivb_entries" in der eigenen ivb_db-Datenbank - komplett getrennt von
/// den Kassajournal-Daten.
/// </summary>
public class SqlRemoteIvbGateway(DatabaseSettings settings) : IRemoteIvbGateway
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
                CREATE TABLE IF NOT EXISTS ivb_entries (
                    id CHAR(36) PRIMARY KEY,
                    entry_date DATE NOT NULL,
                    amount DECIMAL(14,2) NOT NULL,
                    is_feiertag TINYINT(1) NOT NULL DEFAULT 0,
                    notiz VARCHAR(500) NULL,
                    created_at_utc DATETIME(3) NOT NULL,
                    updated_at_utc DATETIME(3) NOT NULL,
                    is_deleted TINYINT(1) NOT NULL DEFAULT 0,
                    INDEX idx_ivb_entries_date (entry_date),
                    INDEX idx_ivb_entries_updated (updated_at_utc)
                ) CHARACTER SET utf8mb4;
                """,
            DatabaseEngine.PostgreSql => """
                CREATE TABLE IF NOT EXISTS ivb_entries (
                    id UUID PRIMARY KEY,
                    entry_date DATE NOT NULL,
                    amount NUMERIC(14,2) NOT NULL,
                    is_feiertag BOOLEAN NOT NULL DEFAULT FALSE,
                    notiz VARCHAR(500) NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    is_deleted BOOLEAN NOT NULL DEFAULT FALSE
                );
                CREATE INDEX IF NOT EXISTS idx_ivb_entries_date ON ivb_entries(entry_date);
                CREATE INDEX IF NOT EXISTS idx_ivb_entries_updated ON ivb_entries(updated_at_utc);
                """,
            DatabaseEngine.SqlServer => """
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ivb_entries')
                BEGIN
                    CREATE TABLE ivb_entries (
                        id UNIQUEIDENTIFIER PRIMARY KEY,
                        entry_date DATE NOT NULL,
                        amount DECIMAL(14,2) NOT NULL,
                        is_feiertag BIT NOT NULL DEFAULT 0,
                        notiz NVARCHAR(500) NULL,
                        created_at_utc DATETIMEOFFSET NOT NULL,
                        updated_at_utc DATETIMEOFFSET NOT NULL,
                        is_deleted BIT NOT NULL DEFAULT 0
                    );
                    CREATE INDEX idx_ivb_entries_date ON ivb_entries(entry_date);
                    CREATE INDEX idx_ivb_entries_updated ON ivb_entries(updated_at_utc);
                END
                """,
            _ => throw new NotSupportedException(),
        };

        await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: ct));
    }

    public async Task PushEntriesAsync(IReadOnlyList<IvbEntry> entries, CancellationToken ct = default)
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
                INSERT INTO ivb_entries (id, entry_date, amount, is_feiertag, notiz, created_at_utc, updated_at_utc, is_deleted)
                VALUES (@Id, @Date, @Amount, @IsFeiertag, @Notiz, @CreatedAtUtc, @UpdatedAtUtc, @IsDeleted)
                ON DUPLICATE KEY UPDATE
                    entry_date = VALUES(entry_date), amount = VALUES(amount), is_feiertag = VALUES(is_feiertag),
                    notiz = VALUES(notiz), updated_at_utc = VALUES(updated_at_utc), is_deleted = VALUES(is_deleted);
                """,
            DatabaseEngine.PostgreSql => """
                INSERT INTO ivb_entries (id, entry_date, amount, is_feiertag, notiz, created_at_utc, updated_at_utc, is_deleted)
                VALUES (@Id, @Date, @Amount, @IsFeiertag, @Notiz, @CreatedAtUtc, @UpdatedAtUtc, @IsDeleted)
                ON CONFLICT (id) DO UPDATE SET
                    entry_date = EXCLUDED.entry_date, amount = EXCLUDED.amount, is_feiertag = EXCLUDED.is_feiertag,
                    notiz = EXCLUDED.notiz, updated_at_utc = EXCLUDED.updated_at_utc, is_deleted = EXCLUDED.is_deleted;
                """,
            DatabaseEngine.SqlServer => """
                MERGE INTO ivb_entries AS target
                USING (SELECT @Id AS id) AS src ON target.id = src.id
                WHEN MATCHED THEN UPDATE SET
                    entry_date = @Date, amount = @Amount, is_feiertag = @IsFeiertag,
                    notiz = @Notiz, updated_at_utc = @UpdatedAtUtc, is_deleted = @IsDeleted
                WHEN NOT MATCHED THEN INSERT (id, entry_date, amount, is_feiertag, notiz, created_at_utc, updated_at_utc, is_deleted)
                    VALUES (@Id, @Date, @Amount, @IsFeiertag, @Notiz, @CreatedAtUtc, @UpdatedAtUtc, @IsDeleted);
                """,
            _ => throw new NotSupportedException(),
        };

        var parameters = entries.Select(e => new
        {
            e.Id,
            Date = e.Date.ToDateTime(TimeOnly.MinValue),
            e.Amount,
            e.IsFeiertag,
            e.Notiz,
            e.CreatedAtUtc,
            e.UpdatedAtUtc,
            e.IsDeleted,
        });

        await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<IvbEntry>> PullChangedSinceAsync(DateTimeOffset since, CancellationToken ct = default)
    {
        await using var connection = RemoteConnectionFactory.Create(settings);
        await connection.OpenAsync(ct);

        const string sql = """
            SELECT id AS Id, entry_date AS Date, amount AS Amount, is_feiertag AS IsFeiertag,
                   notiz AS Notiz, created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc, is_deleted AS IsDeleted
            FROM ivb_entries
            WHERE updated_at_utc > @Since
            """;

        var rows = await connection.QueryAsync(new CommandDefinition(sql, new { Since = since }, cancellationToken: ct));

        return rows.Select(row => new IvbEntry
        {
            Id = (Guid)row.Id,
            Date = ToDateOnly(row.Date),
            Amount = Convert.ToDecimal(row.Amount),
            IsFeiertag = Convert.ToBoolean(row.IsFeiertag),
            Notiz = row.Notiz,
            CreatedAtUtc = ToUtcOffset(row.CreatedAtUtc),
            UpdatedAtUtc = ToUtcOffset(row.UpdatedAtUtc),
            IsDeleted = Convert.ToBoolean(row.IsDeleted),
            IsSynced = true,
        }).ToList();
    }

    private static DateTimeOffset ToUtcOffset(object value) => value switch
    {
        DateTimeOffset dto => dto.ToUniversalTime(),
        DateTime dt => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc)),
        _ => throw new InvalidOperationException($"Unerwarteter Zeitstempel-Typ: {value.GetType()}"),
    };

    private static DateOnly ToDateOnly(object value) => value switch
    {
        DateOnly d => d,
        DateTime dt => DateOnly.FromDateTime(dt),
        _ => throw new InvalidOperationException($"Unerwarteter Datums-Typ: {value.GetType()}"),
    };
}
