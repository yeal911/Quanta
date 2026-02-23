# Quanta - Windows Schnellstarter

<p align="center">
  <a href="README.md">中文</a> | <a href="README_EN.md">English</a> | <a href="README_ES.md">Español</a> | <a href="README_JA.md">日本語</a> | <a href="README_KO.md">한국어</a> | <a href="README_FR.md">Français</a> | <b>Deutsch</b> | <a href="README_PT.md">Português</a> | <a href="README_RU.md">Русский</a> | <a href="README_IT.md">Italiano</a> | <a href="README_AR.md">العربية</a>
</p>

<p align="center">
  <img src="Resources\imgs\quanta.ico" width="64" alt="Quanta"/>
</p>

<p align="center">
  Ein leichtgewichtiger Windows-Schnellstarter. Aufruf mit globaler Tastenkombination, Fuzzy-Suche, sofortige Ausführung.
</p>

---

## Funktionen

### Kernfunktionen

- **Globale Tastenkombination** - Standard `Alt+R`, in den Einstellungen anpassbar
- **Fuzzy-Suche** - Intelligente Übereinstimmung nach Schlüsselwort / Name / Beschreibung, Unterstützung für Präfix-Fuzzy (geben Sie `rec` ein, um `record` zu finden)
- **Gruppierte Ergebnisse** - Ergebnisse nach Kategorie geordnet, nach Übereinstimmungspunktzahl innerhalb jeder Gruppe sortiert
- **Benutzerdefinierte Befehle** - Unterstützt Url, Program, Shell, Directory, Calculator Typen
- **Parametermodus** - Drücken Sie `Tab` zur Parametereingabe (z.B. `g > rust` für Google-Suche)
- **Strg+Zahl** - `Strg+1~9` um das N-te Ergebnis sofort auszuführen
- **Automatisch Ausblenden** - Blendet sich automatisch aus, wenn Sie woanders klicken

---

## Integrierte Befehle

Alle integrierten Befehle unterstützen Fuzzy-Suche.

### Kommandozeilentools

| Schlüsselwort | Beschreibung |
|--------------|--------------|
| `cmd` | Eingabeaufforderung öffnen |
| `powershell` | PowerShell öffnen |

### Apps

| Schlüsselwort | Beschreibung |
|--------------|--------------|
| `notepad` | Editor |
| `calc` | Windows Rechner |
| `mspaint` | Paint |
| `explorer` | Datei-Explorer |
| `winrecord` | Windows Aufnahmefunktion |

### Systemverwaltung

| Schlüsselwort | Beschreibung |
|--------------|--------------|
| `taskmgr` | Task-Manager |
| `devmgmt` | Geräte-Manager |
| `services` | Dienste |
| `regedit` | Registrierungs-Editor |
| `control` | Systemsteuerung |
| `emptybin` | Papierkorb leeren |

### Netzwerkdiagnose

| Schlüsselwort | Beschreibung |
|--------------|--------------|
| `ipconfig` | IP-Konfiguration anzeigen |
| `ping` | Ping (in PowerShell ausführen) |
| `tracert` | Routenverfolgung |
| `nslookup` | DNS-Abfrage |
| `netstat` | Netzwerkverbindungsstatus |

### Energieverwaltung

| Schlüsselwort | Beschreibung |
|--------------|--------------|
| `lock` | Bildschirm sperren |
| `shutdown` | In 10 Sekunden herunterfahren |
| `restart` | In 10 Sekunden neu starten |
| `sleep` | In Ruhezustand versetzen |

### Besondere Funktionen

| Schlüsselwort | Beschreibung |
|--------------|--------------|
| `record` | Quanta Aufnahmefunktion starten |
| `clip` | Zwischenablage-Verlauf durchsuchen |

### Quanta Systembefehle

| Schlüsselwort | Beschreibung |
|--------------|--------------|
| `setting` | Einstellungen öffnen |
| `about` | Über das Programm |
| `exit` | Beenden |
| `english` | Oberfläche auf Englisch umstellen |
| `chinese` | Oberfläche auf Chinesisch umstellen |
| `spanish` | Oberfläche auf Spanisch umstellen |

---

## Intelligente Eingabe

Keine Befehlsauswahl erforderlich. Geben Sie direkt in die Suchleiste ein, Ergebnisse werden in Echtzeit erkannt und angezeigt.

### Mathematische Berechnungen

Geben Sie direkt mathematische Ausdrücke ein:

```
2+2*3         → 8
(100-32)/1.8  → 37.778
2^10          → 1024
15%4          → 3
```

