# Manual Actions Checklist

These are the remaining steps that require the project owner to act directly.

## Release-related

- Verify the GitHub repository page after screenshot replacement
- Decide whether to publish the same asset set to `itch.io`
- Provide any final screenshots if you want support/donation windows shown on the public page

## Code signing

- Choose one signing path:
  - OV code-signing certificate
  - EV code-signing certificate
  - Microsoft Trusted Signing
- Complete identity verification with the signing provider
- Provide the final signing material on this PC:
  - PFX file + password, or
  - installed certificate in Windows certificate store, or
  - Azure Trusted Signing account ready

Current machine status checked on `2026-04-29`:
- `signtool.exe` installed
- preferred path: `C:\Program Files (x86)\Windows Kits\10\bin\10.0.19041.0\x64\signtool.exe`
- no code-signing certificate found in `Cert:\CurrentUser\My`
- no code-signing certificate found in `Cert:\LocalMachine\My`

## Optional trust-building

- Test install and first launch on a clean Windows 10 machine
- Test install and first launch on a clean Windows 11 machine
- Confirm whether to apply for SignPath Foundation later as an open-source signing path
