# Microsoft Store Submission Checklist

## Current Project State

- Microsoft Store developer account: ready
- Public GitHub repository: ready
- Public release assets: ready
- Privacy notice: ready
- Store listing draft: ready
- Clean screenshots: ready
- Traditional Windows code signing certificate: not available
- MSIX packaging project: not created yet
- `makeappx.exe` / `makepri.exe`: not installed on this PC

## Best Free Route

For a no-cost Microsoft Store submission, prefer:

1. `MSIX` packaging
2. Store-managed signing after submission

Avoid the `EXE/MSI` submission path for now, because that path still expects your own signed installer.

## What Is Already Prepared

- `PRIVACY.md`
- `STORE-LISTING.md`
- `docs/screenshots/`
- release cover image
- public GitHub URLs

## What Still Needs To Be Done

1. Reserve the app name in Partner Center if it is not already reserved.
2. Confirm the final Store display name:
   - `X Recorder`
3. Collect the Partner Center package identity values:
   - Package / identity name
   - Publisher / CN value
4. Install MSIX packaging tools:
   - `makeappx.exe`
   - `makepri.exe`
5. Create an MSIX package from the app publish output.
6. Upload the package in Partner Center.
7. Fill in listing text from `STORE-LISTING.md`.
8. Upload screenshots in the planned order.
9. Set the privacy policy URL.
10. Submit for certification.

## Manual Values We Will Need Later

- Reserved Store app name
- Publisher display name
- Publisher identity string from Partner Center
- Final package identity name from Partner Center

## Local Packaging Source

Use this publish output as the base content for Store packaging:

- `C:\Users\Administrator\Documents\Codex\2026-04-24-windos\publish\WindosRecorder-single`

Fallback source if needed:

- `C:\Users\Administrator\Documents\Codex\2026-04-24-windos\publish\WindosRecorder`

## Recommended Next Execution Order

1. Finish the Partner Center app setup pages.
2. Capture the package identity values.
3. Install MSIX packaging tools.
4. Generate Store package scaffolding locally.
5. Build the first MSIX package.
