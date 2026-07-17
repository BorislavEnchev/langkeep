# Release Notes — LangKeep v0.2.2

## 🐛 Bug Fixes

- **Fixed Notepad not detected** — Notepad.exe is now properly recognized and language preferences are applied correctly.
- **Fixed unknown language for cmd.exe** — The Command Prompt window no longer shows "Unknown Language" in the preferences list.
- **Fixed crash when deleting a setting** — Deleting a language preference from the settings window no longer causes the app to crash.
- **Fixed "Add Current" behavior** — The "Add Current" button now correctly captures the last active foreground window instead of a stale reference.

## ⚡ Performance

- **Faster keyboard layout switching** — Reduced latency when restoring keyboard layouts during window switches for a snappier experience.

## 🏪 Distribution

- **Microsoft Store readiness** — Prepared the MSIX package for Microsoft Store submission, enabling a one-click install experience with automatic updates and no Developer Mode requirement.

---

**Full Changelog:** https://github.com/BorislavEnchev/langkeep/commits/v0.2.2
