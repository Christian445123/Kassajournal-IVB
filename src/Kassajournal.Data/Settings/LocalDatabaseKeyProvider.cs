using System.Security.Cryptography;
using Kassajournal.Core;

namespace Kassajournal.Data.Settings;

/// <summary>
/// Erzeugt beim allerersten Start einen zufälligen Schlüssel für die lokale, verschlüsselte
/// SQLite-Datenbank (SQLCipher) und speichert ihn DPAPI-geschützt. Bei jedem weiteren Start
/// wird derselbe Schlüssel wieder entschlüsselt, ohne dass der Benutzer ein Passwort eingeben muss.
/// </summary>
public static class LocalDatabaseKeyProvider
{
    public static string GetOrCreateKey()
    {
        var existing = ProtectedFileStore.ReadProtected(AppPaths.LocalDatabaseKeyFile);
        if (!string.IsNullOrEmpty(existing))
        {
            return existing;
        }

        var keyBytes = RandomNumberGenerator.GetBytes(32);
        var key = Convert.ToBase64String(keyBytes);
        ProtectedFileStore.WriteProtected(AppPaths.LocalDatabaseKeyFile, key);
        return key;
    }
}
