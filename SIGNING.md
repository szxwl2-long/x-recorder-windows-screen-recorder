# Signing Guide

## Why signing matters

For normal Windows downloads, code signing is the main way to reduce SmartScreen friction and show users a trusted publisher instead of an unknown publisher.

Signing does **not** guarantee that SmartScreen warnings disappear immediately in every case, but it is the standard and most important improvement for public distribution.

## Realistic options for this project

### Option 1: OV code signing certificate

Good default option for an independent developer or small team.

Pros:
- Lower cost than EV
- Lets you sign your EXE and installer
- Improves trust and publisher display

Cons:
- SmartScreen reputation may still need time to build
- Requires identity validation

### Option 2: EV code signing certificate

Higher-assurance option.

Pros:
- Best chance of reducing SmartScreen friction faster
- Stronger trust posture

Cons:
- Higher cost
- More validation effort
- More operational overhead

### Option 3: Microsoft Artifact Signing / Trusted cloud signing path

Microsoft now provides a managed signing service in Azure.

Pros:
- Managed certificate lifecycle
- Better long-term pipeline integration

Cons:
- More setup complexity
- Azure-based workflow
- Not the fastest first step for a small project

## Recommendation for X Recorder

If your goal is to publish soon and reduce warnings:

1. Start with an OV code signing certificate
2. Sign:
   - `dist/WindosRecorder-portable-folder/WindosRecorder.exe`
   - `dist/installer/X-Recorder-Setup.exe`
3. Timestamp signatures
4. Keep using the same publisher identity and project name consistently

## Supported signing script

This repo includes:

- `scripts/Sign-Release.ps1`
- `scripts/Install-SigningTools.ps1`

It signs:
- the portable EXE
- the installer EXE

It supports:
- PFX file signing
- Windows certificate store signing by SHA-1 thumbprint
- Windows certificate store signing by subject name

The helper installer script:
- downloads the official Windows SDK installer
- attempts to install only the Signing Tools component
- checks whether `signtool.exe` becomes available

Example:

```powershell
.\scripts\Install-SigningTools.ps1
```

Current machine status checked on `2026-04-29`:

- `signtool.exe` is now available
- recommended local path:
  `C:\Program Files (x86)\Windows Kits\10\bin\10.0.19041.0\x64\signtool.exe`

## Example: PFX-based signing

```powershell
.\scripts\Sign-Release.ps1 `
  -SignToolPath "C:\Program Files (x86)\Windows Kits\10\App Certification Kit\signtool.exe" `
  -CertFile "C:\path\to\codesign.pfx" `
  -CertPassword "your-password"
```

## Example: certificate store signing by thumbprint

```powershell
.\scripts\Sign-Release.ps1 `
  -SignToolPath "C:\Program Files (x86)\Windows Kits\10\App Certification Kit\signtool.exe" `
  -CertThumbprint "YOUR_CERT_SHA1_THUMBPRINT"
```

## Example: certificate store signing by subject

```powershell
.\scripts\Sign-Release.ps1 `
  -SignToolPath "C:\Program Files (x86)\Windows Kits\10\App Certification Kit\signtool.exe" `
  -CertSubject "Your Company Name"
```

## Notes

- Always use timestamping
- Always sign both the app EXE and the installer EXE
- If you rotate certificates later, keep the publisher name consistent when possible
- After public release, you can revoke or rotate old tokens used for publishing automation

## Manual actions still required

- Buy or obtain a real Windows code-signing certificate, or choose Microsoft Trusted Signing
- Complete any certificate identity verification requested by the provider
- If using a PFX certificate, provide the final `.pfx` file and password on this PC
- If using certificate-store signing, install the certificate into the Windows certificate store
- If using Azure Trusted Signing, complete the Azure account and service setup
