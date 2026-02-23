# Quanta - Iniciador Rápido para Windows

<p align="center">
  <a href="README.md">中文</a> | <a href="README_EN.md">English</a> | <a href="README_ES.md">Español</a> | <a href="README_JA.md">日本語</a> | <a href="README_KO.md">한국어</a> | <a href="README_FR.md">Français</a> | <a href="README_DE.md">Deutsch</a> | <b>Português</b> | <a href="README_RU.md">Русский</a> | <a href="README_IT.md">Italiano</a> | <a href="README_AR.md">العربية</a>
</p>

<p align="center">
  <img src="Resources\imgs\quanta.ico" width="64" alt="Quanta"/>
</p>

<p align="center">
  Um iniciador rápido leve para Windows. Chame-o com uma tecla de atalho global, busca difusa, execução instantânea.
</p>

---

## Recursos

### Recursos Principais

- **Tecla de Atalho Global** - Padrão `Alt+R`, personalizável nas configurações
- **Busca Difusa** - Correspondência inteligente por palavra-chave / nome / descrição, suporte a correspondência difusa de prefixo (digite `rec` para corresponder a `record`)
- **Resultados Agrupados** - Resultados organizados por categoria, ordenados por pontuação de correspondência dentro de cada grupo
- **Comandos Personalizados** - Suporta tipos Url, Program, Shell, Directory, Calculator
- **Modo Parâmetro** - Pressione `Tab` para entrar no modo de parâmetro (ex: `g > rust` para buscar no Google)
- **Ctrl+Número** - `Ctrl+1~9` para executar instantaneamente o N-ésimo resultado
- **Ocultar Automaticamente** - Oculta automaticamente quando você clica em outro lugar

---

## Comandos Integrados

Todos os comandos integrados suportam busca difusa.

### Ferramentas de Linha de Comando

| Palavra-chave | Descrição |
|---------------|-----------|
| `cmd` | Abrir prompt de comando |
| `powershell` | Abrir PowerShell |

### Aplicativos

| Palavra-chave | Descrição |
|---------------|-----------|
| `notepad` | Bloco de notas |
| `calc` | Calculadora do Windows |
| `mspaint` | Paint |
| `explorer` | Explorador de Arquivos |
| `winrecord` | Função de gravação do Windows |

### Administração do Sistema

| Palavra-chave | Descrição |
|---------------|-----------|
| `taskmgr` | Gerenciador de Tarefas |
| `devmgmt` | Gerenciador de Dispositivos |
| `services` | Serviços |
| `regedit` | Editor do Registro |
| `control` | Painel de Controle |
| `emptybin` | Esvaziar a Lixeira |

### Diagnóstico de Rede

| Palavra-chave | Descrição |
|---------------|-----------|
| `ipconfig` | Mostrar configuração de IP |
| `ping` | Ping (executar no PowerShell) |
| `tracert` | Rastrear rota |
| `nslookup` | Consulta DNS |
| `netstat` | Status das conexões de rede |

### Gerenciamento de Energia

| Palavra-chave | Descrição |
|---------------|-----------|
| `lock` | Bloquear tela |
| `shutdown` | Desligar em 10 segundos |
| `restart` | Reiniciar em 10 segundos |
| `sleep` | Entrar em modo de suspensão |

### Recursos Especiais

| Palavra-chave | Descrição |
|---------------|-----------|
| `record` | Iniciar função de gravação do Quanta |
| `clip` | Pesquisar histórico da área de transferência |

### Comandos do Sistema Quanta

| Palavra-chave | Descrição |
|---------------|-----------|
| `setting` | Abrir configurações |
| `about` | Sobre o programa |
| `exit` | Sair |
| `english` | Mudar interface para inglês |
| `chinese` | Mudar interface para chinês |
| `spanish` | Mudar interface para espanhol |

---

## Entrada Inteligente

Não há necessidade de selecionar um comando. Digite diretamente na barra de pesquisa, os resultados são reconhecidos e exibidos em tempo real.

### Cálculos Matemáticos

Digite expressões matemáticas diretamente:

```
2+2*3         → 8
(100-32)/1.8  → 37.778
2^10          → 1024
15%4          → 3
```

