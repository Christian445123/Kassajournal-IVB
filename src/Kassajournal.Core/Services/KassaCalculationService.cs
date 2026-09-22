using Kassajournal.Core.Models;

namespace Kassajournal.Core.Services;

/// <summary>
/// Reine Berechnungslogik, 1:1 nachgebaut aus den Formeln der Excel-Vorlage
/// "Kassajournal Vorlage.xlsx". Kein Seiteneffekt, keine Abhängigkeit von UI oder Datenbank
/// -> einfach testbar und die "Wahrheit" für alle Anzeigen in der App.
/// </summary>
public static class KassaCalculationService
{
    /// <summary>
    /// Tagessumme für ein einzelnes Datum (entspricht z. B. D12/E12 = SUMME(D6:D11)/SUMME(E6:E11)).
    /// </summary>
    public static DaySummary CalculateDaySummary(DateOnly date, IEnumerable<KassaEntry> entries)
    {
        var dayEntries = entries.Where(e => e.Date == date && !e.IsDeleted).ToList();
        decimal soll = dayEntries.Where(e => e.Category.IsSoll()).Sum(e => e.Amount);
        decimal haben = dayEntries.Where(e => !e.Category.IsSoll()).Sum(e => e.Amount);
        return new DaySummary(date, soll, haben);
    }

    /// <summary>
    /// Monatsauswertung (entspricht D2/E2/E3 im Monatsblatt): Summe Soll, Summe Haben und Saldo
    /// über den gesamten Monat, ausgehend vom (fixen) Anfangssaldo.
    /// </summary>
    public static MonthSummary CalculateMonthSummary(int year, int month, decimal anfangssaldo, IEnumerable<KassaEntry> entries)
    {
        var monthEntries = entries.Where(e => e.Date.Year == year && e.Date.Month == month && !e.IsDeleted).ToList();
        decimal soll = monthEntries.Where(e => e.Category.IsSoll()).Sum(e => e.Amount);
        decimal haben = monthEntries.Where(e => !e.Category.IsSoll()).Sum(e => e.Amount);
        return new MonthSummary(year, month, anfangssaldo, soll, haben);
    }

    /// <summary>
    /// Laufender Saldo direkt nach einem bestimmten Tag (Anfangssaldo + alle Soll/Haben bis inklusive diesem Tag).
    /// Nützlich, um in der Tagesansicht den aktuellen Kassenstand live anzuzeigen.
    /// </summary>
    public static decimal CalculateRunningSaldo(DateOnly uptoAndIncluding, decimal anfangssaldo, IEnumerable<KassaEntry> monthEntriesSoFar)
    {
        var relevant = monthEntriesSoFar.Where(e => e.Date <= uptoAndIncluding && !e.IsDeleted);
        decimal soll = relevant.Where(e => e.Category.IsSoll()).Sum(e => e.Amount);
        decimal haben = relevant.Where(e => !e.Category.IsSoll()).Sum(e => e.Amount);
        return anfangssaldo + soll - haben;
    }

    /// <summary>Jahresübersicht (entspricht dem "Auswertung"-Blatt: Summe Haben je Monat + Gesamtumsatz).</summary>
    public static IReadOnlyList<(int Month, decimal Summe)> CalculateYearOverview(int year, IEnumerable<KassaEntry> yearEntries)
    {
        var result = new List<(int, decimal)>();
        for (int month = 1; month <= 12; month++)
        {
            decimal haben = yearEntries
                .Where(e => e.Date.Year == year && e.Date.Month == month && !e.IsDeleted && !e.Category.IsSoll())
                .Sum(e => e.Amount);
            result.Add((month, haben));
        }
        return result;
    }
}
