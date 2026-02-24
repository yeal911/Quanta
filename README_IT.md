# Quanta - Launcher Rapido per Windows

<p align="center">
  <a href="README.md">中文</a> | <a href="README_EN.md">English</a> | <a href="README_ES.md">Español</a> | <a href="README_JA.md">日本語</a> | <a href="README_KO.md">한국어</a> | <a href="README_FR.md">Français</a> | <a href="README_DE.md">Deutsch</a> | <a href="README_PT.md">Português</a> | <a href="README_RU.md">Русский</a> | <b>Italiano</b> | <a href="README_AR.md">العربية</a>
</p>

<p align="center">
  <img src="Resources\imgs\quanta.ico" width="64" alt="Quanta"/>
</p>

<p align="center">
  Un leggero launcher rapido per Windows. Chiamalo con un tasto di scelta rapida globale, ricerca fuzzy, esecuzione istantanea.
</p>

---

## Funzionalità

### Funzionalità Principali

- **Tasto di Scelta Rapida Globale** - Predefinito `Alt+R`, personalizzabile nelle impostazioni
- **Ricerca Fuzzy** - Corrispondenza intelligente per parola chiave / nome / descrizione, supporta corrispondenza fuzzy prefissa (digita `rec` per trovare `record`)
- **Risultati Raggruppati** - Risultati organizzati per categoria, ordinati per punteggio di corrispondenza all'interno di ogni gruppo
- **Comandi Personalizzati** - Supporta tipi Url, Program, Shell, Directory, Calculator
- **Modo Parametro** - Premi `Tab` per inserire il parametro (es. `g > rust` per cercare su Google)
- **Ctrl+Numero** - `Ctrl+1~9` per eseguire istantaneamente l'N-esimo risultato
- **Nascondi Automaticamente** - Si nasconde automaticamente quando fai clic altrove

---

## Comandi Integrati

Tutti i comandi integrati supportano la ricerca fuzzy.

### Strumenti da Riga di Comando

| Parola Chiave | Descrizione |
|---------------|-------------|
| `cmd` | Apri prompt dei comandi |
| `powershell` | Apri PowerShell |

### Applicazioni

| Parola Chiave | Descrizione |
|---------------|-------------|
| `notepad` | Blocco note |
| `calc` | Calcolatrice Windows |
| `mspaint` | Paint |
| `explorer` | Esplora file |
| `winrecord` | Funzione di registrazione Windows |

### Amministrazione Sistema

| Parola Chiave | Descrizione |
|---------------|-------------|
| `taskmgr` | Gestione attività |
| `devmgmt` | Gestione dispositivi |
| `services` | Servizi |
| `regedit` | Editor del registro |
| `control` | Pannello di controllo |
| `emptybin` | Svuota cestino |

### Diagnosi di Rete

| Parola Chiave | Descrizione |
|---------------|-------------|
| `ipconfig` | Mostra configurazione IP |
| `ping` | Ping (esegui in PowerShell) |
| `tracert` | Traccia percorso |
| `nslookup` | Query DNS |
| `netstat` | Stato connessioni di rete |

### Gestione Alimentazione

| Parola Chiave | Descrizione |
|---------------|-------------|
| `lock` | Blocca schermo |
| `shutdown` | Spegni tra 10 secondi |
| `restart` | Riavvia tra 10 secondi |
| `sleep` | Vai in modalità sospensione |

### Funzionalità Speciali

| Parola Chiave | Descrizione |
|---------------|-------------|
| `record` | Avvia funzione registrazione Quanta |
| `clip` | Cerca nella cronologia appunti |

### Comandi di Sistema Quanta

| Parola Chiave | Descrizione |
|---------------|-------------|
| `setting` | Apri impostazioni |
| `about` | Informazioni sul programma |
| `exit` | Esci |
| `english` | Cambia interfaccia in inglese |
| `chinese` | Cambia interfaccia in cinese |
| `spanish` | Cambia interfaccia in spagnolo |

---

## Input Intelligente

Non c'è bisogno di selezionare un comando. Digita direttamente nella barra di ricerca, i risultati vengono riconosciuti e visualizzati in tempo reale.

### Calcoli Matematici

Inserisci espressioni matematiche direttamente:

```
2+2*3         → 8
(100-32)/1.8  → 37.778
2^10          → 1024
15%4          → 3
-5+2          → -3
2^-3          → 0.125
2^3^2         → 512
```

Operatori supportati: `+ - * / % ^`, con parentesi, decimali e segni unari (ad es. `-5`, `+3`).

Precedenza: `^` (potenza) > `* / %` > `+ -`; la potenza è associativa a destra (es. `2^3^2 = 2^(3^2)`).

### Conversione Unità

Formato: `valore unità_sorgente to unità_destinazione`

```
100 km to miles    → 62.137 miles
100 c to f         → 212 °F
30 f to c          → -1.111 °C
1 kg to lbs        → 2.205 lbs
500 g to oz        → 17.637 oz
1 gallon to l      → 3.785 l
1 acre to m2       → 4046.856 m²
```

Categorie supportate: lunghezza, peso, temperatura, area, volume

### Conversione Valute

Formato: `valore valuta_sorgente to valuta_destinazione` (richiede API Key nelle impostazioni)

```
100 usd to cny     → ¥736.50 CNY
1000 cny to jpy    → ¥20,500 JPY
50 eur to gbp      → £42.30 GBP
```

Supporto per più di 40 valute. Dati sui tassi di cambio in cache locale (predefinito 1 ora).

### Conversione Colori

Supporto conversione HEX, RGB, HSL:

```
#E67E22            → rgb(230,126,34)  hsl(28,79%,52%)
rgb(230,126,34)    → #E67E22  hsl(28,79%,52%)
hsl(28,79,52%)     → #E67E22  rgb(230,126,34)
255,165,0          → #FFA500  hsl(38,100%,50%)
```

### Strumenti Testo

| Prefisso | Funzione | Esempio |
|----------|----------|---------|
| `base64 testo` | Codifica Base64 | `base64 hello` → `aGVsbG8=` |
| `base64d testo` | Decodifica Base64 | `base64d aGVsbG8=` → `hello` |
| `md5 testo` | Hash MD5 | `md5 hello` → `5d41402a...` |
| `sha256 testo` | Hash SHA-256 | `sha256 hello` → `2cf24dba...` |
| `url testo` | Codifica/Decodifica URL | `url hello world` → `hello%20world` |
| `json stringaJSON` | Formattazione JSON | `json {"a":1}` → output formattato |

### Ricerca Google

Formato: `g parola_chiave`

```
g rust programming  → Apri ricerca Google nel browser
```

### Esecuzione PowerShell

Prefisso `>` per eseguire comandi PowerShell direttamente:

```
> Get-Process      → Mostra tutti i processi
> dir C:\          → Elenca file C:\
```

### Generazione QR Code

Quando il numero di caratteri supera la soglia (predefinito 20), genera automaticamente un QR code e lo copia negli appunti:

```
https://github.com/yeal911/Quanta   → Generazione automatica QR code
```

---

## Funzione di Registrazione

Digita `record` per mostrare il pannello di registrazione:

- **Sorgente Audio**: Microfono / Altoparlante (audio di sistema) / Microfono + Altoparlante
- **Formato**: m4a (AAC) / mp3
- **Bitrate**: 32 / 64 / 96 / 128 / 160 kbps
- **Canali**: Mono / Stereo
- **Operazioni**: Avvia → Pausa/Riprendi → Stop, salvato nella directory specificata
- **Clic destro sui parametri** per cambiare

---

## Cronologia Appunti

Digita `clip` o `clip parola_chiave` per cercare nella cronologia appunti:

- Registra automaticamente il testo copiato (massimo 50 voci)
- Supporto filtro per parola chiave, mostra i più recenti 20
- Fai clic su un risultato per copiare negli appunti
- Dati salvati permanentemente

---

## Interfaccia ed Esperienza

- **Tema Chiaro/Scuro** - Cambia nelle impostazioni, salvato automaticamente
- **Multilingua** - 中文 / English / Español / 日本語 / 한국어 / Français / Deutsch / Português / Русский / Italiano / العربية, cambio istantaneo
- **Barra di Sistema** - Risiede nella barra, menu clic destro per azioni rapide
- **Avvio con Windows** - Attivazione con un clic nelle impostazioni
- **Notifiche Toast** - Successo/errore delle operazioni segnalato istantaneamente

---

## Avvio Rapido

### Requisiti