Operadores suportados: `+ - * / % ^`, suporta parênteses e decimais.

### Conversão de Unidades

Formato: `valor unidade_origem to unidade_destino`

```
100 km to miles    → 62.137 miles
100 c to f         → 212 °F
30 f to c          → -1.111 °C
1 kg to lbs        → 2.205 lbs
500 g to oz        → 17.637 oz
1 gallon to l      → 3.785 l
1 acre to m2       → 4046.856 m²
```

Categorias suportadas: comprimento, peso, temperatura, área, volume

### Conversão de Moedas

Formato: `valor moeda_origem to moeda_destino` (requer API Key nas configurações)

```
100 usd to cny     → ¥736.50 CNY
1000 cny to jpy    → ¥20,500 JPY
50 eur to gbp      → £42.30 GBP
```

Suporte para mais de 40 moedas. Dados de câmbio em cache local (padrão 1 hora).

### Conversão de Cores

Suporte para conversão HEX, RGB, HSL:

```
#E67E22            → rgb(230,126,34)  hsl(28,79%,52%)
rgb(230,126,34)    → #E67E22  hsl(28,79%,52%)
hsl(28,79,52%)     → #E67E22  rgb(230,126,34)
255,165,0          → #FFA500  hsl(38,100%,50%)
```

### Ferramentas de Texto

| Prefixo | Função | Exemplo |
|---------|--------|---------|
| `base64 texto` | Codificação Base64 | `base64 hello` → `aGVsbG8=` |
| `base64d texto` | Decodificação Base64 | `base64d aGVsbG8=` → `hello` |
| `md5 texto` | Hash MD5 | `md5 hello` → `5d41402a...` |
| `sha256 texto` | Hash SHA-256 | `sha256 hello` → `2cf24dba...` |
| `url texto` | Codificação/Decodificação URL | `url hello world` → `hello%20world` |
| `json cadeiaJSON` | Formatação JSON | `json {"a":1}` → saída formatada |

### Busca no Google

Formato: `g palavra-chave`

```
g rust programming  → Abrir busca no Google no navegador
```

### Execução PowerShell

Prefixo `>` para executar comandos PowerShell diretamente:

```
> Get-Process      → Mostrar todos os processos
> dir C:\          → Listar arquivos de C:\
```

### Geração de QR Code

Quando o número de caracteres excede o limite (padrão 20), gera automaticamente um QR code e copia para a área de transferência:

```
https://github.com/yeal911/Quanta   → Geração automática de QR code
```

---

## Função de Gravação

Digite `record` para mostrar o painel de gravação:

- **Fonte de Áudio**: Microfone / Alto-falante (áudio do sistema) / Microfone + Alto-falante
- **Formato**: m4a (AAC) / mp3
- **Taxa de Bits**: 32 / 64 / 96 / 128 / 160 kbps
- **Canais**: Mono / Estéreo
- **Operações**: Iniciar → Pausar/Retomar → Parar, salvo no diretório especificado
- **Clique direito nos parâmetros** para alternar

---

## Histórico da Área de Transferência

Digite `clip` ou `clip palavra-chave` para pesquisar no histórico da área de transferência:

- Registra automaticamente o texto copiado (máximo 50 entradas)
- Suporte a filtragem por palavra-chave, mostra os 20 mais recentes
- Clique em um resultado para copiar para a área de transferência
- Dados salvos permanentemente

---

## Interface e Experiência

- **Tema Claro/Escuro** - Alterne nas configurações, salvo automaticamente
- **Multilíngue** - 中文 / English / Español / 日本語 / 한국어 / Français / Deutsch / Português / Русский / Italiano / العربية, mudança instantânea
- **Bandeja do Sistema** - Residente na bandeja, menu de clique direito para ações rápidas
- **Iniciar com o Windows** - Ativação com um clique nas configurações
- **Notificações Toast** - Sucesso/erro de operações reportado instantaneamente

---

## Início Rápido

### Requisitos

