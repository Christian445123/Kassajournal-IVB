namespace Kassajournal.Core.Models;

/// <summary>
/// Die sechs festen Buchungsarten pro Tag, exakt wie in der Excel-Vorlage
/// "Kassajournal Vorlage.xlsx" (Blatt je Monat, Spalten C/I "Text").
/// </summary>
public enum KassaCategory
{
    /// <summary>Tageslosung (Soll) – die Tageseinnahmen, die auf die übrigen Kategorien aufgeteilt werden.</summary>
    Tageslosung = 0,

    /// <summary>Auszahlungen (Haben) – bar bezahlte Ausgaben.</summary>
    Auszahlungen = 1,

    /// <summary>Einzahlung Bank (Haben) – Bargeld, das auf das Bankkonto eingezahlt wurde.</summary>
    EinzahlungBank = 2,

    /// <summary>Bankomat/Kredit (Haben) – unbar bezahlte Umsätze (Bankomat- oder Kreditkarte).</summary>
    BankomatKredit = 3,

    /// <summary>Bankomat/Automat (Haben) – unbar bezahlte Umsätze über Automat/Terminal.</summary>
    BankomatAutomat = 4,

    /// <summary>Geldentnahme (Haben) – private Bargeldentnahme aus der Kasse.</summary>
    Geldentnahme = 5,
}

public static class KassaCategoryExtensions
{
    /// <summary>
    /// In der Vorlage steht nur "Tageslosung" auf der Soll-Seite (Spalte D/J),
    /// alle anderen fünf Kategorien stehen auf der Haben-Seite (Spalte E/K).
    /// </summary>
    public static bool IsSoll(this KassaCategory category) => category == KassaCategory.Tageslosung;

    public static string DisplayName(this KassaCategory category) => category switch
    {
        KassaCategory.Tageslosung => "Tageslosung",
        KassaCategory.Auszahlungen => "Auszahlungen",
        KassaCategory.EinzahlungBank => "Einzahlung Bank",
        KassaCategory.BankomatKredit => "Bankomat/Kredit",
        KassaCategory.BankomatAutomat => "Bankomat/Automat",
        KassaCategory.Geldentnahme => "Geldentnahme",
        _ => category.ToString(),
    };

    /// <summary>Reihenfolge, in der die Kategorien im Tagesblock angezeigt werden (wie in der Excel-Vorlage).</summary>
    public static readonly IReadOnlyList<KassaCategory> AllInOrder =
    [
        KassaCategory.Tageslosung,
        KassaCategory.Auszahlungen,
        KassaCategory.EinzahlungBank,
        KassaCategory.BankomatKredit,
        KassaCategory.BankomatAutomat,
        KassaCategory.Geldentnahme,
    ];
}
