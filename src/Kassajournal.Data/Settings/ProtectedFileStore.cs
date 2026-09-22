using System.Security.Cryptography;
using System.Text;

namespace Kassajournal.Data.Settings;

/// <summary>
/// Verschlüsselt beliebige Klartext-Strings mit der Windows Data Protection API (DPAPI),
/// gebunden an das aktuell angemeldete Windows-Benutzerkonto (Scope = CurrentUser).
/// Damit kann die Datei auf der Festplatte niemand lesen außer genau diesem Windows-Konto
/// auf genau diesem Rechner – selbst ein Kopieren der Datei auf einen anderen PC nützt nichts.
/// Wird verwendet für: Datenbank-Zugangsdaten (Einstellungen) und den lokalen SQLite-Verschlüsselungsschlüssel.
/// </summary>
public static class ProtectedFileStore
{
    private static readonly byte[] Entropy = "Kassajournal.v1"u8.ToArray();

    public static void WriteProtected(string filePath, string plainText)
    {
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var protectedBytes = ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(filePath, protectedBytes);
    }

    public static string? ReadProtected(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        var protectedBytes = File.ReadAllBytes(filePath);
        var plainBytes = ProtectedData.Unprotect(protectedBytes, Entropy, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(plainBytes);
    }
}
