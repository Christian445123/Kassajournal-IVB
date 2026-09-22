using System.Windows;
using System.Windows.Threading;
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
    public App()
    {
        // Ohne diese Handler würde ein unerwarteter Fehler die App lautlos beenden ("es passiert
        // einfach nichts") - stattdessen wird jetzt immer eine verständliche Meldung angezeigt.
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
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
            var updateManager = new UpdateManagerViewModel(updateChecker);

            DayEntryViewModel DayFactory() => new(kassaRepository, kassaSyncService);
            IvbDayRowViewModel IvbRowFactory() => new(ivbRepository, ivbSyncService);

            var kassaAuswertung = new KassaAuswertungViewModel(kassaRepository);
            var kassaVergleich = new KassaVergleichViewModel(kassaRepository);
            var kassaModule = new KassaModuleViewModel(kassaRepository, DayFactory, kassaAuswertung, kassaVergleich);

            var ivbAuswertung = new IvbAuswertungViewModel(ivbRepository);
            var ivbModule = new IvbModuleViewModel(ivbRepository, IvbRowFactory, ivbAuswertung);

            var mainViewModel = new MainViewModel(kassaModule, ivbModule, kassaSyncService, ivbSyncService, updateManager);

            var mainWindow = new MainWindow(mainViewModel);
            MainWindow = mainWindow;
            mainWindow.Show();

            // Initialisierung (inkl. Ersteinrichtungs-Dialog, falls noch keine DB konfiguriert ist)
            // passiert in MainWindow.Loaded, damit das Fenster schon sichtbar ist, bevor ggf. der
            // Einstellungen-Dialog aufgeht.
        }
        catch (Exception ex)
        {
            ShowFatalError("Kassajournal & IVB konnte nicht gestartet werden.", ex);
            Shutdown(1);
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ShowFatalError("Es ist ein unerwarteter Fehler aufgetreten.", e.Exception);
        e.Handled = true; // App am Leben halten, sofern möglich, statt lautlos abzustürzen.
    }

    private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            ShowFatalError("Es ist ein schwerwiegender Fehler aufgetreten. Die App wird beendet.", ex);
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        ShowFatalError("Im Hintergrund ist ein Fehler aufgetreten.", e.Exception);
        e.SetObserved();
    }

    private static void ShowFatalError(string titel, Exception ex)
    {
        try
        {
            MessageBox.Show(
                $"{titel}\n\n{ex.GetType().Name}: {ex.Message}\n\n{ex.StackTrace}",
                "Kassajournal & IVB - Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch
        {
            // Wenn selbst die Fehleranzeige scheitert, gibt es nichts mehr zu tun.
        }
    }
}
