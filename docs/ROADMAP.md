# LangKeep Roadmap

## ✅ Done (v0.2.2)

- [x] Active window detection via WinEvent hooks
- [x] Keyboard layout detection via polling
- [x] Automatic layout switching on window focus change
- [x] Automatic learning when user manually changes layout
- [x] System tray application with context menu
- [x] Settings UI for managing preferences (add, delete, export, import)
- [x] JSON persistence under `%AppData%\LangKeep\`
- [x] "Start with Windows" registration
- [x] Microsoft Store submission prep (WACK fixes, identity, icons)
- [x] Unit tests for domain models and application services

## Up Next

- [ ] **Per-window rule matching** — Match on window title contents
- [ ] **Rule priority ordering** — Drag-to-reorder in settings UI
- [ ] **Filter/search** — Filter preferences by process name
- [ ] **Regex matching** — Match process names and window titles with regex
- [ ] **Hotkey to toggle** — Configurable global hotkey to enable/disable auto-switching
- [ ] **Better error reporting** — Show last error details in the tray menu

## Future Ideas

- [ ] **Startup delay configuration** — Configurable delay before auto-switching activates
- [ ] **Temporary override** — Temporarily disable auto-switching for N minutes
- [ ] **Statistics** — Track how many times each layout switch was triggered
- [ ] **Log viewer** — Built-in log viewer in the settings window

---

## Contributing

See the [CONTRIBUTING.md](CONTRIBUTING.md) for how to get involved.
