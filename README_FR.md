# Quanta - Lanceur Rapide pour Windows

<p align="center">
  <a href="README.md">中文</a> | <a href="README_EN.md">English</a> | <a href="README_ES.md">Español</a> | <a href="README_JA.md">日本語</a> | <a href="README_KO.md">한국어</a> | <b>Français</b> | <a href="README_DE.md">Deutsch</a> | <a href="README_PT.md">Português</a> | <a href="README_RU.md">Русский</a> | <a href="README_IT.md">Italiano</a> | <a href="README_AR.md">العربية</a>
</p>

<p align="center">
  <img src="Resources\imgs\quanta.ico" width="64" alt="Quanta"/>
</p>

<p align="center">
  Un lanceur rapide léger pour Windows. Appelez-le avec un raccourci global, recherche floue, exécution instantanée.
</p>

---

## Fonctionnalités

### Fonctionnalités Principales

- **Raccourci Global** - Par défaut `Alt+R`, personnalisable dans les paramètres
- **Recherche Floue** - Correspondance intelligente par mot-clé / nom / description, support de correspondance floue par préfixe (tapez `rec` pour correspondre à `record`)
- **Résultats Groupés** - Résultats organisés par catégorie, triés par score de correspondance dans chaque groupe
- **Commandes Personnalisées** - Supporte les types Url, Program, Shell, Directory, Calculator
- **Mode Paramètre** - Appuyez sur `Tab` pour entrer en mode paramètre (ex: `g > rust` pour rechercher sur Google)
- **Ctrl+Nombre** - `Ctrl+1~9` pour exécuter instantanément le Nième résultat
- **Masquage Auto** - Se masque automatiquement lorsque vous cliquez ailleurs

---

## Commandes Intégrées

Toutes les commandes intégrées supportent la recherche floue.

### Outils en Ligne de Commande

| Mot-clé | Description |
|---------|-------------|
| `cmd` | Ouvrir l'invite de commandes |
| `powershell` | Ouvrir PowerShell |

### Applications

| Mot-clé | Description |
|---------|-------------|
| `notepad` | Bloc-notes |
| `calc` | Calculatrice Windows |
| `mspaint` | Paint |
| `explorer` | Explorateur Windows |
| `winrecord` | Fonction d'enregistrement Windows |

### Administration Système

| Mot-clé | Description |
|---------|-------------|
| `taskmgr` | Gestionnaire de tâches |
| `devmgmt` | Gestionnaire de périphériques |
| `services` | Services |
| `regedit` | Éditeur de registre |
| `control` | Panneau de configuration |
| `emptybin` | Vider la corbeille |

### Diagnostic Réseau

| Mot-clé | Description |
|---------|-------------|
| `ipconfig` | Afficher la configuration IP |
| `ping` | Ping (exécuter dans PowerShell) |
| `tracert` | Trace de route |
| `nslookup` | Recherche DNS |
| `netstat` | État des connexions réseau |

### Gestion de l'Alimentation

| Mot-clé | Description |
|---------|-------------|
| `lock` | Verrouiller l'écran |
| `shutdown` | Éteindre dans 10 secondes |
| `restart` | Redémarrer dans 10 secondes |
| `sleep` | Mettre en veille |

### Fonctionnalités Spéciales

| Mot-clé | Description |
|---------|-------------|
| `record` | Démarrer la fonction d'enregistrement Quanta |
| `clip` | Rechercher dans l'historique du presse-papiers |

### Commandes Système Quanta

| Mot-clé | Description |
|---------|-------------|
| `setting` | Ouvrir les paramètres |
| `about` | À propos du programme |
| `exit` | Quitter |
| `english` | Passer l'interface en anglais |
| `chinese` | Passer l'interface en chinois |
| `spanish` | Passer l'interface en espagnol |

---

## Saisie Intelligente

Pas besoin de sélectionner une commande. Tapez directement dans la barre de recherche, les résultats sont reconnus et affichés en temps réel.

### Calculs Mathématiques

Entrez directement des expressions mathématiques :

```
2+2*3         → 8
(100-32)/1.8  → 37.778
2^10          → 1024
15%4          → 3
-5+2          → -3
2^-3          → 0.125
2^3^2         → 512
```

