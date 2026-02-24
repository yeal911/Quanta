# Quanta - Windows 빠른 실행기

<p align="center">
  <a href="README.md">中文</a> | <a href="README_EN.md">English</a> | <a href="README_ES.md">Español</a> | <a href="README_JA.md">日本語</a> | <b>한국어</b> | <a href="README_FR.md">Français</a> | <a href="README_DE.md">Deutsch</a> | <a href="README_PT.md">Português</a> | <a href="README_RU.md">Русский</a> | <a href="README_IT.md">Italiano</a> | <a href="README_AR.md">العربية</a>
</p>

<p align="center">
  <img src="Resources\imgs\quanta.ico" width="64" alt="Quanta"/>
</p>

<p align="center">
  가벼운 Windows 빠른 실행기입니다. 전역 단축키로 호출, 퍼지 검색, 즉시 실행.
</p>

---

## 기능

### 핵심 기능

- **전역 단축키** - 기본 `Alt+R`, 설정에서 사용자 정의 가능
- **퍼지 검색** - 키워드/이름/설명의 스마트 매칭, 접두사 퍼지 지원 (`rec` 입력 시 `record` 매칭)
- **그룹 결과** - 카테고리별로 구성, 그룹 내에서 점수순 정렬
- **사용자 정의 명령** - Url, Program, Shell, Directory, Calculator 유형 지원
- **매개변수 모드** - `Tab` 키로 매개변수 입력 전환 (예: `g > rust` Google 검색)
- **Ctrl+숫자** - `Ctrl+1~9`로 N번째 결과 즉시 실행
- **자동 숨기기** - 다른 곳을 클릭하면 자동으로 숨겨짐

---

## 기본 제공 명령

모든 기본 제공 명령은 퍼지 검색을 지원합니다.

### 명령줄 도구

| 키워드 | 설명 |
|--------|------|
| `cmd` | 명령 프롬프트 열기 |
| `powershell` | PowerShell 열기 |

### 앱

| 키워드 | 설명 |
|--------|------|
| `notepad` | 메모장 |
| `calc` | Windows 계산기 |
| `mspaint` | 그림판 |
| `explorer` | 파일 탐색기 |
| `winrecord` | Windows 녹음 기능 |

### 시스템 관리

| 키워드 | 설명 |
|--------|------|
| `taskmgr` | 작업 관리자 |
| `devmgmt` | 장치 관리자 |
| `services` | 서비스 |
| `regedit` | 레지스트리 편집기 |
| `control` | 제어판 |
| `emptybin` | 휴지통 비우기 |

### 네트워크 진단

| 키워드 | 설명 |
|--------|------|
| `ipconfig` | IP 구성 보기 |
| `ping` | Ping (PowerShell에서 실행) |
| `tracert` | 경로 추적 |
| `nslookup` | DNS 조회 |
| `netstat` | 네트워크 연결 상태 |

### 전원 관리

| 키워드 | 설명 |
|--------|------|
| `lock` | 화면 잠금 |
| `shutdown` | 10초 후 종료 |
| `restart` | 10초 후 다시 시작 |
| `sleep` | 최대 절전 모드 |

### 특별 기능

| 키워드 | 설명 |
|--------|------|
| `record` | Quanta 녹음 기능 시작 |
| `clip` | 클립보드 기록 검색 |

### Quanta 시스템 명령

| 키워드 | 설명 |
|--------|------|
| `setting` | 설정 화면 열기 |
| `about` | 프로그램 정보 |
| `exit` | 종료 |
| `english` | 영어로 인터페이스 언어 전환 |
| `chinese` | 중국어로 인터페이스 언어 전환 |
| `spanish` | 스페인어로 인터페이스 언어 전환 |

---

## 스마트 입력

명령을 선택할 필요가 없습니다. 검색 상자에 직접 입력하면 실시간으로 결과를 인식하고 표시합니다.

### 수학 계산

수학식을 직접 입력하여 계산:

```
2+2*3         → 8
(100-32)/1.8  → 37.778
2^10          → 1024
15%4          → 3
-5+2          → -3
2^-3          → 0.125
2^3^2         → 512
```

지원 연산자: `+ - * / % ^` (괄호, 소수, 단항 부호 `-5`/`+3` 포함).

