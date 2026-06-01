# Releasing LangKeep

This document describes the release process for LangKeep maintainers.

---

## Overview

Releases are fully automated via **GitHub Actions**. When a version tag (e.g., `v0.1.0`) is pushed to the repository, the CI pipeline:

1. Restores dependencies and builds the solution.
2. Runs all unit tests.
3. Publishes the WPF application as a self-contained deployment.
4. Creates a **portable ZIP** package.
5. Builds an **MSIX** installer package.
6. Signs the MSIX package with a self-signed certificate (generated fresh per build).
7. Creates a **GitHub Release** with release notes and uploads both artifacts.
8. Automatically submits a **Winget** package update to `microsoft/winget-pkgs` (via the `Submit to Winget` workflow).

---

## Release Steps

### 1. Prepare the Release

- Ensure all desired changes are merged into `main`.
- Verify the build passes locally:
  ```bash
  dotnet build
  dotnet test
  ```
- Review the [changelog](../README.md) and update the [ROADMAP.md](../docs/ROADMAP.md) if needed.
- Check that all tests pass on the `main` branch CI.

### 2. Tag the Release

Create a semantic version tag and push it to GitHub:

```bash
# Ensure you're on the latest main
git checkout main
git pull origin main

# Create a tag (examples: v0.1.0, v0.2.0, v1.0.0)
git tag v0.1.0

# Push the tag
git push origin v0.1.0
```

### 3. Wait for CI

The GitHub Actions workflow (`Release`) will trigger automatically.

**Monitor progress**: Go to your repository's **Actions** tab and watch the `Release` workflow run.

The workflow runs on `windows-latest` and typically takes **3–5 minutes**.

### 4. Verify the Release

Once the workflow completes:

1. Go to the repository's **Releases** page.
2. Verify the release exists with the correct version number.
3. Verify both artifacts are attached:
   - `LangKeep-{version}-x64.msix`
   - `LangKeep-{version}-portable-x64.zip`
4. Download and test the portable ZIP (extract and run).
5. Check the **Actions** tab — the `Submit to Winget` workflow should have triggered and opened a PR to `microsoft/winget-pkgs`. Verify the PR is green (checks pass).

---

## 📦 Winget (Windows Package Manager)

