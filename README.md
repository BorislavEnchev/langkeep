# LangKeep

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![Platform](https://img.shields.io/badge/Platform-Windows-0078D4)](https://www.microsoft.com/windows)
[![Release](https://img.shields.io/github/v/release/BorislavEnchev/langkeep?include_prereleases&sort=semver)](https://github.com/BorislavEnchev/langkeep/releases)

**LangKeep** is a Windows tray application that automatically remembers and restores your preferred keyboard input language for each application.

Tired of manually switching between keyboard layouts every time you switch from Visual Studio to Teams to Chrome? LangKeep handles it for you — automatically.

---

## ✨ Features

- **🧠 Automatic Language Restoration** — Switch focus between apps, and LangKeep activates the right keyboard layout automatically.
- **🎯 Per-Application Learning** — Change the layout manually while an app is focused, and LangKeep remembers your preference.
- **🖥️ System Tray Integration** — Minimal, unobtrusive tray icon with a clean context menu.
- **⚙️ Settings UI** — View, add, edit, delete, export, and import language preferences.
- **🚀 Start with Windows** — Optional auto-start integration.
- **📝 Structured Logging** — Detailed logging via `ILogger<T>` for troubleshooting.
- **🔓 Open Source** — MIT-licensed. Contributions welcome.
- **💻 Windows 10/11 Support** — Runs on all modern Windows versions.

---

## 📥 Installation

### Option 1 — MSIX Installer (Recommended)

Download the latest MSIX package from the [releases page](https://github.com/BorislavEnchev/langkeep/releases/latest).

1. Download `LangKeep-{version}-x64.msix`.
2. Open the downloaded file.
3. Click **Install**.
4. Launch LangKeep from the Start Menu.

> **Note**: The MSIX package is unsigned. If installation is blocked, enable **Developer Mode** in Windows Settings → Privacy & security → For developers.

### Option 2 — Portable Version

Download the latest ZIP from the [releases page](https://github.com/BorislavEnchev/langkeep/releases/latest).

1. Download `LangKeep-{version}-portable-x64.zip`.
2. Extract the ZIP to any folder.
3. Run `LangKeep.exe`.

No installation required — perfect for USB drives or temporary use.

### Detailed Instructions

For full installation details, including troubleshooting, see [docs/installation.md](docs/installation.md).

---

## 🚀 Quick Start (from Source)

### Prerequisites

- Windows 10 or Windows 11
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### Build & Run

```bash
# Clone the repository
git clone https://github.com/BorislavEnchev/langkeep.git
cd langkeep

# Build the solution
dotnet build

# Run the application
dotnet run --project src/LangKeep.UI.Wpf
```

### Run from Explorer

You can also double-click `src/LangKeep.UI.Wpf/bin/Debug/net9.0-windows/LangKeep.exe` after building.

---

## 🏗️ Architecture

LangKeep follows **Clean Architecture** principles with clear separation of concerns:

```
LangKeep.sln
├── src/
│   ├── LangKeep.Core/              # Domain models & interfaces (platform-agnostic)
│   ├── LangKeep.Application/       # Use cases & application services
│   ├── LangKeep.Infrastructure.Windows/  # Win32 interop, persistence, startup
│   └── LangKeep.UI.Wpf/            # WPF tray application (MVVM)
├── tests/
│   ├── LangKeep.Core.Tests/        # Domain model unit tests
│   └── LangKeep.Application.Tests/ # Application service unit tests
└── spikes/
    └── LangKeep.Spike/             # Exploratory Win32 validation project
```

For detailed architecture documentation, see [docs/architecture/ARCHITECTURE.md](docs/architecture/ARCHITECTURE.md).

## 🔧 How It Works

1. **Active Window Monitoring** — LangKeep uses `SetWinEventHook` (Win32 API) to detect when the foreground window changes.
2. **Layout Detection** — A polling timer checks `GetKeyboardLayout` for the current window's thread every 500ms.
3. **Rule Matching** — When a window switch is detected, LangKeep evaluates matching rules by process name.
4. **Layout Switching** — If a matching rule is found, LangKeep uses `ActivateKeyboardLayout` to switch to the preferred layout.
5. **Automatic Learning** — When the user manually changes the keyboard layout, LangKeep saves this preference for the active application.

## 💾 Data Storage

Preferences are stored as JSON at:

```
%AppData%\LangKeep\preferences.json
```

Example:

```json
{
  "version": 1,
  "preferences": [
    { "processName": "Code.exe", "languageTag": "en-US", "isEnabled": true, "sortOrder": 0 },
    { "processName": "Teams.exe", "languageTag": "de-DE", "isEnabled": true, "sortOrder": 1 }
  ]
}
```

---

## 🧪 Running Tests

```bash
# Run all tests
dotnet test

# Run tests for a specific project
dotnet test tests/LangKeep.Core.Tests
dotnet test tests/LangKeep.Application.Tests
```

---

## 🧑‍💻 Development

### Prerequisites

- Windows 10 or Windows 11
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (recommended) or any .NET-compatible editor

### Commands

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run all tests
dotnet test

# Run the application
dotnet run --project src/LangKeep.UI.Wpf
```

### Project Structure

```
src/
├── LangKeep.Core/                 # Domain models, value objects, interfaces
├── LangKeep.Application/          # Services, use cases, event orchestration
├── LangKeep.Infrastructure.Windows/  # Win32 interop, persistence, startup
└── LangKeep.UI.Wpf/               # WPF tray application, MVVM

tests/
├── LangKeep.Core.Tests/           # Domain model unit tests
└── LangKeep.Application.Tests/    # Application service unit tests
```

### Release Process

To create a new release:

```bash
# Tag the release
git tag v0.1.0
git push origin v0.1.0
```

GitHub Actions automatically builds, packages, and publishes the release. See [docs/releasing.md](docs/releasing.md) for details.

---

## 🗺️ Roadmap

For the full roadmap, see [docs/ROADMAP.md](docs/ROADMAP.md).

Highlights:

- **Per-window matching** based on window title
- **Browser extension** integration for tab-level language switching
- **macOS support** via CoreGraphics and InputMethodKit
- **Enhanced rule engine** with regex, glob patterns, and priority-based matching
- **Real-time rule evaluation preview** in the settings UI

---

## 🤝 Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## 📄 License

This project is licensed under the MIT License — see [LICENSE](LICENSE) for details.
