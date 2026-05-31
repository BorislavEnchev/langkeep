# Installation

LangKeep is available as an **MSIX Installer** or a **Portable ZIP**. Both are produced automatically for every tagged release.

---

## System Requirements

- **Operating System**: Windows 10 (version 1809 or later) or Windows 11
- **Architecture**: x64 (64-bit)
- **Storage**: ~50 MB
- **Permissions**: No administrator rights required for the portable version

---

## Option 1 — MSIX Installer (Recommended)

The MSIX package provides a proper Windows installation experience with automatic updates support and clean uninstall.

### Download

Get the latest MSIX from the [releases page](https://github.com/BorislavEnchev/langkeep/releases/latest).

### Install

#### Method A — Double-click (easiest)

1. Download `LangKeep-{version}-x64.msix`.
2. Double-click the file.
3. Click **Install**.
4. Launch LangKeep from the Start Menu.

#### Method B — PowerShell

```powershell
Add-AppPackage -Path "LangKeep-{version}-x64.msix"
```

#### Method C — If installation is blocked

If Windows displays "This app can't run on your PC" or the install button is grayed out:

1. **Enable Developer Mode**:
   - Open **Settings** → **Privacy & security** → **For developers**
   - Turn on **Developer Mode**

2. **Enable sideloading** (alternative):
   - Open **Settings** → **Apps** → **Apps & features**
   - Under **Choose where to get apps**, select **Sideload apps**

3. **Install via PowerShell with bypass**:
   ```powershell
   Add-AppPackage -Path "LangKeep-{version}-x64.msix"
   ```

### Uninstall

**Settings** → **Apps** → **Installed apps** → **LangKeep** → **Uninstall**

Or via PowerShell:

```powershell
Get-AppPackage -Name "LangKeep" | Remove-AppPackage
```

---

## Option 2 — Portable ZIP

The portable version runs without installation — ideal for USB drives, temporary use, or systems where you cannot install software.

### Download

Get the latest ZIP from the [releases page](https://github.com/BorislavEnchev/langkeep/releases/latest).

### Install

1. Download `LangKeep-{version}-portable-x64.zip`.
2. Extract the ZIP to a folder of your choice (e.g., `C:\Tools\LangKeep`).
3. Run `LangKeep.exe`.

> **Tip**: Pin `LangKeep.exe` to your taskbar or Start Menu for quick access.

### Uninstall

Simply delete the extracted folder. LangKeep stores its preferences at `%AppData%\LangKeep\` — if you want a clean removal, delete that folder too.

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
