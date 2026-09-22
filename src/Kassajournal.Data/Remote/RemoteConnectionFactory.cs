using System.Data.Common;
using Kassajournal.Core.Models;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;

namespace Kassajournal.Data.Remote;

/// <summary>
/// Baut die Verbindung zur zentralen Datenbank aus den Einstellungen auf.
/// Wichtig: <see cref="DatabaseSettings.RequireEncryption"/> erzwingt in jedem der drei
/// unterstützten Engines eine TLS/SSL-verschlüsselte Verbindung, damit niemand im Netzwerk
/// die Kassadaten mitlesen kann.
/// </summary>
public static class RemoteConnectionFactory
{
    public static DbConnection Create(DatabaseSettings settings)
    {
        return settings.Engine switch
        {
            DatabaseEngine.MySql => new MySqlConnection(BuildMySqlConnectionString(settings)),
            DatabaseEngine.PostgreSql => new NpgsqlConnection(BuildPostgreSqlConnectionString(settings)),
            DatabaseEngine.SqlServer => new SqlConnection(BuildSqlServerConnectionString(settings)),
            _ => throw new NotSupportedException($"Unbekannte Datenbank-Engine: {settings.Engine}"),
        };
    }

    private static string BuildMySqlConnectionString(DatabaseSettings s) => new MySqlConnectionStringBuilder
    {
        Server = s.Host,
        Port = (uint)s.Port,
        Database = s.DatabaseName,
        UserID = s.Username,
        Password = s.Password,
        SslMode = s.RequireEncryption ? MySqlSslMode.Required : MySqlSslMode.Preferred,
        ConnectionTimeout = 10,
    }.ConnectionString;

    private static string BuildPostgreSqlConnectionString(DatabaseSettings s) => new NpgsqlConnectionStringBuilder
    {
        Host = s.Host,
        Port = s.Port,
        Database = s.DatabaseName,
        Username = s.Username,
        Password = s.Password,
        SslMode = s.RequireEncryption ? SslMode.Require : SslMode.Prefer,
        Timeout = 10,
    }.ConnectionString;

    private static string BuildSqlServerConnectionString(DatabaseSettings s) => new SqlConnectionStringBuilder
    {
        DataSource = $"{s.Host},{s.Port}",
        InitialCatalog = s.DatabaseName,
        UserID = s.Username,
        Password = s.Password,
        Encrypt = s.RequireEncryption,
        TrustServerCertificate = false,
        ConnectTimeout = 10,
    }.ConnectionString;
}
