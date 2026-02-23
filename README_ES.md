# Quanta - Lanzador Rápido para Windows

<p align="center">
  <a href="README.md">中文</a> | <a href="README_EN.md">English</a> | <b>Español</b>
</p>

<p align="center">
  <img src="Resources\imgs\quanta.ico" width="64" alt="Quanta"/>
</p>

<p align="center">
  Un lanzador rápido y ligero para Windows. Invócalo con un atajo global, búsqueda difusa, ejecución instantánea.
</p>

---

## Características

### Núcleo

- **Atajo global** - Por defecto `Alt+R`, personalizable en ajustes
- **Búsqueda difusa** - Coincidencia inteligente en clave / nombre / descripción, con prefijo difuso (escribe `rec` para encontrar `record`)
- **Resultados agrupados** - Organizados por categoría, ordenados por puntuación dentro de cada grupo
- **Comandos personalizados** - Tipos Url, Program, Shell, Directory, Calculator
- **Modo parámetro** - Pulsa `Tab` para introducir parámetros (p. ej. `g > rust` busca en Google)
- **Ctrl+Número** - `Ctrl+1~9` para ejecutar directamente el enésimo resultado
- **Ocultar al perder foco** - Se oculta automáticamente al hacer clic en otra ventana

---

## Comandos Integrados

Todos los comandos integrados admiten búsqueda difusa.

### Línea de Comandos

| Palabra clave | Descripción |
|---------------|-------------|
| `cmd` | Abrir símbolo del sistema |
| `powershell` | Abrir PowerShell |

### Aplicaciones

| Palabra clave | Descripción |
|---------------|-------------|
| `notepad` | Bloc de notas |
| `calc` | Calculadora de Windows |
| `mspaint` | Paint |
| `explorer` | Explorador de archivos |
| `winrecord` | Grabadora de voz integrada de Windows |

### Gestión del Sistema

| Palabra clave | Descripción |
|---------------|-------------|
| `taskmgr` | Administrador de tareas |
| `devmgmt` | Administrador de dispositivos |
| `services` | Servicios |
| `regedit` | Editor del registro |
| `control` | Panel de control |
| `emptybin` | Vaciar papelera de reciclaje |

### Diagnóstico de Red

| Palabra clave | Descripción |
|---------------|-------------|
| `ipconfig` | Ver configuración IP |
| `ping` | Ping (se ejecuta en PowerShell) |
| `tracert` | Rastrear ruta |
| `nslookup` | Consulta DNS |
| `netstat` | Ver conexiones de red |

### Gestión de Energía

| Palabra clave | Descripción |
|---------------|-------------|
| `lock` | Bloquear pantalla |
| `shutdown` | Apagar en 10 segundos |
| `restart` | Reiniciar en 10 segundos |
| `sleep` | Entrar en suspensión |

### Funciones Especiales

| Palabra clave | Descripción |
|---------------|-------------|
| `record` | Iniciar grabación Quanta |
| `clip` | Buscar historial del portapapeles |

### Comandos del Sistema Quanta

| Palabra clave | Descripción |
|---------------|-------------|
| `setting` | Abrir ajustes |
| `about` | Acerca de |
| `exit` | Salir de Quanta |
| `english` | Cambiar idioma a inglés |
| `chinese` | Cambiar idioma a chino |
| `spanish` | Cambiar idioma a español |

---

## Entrada Inteligente

Escribe directamente en el cuadro de búsqueda — los resultados aparecen en tiempo real.

### Calculadora Matemática

Escribe cualquier expresión matemática:

```
2+2*3         → 8
(100-32)/1.8  → 37.778
2^10          → 1024
15%4          → 3
```

Operadores admitidos: `+ - * / % ^`, paréntesis y decimales.

### Conversión de Unidades

Formato: `valor unidad_origen to unidad_destino`

```
100 km to miles    → 62.137 miles
100 c to f         → 212 °F
30 f to c          → -1.111 °C
1 kg to lbs        → 2.205 lbs
500 g to oz        → 17.637 oz
1 gallon to l      → 3.785 l
1 acre to m2       → 4046.856 m²
```

Categorías admitidas: longitud, peso, temperatura, área, volumen

### Conversión de Divisas

Formato: `valor divisa_origen to divisa_destino` (requiere API Key en ajustes)

```
100 usd to cny     → ¥736.50 CNY
1000 cny to jpy    → ¥20,500 JPY
50 eur to gbp      → £42.30 GBP
```

Admite 40+ divisas. Tasas en caché local (duración configurable, 1 hora por defecto).

### Conversión de Colores

Admite HEX, RGB, HSL — convierte automáticamente entre formatos con vista previa:

```
#E67E22            → rgb(230,126,34)  hsl(28,79%,52%)
rgb(230,126,34)    → #E67E22  hsl(28,79%,52%)
hsl(28,79%,52%)    → #E67E22  rgb(230,126,34)
255,165,0          → #FFA500  hsl(38,100%,50%)
```

