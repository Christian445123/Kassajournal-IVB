using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kassajournal.Core.Models;
using Kassajournal.Data.Settings;
using Kassajournal.Data.Sync;
using Kassajournal.Update;

namespace Kassajournal.App.ViewModels;

/// <summary>
/// Haupt-ViewModel: EIN Programm ("Kassajournal &amp; IVB") mit Umschalter zwischen den zwei
/// fachlichen Bereichen. Sorgt dafür, dass beim Start in beiden Bereichen der heutige Tag bereitsteht.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly SyncService _kassaSyncService;
    private readonly IvbSyncService _ivbSyncService;
    private readonly GitHubUpdateChecker _updateChecker;

    public MainViewModel(
        KassaModuleViewModel kassajournal,
        IvbModuleViewModel ivb,
        SyncService kassaSyncService,
        IvbSyncService ivbSyncService,
        GitHubUpdateChecker updateChecker)
    {
        Kassajournal = kassajournal;
        Ivb = ivb;
        _kassaSyncService = kassaSyncService;
        _ivbSyncService = ivbSyncService;
        _updateChecker = updateChecker;
    }

    public KassaModuleViewModel Kassajournal { get; }

    public IvbModuleViewModel Ivb { get; }

    [ObservableProperty]
    private AppModule _aktivesModul = AppModule.Kassajournal;

    public bool IstKassajournalAktiv => AktivesModul == AppModule.Kassajournal;

    public bool IstIvbAktiv => AktivesModul == AppModule.Ivb;

    partial void OnAktivesModulChanged(AppModule value)
    {
        OnPropertyChanged(nameof(IstKassajournalAktiv));
        OnPropertyChanged(nameof(IstIvbAktiv));
    }

    [ObservableProperty]
    private bool _istKassaDbKonfiguriert;

    [ObservableProperty]
    private bool _istIvbDbKonfiguriert;

    [ObservableProperty]
    private string _updateHinweis = string.Empty;

    [ObservableProperty]
    private bool _updateVerfuegbar;

    private UpdateInfo? _pendingUpdate;

    public string AppVersion => System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";

    /// <summary>Wird beim App-Start aufgerufen: öffnet in beiden Bereichen den heutigen Tag, prüft Konfiguration und Updates im Hintergrund.</summary>
    public async Task InitializeAsync()
    {
        RefreshDbStatus();

        await Kassajournal.InitializeAsync();
        await Ivb.InitializeAsync();

        _ = _kassaSyncService.SyncNowAsync();
        _ = _ivbSyncService.SyncNowAsync();
        _ = PruefeUpdateAsync();
    }

    public void RefreshDbStatus()
    {
        IstKassaDbKonfiguriert = new DatabaseSettingsStore(AppModule.Kassajournal).Load().IsConfigured;
        IstIvbDbKonfiguriert = new DatabaseSettingsStore(AppModule.Ivb).Load().IsConfigured;
    }

    [RelayCommand]
    private void ZeigeKassajournal() => AktivesModul = AppModule.Kassajournal;

    [RelayCommand]
    private void ZeigeIvb() => AktivesModul = AppModule.Ivb;

    private async Task PruefeUpdateAsync()
    {
        var currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);
        var info = await _updateChecker.CheckForUpdateAsync(currentVersion);
        if (info.IsUpdateAvailable)
        {
            _pendingUpdate = info;
            UpdateVerfuegbar = true;
            UpdateHinweis = $"Neue Version {info.LatestVersion} verfügbar.";
        }
    }

    [RelayCommand]
    private async Task UpdateInstallierenAsync()
    {
        if (_pendingUpdate?.DownloadUrl is null)
        {
            return;
        }

        UpdateHinweis = "Update wird heruntergeladen …";
        var path = await UpdateInstaller.DownloadAsync(_pendingUpdate.DownloadUrl, progress: null);
        UpdateInstaller.LaunchInstallerAndExit(path);
    }
}
