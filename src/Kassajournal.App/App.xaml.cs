using System.Windows;
using Kassajournal.App.ViewModels;
using Kassajournal.Core.Models;
using Kassajournal.Core.Services;
using Kassajournal.Data.Local;
using Kassajournal.Data.Remote;
using Kassajournal.Data.Settings;
using Kassajournal.Data.Sync;
using Kassajournal.Update;

namespace Kassajournal.App;

/// <summary>
/// Composition Root: verdrahtet lokale (verschlüsselte) Speicherung, die zwei getrennten
/// Zentraldatenbank-Anbindungen (Kassajournal / IVB) und die ViewModels, ohne einen
/// zusätzlichen DI-Container - die App ist dafür klein genug.
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var localDb = LocalDbContextFactory.Create();

        IKassaRepository kassaRepository = new LocalKassaRepository(localDb);
        IIvbRepository ivbRepository = new LocalIvbRepository(localDb);

        var kassaDbSettings = new DatabaseSettingsStore(AppModule.Kassajournal).Load();
        var ivbDbSettings = new DatabaseSettingsStore(AppModule.Ivb).Load();

        IRemoteKassaGateway kassaGateway = new SqlRemoteKassaGateway(kassaDbSettings);
        IRemoteIvbGateway ivbGateway = new SqlRemoteIvbGateway(ivbDbSettings);

        var kassaSyncService = new SyncService(kassaRepository, kassaGateway);
        var ivbSyncService = new IvbSyncService(ivbRepository, ivbGateway);

        var updateChecker = new GitHubUpdateChecker(AppConfig.GitHubOwner, AppConfig.GitHubRepository);

        DayEntryViewModel DayFactory() => new(kassaRepository, kassaSyncService);
        IvbDayRowViewModel IvbRowFactory() => new(ivbRepository, ivbSyncService);

        var kassaAuswertung = new KassaAuswertungViewModel(kassaRepository);
        var kassaVergleich = new KassaVergleichViewModel(kassaRepository);
        var kassaModule = new KassaModuleViewModel(kassaRepository, DayFactory, kassaAuswertung, kassaVergleich);

        var ivbAuswertung = new IvbAuswertungViewModel(ivbRepository);
        var ivbModule = new IvbModuleViewModel(ivbRepository, IvbRowFactory, ivbAuswertung);

        var mainViewModel = new MainViewModel(kassaModule, ivbModule, kassaSyncService, ivbSyncService, updateChecker);

        var mainWindow = new MainWindow(mainViewModel);
        MainWindow = mainWindow;
        mainWindow.Show();

        // Initialisierung (inkl. Ersteinrichtungs-Dialog, falls noch keine DB konfiguriert ist)
        // passiert in MainWindow.Loaded, damit das Fenster schon sichtbar ist, bevor ggf. der
        // Einstellungen-Dialog aufgeht.
    }
}
