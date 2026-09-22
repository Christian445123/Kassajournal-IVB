using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Kassajournal.Core.Services;

namespace Kassajournal.App.ViewModels;

/// <summary>
/// Die Startseite der App: zeigt den heutigen Tag zum direkten Eintippen - einmal für
/// Kassajournal, einmal für IVB - genau das, was beim täglichen Öffnen des Programms
/// gebraucht wird, ohne durch Monats-Reiter navigieren zu müssen. Lädt bei jedem Aufruf
/// frisch, damit sie nie veraltete Daten zeigt (z. B. nachdem in der Monatsübersicht etwas
/// geändert wurde).
/// </summary>
public partial class HeuteViewModel(Func<DayEntryViewModel> kassaDayFactory, Func<IvbDayRowViewModel> ivbRowFactory) : ObservableObject
{
    [ObservableProperty]
    private DayEntryViewModel? _kassaHeute;

    [ObservableProperty]
    private IvbDayRowViewModel? _ivbHeute;

    [ObservableProperty]
    private bool _isLoading;

    public string HeutigesDatum => DateOnly.FromDateTime(DateTime.Today)
        .ToString("dddd, dd. MMMM yyyy", CultureInfo.GetCultureInfo("de-AT"));

    public async Task RefreshAsync()
    {
        IsLoading = true;
        try
        {
            var heute = DateOnly.FromDateTime(DateTime.Today);

            if (KassaHeute is not null)
            {
                KassaHeute.DateChanged -= OnKassaDateChanged;
            }

            var kassaTag = kassaDayFactory();
            await kassaTag.LoadAsync(heute);

            if (kassaTag.IsNewDay && AustrianHolidays.IsHoliday(heute))
            {
                // Wie im Monats-Reiter: neu angelegte Tage automatisch als Feiertag erkennen.
                await kassaTag.ToggleFeiertagAsync(true);
            }

            kassaTag.DateChanged += OnKassaDateChanged;
            KassaHeute = kassaTag;

            var ivbTag = ivbRowFactory();
            await ivbTag.LoadAsync(heute);
            IvbHeute = ivbTag;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnKassaDateChanged(object? sender, EventArgs e)
    {
        // Der heutige Tag wurde auf ein anderes Datum umgebucht - Seite neu laden, damit hier
        // wieder ein frischer (leerer) heutiger Tag bereitsteht.
        _ = RefreshAsync();
    }
}
