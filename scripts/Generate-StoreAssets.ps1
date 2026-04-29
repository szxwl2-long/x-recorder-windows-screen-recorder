[CmdletBinding()]
param(
    [string]$SourceIcon = "C:\Users\Administrator\Documents\Codex\2026-04-24-windos\WindosRecorder\Assets\app-icon.png",
    [string]$OutputDirectory = "C:\Users\Administrator\Documents\Codex\2026-04-24-windos\store\Assets"
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

if (-not (Test-Path $SourceIcon)) {
    throw "Source icon not found: $SourceIcon"
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

$sizes = @(
    @{ Name = "Square44x44Logo.png"; Width = 44; Height = 44 },
    @{ Name = "Square71x71Logo.png"; Width = 71; Height = 71 },
    @{ Name = "Square150x150Logo.png"; Width = 150; Height = 150 },
    @{ Name = "Square310x310Logo.png"; Width = 310; Height = 310 },
    @{ Name = "StoreLogo.png"; Width = 50; Height = 50 }
)

$image = [System.Drawing.Image]::FromFile($SourceIcon)

try {
    foreach ($size in $sizes) {
        $bitmap = New-Object System.Drawing.Bitmap $size.Width, $size.Height
        try {
            $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
            try {
                $graphics.Clear([System.Drawing.Color]::Transparent)
                $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
                $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                $graphics.DrawImage($image, 0, 0, $size.Width, $size.Height)

                $target = Join-Path $OutputDirectory $size.Name
                $bitmap.Save($target, [System.Drawing.Imaging.ImageFormat]::Png)
                Write-Output "Generated $target"
            }
            finally {
                $graphics.Dispose()
            }
        }
        finally {
            $bitmap.Dispose()
        }
    }
}
finally {
    $image.Dispose()
}
