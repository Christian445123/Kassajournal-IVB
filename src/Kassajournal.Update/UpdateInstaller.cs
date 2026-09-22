using System.Diagnostics;

namespace Kassajournal.Update;

/// <summary>
/// Lädt die neue MSI von GitHub herunter und startet die Windows-Installation.
/// msiexec übernimmt danach (inkl. Beenden/Neustart der App) - hier wird nur angestoßen.
/// </summary>
public static class UpdateInstaller
{
    public static async Task<string> DownloadAsync(string downloadUrl, IProgress<double>? progress, CancellationToken ct = default)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"Kassajournal-Update-{Guid.NewGuid():N}.msi");

        using var client = new HttpClient(new HttpClientHandler(), disposeHandler: true) { Timeout = TimeSpan.FromMinutes(5) };
        using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
        await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
        await using var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);

        var buffer = new byte[81920];
        long totalRead = 0;
        int bytesRead;
        while ((bytesRead = await contentStream.ReadAsync(buffer, ct)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
            totalRead += bytesRead;
            if (totalBytes > 0)
            {
                progress?.Report((double)totalRead / totalBytes * 100.0);
            }
        }

        return tempFile;
    }

    /// <summary>
    /// Startet die MSI-Installation mit sichtbarer UI (der Benutzer bestätigt den Windows/SmartScreen-Hinweis,
    /// da die MSI unsigniert ist) und beendet danach den aktuellen Prozess, damit die laufende .exe
    /// überschrieben werden kann.
    /// </summary>
    public static void LaunchInstallerAndExit(string msiPath)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "msiexec",
            Arguments = $"/i \"{msiPath}\" /promptrestart",
            UseShellExecute = true,
        });

        Environment.Exit(0);
    }
}
