using CommunityToolkit.Mvvm.ComponentModel;
using Kassajournal.Core.Services;

namespace Kassajournal.App.ViewModels;

/// <summary>
/// Der komplette Kassajournal-Bereich: 12 Monats-Reiter (Jänner-Dezember) + Auswertung + Vergleich,
/// genau wie die Excel-Vorlage "Kassajournal Vorlage.xlsx".
/// </summary>
public partial class KassaModuleViewModel : ObservableObject
{
    public KassaModuleViewModel(IKassaRepository repository, Func<DayEntryViewModel> dayFactory, KassaAuswertungViewModel auswertung, KassaVergleichViewModel vergleich)
    {
        Auswertung = auswertung;
        Vergleich = vergleich;
        Monate = Enumerable.Range(1, 12)
            .Select(_ => new KassaMonthViewModel(repository, dayFactory))
            .ToList();

        Auswertung.MonatAusgewaehlt += monat => _ = SelectMonthAsync(monat);
    }

    public IReadOnlyList<KassaMonthViewModel> Monate { get; }

    public KassaAuswertungViewModel Auswertung { get; }

    public KassaVergleichViewModel Vergleich { get; }

    [ObservableProperty]
    private int _jahr = DateTime.Today.Year;

    /// <summary>0-11 = Monats-Reiter, 12 = Auswertung, 13 = Vergleich.</summary>
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
        else if (index == 13)
        {
            await Vergleich.LoadAsync();
        }
    }

    private async Task SelectMonthAsync(int monat)
    {
        SelectedTabIndex = monat - 1;
        await Monate[monat - 1].EnsureLoadedAsync(Jahr, monat);
    }
}
