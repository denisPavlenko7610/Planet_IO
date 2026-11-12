param(
    [Parameter(Mandatory = $true)][string[]]$Files,
    [string]$Source,
    [string]$Output,
    [int]$Width = 1920,
    [int]$Height = 1080
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$Files = $Files | ForEach-Object { $_ -split ',' } | Where-Object { $_ }
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
if (-not $Source) { $Source = Join-Path $root 'Assets\Screenshots' }
if (-not $Output) { $Output = Join-Path $root 'docs\PlayStore\screenshots' }

New-Item -ItemType Directory -Force -Path $Output | Out-Null
$index = 1
$targetRatio = $Width / $Height

foreach ($name in $Files) {
    $path = if (Test-Path $name) { $name } else { Join-Path $Source $name }
    if (-not (Test-Path $path)) { throw "Screenshot not found: $name" }

    $image = [System.Drawing.Image]::FromFile((Resolve-Path $path))
    try {
        $ratio = $image.Width / $image.Height
        if ($ratio -gt $targetRatio) {
            $cropHeight = $image.Height
            $cropWidth = [int][Math]::Round($image.Height * $targetRatio)
        } else {
            $cropWidth = $image.Width
            $cropHeight = [int][Math]::Round($image.Width / $targetRatio)
        }
        $cropX = [int][Math]::Floor(($image.Width - $cropWidth) / 2)
        $cropY = [int][Math]::Floor(($image.Height - $cropHeight) / 2)

        $bitmap = New-Object System.Drawing.Bitmap $Width, $Height, ([System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.DrawImage(
            $image,
            (New-Object System.Drawing.Rectangle 0, 0, $Width, $Height),
            $cropX, $cropY, $cropWidth, $cropHeight,
            [System.Drawing.GraphicsUnit]::Pixel)

        $target = Join-Path $Output ('{0:D2}.png' -f $index)
        $bitmap.Save($target, [System.Drawing.Imaging.ImageFormat]::Png)
        $graphics.Dispose()
        $bitmap.Dispose()
        Write-Host "$name -> $target"
        $index++
    }
    finally {
        $image.Dispose()
    }
}
