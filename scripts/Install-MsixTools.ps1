[CmdletBinding()]
param(
    [string]$InstallerUrl = "https://go.microsoft.com/fwlink/?linkid=2311805",
    [string]$DownloadPath = "$env:USERPROFILE\Downloads\winsdksetup-19041.exe",
    [switch]$SkipDownload
)

$ErrorActionPreference = "Stop"

function Find-PackageTool {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ToolName
    )

    $roots = @(
        "C:\Program Files (x86)\Windows Kits",
        "C:\Program Files\Windows Kits",
        "C:\Program Files (x86)\Microsoft SDKs"
    )

    foreach ($root in $roots) {
        if (Test-Path $root) {
            $tool = Get-ChildItem -Path $root -Recurse -Filter $ToolName -ErrorAction SilentlyContinue |
                Sort-Object FullName -Descending |
                Select-Object -First 1
            if ($tool) {
                return $tool.FullName
            }
        }
    }

    return $null
}

$makeAppx = Find-PackageTool -ToolName "makeappx.exe"
$makePri = Find-PackageTool -ToolName "makepri.exe"

if ($makeAppx -and $makePri) {
    Write-Output "MSIX packaging tools already available:"
    Write-Output "makeappx.exe => $makeAppx"
    Write-Output "makepri.exe  => $makePri"
    exit 0
}

if (-not $SkipDownload) {
    Write-Output "Downloading Windows SDK installer..."
    Invoke-WebRequest -Uri $InstallerUrl -OutFile $DownloadPath
}

if (-not (Test-Path $DownloadPath)) {
    throw "Installer not found: $DownloadPath"
}

Write-Output "Running Windows SDK installer for MSIX packaging tools..."
Write-Output "If the feature identifier changes on a future SDK, manual installation may still be required."
Start-Process -FilePath $DownloadPath `
    -ArgumentList "/quiet", "/norestart", "/ceip off", "/features OptionId.UWPManaged,OptionId.DesktopCPPx64" `
    -Wait

$makeAppx = Find-PackageTool -ToolName "makeappx.exe"
$makePri = Find-PackageTool -ToolName "makepri.exe"

if ($makeAppx -and $makePri) {
    Write-Output "MSIX packaging tools installed:"
    Write-Output "makeappx.exe => $makeAppx"
    Write-Output "makepri.exe  => $makePri"
    exit 0
}

Write-Warning "MSIX packaging tools were not found after the installer finished."
Write-Warning "You may need to run the Windows SDK installer manually and select the packaging tools."
exit 1
