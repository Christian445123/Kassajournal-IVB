using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kassajournal.Core.Models;
using Kassajournal.Core.Services;

namespace Kassajournal.App.ViewModels;

public partial class DayRowViewModel(DaySummary summary) : ObservableObject
{
    public DateOnly Date { get; } = summary.Date;

    public string DateDisplay { get; } = summary.Date.ToString("ddd, dd.MM.yyyy", CultureInfo.GetCultureInfo("de-AT"));

    public decimal SollSumme { get; } = summary.SollSumme;

    public decimal HabenSumme { get; } = summary.HabenSumme;

    public bool IstAusgeglichen { get; } = summary.IstAusgeglichen;
}

/// <summary>Übersicht über einen ganzen Monat: Tagesliste + Kopfzahlen, entspricht dem Monatsblatt der Excel-Vorlage.</summary>
public partial class MonthOverviewViewModel(IKassaRepository repository) : ObservableObject
{
    [ObservableProperty]
    private int _year = DateTime.Today.Year;

    [ObservableProperty]
    private int _month = DateTime.Today.Month;

    [ObservableProperty]
    private decimal _anfangssaldo;

    [ObservableProperty]
    private decimal _summeSoll;

    [ObservableProperty]
    private decimal _summeHaben;

    [ObservableProperty]
    private decimal _saldo;

    [ObservableProperty]
    private bool _isEditingAnfangssaldo;

    [ObservableProperty]
    private string _anfangssaldoText = "820,00";

    public ObservableCollection<DayRowViewModel> Tage { get; } = [];

    public string MonatsName => new DateOnly(Year, Month, 1).ToString("MMMM yyyy", CultureInfo.GetCultureInfo("de-AT"));

    public event Action<DateOnly>? TagAusgewaehlt;

    public async Task LoadAsync(int year, int month)
    {
        Year = year;
        Month = month;
        OnPropertyChanged(nameof(MonatsName));

        var settings = await repository.GetOrCreateMonthSettingsAsync(year, month);
        Anfangssaldo = settings.Anfangssaldo;
        AnfangssaldoText = settings.Anfangssaldo.ToString("N2");

        var entries = await repository.GetEntriesForMonthAsync(year, month);
        var summary = KassaCalculationService.CalculateMonthSummary(year, month, settings.Anfangssaldo, entries);
        SummeSoll = summary.SummeSoll;
        SummeHaben = summary.SummeHaben;
        Saldo = summary.Saldo;

        Tage.Clear();
        foreach (var date in entries.Select(e => e.Date).Distinct().OrderBy(d => d))
        {
            var daySummary = KassaCalculationService.CalculateDaySummary(date, entries);
            Tage.Add(new DayRowViewModel(daySummary));
        }
    }

    [RelayCommand]
    private void TagOeffnen(DayRowViewModel row) => TagAusgewaehlt?.Invoke(row.Date);

    [RelayCommand]
    private void AnfangssaldoBearbeiten() => IsEditingAnfangssaldo = true;

    [RelayCommand]
    private async Task AnfangssaldoSpeichernAsync()
    {
        var normalized = AnfangssaldoText.Replace(".", "").Replace(",", ".").Trim();
        if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) && parsed >= 0)
        {
            await repository.SaveMonthSettingsAsync(new MonthSettings { Year = Year, Month = Month, Anfangssaldo = parsed });
            await LoadAsync(Year, Month);
        }

        IsEditingAnfangssaldo = false;
    }
}