LangKeep is published to the [community winget-pkgs repository](https://github.com/microsoft/winget-pkgs). Users can install it with:

```powershell
winget install BorislavEnchev.LangKeep
```

Winget installs the **portable ZIP** version (no Developer Mode needed, no MSIX signing required).

### Initial Setup (One-Time)

The first submission to winget-pkgs must be done manually. After that, the CI handles updates automatically.

**Prerequisites:**
- A [GitHub Personal Access Token (PAT)](https://github.com/settings/tokens) with `repo` scope
- The PAT added as a repository secret named `WINGET_TOKEN` (go to `Settings → Secrets and variables → Actions`)

**Steps:**

1. Download the portable ZIP from the latest GitHub Release:
   ```
   https://github.com/BorislavEnchev/langkeep/releases
   ```

2. Install `wingetcreate`:
   ```powershell
   dotnet tool install -g wingetcreate
   ```

3. Generate the manifest from the ZIP URL:
   ```powershell
   wingetcreate new --url https://github.com/BorislavEnchev/langkeep/releases/download/v0.2.1/LangKeep-0.2.1-portable-x64.zip
   ```
   This interactively asks for metadata (publisher, name, description, license, etc.).

4. Submit the manifest:
   ```powershell
   wingetcreate submit --token YOUR_PAT_HERE manifests/b/BorislavEnchev/LangKeep
   ```

Once merged, the CI workflow (`.github/workflows/winget.yml`) automatically updates the package on every subsequent release.

### How the CI Works

On every `release` (published) event, the `Submit to Winget` workflow:

1. Installs `wingetcreate` on a Windows runner.
2. Runs `wingetcreate update BorislavEnchev.LangKeep` with the new version and installer URL.
3. The tool forks `microsoft/winget-pkgs`, updates the manifest (including the SHA-256 hash), and opens a PR.

**You just need to verify the PR is green and merge it.**

---

## 🏪 Microsoft Store (Optional)

Publishing to the Microsoft Store gives users a one-click install experience with automatic updates and no "Unknown publisher" warnings. As of 2025, Microsoft has removed onboarding fees for individual developers.

### Prerequisites

- A [Microsoft Partner Center](https://partner.microsoft.com/) developer account (free for individuals)
- Your app must pass the [Windows App Certification Kit (WACK)](https://learn.microsoft.com/en-us/windows/apps/publish/store/windows-app-certification-kit)

### Benefits

- ✅ **No Developer Mode required** — Microsoft signs the MSIX
- ✅ **Automatic updates** — Store handles them silently
- ✅ **Discoverability** — users browsing the Store can find LangKeep
- ✅ **Trusted publisher** — no security warnings

### CI Integration (Future)

If you decide to publish to the Store, the release pipeline can be extended to:
1. Run the WACK tests on the built MSIX
2. Submit the MSIX to the Partner Center via the Microsoft Store submission API

This is a future enhancement — not yet implemented.

---

## Generated Artifacts

| Artifact | Format | Description |
|----------|--------|-------------|
| `LangKeep-{version}-x64.msix` | MSIX package (self-signed) | Self-signed Windows app package, installable on Windows 10/11. ⚠️ Requires Developer Mode (or a trusted signing cert). |
| `LangKeep-{version}-portable-x64.zip` | ZIP archive | Self-contained executable with all dependencies. Run without installation. |

### Portable ZIP Contents

```
LangKeep.exe
LangKeep.dll
*.dll (all runtime dependencies)
*.json (configuration defaults)
LICENSE.txt
```

### MSIX Package Details

- **Identity**: `LangKeep`
- **Publisher**: `CN=BorislavEnchev`
- **Signing**: Self-signed certificate generated fresh each build via `New-SelfSignedCertificate`
- **Signature algorithm**: SHA256 with timestamp from DigiCert (`http://timestamp.digicert.com`)
- **Min OS Version**: Windows 10.0.17763.0
- **Capabilities**: `runFullTrust` (full trust desktop app)

> ⚠️ A **self-signed MSIX** requires **Developer Mode** enabled in Windows Settings (`Settings → Privacy & security → For developers → Developer Mode`) **or** installing the certificate's `.cer` file into the `Trusted Root Certification Authorities` store. This is a significant friction point. Until a trusted certificate is obtained, **the portable ZIP is the recommended distribution method** — it requires no special setup.
>
> See [Code Signing](#-code-signing-upgrading-to-a-trusted-certificate) below for upgrading to a trusted certificate, or [Microsoft Store Publishing](#microsoft-store-optional) for the smoothest install experience.

---

## Versioning

LangKeep follows **Semantic Versioning** (SemVer):

- **vMAJOR.MINOR.PATCH** — e.g., `v1.0.0`, `v0.2.0`, `v0.1.1`

Tag format: `v` + semver (e.g., `v0.1.0`).

The version is derived from the Git tag by stripping the leading `v`. For MSIX (which requires 4-part versions), the version is padded with zeros (e.g., `0.1.0` → `0.1.0.0`).

---

## 🔐 Code Signing (Upgrading to a Trusted Certificate)

### Current Setup: Self-Signed

The release pipeline **automatically generates a self-signed certificate** during each build and uses it to sign the MSIX. This happens in the `Sign MSIX with self-signed certificate` step:

1. `New-SelfSignedCertificate` creates a code-signing cert (SHA256, 3-year validity)
2. The cert is exported as a temporary `.pfx` file
3. `signtool.exe` (from the Windows SDK) signs the MSIX with the cert + timestamp
4. The cert is discarded after the build

This means:
- ❌ **Developer Mode required** — users must enable Developer Mode in Windows Settings
- ❌ **Publisher shows as "Unknown"** — because the cert is self-signed, not from a trusted CA
- ❌ **New warning per install** — each build generates a new cert, so users may see the warning on each update
- ✅ **No manual setup needed** — everything happens automatically in CI

### Upgrading to a Trusted Certificate

For a clean install experience (no warnings, shows your name as publisher):

1. **Purchase** a code-signing certificate from a trusted CA (e.g., DigiCert, Sectigo, GlobalSign).
2. **Export** the certificate as a `.pfx` file with a password.
3. **Base64-encode** the `.pfx`:
   ```bash
   openssl base64 -in certificate.pfx -out certificate.pfx.b64
   ```
4. **Add as GitHub Secrets**:
   - `MSIX_SIGNING_CERTIFICATE` — the base64-encoded `.pfx` content
   - `MSIX_SIGNING_PASSWORD` — the certificate password
5. The workflow's `Get signing certificate` step checks for these secrets first. If present, it uses your certificate. If not, it falls back to generating a self-signed cert.

**No other code changes needed** — the workflow already handles both paths automatically.

You can also verify the signing is working by checking the workflow logs for a green ✅ "MSIX signed successfully!" message.

---

## Troubleshooting

### Build Fails in CI

| Symptom | Cause | Fix |
|---------|-------|-----|
| `dotnet restore` fails | Network issue or NuGet source problem | Re-run the workflow. Check `nuget.config` if present. |
| MSIX build fails | Missing Visual Studio Build Tools | Ensure `windows-latest` runner is used (it includes Build Tools). |
| `msbuild` not found | MSBuild not on PATH | The `microsoft/setup-msbuild` action should be used before the MSIX build step. |
| Test failures | Code regression | Fix tests, merge fix to `main`, and re-tag. |
| Release creation fails | `GITHUB_TOKEN` permissions | Ensure the workflow has `contents: write` permission (default for `GITHUB_TOKEN` on tag push). |

### Can't Push Tag

```bash
# If tag already exists locally
git tag -d v0.1.0

# Delete remote tag (if needed)
git push --delete origin v0.1.0

# Re-create and push
git tag v0.1.0
git push origin v0.1.0
```

---

## FAQ

### Can I create a release without a tag?

No. The release workflow only triggers on tag pushes (`v*`). If you need a manual release, create a tag locally and push it.

### Can I customize the release notes?

Yes. The release notes are auto-generated from the workflow's `body` field. To add custom notes, edit the `body` in `.github/workflows/release.yml`.

### What if I need to update an existing release?

Delete the release and the tag from GitHub, then push the corrected tag again.

---

## Related

- [Installation Guide](installation.md)
- [Architecture](architecture/ARCHITECTURE.md)
- [Contributing](../CONTRIBUTING.md)
