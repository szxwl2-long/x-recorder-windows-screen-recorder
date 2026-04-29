# Microsoft Store Packaging Notes

## Current Direction

Because this project does not yet have a paid code-signing certificate, the preferred Microsoft Store path is:

1. package the app as `MSIX`
2. submit through Microsoft Store
3. let the Store handle the final trust path instead of relying on an unsigned EXE installer

## Local Status On 2026-04-29

- Store developer account: ready
- Existing app type: regular WPF desktop app
- Existing package project: none
- Existing app manifest: none
- Existing MSIX tools on this PC:
  - `signtool.exe`: available
  - `makeappx.exe`: missing
  - `makepri.exe`: missing

## Files Prepared

- `STORE-LISTING.md`
- `STORE-SUBMISSION-CHECKLIST.md`
- `store\AppxManifest.template.xml`
- `scripts\Generate-StoreAssets.ps1`
- `scripts\Install-MsixTools.ps1`

## Store Assets

Run this to generate logo assets from the existing app icon:

```powershell
.\scripts\Generate-StoreAssets.ps1
```

Expected output folder:

- `store\Assets`

## MSIX Tooling

Attempted helper:

```powershell
.\scripts\Install-MsixTools.ps1
```

Current result:
- helper did not find `makeappx.exe` or `makepri.exe` after the SDK run
- this likely means the exact packaging component still needs manual installation or a different SDK feature selection

## Identity Values Still Needed

These must come from Partner Center before a final manifest can be generated:

- package identity name
- publisher identity string
- final reserved Store app name

## Base Publish Folder

Preferred source for Store packaging:

- `publish\WindosRecorder-single`

Fallback source:

- `publish\WindosRecorder`