Unterstützte Operatoren: `+ - * / % ^`, Klammern und Dezimalzahlen unterstützt.

### Einheitenumrechnung

Format: `Wert QuellEinheit to ZielEinheit`

```
100 km to miles    → 62.137 miles
100 c to f         → 212 °F
30 f to c          → -1.111 °C
1 kg to lbs        → 2.205 lbs
500 g to oz        → 17.637 oz
1 gallon to l      → 3.785 l
1 acre to m2       → 4046.856 m²
```

Unterstützte Kategorien: Länge, Gewicht, Temperatur, Fläche, Volumen

### Währungsumrechnung

Format: `Wert QuellWährung to ZielWährung` (API Key in den Einstellungen erforderlich)

```
100 usd to cny     → ¥736.50 CNY
1000 cny to jpy    → ¥20,500 JPY
50 eur to gbp      → £42.30 GBP
```

Unterstützung für über 40 Währungen. Wechselkursdaten lokal zwischengespeichert (standardmäßig 1 Stunde).

### Farbkonvertierung

HEX, RGB, HSL Konvertierung unterstützt:

```
#E67E22            → rgb(230,126,34)  hsl(28,79%,52%)
rgb(230,126,34)    → #E67E22  hsl(28,79%,52%)
hsl(28,79,52%)     → #E67E22  rgb(230,126,34)
255,165,0          → #FFA500  hsl(38,100%,50%)
```

### Textwerkzeuge

| Präfix | Funktion | Beispiel |
|--------|----------|----------|
| `base64 Text` | Base64-Kodierung | `base64 hello` → `aGVsbG8=` |
| `base64d Text` | Base64-Dekodierung | `base64d aGVsbG8=` → `hello` |
| `md5 Text` | MD5-Hash | `md5 hello` → `5d41402a...` |
| `sha256 Text` | SHA-256-Hash | `sha256 hello` → `2cf24dba...` |
| `url Text` | URL-Kodierung/Dekodierung | `url hello world` → `hello%20world` |
| `json JSONZeichenkette` | JSON-Formatierung | `json {"a":1}` → formatierte Ausgabe |

### Google-Suche

Format: `g Schlüsselwort`

```
g rust programming  → Google-Suche im Browser öffnen
```

### PowerShell-Ausführung

Präfix `>` für direkte PowerShell-Befehlsausführung:

```
> Get-Process      → Alle Prozesse anzeigen
> dir C:\          → C:\-Dateien auflisten
```

### QR-Code-Generierung

Wenn die Zeichenanzahl den Schwellenwert überschreitet (standardmäßig 20), wird automatisch ein QR-Code generiert und in die Zwischenablage kopiert:

```
https://github.com/yeal911/Quanta   → Automatische QR-Code-Generierung
```

---

## Aufnahmefunktion

Geben Sie `record` ein, um das Aufnahmepanel anzuzeigen:

- **Audioquelle**: Mikrofon / Lautsprecher (Systemaudio) / Mikrofon + Lautsprecher
- **Format**: m4a (AAC) / mp3
- **Bitrate**: 32 / 64 / 96 / 128 / 160 kbps
- **Kanäle**: Mono / Stereo
- **Bedienung**: Start → Pause/Fortsetzen → Stopp, im angegebenen Verzeichnis gespeichert
- **Rechtsklick auf Parameter** zum Umschalten

---

## Zwischenablageverlauf

Geben Sie `clip` oder `clip Schlüsselwort` ein, um den Zwischenablageverlauf zu durchsuchen:

- Automatische Aufzeichnung von kopiertem Text (maximal 50 Einträge)
- Schlüsselwortfilterung unterstützt, zeigt die neuesten 20 an
- Klicken auf ein Ergebnis kopiert in die Zwischenablage
- Daten dauerhaft gespeichert

---

## Oberfläche und Benutzererlebnis

- **Hell/Dunkel-Thema** - In den Einstellungen umschaltbar, automatisch gespeichert
- **Mehrsprachig** - 中文 / English / Español / 日本語 / 한국어 / Français / Deutsch / Português / Русский / Italiano / العربية, sofortige Umschaltung
- **Systemleiste** - In der Taskleiste, Rechtsklick-Menü für schnelle Aktionen
- **Autostart** - Ein-Klick-Aktivierung in den Einstellungen
- **Toast-Benachrichtigungen** - Erfolg/Fehler von Operationen sofort zurückgemeldet

---

## Schnellstart

### Systemanforderungen

