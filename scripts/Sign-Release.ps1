[CmdletBinding()]
param(
    [string]$SignToolPath = "C:\Program Files (x86)\Windows Kits\10\App Certification Kit\signtool.exe",
    [string]$CertFile,
    [string]$CertPassword,
    [string]$CertThumbprint,
    [string]$CertSubject,
    [string]$TimestampUrl = "http://timestamp.digicert.com",
    [string]$Root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
)

$ErrorActionPreference = "Stop"

function Assert-PathExists {
    param(
        [string]$Path,
        [string]$Label
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "$Label not found: $Path"
    }
}

function Get-SignTargets {
    param(
        [string]$ProjectRoot
    )

    @(
        (Join-Path $ProjectRoot "dist\WindosRecorder-portable-folder\WindosRecorder.exe"),
        (Join-Path $ProjectRoot "dist\installer\X-Recorder-Setup.exe")
    )
}

function New-SignArguments {
    param(
        [string]$FilePath
    )

    $arguments = @(
        "sign",
        "/fd", "SHA256",
        "/td", "SHA256",
        "/tr", $TimestampUrl
    )

    if ($CertFile) {
        $arguments += @("/f", $CertFile)
        if ($CertPassword) {
            $arguments += @("/p", $CertPassword)
        }
    }
    elseif ($CertThumbprint) {
        $arguments += @("/sha1", $CertThumbprint)
    }
    elseif ($CertSubject) {
        $arguments += @("/n", $CertSubject)
    }
    else {
        $arguments += "/a"
    }

    $arguments += $FilePath
    return ,$arguments
}

Assert-PathExists -Path $SignToolPath -Label "signtool"

if ($CertFile) {
    Assert-PathExists -Path $CertFile -Label "certificate file"
}

$targets = Get-SignTargets -ProjectRoot $Root

foreach ($target in $targets) {
    Assert-PathExists -Path $target -Label "sign target"
}

foreach ($target in $targets) {
    Write-Host "Signing $target"
    $arguments = New-SignArguments -FilePath $target
    & $SignToolPath @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "signtool failed for $target with exit code $LASTEXITCODE"
    }
}

Write-Host "Verifying signatures..."
foreach ($target in $targets) {
    & $SignToolPath verify /pa $target
    if ($LASTEXITCODE -ne 0) {
        throw "Signature verification failed for $target with exit code $LASTEXITCODE"
    }
}

Write-Host "Signing complete."
