using Kassajournal.Core.Models;
using Kassajournal.Core.Services;

namespace Kassajournal.Core.Tests;

public class IvbCalculationServiceTests
{
    private static IvbEntry Entry(DateOnly date, decimal amount, bool feiertag = false) => new()
    {
        Date = date,
        Amount = amount,
        IsFeiertag = feiertag,
    };

    /// <summary>Reale Werte aus IVB 2025.xlsx, Blatt "Jänner 2025" (siehe Formeln E9/E16/E23/E30/E37/F38).</summary>
    [Fact]
    public void BuildWeeks_And_MonthTotal_MatchExcel_Januar2025()
    {
        var entries = new List<IvbEntry>
        {
            Entry(new DateOnly(2025, 1, 1), 0, feiertag: true),
            Entry(new DateOnly(2025, 1, 2), 121.40m),
            Entry(new DateOnly(2025, 1, 3), 52.30m),
            Entry(new DateOnly(2025, 1, 4), 6.00m),

            Entry(new DateOnly(2025, 1, 6), 0, feiertag: true),
            Entry(new DateOnly(2025, 1, 7), 100.50m),
            Entry(new DateOnly(2025, 1, 8), 78.30m),
            Entry(new DateOnly(2025, 1, 9), 83.60m),
            Entry(new DateOnly(2025, 1, 10), 12.60m),
            Entry(new DateOnly(2025, 1, 11), 64.50m),

            Entry(new DateOnly(2025, 1, 13), 120.30m),
            Entry(new DateOnly(2025, 1, 14), 31.10m),
            Entry(new DateOnly(2025, 1, 15), 26.20m),
            Entry(new DateOnly(2025, 1, 16), 62.20m),
            Entry(new DateOnly(2025, 1, 17), 59.00m),
            Entry(new DateOnly(2025, 1, 18), 0.00m),

            Entry(new DateOnly(2025, 1, 20), 122.50m),
            Entry(new DateOnly(2025, 1, 21), 65.10m),
            Entry(new DateOnly(2025, 1, 22), 33.80m),
            Entry(new DateOnly(2025, 1, 23), 26.00m),
            Entry(new DateOnly(2025, 1, 24), 115.50m),
            Entry(new DateOnly(2025, 1, 25), 47.40m),

            Entry(new DateOnly(2025, 1, 27), 35.70m),
            Entry(new DateOnly(2025, 1, 28), 4.60m),
            Entry(new DateOnly(2025, 1, 29), 62.20m),
            Entry(new DateOnly(2025, 1, 30), 46.70m),
            Entry(new DateOnly(2025, 1, 31), 82.70m),
        };

        var weeks = IvbCalculationService.BuildWeeks(2025, 1, entries);

        Assert.Equal(5, weeks.Count);
        Assert.Equal(179.70m, weeks[0].WochenSumme);
        Assert.Equal(339.50m, weeks[1].WochenSumme);
        Assert.Equal(298.80m, weeks[2].WochenSumme);
        Assert.Equal(410.30m, weeks[3].WochenSumme);
        Assert.Equal(231.90m, weeks[4].WochenSumme);

        var monthTotal = IvbCalculationService.CalculateMonthTotal(weeks);
        Assert.Equal(1460.20m, monthTotal);
    }

    [Fact]
    public void BuildWeeks_ExcludesSunday()
    {
        var entries = new List<IvbEntry>();
        var weeks = IvbCalculationService.BuildWeeks(2025, 1, entries);

        Assert.All(weeks, week => Assert.All(week.Tage, day => Assert.NotEqual(DayOfWeek.Sunday, day.DayOfWeek)));
    }

    [Fact]
    public void YearOverview_ReturnsTwelveMonths()
    {
        var entries = new List<IvbEntry> { Entry(new DateOnly(2025, 1, 2), 100m) };
        var overview = IvbCalculationService.CalculateYearOverview(2025, entries);

        Assert.Equal(12, overview.Count);
        Assert.Equal(100m, overview.Single(o => o.Month == 1).Summe);
    }
}
