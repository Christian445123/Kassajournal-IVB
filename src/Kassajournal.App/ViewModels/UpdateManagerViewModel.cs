using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kassajournal.Update;

namespace Kassajournal.App.ViewModels;

/// <summary>
/// Prüft auf neue Versionen (GitHub Releases) und stößt die Installation an. Eine einzige Instanz
/// wird sowohl vom Hinweis-Banner im Hauptfenster als auch vom "Nach Updates suchen"-Bereich in den
/// Einstellungen verwendet, damit beide immer denselben Stand zeigen.
/// </summary>
public partial class UpdateManagerViewModel(GitHubUpdateChecker checker) : ObservableObject
{
    private UpdateInfo? _pendingUpdate;

    public string AktuelleVersion => Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";

    [ObservableProperty]
    private bool _istPruefungAmLaufen;

    [ObservableProperty]
    private bool _updateVerfuegbar;

    [ObservableProperty]
    private bool _istAmNeuesten;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private bool _laedtHerunter;

    [ObservableProperty]
    private double _downloadFortschritt;

    /// <summary>Wird beim App-Start automatisch im Hintergrund aufgerufen (ohne den Start zu blockieren).</summary>
    public async Task PruefeBeimStartAsync()
    {
        await NachUpdatesSuchenAsync();
    }

    [RelayCommand]
    private async Task NachUpdatesSuchenAsync()
    {
        IstPruefungAmLaufen = true;
        IstAmNeuesten = false;
        StatusText = "Suche nach Updates …";
        try
        {
            var currentVersionText = AktuelleVersion;
            var currentVersion = Version.TryParse(currentVersionText, out var v) ? v : new Version(0, 0, 0);
            var info = await checker.CheckForUpdateAsync(currentVersion);

            if (info.IsUpdateAvailable)
            {
                _pendingUpdate = info;
                UpdateVerfuegbar = true;
                StatusText = $"Neue Version {info.LatestVersion} verfügbar.";
            }
            else
            {
                UpdateVerfuegbar = false;
                IstAmNeuesten = true;
                StatusText = "Du verwendest bereits die neueste Version.";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Update-Prüfung fehlgeschlagen: {ex.Message}";
        }
        finally
        {
            IstPruefungAmLaufen = false;
        }
    }

    [RelayCommand]
    private async Task JetztAktualisierenAsync()
    {
        if (_pendingUpdate?.DownloadUrl is null)
        {
            return;
        }

        LaedtHerunter = true;
        StatusText = "Update wird heruntergeladen …";
        try
        {
            var progress = new Progress<double>(p => DownloadFortschritt = p);
            var path = await UpdateInstaller.DownloadAsync(_pendingUpdate.DownloadUrl, progress);
            StatusText = "Installation wird gestartet …";
            UpdateInstaller.LaunchInstallerAndExit(path);
        }
        catch (Exception ex)
        {
            LaedtHerunter = false;
            StatusText = $"Update fehlgeschlagen: {ex.Message}";
        }
    }
}
