namespace Kassajournal.Core.Models;

/// <summary>
/// Eine einzelne Buchungszeile im Kassajournal (entspricht einer Zeile im Tagesblock der Excel-Vorlage,
/// z. B. "Tageslosung" oder "Auszahlungen" mit dem jeweiligen Betrag).
/// </summary>
public class KassaEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateOnly Date { get; set; }

    public KassaCategory Category { get; set; }

    public decimal Amount { get; set; }

    /// <summary>Beleg-Nummer (Spalte A/G "Blgnr." in der Vorlage) – optional, wird in der Praxis selten genutzt.</summary>
    public string? BelegNr { get; set; }

    public string? Notiz { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Soft-Delete-Flag, damit Löschungen korrekt mit der Zentraldatenbank synchronisiert werden können.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Wird von der Synchronisation gesetzt/verwendet, um Änderungen seit dem letzten Abgleich zu erkennen.</summary>
    public bool IsSynced { get; set; }
}