Opérateurs pris en charge : `+ - * / % ^`, avec parenthèses, décimales et signes unaires (comme `-5`, `+3`).

Priorité : `^` (puissance) > `* / %` > `+ -` ; la puissance est associative à droite (ex. `2^3^2 = 2^(3^2)`).

### Conversion d'Unités

Format : `valeur unité_source to unité_cible`

```
100 km to miles    → 62.137 miles
100 c to f         → 212 °F
30 f to c          → -1.111 °C
1 kg to lbs        → 2.205 lbs
500 g to oz        → 17.637 oz
1 gallon to l      → 3.785 l
1 acre to m2       → 4046.856 m²
```

Catégories supportées : longueur, poids, température, superficie, volume

### Conversion de Devises

Format : `valeur devise_source to devise_cible` (nécessite une API Key dans les paramètres)

```
100 usd to cny     → ¥736.50 CNY
1000 cny to jpy    → ¥20,500 JPY
50 eur to gbp      → £42.30 GBP
```

Support de plus de 40 devises. Données de change en cache local (par défaut 1 heure).

### Conversion de Couleurs

Support de la conversion HEX, RGB, HSL :

```
#E67E22            → rgb(230,126,34)  hsl(28,79%,52%)
rgb(230,126,34)    → #E67E22  hsl(28,79%,52%)
hsl(28,79,52%)     → #E67E22  rgb(230,126,34)
255,165,0          → #FFA500  hsl(38,100%,50%)
```

### Outils Texte

| Préfixe | Fonction | Exemple |
|---------|----------|---------|
| `base64 texte` | Encodage Base64 | `base64 hello` → `aGVsbG8=` |
| `base64d texte` | Décodage Base64 | `base64d aGVsbG8=` → `hello` |
| `md5 texte` | Hash MD5 | `md5 hello` → `5d41402a...` |
| `sha256 texte` | Hash SHA-256 | `sha256 hello` → `2cf24dba...` |
| `url texte` | Encodage/Décodage URL | `url hello world` → `hello%20world` |
| `json chaîneJSON` | Formatage JSON | `json {"a":1}` → sortie formatée |

### Recherche Google

Format : `g mot-clé`

```
g rust programming  → Ouvrir recherche Google dans le navigateur
```

### Exécution PowerShell

Préfixe `>` pour exécuter des commandes PowerShell directement :

```
> Get-Process      → Afficher tous les processus
> dir C:\          → Lister les fichiers C:\
```

### Génération de Code QR

Lorsque le nombre de caractères dépasse le seuil (par défaut 20), génère automatiquement un code QR et le copie dans le presse-papiers :

```
https://github.com/yeal911/Quanta   → Génération automatique de code QR
```

---

## Fonction d'Enregistrement

Entrez `record` pour afficher le panneau d'enregistrement :

- **Source Audio** : Micro / Haut-parleur (audio système) / Micro + Haut-parleur
- **Format** : m4a (AAC) / mp3
- **Débit** : 32 / 64 / 96 / 128 / 160 kbps
- **Canaux** : Mono / Stéréo
- **Opérations** : Démarrer → Pause/Reprendre → Arrêter, enregistré dans le répertoire spécifié
- **Clic droit sur les paramètres** pour basculer

---

## Historique du Presse-papiers

Entrez `clip` ou `clip mot-clé` pour rechercher dans l'historique du presse-papiers :

- Enregistre automatiquement le texte copié (maximum 50 entrées)
- Support du filtrage par mot-clé, affiche les 20 plus récents
- Cliquez sur un résultat pour copier dans le presse-papiers
- Données sauvegardées de manière permanente

---

## Interface et Expérience

- **Thème Clair/Sombre** - Basculez dans les paramètres, sauvegarde automatique
- **Multilingue** - 中文 / English / Español / 日本語 / 한국어 / Français / Deutsch / Português / Русский / Italiano / العربية, changement instantané
- **Barre d'État Système** - Réside dans la barre d'état, menu clic droit pour actions rapides
- **Démarrage avec Windows** - Activation en un clic dans les paramètres
- **Notifications Toast** - Succès/échec des opérations signalé instantanément

---

## Démarrage Rapide

### Configuration Requise

