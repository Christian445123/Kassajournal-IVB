using CommunityToolkit.Mvvm.ComponentModel;
using Kassajournal.Core.Services;

namespace Kassajournal.App.ViewModels;

/// <summary>Der komplette IVB-Bereich: 12 Monats-Reiter + Auswertung, genau wie "IVB Vorlage.xlsx".</summary>
public partial class IvbModuleViewModel : ObservableObject
{
    public IvbModuleViewModel(IIvbRepository repository, Func<IvbDayRowViewModel> rowFactory, IvbAuswertungViewModel auswertung)
    {
        Auswertung = auswertung;
        Monate = Enumerable.Range(1, 12)
            .Select(_ => new IvbMonthViewModel(repository, rowFactory))
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
        await Monate[today.Month - 1].EnsureLoadedAsync(Jahr, today.Month);
    }

    public async Task OnTabSelectedAsync(int index)
    {
        SelectedTabIndex = index;
        if (index >= 0 && index < 12)
        {
            await Monate[index].EnsureLoadedAsync(Jahr, index + 1);
        }
        else if (index == 12)
        {
            await Auswertung.LoadAsync(Jahr);
        }
    }

    private async Task SelectMonthAsync(int monat)
    {
        SelectedTabIndex = monat - 1;
        await Monate[monat - 1].EnsureLoadedAsync(Jahr, monat);
    }
}
