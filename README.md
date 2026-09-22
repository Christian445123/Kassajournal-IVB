# Kassajournal & IVB

Ein Windows-Programm, das die beiden Excel-Vorlagen **„Kassajournal Vorlage.xlsx"** und
**„IVB Vorlage.xlsx"** ersetzt: gleiche Struktur und Berechnungslogik wie die Excel-Dateien
(alle Monats-Reiter, Auswertung, Vergleich), aber als „Rechenprogramm" mit sofortiger
Prüfung, ob ein Tag passt oder wo etwas fehlt.

- **Ein** Programm für zwei getrennte Bereiche: **Kassajournal** und **IVB**
- Beim Öffnen steht in beiden Bereichen immer der **heutige Tag** ganz oben bereit
- Zahlen einfach eintippen – das Programm rechnet live mit und zeigt **✓ passt** oder
  **⚠ fehlt X €** an
- Alle Daten werden **lokal verschlüsselt** gespeichert (funktioniert auch offline) **und**
  zusätzlich mit einer **zentralen Datenbank** synchronisiert, sobald eine Verbindung besteht
  – so liegt immer ein Backup vor
- Automatische Updates über GitHub Releases

---

## Inhalt

1. [Installation](#installation)
2. [Erste Einrichtung: Datenbank-Zugangsdaten](#erste-einrichtung-datenbank-zugangsdaten)
3. [Bedienung – Kassajournal](#bedienung--kassajournal)
4. [Bedienung – IVB](#bedienung--ivb)
5. [Daten, Sicherheit & Verschlüsselung](#daten-sicherheit--verschlüsselung)
6. [Automatische Updates](#automatische-updates)
7. [Für Entwickler: Projekt bauen & veröffentlichen](#für-entwickler-projekt-bauen--veröffentlichen)
8. [Projektstruktur](#projektstruktur)

---

## Installation

1. Auf der [Releases-Seite](https://github.com/Christian445123/Kassajournal-IVB/releases) die
   neueste Version herunterladen (Datei `Kassajournal-IVB-Setup.msi`).
2. Datei doppelklicken und der Installation folgen.
3. **Windows SmartScreen-Warnung:** Da die MSI aktuell nicht mit einem kostenpflichtigen
   Code-Signing-Zertifikat signiert ist, zeigt Windows beim ersten Ausführen eventuell eine
   Warnung. Auf **"Weitere Informationen"** und dann **"Trotzdem ausführen"** klicken.
4. Nach der Installation liegt eine Verknüpfung auf dem Desktop und im Startmenü unter
   **„Kassajournal & IVB"**.

Systemvoraussetzung: Windows 10/11 (64-Bit). Es muss nichts weiter installiert werden – die App
bringt die benötigte .NET-Laufzeit selbst mit.

## Erste Einrichtung: Datenbank-Zugangsdaten

Kassajournal und IVB verwenden **zwei komplett getrennte Datenbanken**:

| Bereich | Datenbankname |
|---|---|
| Kassajournal | `kassajournal_db` |
| IVB | `ivb_db` |

So richtest du die Verbindung ein:

1. App öffnen → oben rechts auf **⚙️ Einstellungen** klicken.
2. Für **jeden der beiden Bereiche** einzeln ausfüllen:
   - **Datenbank-Typ**: MySQL, PostgreSQL oder SQL Server
   - **Server/Host**, **Port**, **Datenbankname**, **Benutzername**, **Passwort**
   - „Verschlüsselte Verbindung erzwingen (TLS/SSL)" sollte aktiviert bleiben (Standard)
3. Auf **„Verbindung testen"** klicken – erst wenn die Verbindung erfolgreich ist, auf
   **„Speichern"** klicken.
4. Fenster schließen. Ab jetzt synchronisiert die App im Hintergrund automatisch.

> Die Zugangsdaten werden **nur einmal eingegeben** und danach verschlüsselt auf diesem PC
> gespeichert (siehe [Sicherheit](#daten-sicherheit--verschlüsselung)). Ohne konfigurierte
> Datenbank funktioniert die App trotzdem uneingeschränkt weiter – es wird dann nur lokal
> gespeichert, bis eine Verbindung eingerichtet wird.

## Bedienung – Kassajournal

Der Aufbau folgt exakt der Excel-Vorlage: ein Reiter je Monat (Jänner–Dezember) plus
**Auswertung** (Jahresübersicht) und **Vergleich** (zwei Jahre gegenüberstellen).

Jeder Tag zeigt sechs Felder nebeneinander, genau wie in der Excel-Tabelle:

| Feld | Bedeutung |
|---|---|
| Tageslosung | Tageseinnahmen (Soll) |
| Auszahlungen | Bar bezahlte Ausgaben |
| Einzahlung Bank | Auf das Bankkonto eingezahltes Bargeld |
| Bankomat/Kredit | Unbare Umsätze über Bankomat-/Kreditkarte |
| Bankomat/Automat | Unbare Umsätze über Automat/Terminal |
| Geldentnahme | Private Bargeldentnahme |

**Wichtigste Regel:** Die Tageslosung muss sich exakt auf die übrigen fünf Felder aufteilen.
Passt das nicht zusammen, zeigt die App rechts **„⚠ fehlt"** mit dem fehlenden Betrag; stimmt
alles, erscheint **„✓ passt"**.

- Beträge einfach eintippen und das Feld verlassen (Tab oder woanders hinklicken) – wird
  **sofort automatisch gespeichert**, kein extra „Speichern"-Knopf nötig.
- **Feiertag**-Häkchen an einem Tag setzen, wenn geschlossen war – dann entfällt die
  Prüfung für diesen Tag.
- Es gibt nur Montag–Samstag (kein Sonntag) – wie im echten Geschäftsbetrieb.
- **„+ Neuer Tag"** oben im Monats-Reiter fügt den nächsten Tag hinzu.
- **Anfangssaldo** des Monats (Standard 820 €) oben anklicken, um ihn anzupassen.
- **Auswertung**: zeigt alle 12 Monate des gewählten Jahres + Gesamtumsatz; ein Monat
  anklicken springt direkt zu diesem Reiter.
- **Vergleich**: zwei Jahre eintragen und „Vergleichen" klicken – zeigt je Monat die
  Differenz und Veränderung in %.

## Bedienung – IVB

Gleiches Prinzip, andere Struktur (wie „IVB Vorlage.xlsx"): pro Monat ein Wochenraster
Montag–Samstag mit dem **täglichen Umsatz**.

- Betrag eintippen → wird beim Verlassen des Felds automatisch gespeichert.
- **Feiertag**-Häkchen für geschlossene Tage (kein Umsatz an diesem Tag).
- Wochensumme und Monatssumme werden automatisch berechnet und angezeigt.
- **Auswertung**: Jahresübersicht aller 12 Monate + Gesamtumsatz.

## Daten, Sicherheit & Verschlüsselung

- **Lokale Kopie zuerst:** Jede Eingabe wird sofort in eine **lokal verschlüsselte
  SQLite-Datenbank** geschrieben (Schlüssel wird pro Windows-Benutzerkonto erzeugt und mit der
  Windows Data Protection API – DPAPI – geschützt; niemand außer diesem Windows-Konto auf
  diesem PC kann die Datei lesen). Die App funktioniert dadurch auch **komplett offline**.
- **Zentrale Datenbank als Backup:** Sobald eine Verbindung besteht, gleicht die App die
  Daten automatisch mit der zentralen Datenbank ab – so liegt immer zusätzlich ein Backup
  außerhalb dieses einen PCs.
- **Verschlüsselte Übertragung:** Die Verbindung zur zentralen Datenbank läuft ausschließlich
  über **TLS/SSL**. Ohne verschlüsselte Verbindung wird nicht synchronisiert – so kann niemand
  im Netzwerk die Kassadaten mitlesen.
- **Zugangsdaten:** Die Datenbank-Zugangsdaten selbst werden nie im Klartext gespeichert,
  sondern ebenfalls DPAPI-verschlüsselt, gebunden an das Windows-Benutzerkonto.
- **Getrennte Datenbanken:** Kassajournal- und IVB-Daten werden nie vermischt – jeder Bereich
  hat seine eigene Datenbank und eigene Zugangsdaten.

## Automatische Updates

Beim Start prüft die App automatisch, ob auf GitHub eine neuere Version veröffentlicht wurde.
Ist das der Fall, erscheint oben ein Hinweis mit einem **„Aktualisieren"**-Button:

1. Klick auf „Aktualisieren" lädt die neue Setup-Datei herunter.
2. Der Windows-Installer startet automatisch (SmartScreen-Hinweis ggf. wieder bestätigen).
3. Die App wird beendet, die neue Version installiert.

Ein neues Release entsteht automatisch, sobald im Repository ein Versions-Tag (z. B. `v1.1.0`)
gepusht wird – siehe [`.github/workflows/release.yml`](.github/workflows/release.yml).

## Für Entwickler: Projekt bauen & veröffentlichen

Voraussetzung: [.NET 10 SDK](https://dotnet.microsoft.com/download) und (für den Installer)
das WiX-Toolset:

```powershell
dotnet tool install --global wix --version 5.0.2
```

**App im Debug-Modus starten:**

```powershell
dotnet run --project src/Kassajournal.App/Kassajournal.App.csproj
```

**Tests ausführen:**

```powershell
dotnet test src/Kassajournal.Core.Tests/Kassajournal.Core.Tests.csproj
```

**Eine neue Version veröffentlichen** (löst automatisch Build + MSI + GitHub Release aus):

```powershell
git tag v1.1.0
git push origin v1.1.0
```

**MSI lokal von Hand bauen** (ohne GitHub Actions, z. B. zum Testen):

```powershell
dotnet publish src/Kassajournal.App/Kassajournal.App.csproj -c Release -r win-x64 --self-contained true -o publish/win-x64
dotnet build installer/Kassajournal.Installer.wixproj -p:ProductVersion=1.1.0 -o installer/bin
```

Das fertige Setup liegt danach unter `installer/bin/Kassajournal-IVB-Setup.msi`.

## Projektstruktur

```
src/
  Kassajournal.Core/        Fachliche Modelle & reine Berechnungslogik (keine UI, keine DB)
  Kassajournal.Core.Tests/  Unit-Tests gegen echte Excel-Werte
  Kassajournal.Data/        Lokale verschlüsselte SQLite-DB, Zentraldatenbank-Anbindung, Sync
  Kassajournal.Update/      Prüft GitHub Releases, lädt/startet Updates
  Kassajournal.App/         WPF-Oberfläche (MVVM)
installer/                  WiX-Setup-Projekt (baut die .msi)
.github/workflows/          GitHub-Actions-Pipeline für automatische Releases
```

Die Berechnungslogik in `Kassajournal.Core` ist 1:1 aus den Formeln der beiden
Original-Excel-Vorlagen nachgebaut und mit echten historischen Werten aus
`Kassajournal 2025.xlsx` bzw. `IVB 2025.xlsx` gegengetestet (siehe
`Kassajournal.Core.Tests`).
