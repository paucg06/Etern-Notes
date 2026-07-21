# Etern-Notes 🚀 (Cross-Platform Edition)

Una aplicación de escritorio moderna, elegante y ligera construida para desarrolladores, creadores y diseñadores que necesitan organizar sus proyectos, tareas y notas en un solo lugar.

![Cross-Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Linux%20%7C%20macOS-blue.svg)
![C# Avalonia UI](https://img.shields.io/badge/C%23-Avalonia%20UI%2011-purple.svg)
![License](https://img.shields.io/badge/License-MIT-green.svg)

---

## 🌍 Compatibilidad Multiplataforma

Etern-Notes está preparado para ejecutarse de forma nativa en:

- 🪟 **Windows**: Executable Standalone (`EternNotes.exe`)
- 🐧 **Linux / Ubuntu**: Binario nativo o AppImage (`EternNotes`)
- 🍎 **macOS**: Soporte para Intel y Apple Silicon M1/M2/M3/M4 (`EternNotes.app`)

---

## 🎨 Características principales

- **📁 Gestión de Proyectos**: Organiza tus trabajos por carpetas o categorías independientes.
- **📋 Tablero Kanban Personalizado**: Columnas configurables con colores personalizados para cada flujo de trabajo.
- **✅ Control de Tareas y Subtareas**: Asigna prioridades (Alta, Media, Baja), fechas límite, etiquetas y sublistas de tareas.
- **🎨 Interfaz Oscura y Moderna**: Diseño elegante con estética Fluent/VSCode para evitar fatiga visual.
- **⚡ Standalone y Ultra Ligero**: Los binarios de cada plataforma son independientes y no requieren configuraciones complejas.

---

## 💻 Compilación Local (C# / Avalonia)

Para compilar la versión multiplataforma en cualquier sistema operativo que tenga el SDK de `.NET 8`:

### Windows
```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### Linux (Ubuntu, Debian, Fedora, Arch)
```bash
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true
```

### macOS (Intel / Apple Silicon)
```bash
# Para Apple Silicon (M1/M2/M3/M4)
dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true

# Para procesadores Intel
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true
```

---

## ⚙️ Compilación Automática (GitHub Actions)

El repositorio incluye un flujo de trabajo de GitHub Actions (`.github/workflows/build.yml`). Cada vez que haces `git push`, GitHub compila automáticamente los binarios nativos para **Windows**, **Linux** y **macOS** y los sube a la pestaña **Actions** de tu repositorio listo para descargar.

---

## 📝 Licencia

Este proyecto está bajo la licencia MIT.
