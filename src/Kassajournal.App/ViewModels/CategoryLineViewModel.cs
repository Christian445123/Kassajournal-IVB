using CommunityToolkit.Mvvm.ComponentModel;
using Kassajournal.Core.Models;

namespace Kassajournal.App.ViewModels;

/// <summary>Eine Eingabezeile im Tagesblock (z. B. "Tageslosung" mit ihrem Betrag).</summary>
public partial class CategoryLineViewModel : ObservableObject
{
    public CategoryLineViewModel(KassaCategory category)
    {
        Category = category;
        DisplayName = category.DisplayName();
        IsSoll = category.IsSoll();
    }

    public KassaCategory Category { get; }

    public string DisplayName { get; }

    /// <summary>Stabile Id des zugehörigen Datenbank-Eintrags, damit "Speichern" ein Update statt einer Dublette macht.</summary>
    public Guid EntryId { get; set; } = Guid.NewGuid();

    public bool IsSoll { get; }

    [ObservableProperty]
    private decimal _amount;

    /// <summary>Freitext-Eingabefeld (erlaubt Komma als Dezimaltrennzeichen); wird beim Verlassen des Feldes geparst.</summary>
    [ObservableProperty]
    private string _amountText = "0,00";

    partial void OnAmountChanged(decimal value)
    {
        AmountText = value.ToString("N2");
    }

    public bool TryCommitAmountText()
    {
        var normalized = AmountText.Replace(".", "").Replace(",", ".").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            Amount = 0m;
            return true;
        }

        if (decimal.TryParse(normalized, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var parsed) && parsed >= 0)
        {
            Amount = parsed;
            return true;
        }

        // Ungültige Eingabe -> alten Wert wiederherstellen
        AmountText = Amount.ToString("N2");
        return false;
    }

    public string? BelegNr { get; set; }

    public string? Notiz { get; set; }
}
