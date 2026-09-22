using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kassajournal.Core.Services;
using Kassajournal.Data.Settings;
using Kassajournal.Data.Sync;
using Kassajournal.Update;

namespace Kassajournal.App.ViewModels;

/// <summary>Haupt-ViewModel: steuert Navigation und sorgt dafür, dass beim Start immer der heutige Tag aufgeht.</summary>
public partial class MainViewModel : ObservableObject
{
    private readonly SyncService _syncService;
    private readonly GitHubUpdateChecker _updateChecker;

    public MainViewModel(DayEntryViewModel dayEntry, MonthOverviewViewModel monthOverview, SyncService syncService, GitHubUpdateChecker updateChecker)
    {
        DayEntry = dayEntry;
        MonthOverview = monthOverview;
        _syncService = syncService;
        _updateChecker = updateChecker;

        MonthOverview.TagAusgewaehlt += date => _ = ZeigeTagAsync(date);
        CurrentView = DayEntry;
    }

    public DayEntryViewModel DayEntry { get; }

    public MonthOverviewViewModel MonthOverview { get; }

    [ObservableProperty]
    private object _currentView = null!;

    [ObservableProperty]
    private bool _istDbKonfiguriert;

    [ObservableProperty]
    private string _updateHinweis = string.Empty;

    [ObservableProperty]
    private bool _updateVerfuegbar;

    private UpdateInfo? _pendingUpdate;

    public string AppVersion => System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";

    /// <summary>Wird beim App-Start aufgerufen: öffnet immer den heutigen Tag, prüft Konfiguration und Updates im Hintergrund.</summary>
    public async Task InitializeAsync()
    {
        IstDbKonfiguriert = new DatabaseSettingsStore().Load().IsConfigured;

        await ZeigeTagAsync(DateOnly.FromDateTime(DateTime.Today));

        _ = _syncService.SyncNowAsync(); // im Hintergrund, blockiert den Start nicht
        _ = PruefeUpdateAsync();
    }

    private async Task ZeigeTagAsync(DateOnly date)
    {
        await DayEntry.LoadAsync(date);
        CurrentView = DayEntry;
    }

    [RelayCommand]
    private Task ZeigeHeute() => ZeigeTagAsync(DateOnly.FromDateTime(DateTime.Today));

    [RelayCommand]
    private Task VorherigerTag() => ZeigeTagAsync(DayEntry.Date.AddDays(-1));

    [RelayCommand]
    private Task NaechsterTag() => ZeigeTagAsync(DayEntry.Date.AddDays(1));

    [RelayCommand]
    private async Task ZeigeMonatsuebersicht()
    {
        await MonthOverview.LoadAsync(DayEntry.Date.Year, DayEntry.Date.Month);
        CurrentView = MonthOverview;
    }

    [RelayCommand]
    private void ZeigeTagesansicht() => CurrentView = DayEntry;

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

    public async Task NeuLadenAsync() => await ZeigeTagAsync(DayEntry.Date);
}
