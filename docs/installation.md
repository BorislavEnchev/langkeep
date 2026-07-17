# Installation

LangKeep is available from the **Microsoft Store**, as an **MSIX Installer**, or as a **Portable ZIP**.

---

## System Requirements

- **Operating System**: Windows 10 (version 1809 or later) or Windows 11
- **Architecture**: x64 (64-bit)
- **Storage**: ~50 MB
- **Permissions**: No administrator rights required

---

## Option 1 — Microsoft Store (Recommended)

Install directly from the [Microsoft Store](https://www.microsoft.com/store/apps/9NFNVQ97185F) — one click, automatic updates, no Developer Mode needed.

1. Open the **Microsoft Store** app.
2. Search for **LangKeep**.
3. Click **Install**.

Or use this link: [https://www.microsoft.com/store/apps/9NFNVQ97185F](https://www.microsoft.com/store/apps/9NFNVQ97185F)

---

## Option 2 — MSIX Installer (GitHub Releases)

### Download

Get the latest MSIX from the [releases page](https://github.com/BorislavEnchev/langkeep/releases/latest).

### Install

1. Download `LangKeep-{version}-x64.msix`.
2. Double-click the file.
3. Click **Install**.
4. Launch LangKeep from the Start Menu.

> **Note**: MSIX packages from GitHub Releases are self-signed. If installation is blocked, enable **Developer Mode** in Windows Settings → Privacy & security → For developers.

### Uninstall

**Settings** → **Apps** → **Installed apps** → **LangKeep** → **Uninstall**

---

## Option 3 — Portable ZIP

The portable version runs without installation — ideal for USB drives, temporary use, or systems where you cannot install software.

1. Download `LangKeep-{version}-portable-x64.zip` from the [releases page](https://github.com/BorislavEnchev/langkeep/releases/latest).
2. Extract to any folder.
3. Run `LangKeep.exe`.

### Uninstall

Delete the extracted folder. Preferences are stored at `%AppData%\LangKeep\` — delete that too for a clean removal.

---

## Post-Installation

### First Run

When you start LangKeep for the first time:

1. A tray icon (keyboard) appears in the system tray (notification area).
2. LangKeep begins monitoring active windows and keyboard layouts.
3. No configuration is needed — LangKeep learns your preferences automatically as you switch between applications and change keyboard layouts.

### Verifying It Works

1. Open **Notepad**.
2. Switch to a different keyboard layout (e.g., German, using Win+Space).
3. Open another application (e.g., a browser).
4. Switch back to Notepad — the keyboard layout should revert to German automatically.

### Troubleshooting

| Problem | Solution |
|---------|----------|
| LangKeep doesn't start | Check if another instance is running (only one instance allowed). Check `%AppData%\LangKeep\logs\` for errors. |
| Layout not switching | Ensure the target keyboard layout is installed in Windows Settings → Time & Language → Language & region. |
| Icon not visible in tray | Click the **^** arrow to show hidden icons. Drag LangKeep to the taskbar to keep it visible. |
| MSIX won't install | Enable Developer Mode (see installation instructions above). |
| Portable version blocked by antivirus | Add the folder to your antivirus exclusions. LangKeep is open source — inspect the source at [github.com/BorislavEnchev/langkeep](https://github.com/BorislavEnchev/langkeep). |

---

## Building from Source

See the [README](../README.md) for development setup instructions.