- Windows 10 / 11 (x64)
- [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

### Exécution

```bash
dotnet run
```

### Publication en Fichier Unique

```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

---

## Raccourcis Clavier

| Raccourci | Fonction |
|-----------|----------|
| `Alt+R` | Afficher/Masquer la fenêtre principale |
| `Enter` | Exécuter la commande sélectionnée |
| `Tab` | Passer en mode paramètre |
| `Esc` | Quitter le mode paramètre / Masquer la fenêtre |
| `↑` / `↓` | Déplacer la sélection haut/bas |
| `Ctrl+1~9` | Exécuter instantanément le Nième résultat |
| `Backspace` | Retour à la recherche normale en mode paramètre |

---

## Gestion des Commandes

Dans les paramètres (`setting` ou clic droit sur la barre d'état), onglet "Gestion des Commandes" :

- Éditez directement les mots-clés, noms, types et chemins dans le tableau
- Support import/export JSON, pratique pour la sauvegarde et la migration

### Types de Commandes

| Type | Description | Exemple |
|------|-------------|---------|
| `Url` | Ouvrir dans le navigateur (supporte `{param}`) | `https://google.com/search?q={param}` |
| `Program` | Lancer un programme | `notepad.exe` |
| `Shell` | Exécuter dans cmd | `ping {param}` |
| `Directory` | Ouvrir un dossier dans l'explorateur | `C:\Users\Public` |
| `Calculator` | Calculer une expression | `{param}` |

---

## Paramètres

| Paramètre | Description |
|-----------|-------------|
| Raccourci | Personnaliser le raccourci global (défaut `Alt+R`) |
| Thème | Light / Dark |
| Langue | 中文 / English / Español / 日本語 / 한국어 / Français / Deutsch / Português / Русский / Italiano / العربية |
| Démarrage | Lancer au démarrage de Windows |
| Affichage Max | Limite du nombre de résultats |
| Texte Long en QR | Seuil de caractères pour génération QR code |
| Clé API Taux de Change | Clé API pour exchangerate-api.com |
| Cache Taux de Change | Durée de validité du cache (heures), se rafraîchit après |
| Source Enregistrement | Micro / Haut-parleur / Les deux |
| Format Enregistrement | m4a / mp3 |
| Débit Enregistrement | 32 / 64 / 96 / 128 / 160 kbps |
| Canaux Enregistrement | Mono / Stéréo |
| Chemin de Sortie | Répertoire de sauvegarde des enregistrements |

---

## Architecture Technique

### Structure du Projet

```
Quanta/
├── App.xaml / App.xaml.cs          # Point d'entrée, barre d'état, thème, cycle de vie
├── Views/                           # Couche Vue
│   ├── MainWindow.xaml              # Fenêtre de recherche principale
│   ├── RecordingOverlayWindow.xaml  # Fenêtre d'enregistrement
│   └── CommandSettingsWindow.xaml   # Fenêtre de paramètres (5 fichiers partial)
├── Domain/
│   ├── Search/                      # Moteur de recherche, Provider, types de résultats
│   │   ├── SearchEngine.cs
│   │   ├── ExchangeRateService.cs
│   │   ├── ApplicationSearchProvider.cs
│   │   ├── FileSearchProvider.cs
│   │   └── RecentFileSearchProvider.cs
│   └── Commands/                    # Routage des commandes, Handlers
│       ├── CommandRouter.cs
│       └── CommandHandlers.cs
├── Core/
│   ├── Config/                      # AppConfig, ConfigLoader
│   └── Services/                    # RecordingService, ToastService
├── Infrastructure/
│   ├── System/                      # Historique presse-papiers, gestion des fenêtres
│   └── Logging/                     # Journalisation
├── Presentation/
│   └── Helpers/                     # LocalizationService
└── Resources/
    └── Themes/                      # LightTheme.xaml, DarkTheme.xaml
```

### Stack Technique

| Projet | Technologie |
|--------|--------------|
| Framework | .NET 8.0 / WPF |
| Architecture | MVVM (CommunityToolkit.Mvvm) |
| Langage | C# |
| Enregistrement | NAudio (WASAPI) |
| Code QR | QRCoder |
| Taux de Change | exchangerate-api.com |

---

## Auteur

**yeal911** · yeal91117@gmail.com

## Licence

MIT License
