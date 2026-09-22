using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Kassajournal.Core.Models;
using Kassajournal.Core.Services;
using Kassajournal.Data.Sync;

namespace Kassajournal.App.ViewModels;

/// <summary>
/// Eine Tageszeile im IVB-Journal: ein einziger Betrag ("täglicher Umsatz") plus Feiertags-Kennzeichnung -
/// entspricht einer Zeile in "IVB Vorlage.xlsx". Speichert wie im Kassajournal sofort bei jeder Änderung.
/// </summary>
public partial class IvbDayRowViewModel(IIvbRepository repository, IvbSyncService syncService) : ObservableObject
{
    private bool _isLoading;

    public event EventHandler? EntrySaved;

    public Guid EntryId { get; private set; } = Guid.NewGuid();

    public DateOnly Date { get; private set; }

    public string Wochentag => Date.ToString("ddd", CultureInfo.GetCultureInfo("de-AT")).ToUpperInvariant().TrimEnd('.');

    public bool IstHeute => Date == DateOnly.FromDateTime(DateTime.Today);

    [ObservableProperty]
    private decimal _amount;

    [ObservableProperty]
    private string _amountText = "0,00";

    [ObservableProperty]
    private bool _isFeiertag;

    public bool IsEditable => !IsFeiertag;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    partial void OnAmountChanged(decimal value) => AmountText = value.ToString("N2");

    /// <summary>
    /// Lädt diesen Tag eigenständig (fragt seine Daten selbst ab, ohne einen übergeordneten
    /// Monats-Reiter zu benötigen) - wird von der "Heute"-Seite verwendet. Erkennt dabei automatisch
    /// Feiertage wie beim regulären Monatsraster.
    /// </summary>
    public async Task LoadAsync(DateOnly date)
    {
        var monthEntries = await repository.GetEntriesForMonthAsync(date.Year, date.Month);
        var existing = monthEntries.FirstOrDefault(e => e.Date == date && !e.IsDeleted);

        if (existing is null && AustrianHolidays.IsHoliday(date))
        {
            existing = new IvbEntry { Date = date, Amount = 0m, IsFeiertag = true };
            await repository.UpsertEntryAsync(existing);
        }

        Populate(date, existing);
    }

    public void Populate(DateOnly date, IvbEntry? existing)
    {
        _isLoading = true;
        try
        {
            Date = date;
            OnPropertyChanged(nameof(Wochentag));
            OnPropertyChanged(nameof(IstHeute));
            EntryId = existing?.Id ?? Guid.NewGuid();
            Amount = existing?.Amount ?? 0m;
            IsFeiertag = existing?.IsFeiertag ?? false;
            StatusMessage = string.Empty;
        }
        finally
        {
            _isLoading = false;
        }
    }

    public bool TryCommitAmountText()
    {
        var normalized = AmountText.Replace(".", "").Replace(",", ".").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            Amount = 0m;
            return true;
        }

        if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) && parsed >= 0)
        {
            Amount = parsed;
            return true;
        }

        AmountText = Amount.ToString("N2");
        return false;
    }

    public async Task CommitAsync()
    {
        if (_isLoading)
        {
            return;
        }

        var entry = new IvbEntry
        {
            Id = EntryId,
            Date = Date,
            Amount = IsFeiertag ? 0m : Amount,
            IsFeiertag = IsFeiertag,
        };

        await repository.UpsertEntryAsync(entry);
        StatusMessage = $"Gespeichert {DateTime.Now:HH:mm:ss}";
        EntrySaved?.Invoke(this, EventArgs.Empty);

        _ = SyncInBackgroundAsync();
    }

    private async Task SyncInBackgroundAsync()
    {
        var result = await syncService.SyncNowAsync();
        StatusMessage = result.Status switch
        {
            SyncStatus.Erfolgreich => $"Synchronisiert {DateTime.Now:HH:mm:ss}",
            SyncStatus.Fehler => $"Lokal gespeichert, Sync fehlgeschlagen: {result.Fehlermeldung}",
            _ => "Lokal gespeichert (keine DB konfiguriert)",
        };
    }

    partial void OnIsFeiertagChanged(bool value)
    {
        OnPropertyChanged(nameof(IsEditable));
        _ = CommitAsync();
    }
}
