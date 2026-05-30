# LangKeep

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![Platform](https://img.shields.io/badge/Platform-Windows-0078D4)](https://www.microsoft.com/windows)

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

## 🚀 Quick Start

### Prerequisites

- Windows 10 or Windows 11
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### Build & Run

```bash
# Clone the repository
git clone https://github.com/your-org/langkeep.git
cd langkeep

# Build the solution
dotnet build

# Run the application
dotnet run --project src/LangKeep.UI.Wpf
```

### Run from Explorer

You can also double-click `src/LangKeep.UI.Wpf/bin/Debug/net9.0-windows/LangKeep.exe` after building.

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

## 📁 Data Storage

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

## 🧪 Running Tests

```bash
# Run all tests
dotnet test

# Run tests for a specific project
dotnet test tests/LangKeep.Core.Tests
dotnet test tests/LangKeep.Application.Tests
```

## 🗺️ Roadmap

For the full roadmap, see [docs/ROADMAP.md](docs/ROADMAP.md).

Highlights:

- **Per-window matching** based on window title
- **Browser extension** integration for tab-level language switching
- **macOS support** via CoreGraphics and InputMethodKit
- **Enhanced rule engine** with regex, glob patterns, and priority-based matching
- **Real-time rule evaluation preview** in the settings UI

## 🤝 Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## 📄 License

This project is licensed under the MIT License — see [LICENSE](LICENSE) for details.
