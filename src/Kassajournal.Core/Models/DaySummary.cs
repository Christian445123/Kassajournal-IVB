namespace Kassajournal.Core.Models;

/// <summary>
/// Tagessumme, entspricht der Summenzeile eines Tagesblocks in der Excel-Vorlage
/// (z. B. D12/E12 = SUMME(D6:D11)/SUMME(E6:E11)).
/// </summary>
/// <param name="Date">Datum des Tages.</param>
/// <param name="SollSumme">Summe der Soll-Zeile (nur "Tageslosung").</param>
/// <param name="HabenSumme">Summe aller Haben-Zeilen (Auszahlungen, Einzahlung Bank, Bankomat/Kredit, Bankomat/Automat, Geldentnahme).</param>
public record DaySummary(DateOnly Date, decimal SollSumme, decimal HabenSumme)
{
    /// <summary>
    /// In der Vorlage müssen Soll und Haben pro Tag exakt übereinstimmen –
    /// die Tageslosung muss vollständig auf die übrigen Kategorien aufgeteilt werden.
    /// </summary>
    public decimal Differenz => SollSumme - HabenSumme;

    public bool IstAusgeglichen => Math.Abs(Differenz) < 0.005m;
}