- Windows 10 / 11 (x64)
- [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

### Execução

```bash
dotnet run
```

### Publicação como Arquivo Único

```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

---

## Atalhos de Teclado

| Atalho | Função |
|--------|--------|
| `Alt+R` | Mostrar/Ocultar janela principal |
| `Enter` | Executar comando selecionado |
| `Tab` | Entrar no modo parâmetro |
| `Esc` | Sair do modo parâmetro / Ocultar janela |
| `↑` / `↓` | Mover seleção para cima/baixo |
| `Ctrl+1~9` | Executar instantaneamente o N-ésimo resultado |
| `Backspace` | Retornar à busca normal no modo parâmetro |

---

## Gerenciamento de Comandos

Nas configurações (comando `setting` ou clique direito na bandeja), aba "Gerenciamento de Comandos":

- Edite diretamente palavras-chave, nomes, tipos e caminhos na tabela
- Suporte a import/export JSON, prático para backup e migração

### Tipos de Comandos

| Tipo | Descrição | Exemplo |
|------|-----------|---------|
| `Url` | Abrir no navegador (suporta `{param}`) | `https://google.com/search?q={param}` |
| `Program` | Iniciar programa | `notepad.exe` |
| `Shell` | Executar no cmd | `ping {param}` |
| `Directory` | Abrir pasta no explorador | `C:\Users\Public` |
| `Calculator` | Calcular expressão | `{param}` |

---

## Configurações

| Configuração | Descrição |
|--------------|-----------|
| Atalho | Personalizar atalho global (padrão `Alt+R`) |
| Tema | Light / Dark |
| Idioma | 中文 / English / Español / 日本語 / 한국어 / Français / Deutsch / Português / Русский / Italiano / العربية |
| Iniciar | Iniciar com o Windows |
| Mostrar Máx | Limite de resultados |
| Texto Longo para QR | Limite de caracteres para geração de QR code |
| Chave API Taxa de Câmbio | Chave API para exchangerate-api.com |
| Tempo de Cache Câmbio | Validade do cache (horas), atualiza após expirar |
| Fonte de Gravação | Microfone / Alto-falante / Ambos |
| Formato de Gravação | m4a / mp3 |
| Taxa de Bits de Gravação | 32 / 64 / 96 / 128 / 160 kbps |
| Canais de Gravação | Mono / Estéreo |
| Caminho de Saída | Diretório para salvar gravações |

---

## Arquitetura Técnica

### Estrutura do Projeto

```
Quanta/
├── App.xaml / App.xaml.cs          # Ponto de entrada, bandeja, tema, ciclo de vida
├── Views/                           # Camada de visualização
│   ├── MainWindow.xaml              # Janela principal de busca
│   ├── RecordingOverlayWindow.xaml  # Janela de sobreposição de gravação
│   └── CommandSettingsWindow.xaml   # Janela de configurações (5 arquivos partial)
├── Domain/
│   ├── Search/                      # Motor de busca, Provider, tipos de resultado
│   │   ├── SearchEngine.cs
│   │   ├── ExchangeRateService.cs
│   │   ├── ApplicationSearchProvider.cs
│   │   ├── FileSearchProvider.cs
│   │   └── RecentFileSearchProvider.cs
│   └── Commands/                    # Roteamento de comandos, Handlers
│       ├── CommandRouter.cs
│       └── CommandHandlers.cs
├── Core/
│   ├── Config/                      # AppConfig, ConfigLoader
│   └── Services/                    # RecordingService, ToastService
├── Infrastructure/
│   ├── System/                      # Histórico da área de transferência, gerenciamento de janelas
│   └── Logging/                     # Registro
├── Presentation/
│   └── Helpers/                     # LocalizationService
└── Resources/
    └── Themes/                      # LightTheme.xaml, DarkTheme.xaml
```

### Stack Tecnológico

| Projeto | Tecnologia |
|---------|------------|
| Framework | .NET 8.0 / WPF |
| Arquitetura | MVVM (CommunityToolkit.Mvvm) |
| Linguagem | C# |
| Gravação | NAudio (WASAPI) |
| QR Code | QRCoder |
| Taxa de Câmbio | exchangerate-api.com |

---

## Autor

**yeal911** · yeal91117@gmail.com

## Licença

MIT License
