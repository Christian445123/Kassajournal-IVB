using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kassajournal.Core.Services;

namespace Kassajournal.App.ViewModels;

/// <summary>Der komplette IVB-Bereich: 12 Monats-Reiter + Auswertung, genau wie "IVB Vorlage.xlsx".</summary>
public partial class IvbModuleViewModel : ObservableObject
{
    public IvbModuleViewModel(IIvbRepository repository, Func<IvbDayRowViewModel> rowFactory, IvbAuswertungViewModel auswertung)
    {
        Auswertung = auswertung;
        Monate = Enumerable.Range(1, 12)
            .Select(month => new IvbMonthViewModel(repository, rowFactory, month))
            .ToList();

        Auswertung.MonatAusgewaehlt += monat => _ = SelectMonthAsync(monat);
    }

    public IReadOnlyList<IvbMonthViewModel> Monate { get; }

    public IvbAuswertungViewModel Auswertung { get; }

    [ObservableProperty]
    private int _jahr = DateTime.Today.Year;

    /// <summary>0-11 = Monats-Reiter, 12 = Auswertung.</summary>
    [ObservableProperty]
    private int _selectedTabIndex;

    public async Task InitializeAsync()
    {
        var today = DateTime.Today;
        Jahr = today.Year;
        SelectedTabIndex = today.Month - 1;
        await Monate[today.Month - 1].EnsureLoadedAsync(Jahr);
    }

    /// <summary>Siehe <see cref="KassaModuleViewModel.RefreshHeutigenMonatAsync"/> - dasselbe Prinzip für IVB.</summary>
    public async Task RefreshHeutigenMonatAsync()
    {
        var heutigerMonat = Monate[DateTime.Today.Month - 1];
        if (heutigerMonat.IsLoaded && heutigerMonat.Year == Jahr)
        {
            await heutigerMonat.ReloadAsync();
        }
    }

    public async Task OnTabSelectedAsync(int index)
    {
        SelectedTabIndex = index;
        if (index >= 0 && index < 12)
        {
            await Monate[index].EnsureLoadedAsync(Jahr);
        }
        else if (index == 12)
        {
            await Auswertung.LoadAsync(Jahr);
        }
    }

    private async Task SelectMonthAsync(int monat)
    {
        SelectedTabIndex = monat - 1;
        await Monate[monat - 1].EnsureLoadedAsync(Jahr);
    }

    [RelayCommand]
    private async Task VorJahrAsync()
    {
        Jahr--;
        await OnTabSelectedAsync(SelectedTabIndex);
    }

    [RelayCommand]
    private async Task NaechstesJahrAsync()
    {
        Jahr++;
        await OnTabSelectedAsync(SelectedTabIndex);
    }
}