우선순위: `^`(거듭제곱) > `* / %` > `+ -`; 거듭제곱은 오른쪽 결합입니다(예: `2^3^2 = 2^(3^2)`).

### 단위 변환

형식: `수치 원단위 to 목표단위`

```
100 km to miles    → 62.137 miles
100 c to f         → 212 °F
30 f to c          → -1.111 °C
1 kg to lbs        → 2.205 lbs
500 g to oz        → 17.637 oz
1 gallon to l      → 3.785 l
1 acre to m2       → 4046.856 m²
```

지원 카테고리: 길이, 무게, 온도, 면적, 부피

### 환율 변환

형식: `수치 원통화 to 목표통화` (설정에서 API Key 필요)

```
100 usd to cny     → ¥736.50 CNY
1000 cny to jpy    → ¥20,500 JPY
50 eur to gbp      → £42.30 GBP
```

40개 이상의 통화 지원. 환율 데이터는 로컬 캐시 (기본 1시간).

### 색상 변환

HEX, RGB, HSL 상호 변환 지원:

```
#E67E22            → rgb(230,126,34)  hsl(28,79%,52%)
rgb(230,126,34)    → #E67E22  hsl(28,79%,52%)
hsl(28,79,52%)     → #E67E22  rgb(230,126,34)
255,165,0          → #FFA500  hsl(38,100%,50%)
```

### 텍스트 도구

| 접두사 | 기능 | 예시 |
|--------|------|------|
| `base64 텍스트` | Base64 인코딩 | `base64 hello` → `aGVsbG8=` |
| `base64d 텍스트` | Base64 디코딩 | `base64d aGVsbG8=` → `hello` |
| `md5 텍스트` | MD5 해시 | `md5 hello` → `5d41402a...` |
| `sha256 텍스트` | SHA-256 해시 | `sha256 hello` → `2cf24dba...` |
| `url 텍스트` | URL 인코딩/디코딩 | `url hello world` → `hello%20world` |
| `json JSON문자열` | JSON 포맷 | `json {"a":1}` → 포맷된 출력 |

### Google 검색

형식: `g 키워드`

```
g rust programming  → 브라우저에서 Google 검색 열기
```

### PowerShell 실행

접두사 `>`로 PowerShell 명령 직접 실행:

```
> Get-Process      → 모든 프로세스 표시
> dir C:\          → C:\ 파일 나열
```

### QR 코드 생성

문자 수 임계값(기본 20) 초과 시 자동으로 QR 코드 생성하여 클립보드에 복사:

```
https://github.com/yeal911/Quanta   → QR 코드 자동 생성
```

---

## 녹음 기능

`record` 입력으로 녹음 패널 표시:

- **음원**: 마이크 / 스피커 (시스템 오디오) / 마이크 + 스피커
- **형식**: m4a (AAC) / mp3
- **비트레이트**: 32 / 64 / 96 / 128 / 160 kbps
- **채널**: 모노 / 스테레오
- **작업**: 시작 → 일시정지/재개 → 중지, 지정한 디렉토리에 저장
- **매개변수 우클릭**으로 전환 가능

---

## 클립보드 기록

`clip` 또는 `clip 키워드`로 클립보드 기록 검색:

- 클립보드에 복사한 텍스트 자동 기록 (최대 50개)
- 키워드 필터링 지원, 최신 20개 표시
- 결과 클릭 시 클립보드에 복사
- 데이터 영구 저장

---

## UI 및 경험

- **라이트/다크 테마** - 설정에서 전환, 자동 저장
- **다국어** - 中文 / English / Español / 日本語 / 한국어 / Français / Deutsch / Português / Русский / Italiano / العربية, 전환 즉시 적용
- **시스템 트레이** - 트레이에 상주, 우클릭 메뉴로 빠른 작업
- **시작 시 실행** - 설정에서 원클릭 활성화
- **Toast 알림** - 작업 성공/실패 즉시 피드백

---

## 빠른 시작

### 요구 환경

