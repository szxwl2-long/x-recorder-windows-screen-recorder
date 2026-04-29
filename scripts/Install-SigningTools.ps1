[CmdletBinding()]
param(
    [string]$InstallerUrl = "https://go.microsoft.com/fwlink/?linkid=2311805",
    [string]$DownloadPath = "$env:USERPROFILE\Downloads\winsdksetup-19041.exe",
    [switch]$SkipDownload
)

$ErrorActionPreference = "Stop"

function Find-SignTool {
    $roots = @(
        "C:\Program Files (x86)\Windows Kits",
        "C:\Program Files\Windows Kits",
        "C:\Program Files (x86)\Microsoft SDKs"
    )

    foreach ($root in $roots) {
        if (Test-Path $root) {
            $tool = Get-ChildItem -Path $root -Recurse -Filter signtool.exe -ErrorAction SilentlyContinue |
                Sort-Object FullName -Descending |
                Select-Object -First 1
            if ($tool) {
                return $tool.FullName
            }
        }
    }

    return $null
}

$existing = Find-SignTool
if ($existing) {
    Write-Output "signtool.exe already available:"
    Write-Output $existing
    exit 0
}

if (-not $SkipDownload) {
    Write-Output "Downloading Windows SDK installer..."
    Invoke-WebRequest -Uri $InstallerUrl -OutFile $DownloadPath
}

if (-not (Test-Path $DownloadPath)) {
    throw "Installer not found: $DownloadPath"
}

Write-Output "Running Windows SDK installer for Signing Tools..."
Start-Process -FilePath $DownloadPath `
    -ArgumentList "/quiet", "/norestart", "/ceip off", "/features OptionId.SigningTools" `
    -Wait

$installed = Find-SignTool
if ($installed) {
    Write-Output "signtool.exe installed:"
    Write-Output $installed
    exit 0
}

Write-Warning "signtool.exe was not found after the installer finished."
Write-Warning "You may need to run the Windows SDK installer manually and ensure Signing Tools are selected."
exit 1
