using Kassajournal.Core.Models;

namespace Kassajournal.Data.Remote;

/// <summary>Zugriff auf die zentrale IVB-Datenbank (ivb_db) - komplett getrennt von der Kassajournal-Datenbank.</summary>
public interface IRemoteIvbGateway
{
    Task<(bool Success, string? ErrorMessage)> TestConnectionAsync(CancellationToken ct = default);

    Task EnsureSchemaAsync(CancellationToken ct = default);

    Task PushEntriesAsync(IReadOnlyList<IvbEntry> entries, CancellationToken ct = default);

    Task<IReadOnlyList<IvbEntry>> PullChangedSinceAsync(DateTimeOffset since, CancellationToken ct = default);
}
