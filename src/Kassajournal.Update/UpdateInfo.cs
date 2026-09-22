namespace Kassajournal.Update;

/// <summary>Ergebnis einer Update-Prüfung gegen GitHub Releases.</summary>
public record UpdateInfo(bool IsUpdateAvailable, Version? LatestVersion, string? DownloadUrl, string? ReleaseNotesUrl, string? ReleaseTag);
