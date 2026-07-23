# Release Notes — LangKeep v0.2.3

## 🐛 Bug Fixes

- **Fixed app not starting with Windows** — Restored the auto-startup registration that was lost in a previous update. The app now registers itself to launch at Windows startup by default. You can still toggle this from the tray icon context menu ("Start with Windows").

## 🧪 Testing

- **Added unit tests for WindowsStartupManager** — 14 new unit tests covering register, unregister, is-registered checks, idempotency, and edge cases for the startup manager.

---

**Full Changelog:** https://github.com/BorislavEnchev/langkeep/commits/v0.2.3
