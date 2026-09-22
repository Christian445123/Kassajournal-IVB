namespace Kassajournal.Core.Models;

/// <summary>
/// Die zwei fachlichen Bereiche der App. Jeder Bereich hat eine eigene Datenbank
/// (kassajournal_db bzw. ivb_db) und eigene Zugangsdaten in den Einstellungen -
/// es bleibt aber technisch EIN Programm ("Kassajournal &amp; IVB").
/// </summary>
public enum AppModule
{
    Kassajournal = 0,
    Ivb = 1,
}

public static class AppModuleExtensions
{
    public static string DisplayName(this AppModule module) => module switch
    {
        AppModule.Kassajournal => "Kassajournal",
        AppModule.Ivb => "IVB",
        _ => module.ToString(),
    };

    /// <summary>Standard-Datenbankname je Bereich, wie vom Benutzer vorgegeben.</summary>
    public static string DefaultDatabaseName(this AppModule module) => module switch
    {
        AppModule.Kassajournal => "kassajournal_db",
        AppModule.Ivb => "ivb_db",
        _ => "app_db",
    };
}
