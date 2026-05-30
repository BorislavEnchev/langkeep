# LangKeep Architecture

## Overview

LangKeep is a Windows tray application that monitors active-window changes and keyboard layout switches, then automatically activates the appropriate keyboard layout for each application.

The architecture follows **Clean Architecture** principles to ensure maintainability, testability, and future extensibility to other platforms (e.g., macOS).

## Layer Diagram

```
┌─────────────────────────────────────────────────────┐
│                  LangKeep.UI.Wpf                    │
│  (WPF Tray App · MVVM · DI Bootstrap · Logging)     │
│         │                          ▲                │
│         ▼                          │                │
│  ┌─────────────────────────────────────┐            │
│  │      LangKeep.Application           │            │
│  │  (Use Cases · Event Orchestration)  │            │
│  │         │                           │            │
│  │         ▼                           │            │
│  │  ┌───────────────────────────┐      │            │
│  │  │      LangKeep.Core        │      │            │
│  │  │  (Domain · Interfaces)    │      │            │
│  │  └───────────────────────────┘      │            │
│  └─────────────────────────────────────┘            │
│         │                          ▲                │
│         ▼                          │                │
│  ┌─────────────────────────────────────┐            │
│  │   LangKeep.Infrastructure.Windows   │            │
│  │  (Win32 Interop · Persistence ·     │            │
│  │   Startup Registry)                 │            │
│  └─────────────────────────────────────┘            │
└─────────────────────────────────────────────────────┘
```

### Dependency Rule

Dependencies point **inward**:
- `LangKeep.UI.Wpf` → `LangKeep.Application` → `LangKeep.Core`
- `LangKeep.Infrastructure.Windows` → `LangKeep.Application` → `LangKeep.Core`
- `LangKeep.Core` has **no dependencies** on any other project

## Core Layer (`LangKeep.Core`)

**No platform-specific code.** This layer contains:

### Domain Models (Value Objects)

| Model | Description |
|-------|-------------|
| `ApplicationIdentity` | Identifies an application by process name (future: + window title, + document path) |
| `KeyboardLayout` | A keyboard input language identified by IETF tag (e.g., "en-US") |
| `ActiveWindowInfo` | Snapshot of the active window's app identity + current layout |
| `LanguagePreference` | A stored association between an app and a layout |
| `MatchingRule` | Future-proof rule definition with priority and optional title matching |
| `RuleMatchResult` | Result of evaluating rules against an application identity |

### Interfaces

| Interface | Purpose |
|-----------|---------|
| `IActiveWindowProvider` | Provides active window info and raises `ActiveWindowChanged` events |
| `IKeyboardLayoutProvider` | Provides current keyboard layout and raises `LayoutChanged` events |
| `IKeyboardLayoutSwitcher` | Switches the keyboard layout for a given application |
| `IPreferenceRepository` | CRUD for persisted language preferences |
| `IRuleMatcher` | Evaluates matching rules against an application identity |
| `IStartupManager` | Manages auto-start registration (platform-specific) |

## Application Layer (`LangKeep.Application`)

**No Windows-specific code.** Orchestrates business logic:

| Service | Responsibility |
|---------|---------------|
| `LanguageTrackingService` | Central coordinator: subscribes to window/layout events, triggers rule evaluation + layout switching, learns preferences automatically |
| `PreferenceManagementService` | CRUD operations for language preferences with error handling |
| `RuleEvaluationService` | Evaluates `MatchingRule`s against an `ApplicationIdentity` by process name |

## Infrastructure.Windows Layer

**All Win32 P/Invoke code lives here**, isolated in `Interop/Win32Native.cs`.

| Component | What it does |
|-----------|-------------|
| `Win32Native` | All P/Invoke declarations (`SetWinEventHook`, `GetKeyboardLayout`, `ActivateKeyboardLayout`, etc.) |
| `Win32ActiveWindowProvider` | Uses `SetWinEventHook(EVENT_SYSTEM_FOREGROUND)` to detect window changes |
| `Win32KeyboardLayoutProvider` | Polls `GetKeyboardLayout` every 500ms to detect layout changes |
| `Win32KeyboardLayoutSwitcher` | Uses `ActivateKeyboardLayout` to switch layouts |
| `JsonPreferenceRepository` | Reads/writes preferences as JSON to `%AppData%\LangKeep\preferences.json` |
| `WindowsStartupManager` | Registers/unregisters via `HKCU\...\Run` registry key |

## UI Layer (`LangKeep.UI.Wpf`)

**No business logic.** Implements MVVM with `CommunityToolkit.Mvvm`:

| Component | Description |
|-----------|-------------|
| `App.xaml.cs` | DI bootstrap via `Microsoft.Extensions.Hosting` |
| `TrayIconService` | `NotifyIcon` with context menu (Open Settings, Toggle Auto, Toggle Startup, Exit) |
| `SettingsViewModel` | ViewModel for the settings window with load/add/delete/export/import commands |
| `SettingsWindow` | WPF window with a DataGrid for managing preferences |
| `ModernStyles.xaml` | Clean, modern WPF styles |

## Event Flow

### Window Switch

```
User focuses Teams
    → Win32ActiveWindowProvider.ActiveWindowChanged fires
    → LanguageTrackingService.OnActiveWindowChanged
    → RuleEvaluationService.Evaluate(application)
    → [If match found] Win32KeyboardLayoutSwitcher.TrySwitchLayout
    → German layout activated
```

### Layout Change (User Manual Change)

```
User presses Win+Space while Teams is focused
    → Win32KeyboardLayoutProvider.LayoutChanged fires (via polling)
    → LanguageTrackingService.OnLayoutChanged
    → PreferenceManagementService.SetPreferenceAsync (auto-learn)
    → RuleEvaluationService.AddOrUpdate (update in-memory rules)
```

## Persistence Format

```json
{
  "version": 1,
  "preferences": [
    {
      "processName": "Code.exe",
      "languageTag": "en-US",
      "isEnabled": true,
      "sortOrder": 0
    }
  ]
}
```

The `version` field enables future schema migrations.

## Future Extensibility

### Per-Window Matching

`MatchingRule.WindowTitleContains` is already defined but not evaluated in MVP. Once enabled, rules can match on window title substrings.

### Per-Document / Browser Tab

Future matching criteria (document extension, URL pattern) can be added to `MatchingRule` without schema redesign.

### macOS Support

Create `LangKeep.Infrastructure.macOS` with:
- `MacOSActiveWindowProvider` (using `NSWorkspace.sharedWorkspace().frontmostApplication`)
- `MacOSKeyboardLayoutProvider` (using `InputMethodKit` or `TISCopyCurrentKeyboardInputSource`)
- `MacOSStartupManager` (using `SMAppService` or LaunchAgents)
- `IRuleMatcher` and `IPreferenceRepository` are already platform-agnostic

## Reliability

- All external calls are wrapped in try/catch with logging
- Polling failures are swallowed and retried
- Missing processes, inaccessible windows, and unsupported layouts are handled gracefully
- The application never terminates on recoverable exceptions
