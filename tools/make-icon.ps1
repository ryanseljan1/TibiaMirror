# Generates src/TibiaMirror/Assets/Icons/app.ico (16/32/48/256 px, PNG-compressed frames).
# Run with Windows PowerShell: powershell -File tools/make-icon.ps1
Add-Type -AssemblyName System.Drawing

$out = Join-Path $PSScriptRoot '..\src\TibiaMirror\Assets\Icons\app.ico'
$out = [System.IO.Path]::GetFullPath($out)
New-Item -ItemType Directory -Force (Split-Path $out) | Out-Null

function New-Frame([int]$s) {
    $bmp = New-Object System.Drawing.Bitmap $s, $s, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.Clear([System.Drawing.Color]::Transparent)
    $dark = [System.Drawing.Color]::FromArgb(255, 0x1E, 0x1E, 0x1E)
    $orange = [System.Drawing.Color]::FromArgb(255, 0xFF, 0x7F, 0x00)
    # dark rounded square
    $r = [Math]::Max(2, [int]($s * 0.22)); $d = $r * 2
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddArc(0, 0, $d, $d, 180, 90)
    $path.AddArc($s - $d - 1, 0, $d, $d, 270, 90)
    $path.AddArc($s - $d - 1, $s - $d - 1, $d, $d, 0, 90)
    $path.AddArc(0, $s - $d - 1, $d, $d, 90, 90)
    $path.CloseFigure()
    $g.FillPath((New-Object System.Drawing.SolidBrush $dark), $path)
    # mirror glyph: two overlapping rectangles
    $pen = New-Object System.Drawing.Pen $orange, ([Math]::Max(1.0, $s / 14.0))
    $w = $s * 0.42; $h = $s * 0.42
    $g.DrawRectangle($pen, [single]($s * 0.18), [single]($s * 0.18), [single]$w, [single]$h)
    $g.FillRectangle((New-Object System.Drawing.SolidBrush $orange), [single]($s * 0.40), [single]($s * 0.40), [single]$w, [single]$h)
    $g.Dispose()
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    return ,$ms.ToArray()
}

$sizes = 16, 32, 48, 256
$frames = $sizes | ForEach-Object { , (New-Frame $_) }
$fs = [System.IO.File]::Create($out)
$bw = New-Object System.IO.BinaryWriter $fs
$bw.Write([uint16]0); $bw.Write([uint16]1); $bw.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]; $data = $frames[$i]
    $dim = if ($s -ge 256) { 0 } else { $s }
    $bw.Write([byte]$dim); $bw.Write([byte]$dim); $bw.Write([byte]0); $bw.Write([byte]0)
    $bw.Write([uint16]1); $bw.Write([uint16]32)
    $bw.Write([uint32]$data.Length); $bw.Write([uint32]$offset)
    $offset += $data.Length
}
foreach ($data in $frames) { $bw.Write($data) }
$bw.Close(); $fs.Close()
Write-Host "Wrote $out"
