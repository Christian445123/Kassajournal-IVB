namespace Kassajournal.Core.Models;

/// <summary>
/// Ein Tageseintrag im IVB-Journal (entspricht einer Zeile "täglicher Umsatz" in der
/// Excel-Vorlage "IVB Vorlage.xlsx"). Im Gegensatz zum Kassajournal gibt es hier nur
/// einen einzigen Betrag pro Tag, keine Soll/Haben-Aufteilung. Sonntag kommt nicht vor
/// (die Vorlage kennt nur MO–SA); an Feiertagen wird kein Betrag erfasst.
/// </summary>
public class IvbEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateOnly Date { get; set; }

    public decimal Amount { get; set; }

    /// <summary>Entspricht dem Text "Feiertag" statt eines Betrags in der Vorlage.</summary>
    public bool IsFeiertag { get; set; }

    public string? Notiz { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public bool IsDeleted { get; set; }

    public bool IsSynced { get; set; }
}
