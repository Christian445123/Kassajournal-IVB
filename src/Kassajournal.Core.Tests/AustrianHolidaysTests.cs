using Kassajournal.Core.Services;

namespace Kassajournal.Core.Tests;

public class AustrianHolidaysTests
{
    [Theory]
    // Fixe Feiertage 2025/2026
    [InlineData(2025, 1, 1)]   // Neujahr
    [InlineData(2025, 1, 6)]   // Heilige Drei Könige
    [InlineData(2025, 5, 1)]   // Staatsfeiertag
    [InlineData(2025, 8, 15)]  // Mariä Himmelfahrt
    [InlineData(2025, 10, 26)] // Nationalfeiertag
    [InlineData(2025, 11, 1)]  // Allerheiligen
    [InlineData(2025, 12, 8)]  // Mariä Empfängnis
    [InlineData(2025, 12, 25)] // Christtag
    [InlineData(2025, 12, 26)] // Stefanitag
    // Bewegliche Feiertage 2025 (offiziell bestätigte Daten)
    [InlineData(2025, 4, 21)]  // Ostermontag
    [InlineData(2025, 5, 29)]  // Christi Himmelfahrt
    [InlineData(2025, 6, 9)]   // Pfingstmontag
    [InlineData(2025, 6, 19)]  // Fronleichnam
    // Bewegliche Feiertage 2026 (Ostersonntag 5.4.2026)
    [InlineData(2026, 4, 6)]   // Ostermontag
    [InlineData(2026, 5, 14)]  // Christi Himmelfahrt
    [InlineData(2026, 5, 25)]  // Pfingstmontag
    [InlineData(2026, 6, 4)]   // Fronleichnam
    public void IsHoliday_RecognizesKnownHolidays(int year, int month, int day)
    {
        Assert.True(AustrianHolidays.IsHoliday(new DateOnly(year, month, day)));
    }

    [Theory]
    [InlineData(2025, 1, 2)]
    [InlineData(2025, 6, 20)]
    [InlineData(2025, 12, 24)] // Heiliger Abend ist kein gesetzlicher Feiertag
    [InlineData(2025, 4, 20)]  // Ostersonntag selbst ist kein Feiertag (nur Ostermontag)
    public void IsHoliday_ReturnsFalseForNonHolidays(int year, int month, int day)
    {
        Assert.False(AustrianHolidays.IsHoliday(new DateOnly(year, month, day)));
    }

    [Fact]
    public void GetHolidays_ReturnsThirteenHolidaysPerYear()
    {
        Assert.Equal(13, AustrianHolidays.GetHolidays(2025).Count);
    }
}
