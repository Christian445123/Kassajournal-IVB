using Kassajournal.Core.Models;

namespace Kassajournal.Core.Services;

/// <summary>Eine Kalenderwoche (Montag–Samstag) innerhalb eines IVB-Monats, inkl. Wochensumme.</summary>
public record IvbWeek(DateOnly WeekStart, IReadOnlyList<DateOnly> Tage, decimal WochenSumme);

/// <summary>
/// Reine Berechnungslogik für das IVB-Journal, nachgebaut aus "IVB Vorlage.xlsx":
/// Wochensumme = Summe der täglichen Umsätze Mo–Sa, Monatssumme = Summe aller Wochensummen.
/// </summary>
public static class IvbCalculationService
{
    /// <summary>Alle Werktage (Mo–Sa, ohne Sonntag) eines Monats, gruppiert nach Kalenderwoche - genau wie die Vorlage.</summary>
    public static IReadOnlyList<IvbWeek> BuildWeeks(int year, int month, IEnumerable<IvbEntry> monthEntries)
    {
        var entriesByDate = monthEntries
            .Where(e => !e.IsDeleted)
            .ToDictionary(e => e.Date, e => e);

        var firstOfMonth = new DateOnly(year, month, 1);
        var lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);

        // Wochenstart = Montag der Woche, in der der Monat beginnt.
        var firstMonday = firstOfMonth.AddDays(-(int)firstOfMonth.DayOfWeek + 1);
        if (firstOfMonth.DayOfWeek == DayOfWeek.Sunday)
        {
            firstMonday = firstOfMonth.AddDays(1);
        }

        var weeks = new List<IvbWeek>();
        var weekStart = firstMonday;

        while (weekStart <= lastOfMonth)
        {
            var tage = new List<DateOnly>();
            decimal wochenSumme = 0m;

            for (int i = 0; i < 6; i++) // Mo..Sa
            {
                var day = weekStart.AddDays(i);
                if (day.Month != month || day.Year != year)
                {
                    continue;
                }

                tage.Add(day);
                if (entriesByDate.TryGetValue(day, out var entry) && !entry.IsFeiertag)
                {
                    wochenSumme += entry.Amount;
                }
            }

            if (tage.Count > 0)
            {
                weeks.Add(new IvbWeek(weekStart, tage, wochenSumme));
            }

            weekStart = weekStart.AddDays(7);
        }

        return weeks;
    }

    public static decimal CalculateMonthTotal(IReadOnlyList<IvbWeek> weeks) => weeks.Sum(w => w.WochenSumme);

    public static decimal CalculateMonthTotal(int year, int month, IEnumerable<IvbEntry> monthEntries)
        => CalculateMonthTotal(BuildWeeks(year, month, monthEntries));

    public static IReadOnlyList<(int Month, decimal Summe)> CalculateYearOverview(int year, IEnumerable<IvbEntry> yearEntries)
    {
        var entries = yearEntries.ToList();
        var result = new List<(int, decimal)>();
        for (int month = 1; month <= 12; month++)
        {
            var monthEntries = entries.Where(e => e.Date.Year == year && e.Date.Month == month);
            result.Add((month, CalculateMonthTotal(year, month, monthEntries)));
        }

        return result;
    }
}
