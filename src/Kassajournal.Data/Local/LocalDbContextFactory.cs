using Kassajournal.Core;
using Kassajournal.Data.Settings;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Kassajournal.Data.Local;

/// <summary>
/// Baut den <see cref="LocalDbContext"/> mit einer verschlüsselten SQLite-Verbindung auf
/// (SQLCipher via SQLitePCLRaw.bundle_e_sqlcipher, Passwort kommt aus <see cref="LocalDatabaseKeyProvider"/>).
/// </summary>
public static class LocalDbContextFactory
{
    public static LocalDbContext Create()
    {
        var password = LocalDatabaseKeyProvider.GetOrCreateKey();

        var connectionStringBuilder = new SqliteConnectionStringBuilder
        {
            DataSource = AppPaths.LocalDatabaseFile,
            Password = password,
        };

        var options = new DbContextOptionsBuilder<LocalDbContext>()
            .UseSqlite(connectionStringBuilder.ConnectionString)
            .Options;

        var context = new LocalDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
