# Contributing to LangKeep

Thank you for your interest in contributing to LangKeep! We welcome contributions from everyone.

## Code of Conduct

By participating in this project, you agree to maintain a respectful, inclusive, and constructive environment.

## How to Contribute

### Reporting Bugs

1. Check existing issues to avoid duplicates.
2. Include:
   - Windows version (e.g., Windows 11 23H2)
   - .NET version (`dotnet --version`)
   - Steps to reproduce
   - Expected and actual behavior
   - Relevant log output (from `%AppData%\LangKeep\` or console)

### Suggesting Features

1. Open a new issue with the **enhancement** label.
2. Describe the feature and its use case.
3. If possible, suggest how the feature fits into the existing architecture.

### Pull Requests

1. **Fork** the repository.
2. **Create a branch** for your changes.
3. **Write tests** for new functionality.
4. **Ensure all tests pass**:
   ```bash
   dotnet test
   ```
5. **Ensure the build succeeds**:
   ```bash
   dotnet build
   ```
6. **Follow coding standards** (see below).
7. **Submit a pull request** with a clear description.

## Development Setup

### Prerequisites

- Windows 10 or Windows 11
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (recommended) or any .NET-compatible editor

### Quick Start

```bash
git clone https://github.com/your-org/langkeep.git
cd langkeep
dotnet restore
dotnet build
dotnet test
dotnet run --project src/LangKeep.UI.Wpf
```

## Coding Standards

### General

- **Nullable reference types**: Enabled everywhere (`<Nullable>enable</Nullable>`)
- **Treat warnings as errors**: Enabled (`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`)
- **XML documentation**: Required on all public APIs
- **Async patterns**: Use `async Task` where appropriate; avoid `async void` except for event handlers
- **Dependency Injection**: Services should be resolved via constructor injection (no service locator pattern)

### Architecture Constraints

- No Win32 P/Invoke code outside `src/LangKeep.Infrastructure.Windows/Interop/`
- No business logic in the UI layer
- No Windows-specific code in `LangKeep.Core` or `LangKeep.Application`
- All Win32 declarations go in `Win32Native.cs` only

### Naming

- **Classes**: PascalCase
- **Methods**: PascalCase
- **Parameters**: camelCase
- **Private fields**: `_camelCase` (underscore prefix)
- **Interfaces**: `I` prefix (e.g., `IActiveWindowProvider`)
- **Tests**: `{MethodName}_Should{Expected}_When{Condition}` or similar descriptive names

### Testing

- Use **xUnit** for test framework
- Use **FluentAssertions** for readable assertions
- Use **NSubstitute** for mocking in application-layer tests
- Keep tests focused and deterministic
- Avoid testing infrastructure (Win32) code directly — use interfaces

## Project Structure

```
src/
├── LangKeep.Core/                 # Domain models, value objects, interfaces
├── LangKeep.Application/          # Services, use cases, event orchestration
├── LangKeep.Infrastructure.Windows/  # Win32 interop, persistence, startup
└── LangKeep.UI.Wpf/               # WPF tray application, MVVM

tests/
├── LangKeep.Core.Tests/           # Domain model unit tests
└── LangKeep.Application.Tests/    # Application service unit tests

spikes/
└── LangKeep.Spike/                # Exploratory Win32 validation project
```

## Spike Projects

When exploring new platform APIs or uncertain design decisions, create a spike project under `spikes/`. The spike should validate the critical assumption and produce a findings document (`FINDINGS.md`) that informs the production architecture.

## Questions?

Open a discussion or issue — we're happy to help!
