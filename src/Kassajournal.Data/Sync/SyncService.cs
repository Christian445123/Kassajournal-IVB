using Kassajournal.Core.Models;
using Kassajournal.Core.Services;
using Kassajournal.Data.Remote;

namespace Kassajournal.Data.Sync;

public enum SyncStatus
{
    NichtKonfiguriert,
    Erfolgreich,
    Fehler,
}

public record SyncResult(SyncStatus Status, int GepushteEintraege, int GezogeneEintraege, string? Fehlermeldung = null);

/// <summary>
/// Gleicht die lokale (verschlüsselte) Datenbank mit der zentralen Datenbank ab:
/// 1) alle lokal geänderten/neuen Einträge werden hochgeladen (push),
/// 2) alle seit dem letzten Abgleich in der Zentraldatenbank geänderten Einträge werden heruntergeladen (pull).
/// Konfliktlösung: "last write wins" über UpdatedAtUtc – für ein internes Kassajournal mit
/// wenigen gleichzeitigen Benutzern ausreichend robust.
/// </summary>
public class SyncService(IKassaRepository localRepository, IRemoteKassaGateway remoteGateway)
{
    public DateTimeOffset LastSyncUtc { get; private set; } = DateTimeOffset.MinValue;

    public async Task<SyncResult> SyncNowAsync(CancellationToken ct = default)
    {
        try
        {
            await remoteGateway.EnsureSchemaAsync(ct);

            var unsynced = await localRepository.GetUnsyncedEntriesAsync(ct);
            if (unsynced.Count > 0)
            {
                await remoteGateway.PushEntriesAsync(unsynced, ct);
                await localRepository.MarkSyncedAsync(unsynced.Select(e => e.Id), ct);
            }

            var remoteChanges = await remoteGateway.PullChangedSinceAsync(LastSyncUtc, ct);
            foreach (var entry in remoteChanges)
            {
                await localRepository.UpsertEntryAsync(entry, ct);
            }

            // Nach erfolgreichem Pull sind auch die frisch heruntergeladenen Einträge "synced" -
            // sie kommen ja bereits 1:1 aus der Zentraldatenbank.
            if (remoteChanges.Count > 0)
            {
                await localRepository.MarkSyncedAsync(remoteChanges.Select(e => e.Id), ct);
            }

            LastSyncUtc = DateTimeOffset.UtcNow;
            return new SyncResult(SyncStatus.Erfolgreich, unsynced.Count, remoteChanges.Count);
        }
        catch (Exception ex)
        {
            return new SyncResult(SyncStatus.Fehler, 0, 0, ex.Message);
        }
    }
}
