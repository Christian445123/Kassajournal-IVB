using Kassajournal.Core.Models;

namespace Kassajournal.Data.Remote;

/// <summary>Zugriff auf die zentrale Datenbank, in der alle Standorte/Geräte ihre Daten ablegen.</summary>
public interface IRemoteKassaGateway
{
    /// <summary>Baut testweise eine Verbindung auf (für den "Verbindung testen"-Button in den Einstellungen).</summary>
    Task<(bool Success, string? ErrorMessage)> TestConnectionAsync(CancellationToken ct = default);

    Task EnsureSchemaAsync(CancellationToken ct = default);

    Task PushEntriesAsync(IReadOnlyList<KassaEntry> entries, CancellationToken ct = default);

    Task<IReadOnlyList<KassaEntry>> PullChangedSinceAsync(DateTimeOffset since, CancellationToken ct = default);
}
