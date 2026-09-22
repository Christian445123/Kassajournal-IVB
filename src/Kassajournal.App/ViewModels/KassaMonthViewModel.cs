using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kassajournal.Core.Models;
using Kassajournal.Core.Services;

namespace Kassajournal.App.ViewModels;

/// <summary>
/// Ein Monats-Reiter im Kassajournal (z. B. "Jänner"): zeigt alle Tage dieses Monats zum direkten
/// Eintippen, sortiert mit dem neuesten Tag ganz oben. Wird erst beim ersten Anzeigen geladen
/// (Lazy Loading), damit der Start der App nicht 12 Monate auf einmal laden muss.
/// </summary>
public partial class KassaMonthViewModel(IKassaRepository repository, Func<DayEntryViewModel> dayFactory, int month) : ObservableObject
{
    private static readonly string[] MonatsNamen =
    [
        "Jänner", "Februar", "März", "April", "Mai", "Juni",
        "Juli", "August", "September", "Oktober", "November", "Dezember",
    ];

    public int Year { get; private set; }

    /// <summary>Fix (1-12), steht von Anfang an fest, damit der Reiter-Titel sofort ohne Laden angezeigt werden kann.</summary>
    public int Month { get; } = month;

    /// <summary>Reiter-Titel, z. B. "Jänner" (entspricht dem Blattnamen in der Excel-Vorlage).</summary>
    public string TabHeader { get; } = MonatsNamen[month - 1];

    public string MonatsName { get; private set; } = string.Empty;

    public ObservableCollection<DayEntryViewModel> Tage { get; } = [];

    [ObservableProperty]
    private bool _isLoaded;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private decimal _anfangssaldo;

    [ObservableProperty]
    private decimal _summeSoll;

    [ObservableProperty]
    private decimal _summeHaben;

    [ObservableProperty]
    private decimal _saldo;

    public async Task EnsureLoadedAsync(int year)
    {
        if (IsLoaded && Year == year)
        {
            return;
        }

        Year = year;
        MonatsName = new DateOnly(year, Month, 1).ToString("MMMM yyyy", CultureInfo.GetCultureInfo("de-AT"));

        await ReloadAsync();
        IsLoaded = true;
    }

    public async Task ReloadAsync()
    {
        IsLoading = true;
        try
        {
            var monthEntries = await repository.GetEntriesForMonthAsync(Year, Month);
            var monthSettings = await repository.GetOrCreateMonthSettingsAsync(Year, Month);
            Anfangssaldo = monthSettings.Anfangssaldo;

            var dates = monthEntries.Select(e => e.Date).Distinct().ToList();

            var today = DateOnly.FromDateTime(DateTime.Today);
            if (today.Year == Year && today.Month == Month && today.DayOfWeek != DayOfWeek.Sunday && !dates.Contains(today))
            {
                // Der Tag, an dem das Programm geöffnet wurde, steht immer bereit - auch ohne Buchungen.
                // Sonntag wird nie automatisch angelegt (Mo-Sa, wie IVB).
                dates.Add(today);
            }

            dates = dates.OrderByDescending(d => d).ToList(); // neuester Tag ganz oben

            // Tage, für die noch keine Buchung existiert (z. B. der neu angelegte heutige Tag) -
            // dafür wird automatisch geprüft, ob es ein österreichischer Feiertag ist.
            var bereitsVorhandeneDaten = monthEntries.Select(e => e.Date).ToHashSet();

            foreach (var existing in Tage)
            {
                existing.EntrySaved -= OnAnyEntrySaved;
                existing.DateChanged -= OnAnyDateChanged;
            }

            Tage.Clear();

            foreach (var date in dates)
            {
                var day = dayFactory();
                day.PopulateFromMonthData(date, monthEntries, monthSettings.Anfangssaldo);
                day.EntrySaved += OnAnyEntrySaved;
                day.DateChanged += OnAnyDateChanged;
                Tage.Add(day);

                if (!bereitsVorhandeneDaten.Contains(date) && AustrianHolidays.IsHoliday(date))
                {
                    // Automatisch erkannt und sofort gespeichert - ohne Zutun des Benutzers.
                    await day.ToggleFeiertagAsync(true);
                }
            }

            RecalculateSummary();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnAnyEntrySaved(object? sender, EventArgs e) => RecalculateSummary();

    /// <summary>Ein Tag hat ein neues Datum bekommen (Korrektur) - kompletter Neu-Ladevorgang, da er den Monat verlassen haben könnte.</summary>
    private void OnAnyDateChanged(object? sender, EventArgs e) => _ = ReloadAsync();

    /// <summary>Berechnet die laufenden Kassenstände neu (chronologisch) und die Kopfzahlen des Monats.</summary>
    private void RecalculateSummary()
    {
        decimal runningSoll = 0m;
        decimal runningHaben = 0m;
        foreach (var day in Tage.OrderBy(d => d.Date))
        {
            day.ApplyOtherDaysTotals(runningSoll, runningHaben);
            runningSoll += day.SollSumme;
            runningHaben += day.HabenSumme;
        }

        SummeSoll = runningSoll;
        SummeHaben = runningHaben;
        Saldo = Anfangssaldo + runningSoll - runningHaben;
    }

    [ObservableProperty]
    private bool _isEditingAnfangssaldo;

    [ObservableProperty]
    private string _anfangssaldoText = "820,00";

    [RelayCommand]
    private void AnfangssaldoBearbeiten()
    {
        AnfangssaldoText = Anfangssaldo.ToString("N2");
        IsEditingAnfangssaldo = true;
    }

    [RelayCommand]
    private async Task AnfangssaldoSpeichernAsync()
    {
        var normalized = AnfangssaldoText.Replace(".", "").Replace(",", ".").Trim();
        if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) && parsed >= 0)
        {
            await repository.SaveMonthSettingsAsync(new MonthSettings { Year = Year, Month = Month, Anfangssaldo = parsed });
            await ReloadAsync();
        }

        IsEditingAnfangssaldo = false;
    }

