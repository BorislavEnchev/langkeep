# LangKeep

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![Platform](https://img.shields.io/badge/Platform-Windows-0078D4)](https://www.microsoft.com/windows)
[![Release](https://img.shields.io/github/v/release/BorislavEnchev/langkeep?include_prereleases&sort=semver)](https://github.com/BorislavEnchev/langkeep/releases)
[![Microsoft Store](https://img.shields.io/badge/Microsoft%20Store-9NFNVQ97185F-0078D4)](https://www.microsoft.com/store/apps/9NFNVQ97185F)

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

### Option 1 — Microsoft Store (Recommended)

Get it from the [Microsoft Store](https://www.microsoft.com/store/apps/9NFNVQ97185F) for one-click install, automatic updates, and no security warnings.

### Option 2 — MSIX Installer (GitHub Releases)

Download the latest MSIX from the [releases page](https://github.com/BorislavEnchev/langkeep/releases/latest).

1. Download `LangKeep-{version}-x64.msix`.
2. Open the downloaded file.
3. Click **Install**.
4. Launch LangKeep from the Start Menu.

> **Note**: MSIX packages from GitHub Releases are self-signed. If installation is blocked, enable **Developer Mode** in Windows Settings → Privacy & security → For developers.

### Option 3 — Portable Version

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
git clone https://github.com/BorislavEnchev/langkeep.git
cd langkeep
dotnet build
dotnet run --project src/LangKeep.UI.Wpf
```

---

## 🔧 How It Works

1. **Active Window Monitoring** — Uses `SetWinEventHook` (Win32) to detect foreground window changes.
2. **Layout Detection** — Polls `GetKeyboardLayout` every 500ms.
3. **Rule Matching** — Evaluates saved preferences by process name.
4. **Layout Switching** — Uses `ActivateKeyboardLayout` to restore the preferred layout.
5. **Automatic Learning** — Saves your preference when you manually change layouts.

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

### Commands

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/LangKeep.UI.Wpf
```

### Release Process

```bash
git tag v0.2.2
git push origin v0.2.2
```

GitHub Actions automatically builds, packages, and publishes the release. See [docs/releasing.md](docs/releasing.md) for details.

---

## 🗺️ Roadmap

For the full roadmap, see [docs/ROADMAP.md](docs/ROADMAP.md).

---

## 🤝 Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## 📄 License

This project is licensed under the MIT License — see [LICENSE](LICENSE) for details.