- Windows 10 / 11 (x64)
- [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

### Esecuzione

```bash
dotnet run
```

### Pubblicazione come File Singolo

```bash
dotnet publish -c Release -r win-x64 --self-contained false
---

## Scorciatoie da Tastiera

| Scorciatoia | Funzione |
|-------------|----------|
| `Alt+R` | Mostra/Nascondi finestra principale |
| `Enter` | Esegui comando selezionato |
| `Tab` | Entra in modo parametro |
| `Esc` | Esci dal modo parametro / Nascondi finestra |
| `↑` / `↓` | Sposta selezione su/giù |
| `Ctrl+1~9` | Esegui istantaneamente l'N-esimo risultato |
| `Backspace` | Torna alla ricerca normale in modo parametro |

---

## Gestione Comandi

Nelle impostazioni (comando `setting` o clic destro sulla barra), scheda "Gestione Comandi":

- Modifica direttamente parole chiave, nomi, tipi e percorsi nella tabella
- Supporto import/export JSON, pratico per backup e migrazione

### Tipi di Comandi

| Tipo | Descrizione | Esempio |
|------|-------------|---------|
| `Url` | Apri nel browser (supporta `{param}`) | `https://google.com/search?q={param}` |
| `Program` | Avvia programma | `notepad.exe` |
| `Shell` | Esegui in cmd | `ping {param}` |
| `Directory` | Apri cartella in Esplora file | `C:\Users\Public` |
| `Calculator` | Calcola espressione | `{param}` |

---

## Impostazioni

| Impostazione | Descrizione |
|--------------|-------------|
| Scorciatoia | Personalizza scorciatoia globale (predefinito `Alt+R`) |
| Tema | Light / Dark |
| Lingua | 中文 / English / Español / 日本語 / 한국어 / Français / Deutsch / Português / Русский / Italiano / العربية |
| Avvio | Avvia con Windows |
| Mostra Max | Limite numero risultati |
| Testo Lungo in QR | Soglia caratteri per generazione QR code |
| API Key Tasso di Cambio | API Key per exchangerate-api.com |
| Tempo Cache Tasso | Validità cache (ore), si aggiorna dopo |
| Sorgente Registrazione | Microfono / Altoparlante / Entrambi |
| Formato Registrazione | m4a / mp3 |
| Bitrate Registrazione | 32 / 64 / 96 / 128 / 160 kbps |
| Canali Registrazione | Mono / Stereo |
| Percorso Output | Directory per salvare registrazioni |

---

## Architettura Tecnica

### Struttura del Progetto

```
Quanta/
├── App.xaml / App.xaml.cs          # Punto di ingresso, barra, tema, ciclo di vita
├── Views/                           # Livello Vista
│   ├── MainWindow.xaml              # Finestra principale di ricerca
│   ├── RecordingOverlayWindow.xaml  # Finestra sovrapposizione registrazione
│   └── CommandSettingsWindow.xaml   # Finestra impostazioni (5 file partial)
├── Domain/
│   ├── Search/                      # Motore di ricerca, Provider, tipi di risultato
│   │   ├── SearchEngine.cs
│   │   ├── ExchangeRateService.cs
│   │   ├── ApplicationSearchProvider.cs
│   │   ├── FileSearchProvider.cs
│   │   └── RecentFileSearchProvider.cs
│   └── Commands/                    # Instradamento comandi, Handler
│       ├── CommandRouter.cs
│       └── CommandHandlers.cs
├── Core/
│   ├── Config/                      # AppConfig, ConfigLoader
│   └── Services/                    # RecordingService, ToastService
├── Infrastructure/
│   ├── System/                      # Cronologia appunti, gestione finestre
│   └── Logging/                     # Registrazione
├── Presentation/
│   └── Helpers/                     # LocalizationService
└── Resources/
    └── Themes/                      # LightTheme.xaml, DarkTheme.xaml
```

### Stack Tecnologico

| Progetto | Tecnologia |
|----------|------------|
| Framework | .NET 8.0 / WPF |
| Architettura | MVVM (CommunityToolkit.Mvvm) |
| Linguaggio | C# |
| Registrazione | NAudio (WASAPI) |
| QR Code | QRCoder |
| Tasso di Cambio | exchangerate-api.com |

---

## Autore

**yeal911** · yeal91117@gmail.com

## Licenza

MIT License
