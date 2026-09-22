namespace Kassajournal.Core.Models;

/// <summary>
/// Entspricht Zelle D1 "Anfangsaldo" je Monatsblatt in der Excel-Vorlage.
/// In der Vorlage ist das ein fixer Betrag (Wechselgeld-Grundstock), der sich
/// NICHT automatisch vom Vormonat fortschreibt (das haben die realen Jahresdateien bestätigt).
/// </summary>
public class MonthSettings
{
    public int Year { get; set; }

    public int Month { get; set; }

    /// <summary>Default 820 € – wie in der Vorlage und allen bisherigen Jahren.</summary>
    public decimal Anfangssaldo { get; set; } = 820m;
}
