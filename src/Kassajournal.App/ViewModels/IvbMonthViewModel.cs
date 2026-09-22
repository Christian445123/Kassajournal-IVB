using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Kassajournal.Core.Models;
using Kassajournal.Core.Services;

namespace Kassajournal.App.ViewModels;

public partial class IvbWeekGroupViewModel(DateOnly weekStart) : ObservableObject
{
    public DateOnly WeekStart { get; } = weekStart;

    public ObservableCollection<IvbDayRowViewModel> Tage { get; } = [];

    [ObservableProperty]
    private decimal _wochenSumme;
}

/// <summary>
/// Ein Monats-Reiter im IVB-Journal: Wochenraster Mo-Sa mit "täglicher Umsatz" je Tag,
/// genau wie "IVB Vorlage.xlsx". Zeigt immer den kompletten Kalendermonat (nicht nur bereits befüllte Tage).
/// </summary>
public partial class IvbMonthViewModel(IIvbRepository repository, Func<IvbDayRowViewModel> rowFactory, int month) : ObservableObject
{
    private static readonly string[] MonatsNamen =
    [
        "Jänner", "Februar", "März", "April", "Mai", "Juni",
        "Juli", "August", "September", "Oktober", "November", "Dezember",
    ];

    public int Year { get; private set; }

    public int Month { get; } = month;

    public string TabHeader { get; } = MonatsNamen[month - 1];

    public string MonatsName { get; private set; } = string.Empty;

    public ObservableCollection<IvbWeekGroupViewModel> Wochen { get; } = [];

    [ObservableProperty]
    private decimal _monatsSumme;

    [ObservableProperty]
    private bool _isLoaded;

    [ObservableProperty]
    private bool _isLoading;

    /// <summary>Der heutige Tag, falls er in diesem Monat liegt - damit die View automatisch dorthin scrollen kann.</summary>
    public DateOnly? HeutigerTagFallsInMonat
    {
        get
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            return today.Year == Year && today.Month == Month ? today : null;
        }
    }

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
            var entries = await repository.GetEntriesForMonthAsync(Year, Month);
            var entriesByDate = entries.Where(e => !e.IsDeleted).ToDictionary(e => e.Date);
            var weeks = IvbCalculationService.BuildWeeks(Year, Month, entries);

            foreach (var group in Wochen)
            {
                foreach (var row in group.Tage)
                {
                    row.EntrySaved -= OnEntrySaved;
                }
            }

            Wochen.Clear();

            foreach (var week in weeks)
            {
                var groupVm = new IvbWeekGroupViewModel(week.WeekStart) { WochenSumme = week.WochenSumme };
                foreach (var date in week.Tage)
                {
                    var row = rowFactory();
                    entriesByDate.TryGetValue(date, out var existing);

                    if (existing is null && AustrianHolidays.IsHoliday(date))
                    {
                        // Automatisch als Feiertag erkannt und sofort gespeichert - ohne Zutun des Benutzers.
                        existing = new IvbEntry { Date = date, Amount = 0m, IsFeiertag = true };
                        await repository.UpsertEntryAsync(existing);
                    }

                    row.Populate(date, existing);
                    row.EntrySaved += OnEntrySaved;
                    groupVm.Tage.Add(row);
                }

                Wochen.Add(groupVm);
            }

            MonatsSumme = IvbCalculationService.CalculateMonthTotal(weeks);
            OnPropertyChanged(nameof(HeutigerTagFallsInMonat));
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnEntrySaved(object? sender, EventArgs e) => _ = RecalculateSummeAsync();

    private async Task RecalculateSummeAsync()
    {
        // Wochensummen aus den aktuell in der UI stehenden Werten neu berechnen (kein DB-Roundtrip nötig).
        decimal monatsSumme = 0m;
        foreach (var group in Wochen)
        {
            group.WochenSumme = group.Tage.Where(t => !t.IsFeiertag).Sum(t => t.Amount);
            monatsSumme += group.WochenSumme;
        }

        MonatsSumme = monatsSumme;
        await Task.CompletedTask;
    }
}
