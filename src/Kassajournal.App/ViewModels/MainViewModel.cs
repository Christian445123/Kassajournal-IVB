using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kassajournal.Core.Models;
using Kassajournal.Data.Settings;
using Kassajournal.Data.Sync;

namespace Kassajournal.App.ViewModels;

/// <summary>Die beiden obersten Seiten der App.</summary>
public enum HauptSeite
{
    /// <summary>Startseite: heutiger Tag für Kassajournal + IVB zum direkten Eintippen.</summary>
    Heute,

    /// <summary>Alle Monate, Auswertung und Vergleich - je Bereich (Kassajournal/IVB).</summary>
    Monatsuebersicht,
}

/// <summary>
/// Haupt-ViewModel: EIN Programm ("Kassajournal &amp; IVB"). Oberste Ebene ist "Heute" (Startseite,
/// direkt zum Eintippen) und "Monatsübersicht" (alle Monate + Auswertung + Vergleich je Bereich).
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly SyncService _kassaSyncService;
    private readonly IvbSyncService _ivbSyncService;

    public MainViewModel(
        HeuteViewModel heute,
        KassaModuleViewModel kassajournal,
        IvbModuleViewModel ivb,
        SyncService kassaSyncService,
        IvbSyncService ivbSyncService,
        UpdateManagerViewModel updateManager)
    {
        Heute = heute;
        Kassajournal = kassajournal;
        Ivb = ivb;
        _kassaSyncService = kassaSyncService;
        _ivbSyncService = ivbSyncService;
        UpdateManager = updateManager;
    }

    public HeuteViewModel Heute { get; }

    public KassaModuleViewModel Kassajournal { get; }

    public IvbModuleViewModel Ivb { get; }

    /// <summary>Wird auch vom Einstellungen-Dialog verwendet, damit beide Stellen denselben Update-Stand zeigen.</summary>
    public UpdateManagerViewModel UpdateManager { get; }

    [ObservableProperty]
    private HauptSeite _aktiveSeite = HauptSeite.Heute;

    public bool IstHeuteAktiv => AktiveSeite == HauptSeite.Heute;

    public bool IstMonatsuebersichtAktiv => AktiveSeite == HauptSeite.Monatsuebersicht;

    partial void OnAktiveSeiteChanged(HauptSeite value)
    {
        OnPropertyChanged(nameof(IstHeuteAktiv));
        OnPropertyChanged(nameof(IstMonatsuebersichtAktiv));
    }

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

    /// <summary>Wird beim App-Start aufgerufen: öffnet die "Heute"-Seite, prüft Konfiguration und Updates im Hintergrund.</summary>
    public async Task InitializeAsync()
    {
        RefreshDbStatus();

        // Nacheinander laden (nicht parallel!) - alle greifen auf dieselbe lokale Datenbankverbindung
        // zu, die nicht für gleichzeitige Zugriffe ausgelegt ist.
        await Heute.RefreshAsync();
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
    private async Task ZeigeHeuteAsync()
    {
        AktiveSeite = HauptSeite.Heute;
        await Heute.RefreshAsync();
    }

    [RelayCommand]
    private async Task ZeigeMonatsuebersichtAsync()
    {
        AktiveSeite = HauptSeite.Monatsuebersicht;

        // Falls über "Heute" gerade etwas für den aktuellen Monat gebucht wurde, hier auffrischen,
        // damit die Monatsübersicht nie veraltete Werte zeigt.
        await Kassajournal.RefreshHeutigenMonatAsync();
        await Ivb.RefreshHeutigenMonatAsync();
    }

    [RelayCommand]
    private void ZeigeKassajournal() => AktivesModul = AppModule.Kassajournal;

    [RelayCommand]
    private void ZeigeIvb() => AktivesModul = AppModule.Ivb;
}
