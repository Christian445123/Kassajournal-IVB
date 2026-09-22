using Kassajournal.Core.Models;
using Kassajournal.Core.Services;

namespace Kassajournal.Core.Tests;

public class KassaCalculationServiceTests
{
    private static KassaEntry Entry(DateOnly date, KassaCategory category, decimal amount) => new()
    {
        Date = date,
        Category = category,
        Amount = amount,
    };

    [Fact]
    public void DaySummary_MatchesExcelExample_02_01_2025()
    {
        // Reale Werte aus Kassajournal 2025.xlsx, Blatt "Jänner", Tag 02.01.2025:
        // Tageslosung 6.856,95 = Auszahlungen 556,90 + Einzahlung Bank 3.372,55 + Bankomat/Kredit 1.850,50 + Bankomat/Automat 1.077,00 + Geldentnahme 0
        var date = new DateOnly(2025, 1, 2);
        var entries = new List<KassaEntry>
        {
            Entry(date, KassaCategory.Tageslosung, 6856.95m),
            Entry(date, KassaCategory.Auszahlungen, 556.90m),
            Entry(date, KassaCategory.EinzahlungBank, 3372.55m),
            Entry(date, KassaCategory.BankomatKredit, 1850.50m),
            Entry(date, KassaCategory.BankomatAutomat, 1077.00m),
        };

        var summary = KassaCalculationService.CalculateDaySummary(date, entries);

        Assert.Equal(6856.95m, summary.SollSumme);
        Assert.Equal(6856.95m, summary.HabenSumme);
        Assert.Equal(0m, summary.Differenz);
        Assert.True(summary.IstAusgeglichen);
    }

    [Fact]
    public void DaySummary_Unbalanced_ReportsDifference()
    {
        var date = new DateOnly(2026, 3, 5);
        var entries = new List<KassaEntry>
        {
            Entry(date, KassaCategory.Tageslosung, 1000m),
            Entry(date, KassaCategory.Auszahlungen, 300m),
        };

        var summary = KassaCalculationService.CalculateDaySummary(date, entries);

        Assert.Equal(1000m, summary.SollSumme);
        Assert.Equal(300m, summary.HabenSumme);
        Assert.Equal(700m, summary.Differenz);
        Assert.False(summary.IstAusgeglichen);
    }

    [Fact]
    public void MonthSummary_MatchesExcelHeaderFormulas()
    {
        // Zwei Tage im Monat, Anfangssaldo 820 wie in der Vorlage (D1), Saldo-Formel E3 = D1+D2-E2.
        var day1 = new DateOnly(2025, 1, 2);
        var day2 = new DateOnly(2025, 1, 3);
        var entries = new List<KassaEntry>
        {
            Entry(day1, KassaCategory.Tageslosung, 6856.95m),
            Entry(day1, KassaCategory.Auszahlungen, 556.90m),
            Entry(day1, KassaCategory.EinzahlungBank, 3372.55m),
            Entry(day1, KassaCategory.BankomatKredit, 1850.50m),
            Entry(day1, KassaCategory.BankomatAutomat, 1077.00m),

            Entry(day2, KassaCategory.Tageslosung, 7808.93m),
            Entry(day2, KassaCategory.Auszahlungen, 363.30m),
            Entry(day2, KassaCategory.EinzahlungBank, 3326.00m),
            Entry(day2, KassaCategory.BankomatKredit, 3934.83m),
            Entry(day2, KassaCategory.BankomatAutomat, 184.80m),
        };

        var summary = KassaCalculationService.CalculateMonthSummary(2025, 1, 820m, entries);

        Assert.Equal(14665.88m, summary.SummeSoll);
        Assert.Equal(14665.88m, summary.SummeHaben);
        Assert.Equal(820m, summary.Saldo); // Soll == Haben, also bleibt der Saldo beim Anfangssaldo
    }

    [Fact]
    public void MonthSummary_IgnoresEntriesFromOtherMonths()
    {
        var entries = new List<KassaEntry>
        {
            Entry(new DateOnly(2025, 1, 31), KassaCategory.Tageslosung, 100m),
            Entry(new DateOnly(2025, 2, 1), KassaCategory.Tageslosung, 999m),
        };

        var summary = KassaCalculationService.CalculateMonthSummary(2025, 1, 820m, entries);

        Assert.Equal(100m, summary.SummeSoll);
    }

    [Fact]
    public void MonthSummary_IgnoresDeletedEntries()
    {
        var date = new DateOnly(2025, 5, 5);
        var entries = new List<KassaEntry>
        {
            Entry(date, KassaCategory.Tageslosung, 500m),
            new() { Date = date, Category = KassaCategory.Tageslosung, Amount = 999m, IsDeleted = true },
        };

        var summary = KassaCalculationService.CalculateMonthSummary(2025, 5, 820m, entries);

        Assert.Equal(500m, summary.SummeSoll);
    }

    [Fact]
    public void RunningSaldo_AccumulatesUpToGivenDate()
    {
        var day1 = new DateOnly(2025, 1, 2);
        var day2 = new DateOnly(2025, 1, 3);
        var entries = new List<KassaEntry>
        {
            Entry(day1, KassaCategory.Tageslosung, 1000m),
            Entry(day1, KassaCategory.Geldentnahme, 200m),
            Entry(day2, KassaCategory.Tageslosung, 500m),
            Entry(day2, KassaCategory.Auszahlungen, 500m),
        };

        var saldoAfterDay1 = KassaCalculationService.CalculateRunningSaldo(day1, 820m, entries);
        var saldoAfterDay2 = KassaCalculationService.CalculateRunningSaldo(day2, 820m, entries);

        Assert.Equal(1620m, saldoAfterDay1); // 820 + 1000 - 200
        Assert.Equal(1620m, saldoAfterDay2); // + 500 - 500 = unverändert
    }

    [Fact]
    public void YearOverview_ReturnsAllTwelveMonths_MissingMonthsAreZero()
    {
        var entries = new List<KassaEntry>
        {
            Entry(new DateOnly(2025, 1, 5), KassaCategory.Auszahlungen, 100m),
            Entry(new DateOnly(2025, 3, 5), KassaCategory.Geldentnahme, 50m),
        };

        var overview = KassaCalculationService.CalculateYearOverview(2025, entries);

        Assert.Equal(12, overview.Count);
        Assert.Equal(100m, overview.Single(o => o.Month == 1).Summe);
        Assert.Equal(0m, overview.Single(o => o.Month == 2).Summe);
        Assert.Equal(50m, overview.Single(o => o.Month == 3).Summe);
    }
}
