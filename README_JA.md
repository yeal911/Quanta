# Quanta - Windows クイックランチャー

<p align="center">
  <a href="README.md">中文</a> | <a href="README_EN.md">English</a> | <a href="README_ES.md">Español</a> | <b>日本語</b> | <a href="README_KO.md">한국어</a> | <a href="README_FR.md">Français</a> | <a href="README_DE.md">Deutsch</a> | <a href="README_PT.md">Português</a> | <a href="README_RU.md">Русский</a> | <a href="README_IT.md">Italiano</a> | <a href="README_AR.md">العربية</a>
</p>

<p align="center">
  <img src="Resources\imgs\quanta.ico" width="64" alt="Quanta"/>
</p>

<p align="center">
  軽量なWindowsクイックランチャー。グローバルホットキーで起動、模糊検索、インスタント実行。
</p>

---

## 機能

### コア機能

- **グローバルホットキー** - デフォルト `Alt+R`、設定でカスタマイズ可能
- **模糊検索** - キーワード/名前/説明のスマートマッチング、前方一致対応（`rec` と入力すると `record` にマッチ）
- **グループ結果** - カテゴリ別に整理され、グループ内で一致度順にソート
- **カスタムコマンド** - Url、Program、Shell、Directory、Calculatorタイプをサポート
- **パラメータモード** - `Tab` キーでパラメータ入力に移行（例：`g > rust` でGoogle検索）
- **Ctrl+数字** - `Ctrl+1~9` でN番目を即実行
- **フォーカス消失時自動非表示** - 他の場所をクリックすると自動的に非表示

---

## 組み込みコマンド

すべての組み込みコマンドは模糊検索に対応。

### コマンドラインツール

| キーワード | 説明 |
|-----------|------|
| `cmd` | コマンドプロンプトを開く |
| `powershell` | PowerShellを開く |

### アプリ

| キーワード | 説明 |
|-----------|------|
| `notepad` | メモ帳 |
| `calc` | Windows 電卓 |
| `mspaint` | ペイント |
| `explorer` | エクスプローラー |
| `winrecord` | Windows 録音機能 |

### システム管理

| キーワード | 説明 |
|-----------|------|
| `taskmgr` | タスクマネージャー |
| `devmgmt` | デバイスマネージャー |
| `services` | サービス |
| `regedit` | レジストリエディタ |
| `control` | コントロールパネル |
| `emptybin` | ごみ箱を空にする |

### ネットワーク診断

| キーワード | 説明 |
|-----------|------|
| `ipconfig` | IP設定を表示 |
| `ping` | Ping（PowerShellで実行） |
| `tracert` | ルートトレース |
| `nslookup` | DNS查询 |
| `netstat` | ネットワーク接続状態 |

### 電源管理

| キーワード | 説明 |
|-----------|------|
| `lock` | 画面をロック |
| `shutdown` | 10秒後にシャットダウン |
| `restart` | 10秒後に再起動 |
| `sleep` | スリープ状態へ |

### 特色機能

| キーワード | 説明 |
|-----------|------|
| `record` | Quanta 録音機能を起動 |
| `clip` | クリップボード履歴を検索 |

### Quanta システムコマンド

| キーワード | 説明 |
|-----------|------|
| `setting` | 設定画面を開く |
| `about` | アプリについて |
| `exit` | 終了 |
| `english` | 英語をインターフェース言語に切り替え |
| `chinese` | 中国語をインターフェース言語に切り替え |
| `spanish` | スペイン語をインターフェース言語に切り替え |

---

## スマート入力

コマンドを選択する必要はありません。検索ボックスに直接入力すると、リアルタイムで結果を認識して表示します。

### 数学計算

数学式を直接入力して計算：

```
2+2*3         → 8
(100-32)/1.8  → 37.778
2^10          → 1024
15%4          → 3
-5+2          → -3
2^-3          → 0.125
2^3^2         → 512
```

演算子：`+ - * / % ^`。括弧・小数・単項符号（例：`-5`、`+3`）に対応。