### Herramientas de Texto

| Prefijo | Función | Ejemplo |
|---------|---------|---------|
| `base64 texto` | Codificar en Base64 | `base64 hello` → `aGVsbG8=` |
| `base64d texto` | Decodificar Base64 | `base64d aGVsbG8=` → `hello` |
| `md5 texto` | Hash MD5 | `md5 hello` → `5d41402a...` |
| `sha256 texto` | Hash SHA-256 | `sha256 hello` → `2cf24dba...` |
| `url texto` | Codificar URL / decodificar auto | `url hello world` → `hello%20world` |
| `json JSON` | Formatear JSON | `json {"a":1}` → salida formateada |

### Búsqueda en Google

Formato: `g palabra_clave`

```
g rust programming  → Abre Google en el navegador
```

### Ejecución PowerShell

Prefijo `>` para ejecutar un comando de PowerShell directamente:

```
> Get-Process      → Lista todos los procesos
> dir C:\          → Lista la unidad C:
```

### Generación de Código QR

Escribe texto más largo que el umbral (20 caracteres por defecto) para generar automáticamente un código QR copiado al portapapeles:

```
https://github.com/yeal911/Quanta   → Código QR generado automáticamente
```

---

## Grabación

Escribe `record` para abrir el panel de grabación:

- **Fuente**: Micrófono / Altavoz (audio del sistema) / Micrófono + Altavoz
- **Formato**: m4a (AAC) / mp3
- **Tasa de bits**: 32 / 64 / 96 / 128 / 160 kbps
- **Canales**: Mono / Estéreo
- **Controles**: Iniciar → Pausar / Reanudar → Detener; guardado en el directorio configurado
- **Clic derecho en los parámetros** para cambiar cualquier parámetro en cualquier momento

---

## Historial del Portapapeles

Escribe `clip` o `clip palabra_clave` para buscar el historial:

- Registra automáticamente el texto copiado, hasta 50 entradas
- Filtrado por palabra clave, muestra las 20 más recientes
- Haz clic en cualquier resultado para copiarlo al portapapeles
- El historial persiste entre reinicios

---

## Interfaz y Experiencia

- **Tema claro / oscuro** - Cambia en ajustes, guardado automático
- **Multilenguaje** - Chino / Inglés / Español, efecto inmediato
- **Bandeja del sistema** - Permanece en la bandeja, menú contextual
- **Inicio con Windows** - Actívalo en ajustes
- **Notificaciones Toast** - Feedback inmediato de éxito / error

---

## Inicio Rápido

### Requisitos

- Windows 10 / 11 (x64)
- [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

### Ejecutar

```bash
dotnet run
```

### Publicar Archivo Único

```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

---

## Atajos de Teclado

| Atajo | Acción |
|-------|--------|
| `Alt+R` | Mostrar / ocultar ventana principal |
| `Enter` | Ejecutar comando seleccionado |
| `Tab` | Entrar en modo parámetro |
| `Esc` | Salir del modo parámetro / ocultar ventana |
| `↑` / `↓` | Mover selección arriba / abajo |
| `Ctrl+1~9` | Ejecutar directamente el enésimo resultado |
| `Backspace` | Volver a búsqueda normal desde modo parámetro |

---

## Gestión de Comandos

Abre ajustes (comando `setting` o clic derecho en bandeja) → pestaña **Comandos**:

- Edita clave, nombre, tipo, ruta directamente en la tabla
- Importa / exporta en JSON para copia de seguridad y migración

### Tipos de Comando

| Tipo | Descripción | Ejemplo |
|------|-------------|---------|
| `Url` | Abrir URL en navegador (admite `{param}`) | `https://google.com/search?q={param}` |
| `Program` | Lanzar programa | `notepad.exe` |
| `Shell` | Ejecutar en cmd | `ping {param}` |
| `Directory` | Abrir carpeta en Explorer | `C:\Users\Public` |
| `Calculator` | Evaluar expresión | `{param}` |

---

## Referencia de Ajustes

| Ajuste | Descripción |
|--------|-------------|
| Atajo | Atajo global (por defecto `Alt+R`) |
| Tema | Claro / Oscuro |
| Idioma | Chino / Inglés / Español |
| Inicio con Windows | Arranque automático |
| Máx. resultados | Límite de resultados de búsqueda |
| Umbral código QR | Caracteres para activar generación QR |
| Clave API Tipo de Cambio | Clave API de exchangerate-api.com |
| Caché Tipo de Cambio | Duración de caché en horas (1 hora por defecto) |
| Fuente de grabación | Micrófono / Altavoz / Ambos |
| Formato de grabación | m4a / mp3 |
| Tasa de bits | 32 / 64 / 96 / 128 / 160 kbps |
| Canales | Mono / Estéreo |
| Ruta de salida | Directorio para las grabaciones |

---

## Autor

**yeal911** · yeal91117@gmail.com

## Licencia

MIT License
