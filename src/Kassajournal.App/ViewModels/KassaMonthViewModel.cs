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

            foreach (var existing in Tage)
            {
                existing.EntrySaved -= OnAnyEntrySaved;
            }

            Tage.Clear();

            foreach (var date in dates)
            {
                var day = dayFactory();
                day.PopulateFromMonthData(date, monthEntries, monthSettings.Anfangssaldo);
                day.EntrySaved += OnAnyEntrySaved;
                Tage.Add(day);
            }

            RecalculateSummary();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnAnyEntrySaved(object? sender, EventArgs e) => RecalculateSummary();

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

    /// <summary>Fügt den nächsten noch fehlenden Tag (nach dem bisher jüngsten) am Anfang der Liste hinzu.</summary>
    [RelayCommand]
    private async Task NeuerTagAsync()
    {
        var letzterTag = Tage.Count > 0 ? Tage.Max(d => d.Date) : new DateOnly(Year, Month, 1).AddDays(-1);
        var naechsterTag = letzterTag.AddDays(1);
        if (naechsterTag.DayOfWeek == DayOfWeek.Sunday)
        {
            naechsterTag = naechsterTag.AddDays(1); // Sonntag überspringen (Mo-Sa, wie IVB)
        }

        if (naechsterTag.Month != Month || naechsterTag.Year != Year)
        {
            return; // Monat ist voll - nächster Tag gehört in den nächsten Monat
        }

        var monthEntries = await repository.GetEntriesForMonthAsync(Year, Month);
        var day = dayFactory();
        day.PopulateFromMonthData(naechsterTag, monthEntries, Anfangssaldo);
        day.EntrySaved += OnAnyEntrySaved;
        Tage.Insert(0, day);
        RecalculateSummary();
    }
}