- Windows 10 / 11 (x64)
- [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

### 실행

```bash
dotnet run
```

### 단일 파일 게시

```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

---

## 단축키

| 단축키 | 기능 |
|--------|------|
| `Alt+R` | 메인 창 표시/숨기기 |
| `Enter` | 선택한 명령 실행 |
| `Tab` | 매개변수 모드로 전환 |
| `Esc` | 매개변수 모드 종료/창 숨기기 |
| `↑` / `↓` | 위/아래로 선택 이동 |
| `Ctrl+1~9` | N번째 항목 즉시 실행 |
| `Backspace` | 매개변수 모드에서 일반 검색으로 돌아가기 |

---

## 명령 관리

설정 화면 (`setting` 명령 또는 트레이 우클릭)의 "명령 관리" 탭에서:

- 테이블에서 직접 키워드, 이름, 유형, 경로 편집
- JSON 가져오기/내보내기 지원, 백업 및 마이그레이션에 편리

### 명령 유형

| 유형 | 설명 | 예시 |
|------|------|------|
| `Url` | 브라우저에서 열기 (`{param}` 매개변수 지원) | `https://google.com/search?q={param}` |
| `Program` | 프로그램 시작 | `notepad.exe` |
| `Shell` | cmd에서 명령 실행 | `ping {param}` |
| `Directory` | 탐색기에서 폴더 열기 | `C:\Users\Public` |
| `Calculator` | 수식 계산 | `{param}` |

---

## 설정

| 설정 항목 | 설명 |
|----------|------|
| 단축키 | 전역 단축키 사용자 정의 (기본 `Alt+R`) |
| 테마 | Light / Dark |
| 언어 | 中文 / English / Español / 日本語 / 한국어 / Français / Deutsch / Português / Русский / Italiano / العربية |
| 시작 시 실행 |开机 자동 실행 |
| 최대 표시 | 검색 결과 수 제한 |
| 긴 텍스트 QR 코드 | QR 코드 생성 문자 수 임계값 |
| 환율 API Key | exchangerate-api.com의 API Key |
| 환율 캐시 시간 | 캐시 유효 시간(시간), 초과 시 다시 호출 |
| 녹음 음원 | 마이크 / 스피커 / 둘 다 |
| 녹음 형식 | m4a / mp3 |
| 녹음 비트레이트 | 32 / 64 / 96 / 128 / 160 kbps |
| 녹음 채널 | 모노 / 스테레오 |
| 녹음 출력 경로 | 녹음 파일 저장 디렉토리 |

---

## 기술 아키텍처

### 프로젝트 구조

```
Quanta/
├── App.xaml / App.xaml.cs          # 진입점, 트레이, 테마, 수명 주기
├── Views/                           # 뷰 레이어
│   ├── MainWindow.xaml              # 메인 검색 창
│   ├── RecordingOverlayWindow.xaml  # 녹음 오버레이 창
│   └── CommandSettingsWindow.xaml   # 설정 창 (5개 partial 파일 포함)
├── Domain/
│   ├── Search/                      # 검색 엔진, Provider, 결과 유형
│   │   ├── SearchEngine.cs
│   │   ├── ExchangeRateService.cs
│   │   ├── ApplicationSearchProvider.cs
│   │   ├── FileSearchProvider.cs
│   │   └── RecentFileSearchProvider.cs
│   └── Commands/                    # 명령 라우팅, 각 Handler
│       ├── CommandRouter.cs
│       └── CommandHandlers.cs
├── Core/
│   ├── Config/                      # AppConfig, ConfigLoader
│   └── Services/                    # RecordingService, ToastService
├── Infrastructure/
│   ├── System/                      # 클립보드 기록, 창 관리
│   └── Logging/                     # 로깅
├── Presentation/
│   └── Helpers/                     # LocalizationService
└── Resources/
    └── Themes/                      # LightTheme.xaml, DarkTheme.xaml
```

### 기술 스택

| 프로젝트 | 기술 |
|----------|------|
| 프레임워크 | .NET 8.0 / WPF |
| 아키텍처 | MVVM (CommunityToolkit.Mvvm) |
| 언어 | C# |
| 녹음 | NAudio (WASAPI) |
| QR 코드 | QRCoder |
| 환율 | exchangerate-api.com |

---

## 작성자

**yeal911** · yeal91117@gmail.com

## 라이선스

MIT License