- Windows 10 / 11 (x64)
- [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

### Ausführung

```bash
dotnet run
```

### Einzelne Datei veröffentlichen

```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

---

## Tastenkombinationen

| Tastenkombination | Funktion |
|-------------------|----------|
| `Alt+R` | Hauptfenster anzeigen/ausblenden |
| `Enter` | Ausgewählten Befehl ausführen |
| `Tab` | In Parametermodus wechseln |
| `Esc` | Parametermodus beenden/Fenster ausblenden |
| `↑` / `↓` | Auswahl nach oben/unten bewegen |
| `Strg+1~9` | N-tes Ergebnis sofort ausführen |
| `Backspace` | Im Parametermodus zur normalen Suche zurückkehren |

---

## Befehlsverwaltung

In den Einstellungen (`setting` Befehl oder Rechtsklick auf Taskleiste), Tab "Befehlsverwaltung":

- Schlüsselwort, Name, Typ und Pfad direkt in der Tabelle bearbeiten
- JSON-Import/Export unterstützt, praktisch für Sicherung und Migration

### Befehlstypen

| Typ | Beschreibung | Beispiel |
|-----|--------------|----------|
| `Url` | Im Browser öffnen (`{param}` unterstützt) | `https://google.com/search?q={param}` |
| `Program` | Programm starten | `notepad.exe` |
| `Shell` | Befehl in cmd ausführen | `ping {param}` |
| `Directory` | Ordner im Explorer öffnen | `C:\Users\Public` |
| `Calculator` | Ausdruck berechnen | `{param}` |

---

## Einstellungen

| Einstellung | Beschreibung |
|-------------|--------------|
| Tastenkombination | Globale Tastenkombination anpassen (Standard `Alt+R`) |
| Thema | Light / Dark |
| Sprache | 中文 / English / Español / 日本語 / 한국어 / Français / Deutsch / Português / Русский / Italiano / العربية |
| Autostart | Beim Windows-Start ausführen |
| Anzeige Max | Ergebnislimit |
| Langer Text zu QR | Schwellenwert für QR-Code-Generierung |
| Wechselkurs-API-Key | API-Key für exchangerate-api.com |
| Wechselkurs-Cache-Zeit | Cache-Gültigkeitsdauer (Stunden), wird danach aktualisiert |
| Aufnahmequelle | Mikrofon / Lautsprecher / Beide |
| Aufnahmeformat | m4a / mp3 |
| Aufnahme-Bitrate | 32 / 64 / 96 / 128 / 160 kbps |
| Aufnahmekanäle | Mono / Stereo |
| Ausgabepfad | Verzeichnis für Aufnahmedateien |

---

## Technische Architektur

### Projektstruktur

```
Quanta/
├── App.xaml / App.xaml.cs          # Einstiegspunkt, Taskleiste, Thema, Lebenszyklus
├── Views/                           # Ansichtsschicht
│   ├── MainWindow.xaml              # Hauptsuchfenster
│   ├── RecordingOverlayWindow.xaml  # Aufnahme-Overlay-Fenster
│   └── CommandSettingsWindow.xaml   # Einstellungsfenster (5 partial-Dateien)
├── Domain/
│   ├── Search/                      # Suchmaschine, Provider, Ergebnistypen
│   │   ├── SearchEngine.cs
│   │   ├── ExchangeRateService.cs
│   │   ├── ApplicationSearchProvider.cs
│   │   ├── FileSearchProvider.cs
│   │   └── RecentFileSearchProvider.cs
│   └── Commands/                    # Befehlsrouting, Handler
│       ├── CommandRouter.cs
│       └── CommandHandlers.cs
├── Core/
│   ├── Config/                      # AppConfig, ConfigLoader
│   └── Services/                    # RecordingService, ToastService
├── Infrastructure/
│   ├── System/                      # Zwischenablageverlauf, Fensterverwaltung
│   └── Logging/                     # Protokollierung
├── Presentation/
│   └── Helpers/                     # LocalizationService
└── Resources/
    └── Themes/                      # LightTheme.xaml, DarkTheme.xaml
```

### Technologie-Stack

| Projekt | Technologie |
|---------|-------------|
| Framework | .NET 8.0 / WPF |
| Architektur | MVVM (CommunityToolkit.Mvvm) |
| Sprache | C# |
| Aufnahme | NAudio (WASAPI) |
| QR-Code | QRCoder |
| Wechselkurse | exchangerate-api.com |

---

## Autor

**yeal911** · yeal91117@gmail.com

## Lizenz

MIT License