優先順位：`^`（累乗）> `* / %` > `+ -`。累乗は右結合（例：`2^3^2 = 2^(3^2)`）。

### 単位変換

形式：`数値 変換元単位 to 変換先単位`

```
100 km to miles    → 62.137 miles
100 c to f         → 212 °F
30 f to c          → -1.111 °C
1 kg to lbs        → 2.205 lbs
500 g to oz        → 17.637 oz
1 gallon to l      → 3.785 l
1 acre to m2       → 4046.856 m²
```

対応カテゴリ：長さ、重さ、温度、面積、体積

### 為替変換

形式：`数値 変換元通貨 to 変換先通貨`（設定でAPI Keyが必要）

```
100 usd to cny     → ¥736.50 CNY
1000 cny to jpy    → ¥20,500 JPY
50 eur to gbp      → £42.30 GBP
```

40以上の通貨に対応。為替データはローカルキャッシュ（デフォルト1時間）。

### カラー変換

HEX、RGB、HSLの相互変換に対応：

```
#E67E22            → rgb(230,126,34)  hsl(28,79%,52%)
rgb(230,126,34)    → #E67E22  hsl(28,79%,52%)
hsl(28,79,52%)     → #E67E22  rgb(230,126,34)
255,165,0          → #FFA500  hsl(38,100%,50%)
```

### テキストツール

| プレフィックス | 機能 | 例 |
|----------------|------|-----|
| `base64 テキスト` | Base64エンコード | `base64 hello` → `aGVsbG8=` |
| `base64d テキスト` | Base64デコード | `base64d aGVsbG8=` → `hello` |
| `md5 テキスト` | MD5ハッシュ | `md5 hello` → `5d41402a...` |
| `sha256 テキスト` | SHA-256ハッシュ | `sha256 hello` → `2cf24dba...` |
| `url テキスト` | URLエンコード/デコード | `url hello world` → `hello%20world` |
| `json JSON文字列` | JSONフォーマット | `json {"a":1}` → 整形出力 |

### Google検索

形式：`g キーワード`

```
g rust programming  → ブラウザでGoogle検索を開く
```

### PowerShell実行

プレフィックス `>` でPowerShellコマンドを直接実行：

```
> Get-Process      → すべてのプロセスを表示
> dir C:\          → C:\のファイルを表示
```

### QRコード生成

文字数閾値（デフォルト20）を超えると、自動的にQRコード生成してクリップボードにコピー：

```
https://github.com/yeal911/Quanta   → QRコードを自動生成
```

---

## 録音機能

`record` で録音パネルを表示：

- **音源**：マイク / スピーカー（システムオーディオ）/ マイク+スピーカー
- **形式**：m4a（AAC）/ mp3
- **ビットレート**：32 / 64 / 96 / 128 / 160 kbps
- **チャンネル**：モノラル / ステレオ
- **操作**：開始 → 一時停止/再開 → 停止、指定したディレクトリに保存
- **パラメータを右クリック**で切り替え可能

---

## クリップボード履歴

`clip` または `clip キーワード` でクリップボード履歴を検索：

- クリップボードにコピーしたテキストを自動記録（最大50件）
- キーワードフィルタリング対応、最新20件を表示
- 結果を 클릭するとクリップボードにコピー
- データは永続化保存

---

## UIと体験

- **ライト/ダークテーマ** - 設定で切り替え、自动保存
- **多言語** - 中文 / English / Español / 日本語 / 한국어 / Français / Deutsch / Português / Русский / Italiano / العربية、切替即时生效
- **システムトレイ** - トレイに常駐、右クリックメニューで簡単操作
- **スタートアップ** - 設定で一键有効化
- **Toast通知** - 操作の成功/失敗を即时フィードバック

---

## クイックスタート

### 動作環境

