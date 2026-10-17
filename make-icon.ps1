Add-Type -AssemblyName System.Drawing
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourcePath = (Resolve-Path -LiteralPath (Join-Path $projectRoot 'Assets\avatar.png')).Path
$destination = Join-Path $projectRoot 'TutorialLibrary\Assets\avatar.ico'
New-Item -ItemType Directory -Force -Path (Split-Path $destination) | Out-Null
$source = [System.Drawing.Bitmap]::new($sourcePath)
$left = $source.Width; $top = $source.Height; $right = 0; $bottom = 0
for ($y = 0; $y -lt $source.Height; $y += 3) {
    for ($x = 0; $x -lt $source.Width; $x += 3) {
        if ($source.GetPixel($x, $y).A -gt 16) {
            $left = [Math]::Min($left, $x); $top = [Math]::Min($top, $y)
            $right = [Math]::Max($right, $x); $bottom = [Math]::Max($bottom, $y)
        }
    }
}
if ($right -le $left -or $bottom -le $top) { throw '图标源文件没有可见像素。' }
$side = [int][Math]::Ceiling([Math]::Max($right - $left, $bottom - $top) * 1.07)
$side = [Math]::Min($side, [Math]::Min($source.Width, $source.Height))
$cropX = [Math]::Clamp([int][Math]::Round(($left + $right - $side) / 2), 0, $source.Width - $side)
$cropY = [Math]::Clamp([int][Math]::Round(($top + $bottom - $side) / 2), 0, $source.Height - $side)
$sourceRect = [System.Drawing.Rectangle]::new($cropX, $cropY, $side, $side)
$sizes = @(16, 24, 32, 48, 64, 128, 256)
$images = [System.Collections.Generic.List[byte[]]]::new()
foreach ($size in $sizes) {
    $bitmap = [System.Drawing.Bitmap]::new($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.Clear([System.Drawing.Color]::Transparent)
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.DrawImage($source, [System.Drawing.Rectangle]::new(0, 0, $size, $size), $sourceRect, [System.Drawing.GraphicsUnit]::Pixel)
    $memory = [System.IO.MemoryStream]::new()
    $bitmap.Save($memory, [System.Drawing.Imaging.ImageFormat]::Png)
    $images.Add($memory.ToArray())
    $memory.Dispose()
    $graphics.Dispose()
    $bitmap.Dispose()
}
$source.Dispose()
$stream = [System.IO.File]::Create($destination)
$writer = [System.IO.BinaryWriter]::new($stream)
$writer.Write([uint16]0)
$writer.Write([uint16]1)
$writer.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $writer.Write([byte]($sizes[$i] % 256))
    $writer.Write([byte]($sizes[$i] % 256))
    $writer.Write([byte]0)
    $writer.Write([byte]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]32)
    $writer.Write([uint32]$images[$i].Length)
    $writer.Write([uint32]$offset)
    $offset += $images[$i].Length
}
foreach ($bytes in $images) { $writer.Write($bytes) }
$writer.Dispose()
Write-Output $destination
