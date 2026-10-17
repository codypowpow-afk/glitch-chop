# Regenerates desktop\app.ico: peach tile with dark waveform chops, as PNG-in-ICO at 16-256 px.
Add-Type -AssemblyName System.Drawing
$sizes = 256, 64, 48, 32, 16
$pngs = foreach ($s in $sizes) {
    $bmp = New-Object Drawing.Bitmap $s, $s
    $g = [Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $r = [single]($s * 0.22); $d = $r * 2; $w = [single]($s - 1)
    $path = New-Object Drawing.Drawing2D.GraphicsPath
    $path.AddArc(0, 0, $d, $d, 180, 90); $path.AddArc($w - $d, 0, $d, $d, 270, 90)
    $path.AddArc($w - $d, $w - $d, $d, $d, 0, 90); $path.AddArc(0, $w - $d, $d, $d, 90, 90); $path.CloseFigure()
    $g.FillPath((New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(255, 158, 94))), $path)
    $ink = New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(22, 17, 26))
    $heights = 0.30, 0.62, 0.44, 0.78, 0.36
    $bw = $s * 0.11; $gap = $s * 0.045; $x = ($s - (5 * $bw + 4 * $gap)) / 2
    for ($i = 0; $i -lt 5; $i++) {
        $h = $s * $heights[$i]
        $g.FillRectangle($ink, [single]$x, [single](($s - $h) / 2), [single]$bw, [single]$h)
        $x += $bw + $gap
    }
    $g.Dispose()
    $ms = New-Object IO.MemoryStream; $bmp.Save($ms, [Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
    , $ms.ToArray()
}
$out = New-Object IO.MemoryStream; $bw = New-Object IO.BinaryWriter $out
$bw.Write([uint16]0); $bw.Write([uint16]1); $bw.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]; $b = [byte]($(if ($s -ge 256) { 0 } else { $s }))
    $bw.Write($b); $bw.Write($b); $bw.Write([byte]0); $bw.Write([byte]0)
    $bw.Write([uint16]1); $bw.Write([uint16]32); $bw.Write([uint32]$pngs[$i].Length); $bw.Write([uint32]$offset)
    $offset += $pngs[$i].Length
}
foreach ($p in $pngs) { $bw.Write($p) }
[IO.File]::WriteAllBytes((Join-Path $PSScriptRoot 'app.ico'), $out.ToArray())
