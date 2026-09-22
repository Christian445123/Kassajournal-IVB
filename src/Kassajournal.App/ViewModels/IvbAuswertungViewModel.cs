using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kassajournal.Core.Models;
using Kassajournal.Core.Services;

namespace Kassajournal.App.ViewModels;

/// <summary>Entspricht dem "Auswertung Monate"-Blatt der IVB-Vorlage.</summary>
public partial class IvbAuswertungViewModel(IIvbRepository repository) : ObservableObject
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
            var entries = await repository.GetEntriesForYearAsync(jahr);
            var overview = IvbCalculationService.CalculateYearOverview(jahr, entries);
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
