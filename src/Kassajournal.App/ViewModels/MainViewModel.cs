using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kassajournal.Core.Models;
using Kassajournal.Data.Settings;
using Kassajournal.Data.Sync;

namespace Kassajournal.App.ViewModels;

/// <summary>
/// Haupt-ViewModel: EIN Programm ("Kassajournal &amp; IVB") mit Umschalter zwischen den zwei
/// fachlichen Bereichen. Sorgt dafür, dass beim Start in beiden Bereichen der heutige Tag bereitsteht.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly SyncService _kassaSyncService;
    private readonly IvbSyncService _ivbSyncService;

    public MainViewModel(
        KassaModuleViewModel kassajournal,
        IvbModuleViewModel ivb,
        SyncService kassaSyncService,
        IvbSyncService ivbSyncService,
        UpdateManagerViewModel updateManager)
    {
        Kassajournal = kassajournal;
        Ivb = ivb;
        _kassaSyncService = kassaSyncService;
        _ivbSyncService = ivbSyncService;
        UpdateManager = updateManager;
    }

    public KassaModuleViewModel Kassajournal { get; }

    public IvbModuleViewModel Ivb { get; }

    /// <summary>Wird auch vom Einstellungen-Dialog verwendet, damit beide Stellen denselben Update-Stand zeigen.</summary>
    public UpdateManagerViewModel UpdateManager { get; }

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

    public string AppVersion => UpdateManager.AktuelleVersion;

    /// <summary>Wird beim App-Start aufgerufen: öffnet in beiden Bereichen den heutigen Tag, prüft Konfiguration und Updates im Hintergrund.</summary>
    public async Task InitializeAsync()
    {
        RefreshDbStatus();

        await Kassajournal.InitializeAsync();
        await Ivb.InitializeAsync();

        _ = _kassaSyncService.SyncNowAsync();
        _ = _ivbSyncService.SyncNowAsync();
        _ = UpdateManager.PruefeBeimStartAsync();
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
}
