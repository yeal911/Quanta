# Quanta - Lanzador Rápido para Windows

<p align="center">
  <a href="README.md">中文</a> | <a href="README_EN.md">English</a> | <b>Español</b>
</p>

<p align="center">
  <img src="Resources\imgs\quanta.ico" width="64" alt="Quanta"/>
</p>

<p align="center">
  Un lanzador ligero para Windows: actívalo con una tecla rápida, búsqueda difusa, ejecución instantánea.
</p>

---

## Características

### Principales

- **Tecla rápida global** — `Alt+R` por defecto, totalmente personalizable
- **Búsqueda difusa** — coincide con palabra clave, nombre y descripción; ordenada por puntuación + uso
- **Comandos personalizados** — soporta tipos Url, Program, Shell, Directory, Calculator
- **Modo de parámetros** — presiona `Tab` para pasar argumentos (ej., `g > rust`)
- **Ctrl+Número** — `Ctrl+1~9` para ejecutar instantáneamente por posición
- **Ejecución con un clic** — haz clic en cualquier resultado para ejecutarlo
- **Ocultar automático al perder foco** — al hacer clic en otra ventana se oculta Quanta

### Comandos Integrados

| Palabra Clave | Descripción | Palabra Clave | Descripción |
|---------------|-------------|---------------|-------------|
| `cmd` | Símbolo del sistema | `powershell` | PowerShell |
| `notepad` | Bloc de notas | `calc` | Calculadora |
| `explorer` | Explorador de archivos | `taskmgr` | Administrador de tareas |
| `control` | Panel de control | `regedit` | Editor del registro |
| `services` | Servicios | `devmgmt` | Administrador de dispositivos |
| `ping` | Ping | `ipconfig` | Configuración IP |
| `tracert` | Traza de ruta | `nslookup` | Búsqueda DNS |
| `netstat` | Estado de red | `mspaint` | Paint |
| `lock` | Bloquear pantalla | `shutdown` | Apagar |
| `restart` | Reiniciar | `sleep` | Suspender |
| `emptybin` | Vaciar papelera | `winrecord` | Grabadora de voz |
| `b` | Búsqueda Baidu | `g` | Búsqueda Google |
| `gh` | Búsqueda GitHub | | |

### Características Especiales

- **Calculadora** — escribe expresiones matemáticas directamente (ej., `2+2`, `sqrt(16)*3`)
- **Conversión de unidades** — soporta longitud, peso, temperatura, área, volumen (ej., `100 km to miles`, `100 c to f`, `1 kg to lbs`)
- **Conversión de moneda** — consulta tipos de cambio (ej., `100 usd to cny`), requiere API Key
- **Historial del portapapeles** — escribe `clip` para ver el historial con filtrado de búsqueda
- **Grabación de pantalla** — escribe `record` para iniciar grabación (micrófono/altavoz), soporta m4a/mp3
- **Generación de código QR** — texto de más de 20 caracteres genera automáticamente QR, copiado al portapapeles

### Interfaz y Experiencia

- **Tema Claro / Oscuro** — cambia con el icono de arriba a la derecha, configuración guardada automáticamente
- **Multilingüe** — 中文 / English / Español, cambio instantáneo
- **Bandeja del sistema** — se ejecuta minimizado; clic derecho para menú
- **Inicio automático** — configurar para ejecutar al iniciar Windows
- **Notificaciones Toast** — retroalimentación de éxito/error

---

## Inicio Rápido

### Requisitos

