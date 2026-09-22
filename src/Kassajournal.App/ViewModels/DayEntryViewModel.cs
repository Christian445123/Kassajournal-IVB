using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Kassajournal.Core.Models;
using Kassajournal.Core.Services;
using Kassajournal.Data.Sync;

namespace Kassajournal.App.ViewModels;

/// <summary>
/// Eine Tageszeile im Kassajournal: sechs Eingabefelder (Tageslosung + 5 Haben-Kategorien),
/// live berechnete Summen und laufender Kassenstand - fachlich 1:1 der Tagesblock aus der Excel-Vorlage.
/// Jede Änderung wird sofort einzeln gespeichert ("einfach die Zahlen eingeben") und im Hintergrund
/// mit der Zentraldatenbank synchronisiert.
/// </summary>
public partial class DayEntryViewModel : ObservableObject
{
    private readonly IKassaRepository _repository;
    private readonly SyncService _syncService;

    private decimal _otherDaysSollBisDatum;
    private decimal _otherDaysHabenBisDatum;
    private bool _isLoading;

    /// <summary>
    /// Wird ausgelöst, nachdem eine einzelne Zeile lokal gespeichert wurde - der Monats-Reiter
    /// nutzt das, um den laufenden Kassenstand aller (auch nachfolgender) Tage neu zu berechnen.
    /// </summary>
    public event EventHandler? EntrySaved;

    public DayEntryViewModel(IKassaRepository repository, SyncService syncService)
    {
        _repository = repository;
        _syncService = syncService;

        Lines = new ObservableCollection<CategoryLineViewModel>(
            KassaCategoryExtensions.AllInOrder.Select(c => new CategoryLineViewModel(c)));

        foreach (var line in Lines)
        {
            line.PropertyChanged += OnLinePropertyChanged;
        }
    }

    public ObservableCollection<CategoryLineViewModel> Lines { get; }

    [ObservableProperty]
    private DateOnly _date = DateOnly.FromDateTime(DateTime.Today);

    [ObservableProperty]
    private bool _isNewDay = true;

    [ObservableProperty]
    private decimal _sollSumme;

    [ObservableProperty]
    private decimal _habenSumme;

    [ObservableProperty]
    private decimal _differenz;

    [ObservableProperty]
    private bool _istAusgeglichen = true;

    [ObservableProperty]
    private decimal _anfangssaldoMonat;

    [ObservableProperty]
    private decimal _laufenderSaldo;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public string DateDisplay => Date.ToString("dddd, dd. MMMM yyyy", CultureInfo.GetCultureInfo("de-AT"));

    public bool IstHeute => Date == DateOnly.FromDateTime(DateTime.Today);

    /// <summary>Lädt einen einzelnen Tag eigenständig (fragt Monatsdaten selbst ab) - für den Alleinstand-Gebrauch.</summary>
    public async Task LoadAsync(DateOnly date)
    {
        IsBusy = true;
        try
        {
            var monthEntries = await _repository.GetEntriesForMonthAsync(date.Year, date.Month);
            var monthSettings = await _repository.GetOrCreateMonthSettingsAsync(date.Year, date.Month);
            PopulateFromMonthData(date, monthEntries, monthSettings.Anfangssaldo);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Befüllt diesen Tag aus bereits geladenen Monatsdaten (kein zusätzlicher DB-Zugriff) -
    /// wird vom Monats-Reiter verwendet, der die Monatsdaten einmal für alle Tage gemeinsam lädt.
    /// </summary>
    public void PopulateFromMonthData(DateOnly date, IReadOnlyList<KassaEntry> monthEntries, decimal anfangssaldo)
    {
        _isLoading = true;
        try
        {
            Date = date;
            OnPropertyChanged(nameof(DateDisplay));
            OnPropertyChanged(nameof(IstHeute));

            var dayEntries = monthEntries.Where(e => e.Date == date).ToList();
            IsNewDay = dayEntries.Count == 0;

            foreach (var line in Lines)
            {
                var match = dayEntries.FirstOrDefault(e => e.Category == line.Category);
                line.EntryId = match?.Id ?? Guid.NewGuid();
                line.BelegNr = match?.BelegNr;
                line.Notiz = match?.Notiz;
                line.Amount = match?.Amount ?? 0m;
            }

            AnfangssaldoMonat = anfangssaldo;

            var otherDays = monthEntries.Where(e => e.Date < date).ToList();
            _otherDaysSollBisDatum = otherDays.Where(e => e.Category.IsSoll()).Sum(e => e.Amount);
            _otherDaysHabenBisDatum = otherDays.Where(e => !e.Category.IsSoll()).Sum(e => e.Amount);

            Recalculate();
            StatusMessage = string.Empty;
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>Wird vom Monats-Reiter nach einer Änderung eines anderen Tages aufgerufen, um den laufenden Kassenstand neu zu ziehen.</summary>
    public void ApplyOtherDaysTotals(decimal soll, decimal haben)
    {
        _otherDaysSollBisDatum = soll;
        _otherDaysHabenBisDatum = haben;
        Recalculate();
    }

    private void OnLinePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CategoryLineViewModel.Amount))
        {
            Recalculate();
        }
    }

    private void Recalculate()
    {
        SollSumme = Lines.Where(l => l.IsSoll).Sum(l => l.Amount);
        HabenSumme = Lines.Where(l => !l.IsSoll).Sum(l => l.Amount);
        Differenz = SollSumme - HabenSumme;
        IstAusgeglichen = Math.Abs(Differenz) < 0.005m;

        LaufenderSaldo = AnfangssaldoMonat
                         + _otherDaysSollBisDatum + SollSumme
                         - _otherDaysHabenBisDatum - HabenSumme;
    }

    /// <summary>
    /// Speichert eine einzelne geänderte Zeile sofort lokal (verschlüsselt) und stößt im Hintergrund
    /// eine Synchronisation mit der Zentraldatenbank an - kein manuelles "Speichern" nötig.
    /// </summary>
    public async Task CommitLineAsync(CategoryLineViewModel line)
    {
        if (_isLoading)
        {
            return;
        }

        var entry = new KassaEntry
        {
            Id = line.EntryId,
            Date = Date,
            Category = line.Category,
            Amount = line.Amount,
            BelegNr = line.BelegNr,
            Notiz = line.Notiz,
        };

        await _repository.UpsertEntryAsync(entry);
        IsNewDay = false;
        StatusMessage = $"Gespeichert um {DateTime.Now:HH:mm:ss} Uhr.";
        EntrySaved?.Invoke(this, EventArgs.Empty);

        _ = SyncInBackgroundAsync();
    }

    private async Task SyncInBackgroundAsync()
    {
        var result = await _syncService.SyncNowAsync();
        StatusMessage = result.Status switch
        {
            SyncStatus.Erfolgreich => $"Gespeichert und mit der Datenbank synchronisiert ({DateTime.Now:HH:mm:ss} Uhr).",
            SyncStatus.Fehler => $"Lokal gespeichert. Synchronisation fehlgeschlagen: {result.Fehlermeldung}",
            _ => "Lokal gespeichert. Keine Datenbankverbindung konfiguriert.",
        };
    }
}
