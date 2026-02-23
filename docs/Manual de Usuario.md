# Manual de Usuario de Quanta

<p align="center">
  <a href="用户手册.md">中文</a> | <a href="User Manual.md">English</a> | <b>Español</b>
</p>

---

## Tabla de Contenidos

1. [Inicio Rápido](#inicio-rápido)
2. [Operaciones Principales](#operaciones-principales)
3. [Comandos Integrados](#comandos-integrados)
4. [Entrada Inteligente](#entrada-inteligente)
5. [Grabación](#grabación)
6. [Historial del Portapapeles](#historial-del-portapapeles)
7. [Comandos Personalizados](#comandos-personalizados)
8. [Referencia de Configuración](#referencia-de-configuración)

---

## Inicio Rápido

### Requisitos

- Windows 10 / 11 (x64)
- [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

### Primeros Pasos

1. Ejecuta `Quanta.exe` — se minimiza automáticamente a la bandeja del sistema
2. Pulsa `Alt+R` (atajo por defecto) para abrir el cuadro de búsqueda
3. Escribe una palabra clave — los resultados aparecen en tiempo real
4. Pulsa `Enter` para ejecutar, `Esc` para cerrar la ventana

> Consejo: Haz clic derecho en el icono de la bandeja para acceder rápidamente a la configuración, cambiar el idioma o salir del programa.

---

## Operaciones Principales

### Atajos de Teclado

| Atajo | Acción |
|-------|--------|
| `Alt+R` | Mostrar / ocultar la ventana principal (personalizable en ajustes) |
| `Enter` | Ejecutar el comando seleccionado |
| `Tab` | Entrar en modo parámetro (añadir parámetros a un comando) |
| `Esc` | Salir del modo parámetro / ocultar la ventana |
| `↑` / `↓` | Mover la selección arriba / abajo |
| `Ctrl+1~9` | Ejecutar directamente el enésimo resultado |
| `Backspace` | Volver a la búsqueda normal desde el modo parámetro |

### Búsqueda Difusa

No necesitas escribir exactamente — Quanta admite coincidencia de prefijo difusa:

- Escribe `rec` → coincide con `record`, `regedit`, etc.
- Escribe `ps` → coincide con `powershell`
- Escribe `tk` → coincide con `taskmgr` (Administrador de tareas)

La búsqueda abarca palabras clave, nombres y descripciones. Los resultados se puntúan y agrupan por categoría.

### Modo Parámetro

Para comandos que admiten un marcador `{param}` (p. ej. Búsqueda en Google, Ping):

1. Localiza el comando deseado (p. ej. `g`)
2. Pulsa `Tab` para entrar en modo parámetro — el cuadro muestra `g >`
3. Escribe el parámetro (p. ej. `rust programming`)
4. Pulsa `Enter` — el parámetro se sustituye y la acción se ejecuta

### Ctrl+Número para Ejecución Rápida

Cada resultado muestra un índice del 1 al 9. Pulsa `Ctrl+1` para ejecutar el primero directamente, sin mover el cursor.

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
| `emptybin` | Vaciar papelera de reciclaje (inmediato, sin confirmación) |

### Diagnóstico de Red

| Palabra clave | Descripción |
|---------------|-------------|
| `ipconfig` | Ver configuración IP (se ejecuta en PowerShell) |
| `ping` | Ping (admite modo parámetro: `ping > 8.8.8.8`) |
| `tracert` | Rastrear ruta |
| `nslookup` | Consulta DNS |
| `netstat` | Ver conexiones de red |

### Gestión de Energía

| Palabra clave | Descripción |
|---------------|-------------|
| `lock` | Bloquear pantalla inmediatamente |
| `shutdown` | Apagar en 10 segundos (cancela con `shutdown /a` en la ventana abierta) |
| `restart` | Reiniciar en 10 segundos |
| `sleep` | Entrar en suspensión |

### Funciones Especiales

| Palabra clave | Descripción |
|---------------|-------------|
| `record` | Abrir el panel de grabación de Quanta |
| `clip` | Buscar en el historial del portapapeles |

### Comandos del Sistema Quanta

| Palabra clave | Descripción |
|---------------|-------------|
| `setting` | Abrir ajustes |
| `about` | Acerca de Quanta |
| `exit` | Salir de Quanta |
| `english` | Cambiar idioma de la interfaz a inglés de inmediato |
| `chinese` | Cambiar idioma de la interfaz a chino de inmediato |
| `spanish` | Cambiar idioma de la interfaz a español de inmediato |

---

## Entrada Inteligente

Escribe directamente en el cuadro de búsqueda — no es necesario seleccionar un comando. Quanta detecta automáticamente el tipo de entrada y muestra los resultados en tiempo real.

### Calculadora Matemática

Escribe cualquier expresión matemática:

| Entrada | Resultado |
|---------|-----------|
| `2+2*3` | `8` |
| `(100-32)/1.8` | `37.778` |
| `2^10` | `1024` |
| `15%4` | `3` |
| `3.14*5*5` | `78.5` |

Operadores admitidos: `+`, `-`, `*`, `/`, `%` (módulo), `^` (potencia), con paréntesis y decimales.

Pulsa `Enter` para copiar el resultado al portapapeles automáticamente.

### Conversión de Unidades

Formato: `valor unidad_origen to unidad_destino`

**Longitud**

| Entrada | Resultado |
|---------|-----------|
| `100 km to miles` | `62.137 miles` |
| `5 ft to cm` | `152.4 cm` |
| `1 inch to mm` | `25.4 mm` |

**Peso**

| Entrada | Resultado |
|---------|-----------|
| `1 kg to lbs` | `2.205 lbs` |
| `500 g to oz` | `17.637 oz` |

**Temperatura**

| Entrada | Resultado |
|---------|-----------|
| `100 c to f` | `212 °F` |
| `30 f to c` | `-1.111 °C` |

**Área / Volumen**

| Entrada | Resultado |
|---------|-----------|
| `1 acre to m2` | `4046.856 m²` |
| `1 gallon to l` | `3.785 l` |

### Conversión de Divisas

Formato: `valor divisa_origen to divisa_destino`

> Requiere una API Key de [exchangerate-api.com](https://www.exchangerate-api.com). Configúrala en Ajustes → Tipo de cambio.

| Entrada | Ejemplo de salida |
|---------|-------------------|
| `100 usd to cny` | `¥736.50 CNY` |
| `1000 cny to jpy` | `¥20,500 JPY` |
| `50 eur to gbp` | `£42.30 GBP` |

Admite más de 40 divisas. Las tasas se almacenan en caché local durante un tiempo configurable (por defecto: 1 hora). Al expirar, se actualiza automáticamente desde la API.

### Conversión de Colores

Admite HEX, RGB y HSL — convierte automáticamente entre los tres formatos con vista previa de color en tiempo real:

| Entrada | Salida |
|---------|--------|
| `#E67E22` | `rgb(230,126,34)  hsl(28,79%,52%)` |
| `rgb(230,126,34)` | `#E67E22  hsl(28,79%,52%)` |
| `hsl(28,79%,52%)` | `#E67E22  rgb(230,126,34)` |
| `255,165,0` | `#FFA500  hsl(38,100%,50%)` |

Pulsa `Enter` para copiar el valor HEX al portapapeles.

### Herramientas de Texto

| Prefijo | Función | Ejemplo de entrada | Resultado |
|---------|---------|--------------------|-----------|
| `base64 texto` | Codificar en Base64 | `base64 hello` | `aGVsbG8=` |
| `base64d texto` | Decodificar Base64 | `base64d aGVsbG8=` | `hello` |
| `md5 texto` | Hash MD5 | `md5 hello` | `5d41402abc4b2a76b9719d911017c592` |
| `sha256 texto` | Hash SHA-256 | `sha256 hello` | `2cf24dba5fb0a30e...` |
| `url texto` | Codificar URL (decodifica automáticamente si ya está codificado) | `url hello world` | `hello%20world` |
| `json JSON` | Formatear JSON | `json {"a":1,"b":2}` | JSON formateado en múltiples líneas |

Pulsa `Enter` para copiar el resultado al portapapeles.

### Búsqueda en Google

Formato: `g palabra_clave`

```
g rust programming  →  Abre la búsqueda de Google en el navegador predeterminado
```

También puedes escribir `g`, pulsar `Tab` para entrar en modo parámetro y luego escribir el término de búsqueda.

### Ejecución de PowerShell

Prefija con `>` para ejecutar cualquier comando de PowerShell directamente:

```
> Get-Process       →  Lista todos los procesos en ejecución
> dir C:\           →  Lista el contenido de la unidad C:
> ping 8.8.8.8      →  Ejecutar ping
```

### Generación de Código QR

Escribe texto más largo que el umbral (por defecto: 20 caracteres, ajustable en la configuración) para generar automáticamente un código QR y copiarlo al portapapeles. Una notificación toast confirma la copia.

```
https://github.com/yeal911/Quanta   →  Código QR generado y copiado automáticamente
```

---

## Grabación

Escribe `record` para abrir el panel de grabación (se muestra como una barra flotante en la parte superior de la pantalla).

### Parámetros de Grabación

| Parámetro | Opciones | Recomendado |
|-----------|----------|-------------|
| Fuente | Micrófono / Altavoz (audio del sistema) / Ambos | Según el caso de uso |
| Formato | m4a (AAC) / mp3 | m4a (mejor calidad) |
| Tasa de bits | 32 / 64 / 96 / 128 / 160 kbps | 32 kbps para reuniones, 128 para música |
| Canales | Mono / Estéreo | Mono para reuniones |

> Consejo para reuniones: m4a + 32 kbps + Mono → ~14 MB por hora.

### Controles de Grabación

1. Haz clic en **Iniciar** para comenzar — el panel muestra el tiempo transcurrido
2. Haz clic en **Pausar** para pausar; haz clic en **Reanudar** para continuar
3. Haz clic en **Detener** para finalizar — el archivo se guarda automáticamente en el directorio configurado
4. **Haz clic derecho en cualquier parámetro** (Fuente, Formato, Tasa de bits, Canales) para cambiar ese parámetro en cualquier momento, incluso durante la grabación

### Archivos de Salida

El directorio de guardado se configura en Ajustes → Grabación → Ruta de salida. Los nombres de archivo siguen el patrón `Grabacion_AAAAMMDD_HHmmss.m4a` (o `.mp3`).

---

## Historial del Portapapeles

Quanta monitorea y registra silenciosamente todo el texto que copies al portapapeles.

### Cómo Usar

- Escribe `clip` → muestra las 20 entradas más recientes del portapapeles
- Escribe `clip palabra_clave` → filtra el historial por palabra clave
- Haz clic en cualquier resultado → copia ese texto de vuelta al portapapeles

### Notas

- Almacena hasta 50 entradas; las más antiguas se eliminan al alcanzar el límite
- El historial persiste entre reinicios (guardado localmente)
- Solo se registra texto plano — las imágenes y los archivos no se capturan

---

## Comandos Personalizados

### Abrir la Gestión de Comandos

Escribe `setting` o haz clic derecho en el icono de la bandeja → Ajustes → pestaña **Comandos**.

### Tipos de Comandos

| Tipo | Descripción | Ejemplo de ruta |
|------|-------------|-----------------|
| `Url` | Abrir una URL en el navegador, admite marcador `{param}` | `https://google.com/search?q={param}` |
| `Program` | Lanzar un ejecutable | `C:\Windows\notepad.exe` o `notepad.exe` |
| `Shell` | Ejecutar un comando en cmd, admite `{param}` | `ping {param}` |
| `Directory` | Abrir una carpeta en el Explorador | `C:\Users\Public\Downloads` |
| `Calculator` | Evaluar una expresión (uso avanzado) | `{param}` |

### Ejemplos de Adición de Comandos

**Añadir un comando "Abrir carpeta de proyecto":**
1. Haz clic en Añadir
2. Palabra clave: `proy`, Nombre: `Mi Proyecto`, Tipo: `Directory`, Ruta: `D:\Proyectos`
3. Haz clic en Guardar
4. Escribe `proy` en el cuadro de búsqueda para abrir la carpeta al instante

**Añadir un comando "Búsqueda en Bing":**
1. Palabra clave: `bing`, Nombre: `Búsqueda Bing`, Tipo: `Url`, Ruta: `https://www.bing.com/search?q={param}`
2. Tras guardar, busca `bing`, pulsa `Tab`, escribe tu consulta y pulsa `Enter`

### Importar / Exportar

En la pestaña Comandos, exporta todos los comandos personalizados como un archivo JSON para copia de seguridad o migración entre equipos.

---

## Referencia de Configuración

Abre desde: cuadro de búsqueda → `setting`, o clic derecho en el icono de bandeja → Ajustes.

### General

| Ajuste | Descripción |
|--------|-------------|
| Atajo | Atajo global (por defecto `Alt+R`) — haz doble clic en el campo y pulsa la combinación deseada |
| Tema | Claro / Oscuro |
| Idioma | Chino / Inglés / Español — efecto inmediato |
| Inicio con Windows | Ejecutar Quanta automáticamente al iniciar el sistema |
| Máx. resultados | Número máximo de resultados de búsqueda a mostrar |
| Umbral código QR | Número de caracteres que activa la generación de código QR (por defecto: 20) |

### Tipo de Cambio

| Ajuste | Descripción |
|--------|-------------|
| Clave API Tipo de Cambio | API Key gratuita de [exchangerate-api.com](https://www.exchangerate-api.com) |
| Duración de Caché | Tiempo durante el cual usar las tasas en caché antes de obtener datos nuevos (en horas, por defecto: 1) |

### Grabación

| Ajuste | Descripción |
|--------|-------------|
| Fuente de grabación | Micrófono / Altavoz / Ambos |
| Formato de grabación | m4a (AAC, recomendado) / mp3 |
| Tasa de bits | 32 / 64 / 96 / 128 / 160 kbps |
| Canales | Mono / Estéreo |
| Ruta de salida | Directorio para las grabaciones — haz clic en Examinar para elegir |

---

## Preguntas Frecuentes

**P: Alt+R no abre la ventana — ¿qué hago?**
R: Otra aplicación puede estar usando Alt+R. Abre Ajustes y cambia el atajo por otra combinación (p. ej., `Alt+Espacio`).

**P: La conversión de divisas siempre muestra "Por favor configure la API Key"?**
R: Ve a Ajustes → panel Tipo de cambio, introduce tu API Key gratuita de exchangerate-api.com y guarda.

**P: ¿Dónde se guardan mis grabaciones?**
R: Por defecto, en la carpeta Documentos. Puedes cambiarlo en Ajustes → Grabación → Ruta de salida.

**P: ¿Cómo cancelo un apagado programado?**
R: Tras ejecutar el comando `shutdown`, se abre una ventana de PowerShell con una cuenta regresiva de 10 segundos. Ejecuta `shutdown /a` en esa ventana antes de que finalice la cuenta para cancelarlo.

---

*Autor: **yeal911** · yeal91117@gmail.com · Licencia MIT*