- Windows 10 / 11 (x64)
- [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

### Ejecutar

```bash
dotnet run
```

### Publicar

```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

---

## Uso

### Flujo Básico

1. Inicia Quanta — se oculta en la bandeja del sistema
2. Presiona `Alt+R` para abrir la caja de búsqueda
3. Escribe una palabra clave (ej., `g`, `notepad`, `clip`)
4. Navega con teclas de dirección o `Ctrl+Número`
5. Presiona `Enter` para ejecutar, `Esc` para ocultar

### Modo de Parámetros

Después de hacer coincidir un comando, presiona `Tab` para pasar argumentos:

```
Escribe: g          → coincide con "Búsqueda Google"
Presiona Tab → g >     → entrar en modo de parámetros
Escribe: g > rust   → ejecuta Búsqueda Google para "rust"
```

### Calculadora y Conversiones

```
Escribe: 2+2*3      → Resultado: 8
Escribe: sqrt(16)   → Resultado: 4
Escribe: 100 km to miles → Conversión de unidades
Escribe: 100 c to f     → Temperatura: 212 °F
Escribe: 1 kg to lbs    → Peso: 2.205 lbs
Escribe: 100 usd to cny → Conversión de moneda
```

### Atajos de Teclado

| Atajo | Acción |
|-------|--------|
| `Alt+R` | Mostrar / ocultar ventana principal |
| `Enter` | Ejecutar comando seleccionado |
| `Tab` | Entrar en modo de parámetros |
| `Esc` | Salir del modo de parámetros / ocultar ventana |
| `↑` / `↓` | Navegar resultados |
| `Ctrl+1~9` | Ejecutar resultado en esa posición |
| `Backspace` | En modo parámetros: volver a búsqueda normal |

### Gestión de Comandos

- **Clic derecho en icono de bandeja** → Abrir Configuración de Comandos
- Editar palabra clave, nombre, tipo, ruta directamente en la tabla
- Importar / Exportar como JSON para respaldo

### Configuración

| Ajuste | Descripción |
|--------|-------------|
| Tecla rápida | Tecla rápida global personalizada |
| Tema | Claro / Oscuro |
| Idioma | 中文 / English / Español |
| Inicio automático | Ejecutar al iniciar Windows |
| Máx. resultados | Límite de resultados de búsqueda |
| Umbral QR | Caracteres para activar QR |

---

## Configuración

El archivo de configuración es `config.json` en el directorio de la aplicación.

### Campos de Comando

| Campo | Descripción |
|-------|-------------|
| `Keyword` | Palabra clave de búsqueda |
| `Name` | Nombre a mostrar |
| `Type` | Url / Program / Shell / Directory / Calculator |
| `Path` | URL o ruta ejecutable; soporta `{param}` |
| `Arguments` | Argumentos de lanzamiento |
| `RunAsAdmin` | Ejecutar con privilegios elevados |
| `RunHidden` | Ocultar consola/ventana |

### Tipos de Comando

| Tipo | Ejemplo | Resultado |
|------|---------|-----------|
| `Url` | `https://google.com/search?q={param}` | Abre en navegador |
| `Program` | `notepad.exe` | Lanza ejecutable |
| `Shell` | `ping {param}` | Ejecuta vía cmd |
| `Directory` | `C:\Users\Public` | Abre en Explorador |
| `Calculator` | `{param}` | Evalúa expresión |

### Ejemplo de Configuración

```json
{
  "Hotkey": {
    "Modifier": "Alt",
    "Key": "R"
  },
  "Theme": "Light",
  "Language": "es-ES",
  "Commands": [
    {
      "Keyword": "g",
      "Name": "Búsqueda Google",
      "Type": "Url",
      "Path": "https://www.google.com/search?q={param}"
    },
    {
      "Keyword": "code",
      "Name": "VS Code",
      "Type": "Program",
      "Path": "C:\\Users\\TuNombre\\AppData\\Local\\Programs\\Microsoft VS Code\\Code.exe"
    }
  ]
}
```

---

## Arquitectura

### Estructura del Proyecto

```
Quanta/
├── App.xaml / App.xaml.cs     # Punto de entrada
├── Quanta.csproj              # Config del proyecto
├── config.json                # Configuración de usuario
├── Views/                     # Capa de interfaz
│   ├── MainWindow.xaml        # Ventana principal de búsqueda
│   ├── SettingsWindow.xaml    # Ventana de configuración
│   └── CommandSettingsWindow.xaml  # Gestión de comandos
├── ViewModels/                # Modelos de vista
├── Models/                    # Modelos de datos
├── Services/                  # Capa de servicios
├── Helpers/                   # Utilidades
├── Infrastructure/            # Infraestructura
│   ├── Storage/              # Almacenamiento de config
│   ├── Logging/              # Servicio de logging
│   └── System/               # Integración del sistema
└── Resources/                 # Recursos
    └── Themes/                # Archivos de tema
```

### Stack Tecnológico

| Elemento | Tecnología |
|----------|------------|
| Framework | .NET 8.0 / WPF |
| Arquitectura | MVVM |
| Lenguaje | C# |
| UI | WPF |

### Dependencias

| Paquete | Versión | Propósito |
|---------|---------|-----------|
| CommunityToolkit.Mvvm | 8.2.2 | Framework MVVM |
| System.Drawing.Common | 8.0.0 | Procesamiento gráfico |

---

## Autor

**yeal911** · yeal91117@gmail.com

## Licencia

MIT License
