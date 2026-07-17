# Privacy Policy for LangKeep

**Last updated:** July 17, 2026

## Overview

LangKeep is a Windows desktop application that automatically remembers and restores your preferred keyboard input language for each application. This privacy policy explains what data LangKeep does and does not collect.

## Data Collection

**LangKeep does not collect, store, or transmit any personal data, usage analytics, or telemetry.**

The application operates entirely locally on your machine. Specifically:

### What LangKeep Accesses (Locally Only)

- **Active window title and process name** — LangKeep reads the title and process name of the currently focused window to determine which application you are using. This information never leaves your computer.
- **Current keyboard layout identifier** — LangKeep reads the active keyboard layout (e.g., "en-US", "de-DE") to associate it with the active application.
- **Application preferences** — Your language-per-application preferences are stored in a local JSON file at `%AppData%\LangKeep\preferences.json`.

### What LangKeep Does NOT Collect

- ❌ No personal information (name, email, address, etc.)
- ❌ No browsing history or websites visited
- ❌ No keystrokes, mouse clicks, or input content
- ❌ No file names, file contents, or document data
- ❌ No network requests or external communications
- ❌ No crash reports or error telemetry
- ❌ No usage analytics or tracking
- ❌ No advertising or marketing data

## Data Storage

All data is stored locally on your device:

| Data | Location | Purpose |
|------|----------|---------|
| Language preferences | `%AppData%\LangKeep\preferences.json` | Remember which keyboard layout you prefer for each application |
| Logs | `%AppData%\LangKeep\logs\` | Debugging and troubleshooting (disabled by default) |

## Data Sharing

LangKeep does not share any data with third parties. The application makes no network requests and communicates with no external servers.

## Data Retention

Your preferences remain on your device until you uninstall LangKeep or manually delete the preferences file at `%AppData%\LangKeep\`.

## Third-Party Services

LangKeep does not integrate with any third-party services, SDKs, or analytics frameworks.

## Changes to This Policy

If this privacy policy changes, the "Last updated" date at the top will be revised. Since LangKeep does not communicate with any external service, changes will be reflected in the application's documentation.

## Contact

For questions about this privacy policy or LangKeep's data practices, please open an issue on the [LangKeep GitHub repository](https://github.com/BorislavEnchev/langkeep/issues).

---

**Borislav Enchev**  
https://github.com/BorislavEnchev
