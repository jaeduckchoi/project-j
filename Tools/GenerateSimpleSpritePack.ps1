Add-Type -AssemblyName System.Drawing

$spriteRoot = Join-Path $PSScriptRoot '..\Assets\Generated\Sprites'

function New-Color {
    param(
        [int]$R,
        [int]$G,
        [int]$B,
        [int]$A = 255
    )

    return [System.Drawing.Color]::FromArgb($A, $R, $G, $B)
}

function New-Point {
    param(
        [int]$X,
        [int]$Y
    )

    return [System.Drawing.Point]::new($X, $Y)
}

function Draw-Shadow {
    param(
        [System.Drawing.Graphics]$Graphics,
        [int]$X,
        [int]$Y,
        [int]$Width,
        [int]$Height,
        [int]$Alpha = 60
    )

    $shadowBrush = [System.Drawing.SolidBrush]::new((New-Color 16 28 44 $Alpha))
    try {
        $Graphics.FillEllipse($shadowBrush, $X, $Y, $Width, $Height)
    }
    finally {
        $shadowBrush.Dispose()
    }
}

function Draw-OutlinePolygon {
    param(
        [System.Drawing.Graphics]$Graphics,
        [System.Drawing.Point[]]$Points,
        [System.Drawing.Color]$FillColor,
        [System.Drawing.Color]$StrokeColor,
        [int]$StrokeWidth = 10
    )

    $fillBrush = [System.Drawing.SolidBrush]::new($FillColor)
    $strokePen = [System.Drawing.Pen]::new($StrokeColor, $StrokeWidth)
    $strokePen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    try {
        $Graphics.FillPolygon($fillBrush, $Points)
        $Graphics.DrawPolygon($strokePen, $Points)
    }
    finally {
        $strokePen.Dispose()
        $fillBrush.Dispose()
    }
}

function Draw-OutlineEllipse {
    param(
        [System.Drawing.Graphics]$Graphics,
        [int]$X,
        [int]$Y,
        [int]$Width,
        [int]$Height,
        [System.Drawing.Color]$FillColor,
        [System.Drawing.Color]$StrokeColor,
        [int]$StrokeWidth = 10
    )

    $fillBrush = [System.Drawing.SolidBrush]::new($FillColor)
    $strokePen = [System.Drawing.Pen]::new($StrokeColor, $StrokeWidth)
    $strokePen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    try {
        $Graphics.FillEllipse($fillBrush, $X, $Y, $Width, $Height)
        $Graphics.DrawEllipse($strokePen, $X, $Y, $Width, $Height)
    }
    finally {
        $strokePen.Dispose()
        $fillBrush.Dispose()
    }
}

function Draw-OutlineRectangle {
    param(
        [System.Drawing.Graphics]$Graphics,
        [int]$X,
        [int]$Y,
        [int]$Width,
        [int]$Height,
        [System.Drawing.Color]$FillColor,
        [System.Drawing.Color]$StrokeColor,
        [int]$StrokeWidth = 10
    )

    $fillBrush = [System.Drawing.SolidBrush]::new($FillColor)
    $strokePen = [System.Drawing.Pen]::new($StrokeColor, $StrokeWidth)
    $strokePen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    try {
        $Graphics.FillRectangle($fillBrush, $X, $Y, $Width, $Height)
        $Graphics.DrawRectangle($strokePen, $X, $Y, $Width, $Height)
    }
    finally {
        $strokePen.Dispose()
        $fillBrush.Dispose()
    }
}

function Draw-ArcStroke {
    param(
        [System.Drawing.Graphics]$Graphics,
        [int]$X,
        [int]$Y,
        [int]$Width,
        [int]$Height,
        [int]$StartAngle,
        [int]$SweepAngle,
        [System.Drawing.Color]$StrokeColor,
        [int]$StrokeWidth = 10
    )

    $strokePen = [System.Drawing.Pen]::new($StrokeColor, $StrokeWidth)
    $strokePen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $strokePen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    try {
        $Graphics.DrawArc($strokePen, $X, $Y, $Width, $Height, $StartAngle, $SweepAngle)
    }
    finally {
        $strokePen.Dispose()
    }
}

