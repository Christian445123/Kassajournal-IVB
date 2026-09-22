using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kassajournal.Update;

/// <summary>
/// Prüft beim Start der App, ob auf GitHub ein neueres Release (mit angehängter .msi) existiert.
/// Nutzt die öffentliche GitHub-REST-API, dafür ist bei einem öffentlichen Repository kein
/// Zugangstoken nötig.
/// </summary>
public class GitHubUpdateChecker(string owner, string repository, HttpClient? httpClient = null)
{
    private readonly HttpClient _http = httpClient ?? CreateDefaultClient();

    private static HttpClient CreateDefaultClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10),
        };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Kassajournal-App", "1.0"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return client;
    }

    public async Task<UpdateInfo> CheckForUpdateAsync(Version currentVersion, CancellationToken ct = default)
    {
        try
        {
            var url = $"https://api.github.com/repos/{owner}/{repository}/releases/latest";
            using var response = await _http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                return new UpdateInfo(false, null, null, null, null);
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var release = JsonSerializer.Deserialize<GitHubRelease>(json);
            if (release is null || string.IsNullOrWhiteSpace(release.TagName))
            {
                return new UpdateInfo(false, null, null, null, null);
            }

            var versionText = release.TagName.TrimStart('v', 'V');
            if (!Version.TryParse(versionText, out var latestVersion))
            {
                return new UpdateInfo(false, null, null, null, release.TagName);
            }

            var msiAsset = release.Assets?.FirstOrDefault(a => a.Name.EndsWith(".msi", StringComparison.OrdinalIgnoreCase));

            var isNewer = latestVersion > currentVersion;
            return new UpdateInfo(
                IsUpdateAvailable: isNewer && msiAsset is not null,
                LatestVersion: latestVersion,
                DownloadUrl: msiAsset?.BrowserDownloadUrl,
                ReleaseNotesUrl: release.HtmlUrl,
                ReleaseTag: release.TagName);
        }
        catch (Exception)
        {
            // Update-Prüfung darf den App-Start niemals verhindern (z. B. kein Internet vorhanden).
            return new UpdateInfo(false, null, null, null, null);
        }
    }

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;

        [JsonPropertyName("assets")]
        public List<GitHubReleaseAsset>? Assets { get; set; }
    }

    private sealed class GitHubReleaseAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = string.Empty;
    }
}
