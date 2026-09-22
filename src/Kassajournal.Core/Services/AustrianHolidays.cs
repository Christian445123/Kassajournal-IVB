namespace Kassajournal.Core.Services;

/// <summary>
/// Berechnet die gesetzlichen Feiertage in Österreich für ein gegebenes Jahr.
/// Wird verwendet, um einen Tag automatisch als "Feiertag" zu erkennen, ohne dass der
/// Benutzer das manuell ankreuzen muss (fixe Feiertage + bewegliche Feiertage, die sich
/// vom Ostersonntag ableiten).
/// </summary>
public static class AustrianHolidays
{
    private static readonly Dictionary<int, IReadOnlySet<DateOnly>> Cache = [];

    public static bool IsHoliday(DateOnly date) => GetHolidays(date.Year).Contains(date);

    public static IReadOnlySet<DateOnly> GetHolidays(int year)
    {
        if (Cache.TryGetValue(year, out var cached))
        {
            return cached;
        }

        var easterSunday = CalculateEasterSunday(year);

        var holidays = new HashSet<DateOnly>
        {
            new(year, 1, 1),               // Neujahr
            new(year, 1, 6),                // Heilige Drei Könige
            easterSunday.AddDays(1),        // Ostermontag
            new(year, 5, 1),                // Staatsfeiertag
            easterSunday.AddDays(39),       // Christi Himmelfahrt
            easterSunday.AddDays(50),       // Pfingstmontag
            easterSunday.AddDays(60),       // Fronleichnam
            new(year, 8, 15),               // Mariä Himmelfahrt
            new(year, 10, 26),              // Nationalfeiertag
            new(year, 11, 1),               // Allerheiligen
            new(year, 12, 8),               // Mariä Empfängnis
            new(year, 12, 25),              // Christtag
            new(year, 12, 26),              // Stefanitag
        };

        Cache[year] = holidays;
        return holidays;
    }

    /// <summary>Gauß'sche Osterformel für den gregorianischen Kalender (liefert den Ostersonntag).</summary>
    private static DateOnly CalculateEasterSunday(int year)
    {
        int a = year % 19;
        int b = year / 100;
        int c = year % 100;
        int d = b / 4;
        int e = b % 4;
        int f = (b + 8) / 25;
        int g = (b - f + 1) / 3;
        int h = (19 * a + b - d - g + 15) % 30;
        int i = c / 4;
        int k = c % 4;
        int l = (32 + 2 * e + 2 * i - h - k) % 7;
        int m = (a + 11 * h + 22 * l) / 451;
        int month = (h + l - 7 * m + 114) / 31;
        int day = ((h + l - 7 * m + 114) % 31) + 1;
        return new DateOnly(year, month, day);
    }
}