    [ObservableProperty]
    private bool _isAddingDay;

    [ObservableProperty]
    private DateTime? _neuerTagDatum;

    [ObservableProperty]
    private string? _neuerTagFehler;

    /// <summary>Öffnet die Datumsauswahl zum Nachtragen eines beliebigen Tages (auch länger zurückliegende).</summary>
    [RelayCommand]
    private void NeuerTagVorbereiten()
    {
        var letzterTag = Tage.Count > 0 ? Tage.Max(d => d.Date) : new DateOnly(Year, Month, 1).AddDays(-1);
        var vorschlag = letzterTag.AddDays(1);
        if (vorschlag.DayOfWeek == DayOfWeek.Sunday)
        {
            vorschlag = vorschlag.AddDays(1);
        }

        if (vorschlag.Month != Month || vorschlag.Year != Year)
        {
            vorschlag = new DateOnly(Year, Month, 1);
        }

        NeuerTagDatum = vorschlag.ToDateTime(TimeOnly.MinValue);
        NeuerTagFehler = null;
        IsAddingDay = true;
    }

    [RelayCommand]
    private void NeuerTagAbbrechen()
    {
        IsAddingDay = false;
        NeuerTagFehler = null;
    }

    /// <summary>Legt einen Tag mit dem gewählten Datum an - egal ob in der Vergangenheit oder für heute/später.</summary>
    [RelayCommand]
    private async Task NeuerTagBestaetigenAsync()
    {
        if (NeuerTagDatum is null)
        {
            NeuerTagFehler = "Bitte ein Datum wählen.";
            return;
        }

        var datum = DateOnly.FromDateTime(NeuerTagDatum.Value);

        if (datum.DayOfWeek == DayOfWeek.Sunday)
        {
            NeuerTagFehler = "Sonntag ist kein gültiger Öffnungstag.";
            return;
        }

        if (datum.Year != Year || datum.Month != Month)
        {
            NeuerTagFehler = "Bitte ein Datum innerhalb dieses Monats wählen.";
            return;
        }

        if (Tage.Any(d => d.Date == datum))
        {
            NeuerTagFehler = "Für diesen Tag gibt es schon einen Eintrag.";
            return;
        }

        var monthEntries = await repository.GetEntriesForMonthAsync(Year, Month);
        var day = dayFactory();
        day.PopulateFromMonthData(datum, monthEntries, Anfangssaldo);
        day.EntrySaved += OnAnyEntrySaved;
        day.DateChanged += OnAnyDateChanged;

        var einfuegeIndex = Tage.ToList().FindIndex(d => d.Date < datum);
        if (einfuegeIndex < 0)
        {
            Tage.Add(day);
        }
        else
        {
            Tage.Insert(einfuegeIndex, day);
        }

        if (AustrianHolidays.IsHoliday(datum))
        {
            await day.ToggleFeiertagAsync(true);
        }

        RecalculateSummary();
        IsAddingDay = false;
        NeuerTagFehler = null;
    }
}
