namespace Kassajournal.Core.Models;

/// <summary>
/// Monatsauswertung, entspricht den Kopfzellen D2/E2/E3 (Summe Soll, Summe Haben, Saldo)
/// im jeweiligen Monatsblatt der Excel-Vorlage.
/// </summary>
public record MonthSummary(int Year, int Month, decimal Anfangssaldo, decimal SummeSoll, decimal SummeHaben)
{
    /// <summary>Entspricht Formel E3 = SUMME(D1+D2-E2): Anfangssaldo + Summe Soll - Summe Haben.</summary>
    public decimal Saldo => Anfangssaldo + SummeSoll - SummeHaben;
}