- Windows 10 / 11（x64）
- [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

### 実行

```bash
dotnet run
```

### 単一ファイルとして公開

```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

---

## ホットキー

| ホットキー | 機能 |
|------------|------|
| `Alt+R` | メインウィンドウを表示/非表示 |
| `Enter` | 選択したコマンドを実行 |
| `Tab` | パラメータモードに移行 |
| `Esc` | パラメータモードを終了/ウィンドウを非表示 |
| `↑` / `↓` | 上/下で選択移動 |
| `Ctrl+1~9` | N番目を即実行 |
| `Backspace` | パラメータモードで普通検索に戻る |

---

## コマンド管理

設定界面（`setting` コマンドまたはトレイ右クリック）の「コマンド管理」タブで：

- テーブル内で直接キーワード、名前、タイプ、パスを編集
- JSONインポート/エクスポート対応、バックアップと移行に便利

### コマンドタイプ

| タイプ | 説明 | 例 |
|--------|------|-----|
| `Url` | ブラウザで開く（`{param}` パラメータ対応） | `https://google.com/search?q={param}` |
| `Program` | プログラムを起動 | `notepad.exe` |
| `Shell` | cmdでコマンド実行 | `ping {param}` |
| `Directory` | エクスプローラーでフォルダを開く | `C:\Users\Public` |
| `Calculator` | 数式を計算 | `{param}` |

---

## 設定

| 項目 | 説明 |
|------|------|
| ホットキー | グローバルホットキーをカスタマイズ（デフォルト `Alt+R`） |
| テーマ | Light / Dark |
| 言語 | 中文 / English / Español / 日本語 / 한국어 / Français / Deutsch / Português / Русский / Italiano / العربية |
| スタートアップ | 开机自动运行 |
| 表示上限 | 検索結果数の上限 |
| 長文QRコード変換 | QRコード生成の文字数閾値 |
| 為替API Key | exchangerate-api.com のAPI Key |
| 為替キャッシュ時間 | キャッシュ有効時間（時間）、超過后再取得 |
| 録音音源 | マイク / スピーカー / 两者 |
| 録音形式 | m4a / mp3 |
| 録音ビットレート | 32 / 64 / 96 / 128 / 160 kbps |
| 録音チャンネル | モノラル / ステレオ |
| 録音出力パス | 録音ファイルの保存ディレクトリ |

---

## 技術アーキテクチャ

### プロジェクト構造

```
Quanta/
├── App.xaml / App.xaml.cs          # エントリーポイント、トレイ、テーマ、ライフサイクル
├── Views/                           # ビュー層
│   ├── MainWindow.xaml              # メイン検索ウィンドウ
│   ├── RecordingOverlayWindow.xaml  # 録音オーバーレイウィンドウ
│   └── CommandSettingsWindow.xaml   # 設定ウィンドウ（5つのpartialファイル含む）
├── Domain/
│   ├── Search/                      # 検索エンジン、Provider、結果タイプ
│   │   ├── SearchEngine.cs
│   │   ├── ExchangeRateService.cs
│   │   ├── ApplicationSearchProvider.cs
│   │   ├── FileSearchProvider.cs
│   │   └── RecentFileSearchProvider.cs
│   └── Commands/                    # コマンドルート、各Handler
│       ├── CommandRouter.cs
│       └── CommandHandlers.cs
├── Core/
│   ├── Config/                      # AppConfig、ConfigLoader
│   └── Services/                    # RecordingService、ToastService
├── Infrastructure/
│   ├── System/                      # クリップボード履歴、ウィンドウ管理
│   └── Logging/                     # ロギング
├── Presentation/
│   └── Helpers/                     # LocalizationService
└── Resources/
    └── Themes/                      # LightTheme.xaml、DarkTheme.xaml
```

### 技術スタック

| プロジェクト | 技術 |
|--------------|------|
| フレームワーク | .NET 8.0 / WPF |
| アーキテクチャ | MVVM（CommunityToolkit.Mvvm） |
| 言語 | C# |
| 録音 | NAudio（WASAPI） |
| QRコード | QRCoder |
| 為替 | exchangerate-api.com |

---

## 著者

**yeal911** · yeal91117@gmail.com

## ライセンス

MIT License
