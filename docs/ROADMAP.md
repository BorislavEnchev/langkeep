# LangKeep Roadmap

## MVP (Current Release)

- [x] Active window detection via WinEvent hooks
- [x] Keyboard layout detection via polling
- [x] Automatic layout switching on window focus change
- [x] Automatic learning when user manually changes layout
- [x] System tray application with context menu
- [x] Settings UI for managing preferences (add, delete, export, import)
- [x] JSON persistence under `%AppData%\LangKeep\`
- [x] "Start with Windows" registration
- [x] Structured logging with `ILogger<T>`
- [x] Unit tests for domain models and application services

## Short-Term (v0.2)

- [ ] **Per-window rule matching** — Match on window title contents
- [ ] **Rule priority ordering** — Drag-to-reorder in settings UI
- [ ] **Inline editing** — Edit language tags directly in the settings DataGrid
- [ ] **Real-time preview** — Show current window's matched rule in settings
- [ ] **Filter/search** — Filter preferences by process name
- [ ] **Better error reporting** — Show last error details in the tray menu
- [ ] **Configuration file backup** — Auto-backup before import

## Medium-Term (v0.3 — v0.5)

- [ ] **Rule groups and profiles** — Save/load named rule profiles (e.g., "Work", "Personal")
- [ ] **Temporary override** — Temporarily disable auto-switching for N minutes
- [ ] **Per-monitor layout support** — Handle different layouts per monitor
- [ ] **Startup delay configuration** — Configurable delay before auto-switching activates
- [ ] **Notification area** — Show toast notifications on layout switches (optional)
- [ ] **Statistics** — Track how many times each layout switch was triggered

## Long-Term

### Browser Extension Integration (v0.6)

- [ ] **Chrome extension** — Detect browser tab language via content script
- [ ] **Edge extension** — Reuse Chrome extension code
- [ ] **Firefox extension** — Native messaging API integration
- [ ] **Per-tab language preferences** — Switch layout when changing browser tabs

### Enhanced Rule Engine (v0.7)

- [ ] **Regex matching** — Match process names and window titles with regex
- [ ] **Glob patterns** — Support `*`, `?` patterns in process names
- [ ] **Composite rules** — AND/OR combinations of matching criteria
- [ ] **Fallback chains** — If first rule matches but layout isn't available, try next
- [ ] **Rule templates** — Predefined rules for common applications

### macOS Support (v1.0)

- [ ] Create `LangKeep.Infrastructure.macOS` project
- [ ] Implement `MacOSActiveWindowProvider` using `NSWorkspace`
- [ ] Implement `MacOSKeyboardLayoutProvider` using `InputMethodKit` / `TISCopyCurrentKeyboardInputSource`
- [ ] Implement `MacOSKeyboardLayoutSwitcher`
- [ ] Implement `MacOSStartupManager` using `SMAppService`
- [ ] Create `LangKeep.UI.Mac` using native AppKit or SwiftUI bridge
- [ ] Bundle as `.app` with macOS signing

### Advanced Features (v2.0+)

- [ ] **Per-document matching** — Switch layout based on file extension or document path
- [ ] **Remote configuration** — Sync preferences across machines via cloud storage
- [ ] **Command-line interface** — Manage rules via CLI for scripting
- [ ] **PowerShell module** — Manage LangKeep via PowerShell
- [ ] **Auto-disable for password fields** — Prevent layout switching when password fields are focused (UIAutomation)
- [ ] **Hotkey to toggle** — Configurable global hotkey to enable/disable auto-switching
- [ ] **Multi-user support** — Per-user profiles on shared machines
- [ ] **Log viewer** — Built-in log viewer in the settings window

---

## Contributing

See the [milestones](https://github.com/your-org/langkeep/milestones) for current sprint goals and [CONTRIBUTING.md](CONTRIBUTING.md) for how to get involved.
