using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kassajournal.Core.Services;

namespace Kassajournal.App.ViewModels;

public partial class KassaMonatsZeileViewModel(int monat, string name) : ObservableObject
{
    public int Monat { get; } = monat;

    public string Name { get; } = name;

    [ObservableProperty]
    private decimal _summe;
}

/// <summary>Entspricht dem "Auswertung"-Blatt der Excel-Vorlage: Summe je Monat + Gesamtumsatz für ein Jahr.</summary>
public partial class KassaAuswertungViewModel(IKassaRepository repository) : ObservableObject
{
    private static readonly string[] MonatsNamen =
    [
        "Jänner", "Februar", "März", "April", "Mai", "Juni",
        "Juli", "August", "September", "Oktober", "November", "Dezember",
    ];

    [ObservableProperty]
    private int _jahr = DateTime.Today.Year;

    [ObservableProperty]
    private decimal _gesamtumsatz;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<KassaMonatsZeileViewModel> Monate { get; } =
        new(Enumerable.Range(1, 12).Select(m => new KassaMonatsZeileViewModel(m, MonatsNamen[m - 1])));

    public event Action<int>? MonatAusgewaehlt;

    public async Task LoadAsync(int jahr)
    {
        Jahr = jahr;
        IsLoading = true;
        try
        {
            var alleEintraege = new List<Core.Models.KassaEntry>();
            for (int m = 1; m <= 12; m++)
            {
                alleEintraege.AddRange(await repository.GetEntriesForMonthAsync(jahr, m));
            }

            var overview = KassaCalculationService.CalculateYearOverview(jahr, alleEintraege);
            foreach (var (monat, summe) in overview)
            {
                Monate.Single(m => m.Monat == monat).Summe = summe;
            }

            Gesamtumsatz = overview.Sum(o => o.Summe);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private Task VorJahrAsync() => LoadAsync(Jahr - 1);

    [RelayCommand]
    private Task NaechstesJahrAsync() => LoadAsync(Jahr + 1);

    [RelayCommand]
    private void MonatOeffnen(KassaMonatsZeileViewModel zeile) => MonatAusgewaehlt?.Invoke(zeile.Monat);
}