function Save-Sprite {
    param(
        [string]$FileName,
        [scriptblock]$DrawAction
    )

    $path = Join-Path $spriteRoot $FileName
    $bitmap = [System.Drawing.Bitmap]::new(256, 256)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.Clear([System.Drawing.Color]::Transparent)

    try {
        & $DrawAction $graphics
        $bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

Save-Sprite 'Fish.png' {
    param($g)

    Draw-Shadow $g 58 186 138 28 54
    $body = @(
        (New-Point 58 136),
        (New-Point 118 86),
        (New-Point 182 94),
        (New-Point 216 128),
        (New-Point 178 160),
        (New-Point 116 170)
    )
    $tail = @(
        (New-Point 48 132),
        (New-Point 20 94),
        (New-Point 22 168)
    )
    Draw-OutlinePolygon $g $body (New-Color 79 188 235) (New-Color 18 77 119) 10
    Draw-OutlinePolygon $g $tail (New-Color 49 151 212) (New-Color 18 77 119) 10
    Draw-OutlineEllipse $g 102 120 78 34 (New-Color 205 241 255 190) (New-Color 205 241 255 0) 0
    Draw-OutlineEllipse $g 155 112 18 18 (New-Color 255 255 255) (New-Color 18 77 119) 6
    Draw-OutlineEllipse $g 160 117 7 7 (New-Color 18 77 119) (New-Color 18 77 119) 2
    $fin = @(
        (New-Point 122 102),
        (New-Point 142 70),
        (New-Point 154 108)
    )
    Draw-OutlinePolygon $g $fin (New-Color 38 125 187) (New-Color 18 77 119) 8
}

Save-Sprite 'Shell.png' {
    param($g)

    Draw-Shadow $g 60 190 132 24 46
    Draw-OutlineEllipse $g 58 78 140 130 (New-Color 248 226 199) (New-Color 145 108 82) 10
    $ridgePen = [System.Drawing.Pen]::new((New-Color 210 176 145), 6)
    try {
        $ridgePen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $ridgePen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        foreach ($x in 82, 104, 128, 152, 176) {
            $g.DrawLine($ridgePen, $x, 96, 128, 198)
        }
    }
    finally {
        $ridgePen.Dispose()
    }
    Draw-ArcStroke $g 74 100 112 88 18 144 (New-Color 255 248 239) 8
}

Save-Sprite 'Seaweed.png' {
    param($g)

    Draw-Shadow $g 62 196 132 26 44
    $baseBrush = [System.Drawing.SolidBrush]::new((New-Color 26 120 73))
    $accentPen = [System.Drawing.Pen]::new((New-Color 81 196 127), 8)
    $strokePen = [System.Drawing.Pen]::new((New-Color 16 79 49), 10)
    $strokePen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $strokePen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    try {
        $fronds = @(
            @(92, 186, 74, 136, 92, 84),
            @(128, 196, 126, 140, 138, 76),
            @(164, 188, 188, 130, 174, 82)
        )
        foreach ($frond in $fronds) {
            $points = @(
                (New-Point ($frond[0] - 10) $frond[1]),
                (New-Point ($frond[0] - 28) ($frond[1] - 34)),
                (New-Point ($frond[0] - 18) ($frond[1] - 76)),
                (New-Point $frond[2] $frond[3]),
                (New-Point ($frond[2] + 10) ($frond[3] - 44)),
                (New-Point $frond[4] $frond[5]),
                (New-Point ($frond[4] + 14) ($frond[5] + 10)),
                (New-Point ($frond[0] + 12) ($frond[1] - 10))
            )
            $g.FillClosedCurve($baseBrush, $points)
            $g.DrawClosedCurve($strokePen, $points)
        }
        $g.DrawBezier($accentPen, 86, 184, 72, 148, 84, 118, 92, 88)
        $g.DrawBezier($accentPen, 128, 194, 118, 150, 126, 116, 138, 78)
        $g.DrawBezier($accentPen, 166, 186, 188, 154, 186, 112, 174, 84)
    }
    finally {
        $strokePen.Dispose()
        $accentPen.Dispose()
        $baseBrush.Dispose()
    }
}

Save-Sprite 'Herb.png' {
    param($g)

    Draw-Shadow $g 66 196 124 26 42
    $stemPen = [System.Drawing.Pen]::new((New-Color 78 112 47), 10)
    $stemPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $stemPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    try {
        $g.DrawLine($stemPen, 126, 196, 124, 106)
        $leafSets = @(
            @(82, 128, 118, 110, 128, 146, 96, 158),
            @(126, 86, 164, 104, 148, 142, 118, 120),
            @(96, 168, 132, 146, 148, 180, 112, 202),
            @(132, 148, 182, 128, 180, 174, 140, 184)
        )
        foreach ($leaf in $leafSets) {
            $points = @(
                (New-Point $leaf[0] $leaf[1]),
                (New-Point $leaf[2] $leaf[3]),
                (New-Point $leaf[4] $leaf[5]),
                (New-Point $leaf[6] $leaf[7])
            )
            Draw-OutlinePolygon $g $points (New-Color 118 198 86) (New-Color 52 118 38) 8
        }
    }
    finally {
        $stemPen.Dispose()
    }
}

Save-Sprite 'Mushroom.png' {
    param($g)

    Draw-Shadow $g 66 196 124 26 44
    Draw-OutlineRectangle $g 104 124 48 84 (New-Color 244 226 199) (New-Color 150 117 88) 8
    $cap = @(
        (New-Point 52 142),
        (New-Point 78 92),
        (New-Point 124 72),
        (New-Point 174 82),
        (New-Point 208 130),
        (New-Point 196 158),
        (New-Point 62 158)
    )
    Draw-OutlinePolygon $g $cap (New-Color 183 105 74) (New-Color 103 55 39) 10
    foreach ($x in 86, 118, 150, 178) {
        Draw-OutlineEllipse $g $x 108 18 18 (New-Color 252 236 214) (New-Color 252 236 214) 2
    }
}

Save-Sprite 'GlowMoss.png' {
    param($g)

    Draw-Shadow $g 52 192 148 30 58
    $glowBrush = [System.Drawing.SolidBrush]::new((New-Color 99 255 210 80))
    try {
        $g.FillEllipse($glowBrush, 56, 98, 132, 112)
        $g.FillEllipse($glowBrush, 118, 84, 84, 94)
    }
    finally {
        $glowBrush.Dispose()
    }
    Draw-OutlineEllipse $g 66 118 82 70 (New-Color 87 233 181) (New-Color 23 108 92) 8
    Draw-OutlineEllipse $g 118 100 92 78 (New-Color 117 246 205) (New-Color 23 108 92) 8
    Draw-OutlineEllipse $g 98 146 72 58 (New-Color 63 208 168) (New-Color 23 108 92) 8
    $sparkPen = [System.Drawing.Pen]::new((New-Color 230 255 244), 6)
    $sparkPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $sparkPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    try {
        $g.DrawLine($sparkPen, 94, 80, 94, 102)
        $g.DrawLine($sparkPen, 84, 91, 104, 91)
        $g.DrawLine($sparkPen, 176, 74, 176, 94)
        $g.DrawLine($sparkPen, 166, 84, 186, 84)
    }
    finally {
        $sparkPen.Dispose()
    }
}

Save-Sprite 'WindHerb.png' {
    param($g)

    Draw-Shadow $g 60 196 134 24 46
    $swirlPen = [System.Drawing.Pen]::new((New-Color 200 239 255), 10)
    $swirlPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $swirlPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    try {
        Draw-ArcStroke $g 56 96 96 72 214 228 (New-Color 200 239 255) 10
        Draw-ArcStroke $g 116 74 86 68 210 232 (New-Color 200 239 255) 10
        $g.DrawBezier($swirlPen, 78, 174, 108, 162, 132, 128, 154, 92)
        $g.DrawBezier($swirlPen, 112, 188, 148, 176, 178, 146, 188, 112)
    }
    finally {
        $swirlPen.Dispose()
    }

    $leaf = @(
        (New-Point 90 170),
        (New-Point 142 126),
        (New-Point 188 110),
        (New-Point 170 170),
        (New-Point 116 194)
    )
    Draw-OutlinePolygon $g $leaf (New-Color 182 224 110) (New-Color 85 126 45) 8
    Draw-ArcStroke $g 102 126 70 54 188 128 (New-Color 233 248 199) 6
}

Save-Sprite 'Portal.png' {
    param($g)

    Draw-Shadow $g 48 188 160 34 60
    Draw-OutlineEllipse $g 52 48 152 152 (New-Color 255 185 92 54) (New-Color 214 109 46) 12
    Draw-OutlineEllipse $g 82 78 92 92 (New-Color 125 219 245 180) (New-Color 46 126 173) 8
    foreach ($angle in 0, 60, 120, 180, 240, 300) {
        $centerX = 128 + [Math]::Cos($angle * [Math]::PI / 180) * 74
        $centerY = 124 + [Math]::Sin($angle * [Math]::PI / 180) * 74
        Draw-OutlineEllipse $g ([int]$centerX - 8) ([int]$centerY - 8) 16 16 (New-Color 255 226 171) (New-Color 214 109 46) 4
    }
}

Save-Sprite 'Selector.png' {
    param($g)

    Draw-Shadow $g 56 190 144 24 36
    $diamond = @(
        (New-Point 128 46),
        (New-Point 208 126),
        (New-Point 128 206),
        (New-Point 48 126)
    )
    Draw-OutlinePolygon $g $diamond (New-Color 255 226 71 60) (New-Color 211 162 22) 10
    $innerDiamond = @(
        (New-Point 128 74),
        (New-Point 180 126),
        (New-Point 128 178),
        (New-Point 76 126)
    )
    Draw-OutlinePolygon $g $innerDiamond (New-Color 255 244 179 50) (New-Color 246 206 74) 8
    Draw-OutlineEllipse $g 116 114 24 24 (New-Color 255 247 196) (New-Color 211 162 22) 6
}

Save-Sprite 'Counter.png' {
    param($g)

    Draw-OutlineRectangle $g 24 48 208 160 (New-Color 170 110 71) (New-Color 95 60 39) 12
    Draw-OutlineRectangle $g 20 34 216 34 (New-Color 201 143 92) (New-Color 95 60 39) 10
    $linePen = [System.Drawing.Pen]::new((New-Color 124 79 53), 6)
    try {
        foreach ($y in 80, 112, 144, 176) {
            $g.DrawLine($linePen, 36, $y, 220, $y)
        }
        foreach ($x in 72, 126, 180) {
            $g.DrawLine($linePen, $x, 66, $x, 208)
        }
    }
    finally {
        $linePen.Dispose()
    }
}

Save-Sprite 'Floor.png' {
    param($g)

    Draw-OutlineRectangle $g 12 12 232 232 (New-Color 232 223 200) (New-Color 169 152 126) 10
    $tilePen = [System.Drawing.Pen]::new((New-Color 194 179 150), 6)
    try {
        foreach ($offset in 72, 128, 184) {
            $g.DrawLine($tilePen, $offset, 18, $offset, 238)
            $g.DrawLine($tilePen, 18, $offset, 238, $offset)
        }
    }
    finally {
        $tilePen.Dispose()
    }
    Draw-OutlineEllipse $g 92 92 72 72 (New-Color 244 237 221 70) (New-Color 244 237 221 0) 0
}

Write-Host 'Simple sprite pack generated.'
