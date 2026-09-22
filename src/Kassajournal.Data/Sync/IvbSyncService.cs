using Kassajournal.Core.Services;
using Kassajournal.Data.Remote;

namespace Kassajournal.Data.Sync;

/// <summary>Gleicht die lokale IVB-Datenbank mit der zentralen ivb_db ab (gleiches Prinzip wie <see cref="SyncService"/>).</summary>
public class IvbSyncService(IIvbRepository localRepository, IRemoteIvbGateway remoteGateway)
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
