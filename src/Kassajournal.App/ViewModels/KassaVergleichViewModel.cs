using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kassajournal.Core.Models;
using Kassajournal.Core.Services;

namespace Kassajournal.App.ViewModels;

public partial class KassaVergleichZeileViewModel(string monatsName) : ObservableObject
{
    public string MonatsName { get; } = monatsName;

    [ObservableProperty]
    private decimal _jahrA;

    [ObservableProperty]
    private decimal _jahrB;

    public decimal Unterschied => JahrB - JahrA;

    public decimal? ProzentVeraenderung => JahrA == 0 ? null : Math.Round((JahrB - JahrA) / JahrA * 100m, 1);

    partial void OnJahrAChanged(decimal value)
    {
        OnPropertyChanged(nameof(Unterschied));
        OnPropertyChanged(nameof(ProzentVeraenderung));
    }

    partial void OnJahrBChanged(decimal value)
    {
        OnPropertyChanged(nameof(Unterschied));
        OnPropertyChanged(nameof(ProzentVeraenderung));
    }
}

/// <summary>Entspricht dem "Vergleich 202x"-Blatt: zwei Jahre nebeneinander, je Monat und gesamt.</summary>
public partial class KassaVergleichViewModel(IKassaRepository repository) : ObservableObject
{
    private static readonly string[] MonatsNamen =
    [
        "Jänner", "Februar", "März", "April", "Mai", "Juni",
        "Juli", "August", "September", "Oktober", "November", "Dezember",
    ];

    [ObservableProperty]
    private int _jahrA = DateTime.Today.Year - 1;

    [ObservableProperty]
    private int _jahrB = DateTime.Today.Year;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<KassaVergleichZeileViewModel> Zeilen { get; } =
        new(MonatsNamen.Select(n => new KassaVergleichZeileViewModel(n)));

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var entriesA = await LadeJahrAsync(JahrA);
            var entriesB = await LadeJahrAsync(JahrB);

            var overviewA = KassaCalculationService.CalculateYearOverview(JahrA, entriesA);
            var overviewB = KassaCalculationService.CalculateYearOverview(JahrB, entriesB);

            for (int i = 0; i < 12; i++)
            {
                Zeilen[i].JahrA = overviewA[i].Summe;
                Zeilen[i].JahrB = overviewB[i].Summe;
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task<List<KassaEntry>> LadeJahrAsync(int jahr)
    {
        var result = new List<KassaEntry>();
        for (int m = 1; m <= 12; m++)
        {
            result.AddRange(await repository.GetEntriesForMonthAsync(jahr, m));
        }

        return result;
    }

    [RelayCommand]
    private Task NeuLadenAsync() => LoadAsync();
}
