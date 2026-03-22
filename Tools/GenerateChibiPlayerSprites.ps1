Add-Type -AssemblyName System.Drawing

$generatedRoot = Join-Path $PSScriptRoot '..\Assets\Generated\Sprites'
$resourceRoot = Join-Path $PSScriptRoot '..\Assets\Resources\Generated\Sprites'

function New-Color {
    param(
        [int]$R,
        [int]$G,
        [int]$B,
        [int]$A = 255
    )

    return [System.Drawing.Color]::FromArgb($A, $R, $G, $B)
}

function Sx {
    param([int]$Width, [double]$Value)
    return [int][Math]::Round($Value * $Width / 700.0)
}

function Sy {
    param([int]$Height, [double]$Value)
    return [int][Math]::Round($Value * $Height / 1050.0)
}

function Sw {
    param([int]$Width, [int]$Height, [double]$Value)
    $scale = (($Width / 700.0) + ($Height / 1050.0)) * 0.5
    return [single]([Math]::Max(1.0, $Value * $scale))
}

function New-ScaledPoint {
    param(
        [int]$Width,
        [int]$Height,
        [double]$X,
        [double]$Y
    )

    return [System.Drawing.Point]::new((Sx $Width $X), (Sy $Height $Y))
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
        [single]$StrokeWidth
    )

    $fillBrush = [System.Drawing.SolidBrush]::new($FillColor)
    try {
        $Graphics.FillEllipse($fillBrush, $X, $Y, $Width, $Height)
    }
    finally {
        $fillBrush.Dispose()
    }

    if ($StrokeWidth -gt 0) {
        $strokePen = [System.Drawing.Pen]::new($StrokeColor, $StrokeWidth)
        $strokePen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
        try {
            $Graphics.DrawEllipse($strokePen, $X, $Y, $Width, $Height)
        }
        finally {
            $strokePen.Dispose()
        }
    }
}

function Draw-OutlinePolygon {
    param(
        [System.Drawing.Graphics]$Graphics,
        [System.Drawing.Point[]]$Points,
        [System.Drawing.Color]$FillColor,
        [System.Drawing.Color]$StrokeColor,
        [single]$StrokeWidth
    )

    $fillBrush = [System.Drawing.SolidBrush]::new($FillColor)
    try {
        $Graphics.FillPolygon($fillBrush, $Points)
    }
    finally {
        $fillBrush.Dispose()
    }

    if ($StrokeWidth -gt 0) {
        $strokePen = [System.Drawing.Pen]::new($StrokeColor, $StrokeWidth)
        $strokePen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
        try {
            $Graphics.DrawPolygon($strokePen, $Points)
        }
        finally {
            $strokePen.Dispose()
        }
    }
}

function Draw-ClosedCurveShape {
    param(
        [System.Drawing.Graphics]$Graphics,
        [System.Drawing.Point[]]$Points,
        [System.Drawing.Color]$FillColor,
        [System.Drawing.Color]$StrokeColor,
        [single]$StrokeWidth,
        [single]$Tension = 0.5
    )

    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $fillBrush = [System.Drawing.SolidBrush]::new($FillColor)
    try {
        $path.AddClosedCurve($Points, $Tension)
        $Graphics.FillPath($fillBrush, $path)
        if ($StrokeWidth -gt 0) {
            $strokePen = [System.Drawing.Pen]::new($StrokeColor, $StrokeWidth)
            $strokePen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
            try {
                $Graphics.DrawPath($strokePen, $path)
            }
            finally {
                $strokePen.Dispose()
            }
        }
    }
    finally {
        $fillBrush.Dispose()
        $path.Dispose()
    }
}

function Draw-OutlinedLine {
    param(
        [System.Drawing.Graphics]$Graphics,
        [int]$X1,
        [int]$Y1,
        [int]$X2,
        [int]$Y2,
        [System.Drawing.Color]$LineColor,
        [System.Drawing.Color]$OutlineColor,
        [single]$LineWidth,
        [single]$OutlineWidth
    )

    $outlinePen = [System.Drawing.Pen]::new($OutlineColor, $OutlineWidth)
    $linePen = [System.Drawing.Pen]::new($LineColor, $LineWidth)
    foreach ($pen in @($outlinePen, $linePen)) {
        $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    }

    try {
        $Graphics.DrawLine($outlinePen, $X1, $Y1, $X2, $Y2)
        $Graphics.DrawLine($linePen, $X1, $Y1, $X2, $Y2)
    }
    finally {
        $linePen.Dispose()
        $outlinePen.Dispose()
    }
}

function Draw-CharacterShadow {
    param(
        [System.Drawing.Graphics]$Graphics,
        [int]$Width,
        [int]$Height
    )

    $shadow = [System.Drawing.SolidBrush]::new((New-Color 17 24 42 56))
    try {
        $Graphics.FillEllipse(
            $shadow,
            (Sx $Width 236),
            (Sy $Height 920),
            (Sx $Width 228),
            (Sy $Height 50))
    }
    finally {
        $shadow.Dispose()
    }
}

function Draw-FrontCharacter {
    param(
        [System.Drawing.Graphics]$Graphics,
        [int]$Width,
        [int]$Height
    )

    $outline = New-Color 20 18 18
    $hair = New-Color 96 70 54
    $hairDark = New-Color 79 56 44
    $skin = New-Color 222 191 150
    $skinShade = New-Color 205 173 136
    $apron = New-Color 83 104 96
    $jacket = New-Color 97 93 84
    $collar = New-Color 61 57 50
    $pants = New-Color 36 40 73
    $bootDark = New-Color 27 28 45
    $eye = New-Color 228 225 218
    $eyeShade = New-Color 160 153 148

    Draw-CharacterShadow $Graphics $Width $Height

    Draw-OutlineEllipse $Graphics (Sx $Width 250) (Sy $Height 882) (Sx $Width 90) (Sy $Height 122) $pants $outline (Sw $Width $Height 10)
    Draw-OutlineEllipse $Graphics (Sx $Width 360) (Sy $Height 882) (Sx $Width 90) (Sy $Height 122) $pants $outline (Sw $Width $Height 10)
    Draw-OutlineEllipse $Graphics (Sx $Width 244) (Sy $Height 918) (Sx $Width 104) (Sy $Height 90) $bootDark $outline (Sw $Width $Height 10)
    Draw-OutlineEllipse $Graphics (Sx $Width 352) (Sy $Height 918) (Sx $Width 104) (Sy $Height 90) $bootDark $outline (Sw $Width $Height 10)

    $outerBody = @(
        (New-ScaledPoint $Width $Height 228 566),
        (New-ScaledPoint $Width $Height 224 812),
        (New-ScaledPoint $Width $Height 262 858),
        (New-ScaledPoint $Width $Height 438 858),
        (New-ScaledPoint $Width $Height 476 812),
        (New-ScaledPoint $Width $Height 472 566),
        (New-ScaledPoint $Width $Height 404 528),
        (New-ScaledPoint $Width $Height 298 528)
    )
    Draw-ClosedCurveShape $Graphics $outerBody $jacket $outline (Sw $Width $Height 11)

    $apronBody = @(
        (New-ScaledPoint $Width $Height 266 566),
        (New-ScaledPoint $Width $Height 258 844),
        (New-ScaledPoint $Width $Height 442 844),
        (New-ScaledPoint $Width $Height 434 566),
        (New-ScaledPoint $Width $Height 400 542),
        (New-ScaledPoint $Width $Height 302 542)
    )
    Draw-ClosedCurveShape $Graphics $apronBody $apron $outline (Sw $Width $Height 9)
    Draw-OutlineEllipse $Graphics (Sx $Width 283) (Sy $Height 533) (Sx $Width 134) (Sy $Height 42) $collar $outline (Sw $Width $Height 8)

    $leftSleeve = @(
        (New-ScaledPoint $Width $Height 237 575),
        (New-ScaledPoint $Width $Height 182 553),
        (New-ScaledPoint $Width $Height 150 618),
        (New-ScaledPoint $Width $Height 223 648)
    )
    $rightSleeve = @(
        (New-ScaledPoint $Width $Height 463 575),
        (New-ScaledPoint $Width $Height 518 553),
        (New-ScaledPoint $Width $Height 550 618),
        (New-ScaledPoint $Width $Height 477 648)
    )
    Draw-OutlinePolygon $Graphics $leftSleeve $jacket $outline (Sw $Width $Height 8)
    Draw-OutlinePolygon $Graphics $rightSleeve $jacket $outline (Sw $Width $Height 8)
    Draw-OutlinedLine $Graphics (Sx $Width 225) (Sy $Height 608) (Sx $Width 156) (Sy $Height 570) $jacket $outline (Sw $Width $Height 24) (Sw $Width $Height 40)
    Draw-OutlinedLine $Graphics (Sx $Width 475) (Sy $Height 608) (Sx $Width 544) (Sy $Height 570) $jacket $outline (Sw $Width $Height 24) (Sw $Width $Height 40)
    Draw-OutlineEllipse $Graphics (Sx $Width 56) (Sy $Height 490) (Sx $Width 118) (Sy $Height 118) $skin $outline (Sw $Width $Height 10)
    Draw-OutlineEllipse $Graphics (Sx $Width 526) (Sy $Height 490) (Sx $Width 118) (Sy $Height 118) $skin $outline (Sw $Width $Height 10)

    Draw-OutlineEllipse $Graphics (Sx $Width 138) (Sy $Height 390) (Sx $Width 44) (Sy $Height 64) $skin $outline (Sw $Width $Height 8)
    Draw-OutlineEllipse $Graphics (Sx $Width 518) (Sy $Height 390) (Sx $Width 44) (Sy $Height 64) $skin $outline (Sw $Width $Height 8)

    Draw-OutlineEllipse $Graphics (Sx $Width 140) (Sy $Height 182) (Sx $Width 420) (Sy $Height 374) $skin $outline (Sw $Width $Height 10)
    Draw-OutlineEllipse $Graphics (Sx $Width 88) (Sy $Height 42) (Sx $Width 524) (Sy $Height 470) $hair $outline (Sw $Width $Height 12)

    $bangLeft = @(
        (New-ScaledPoint $Width $Height 180 224),
        (New-ScaledPoint $Width $Height 218 140),
        (New-ScaledPoint $Width $Height 248 228),
        (New-ScaledPoint $Width $Height 220 356)
    )
    $bangCenterLeft = @(
        (New-ScaledPoint $Width $Height 244 210),
        (New-ScaledPoint $Width $Height 296 126),
        (New-ScaledPoint $Width $Height 320 284),
        (New-ScaledPoint $Width $Height 282 406)
    )
    $bangCenter = @(
        (New-ScaledPoint $Width $Height 308 206),
        (New-ScaledPoint $Width $Height 348 132),
        (New-ScaledPoint $Width $Height 370 308),
        (New-ScaledPoint $Width $Height 330 428)
    )
    $bangCenterRight = @(
        (New-ScaledPoint $Width $Height 374 214),
        (New-ScaledPoint $Width $Height 436 146),
        (New-ScaledPoint $Width $Height 440 300),
        (New-ScaledPoint $Width $Height 398 382)
    )
    $bangRight = @(
        (New-ScaledPoint $Width $Height 432 230),
        (New-ScaledPoint $Width $Height 510 172),
        (New-ScaledPoint $Width $Height 488 288),
        (New-ScaledPoint $Width $Height 448 334)
    )
    Draw-OutlinePolygon $Graphics $bangLeft $hairDark $outline (Sw $Width $Height 8)
    Draw-OutlinePolygon $Graphics $bangCenterLeft $hairDark $outline (Sw $Width $Height 8)
    Draw-OutlinePolygon $Graphics $bangCenter $hairDark $outline (Sw $Width $Height 8)
    Draw-OutlinePolygon $Graphics $bangCenterRight $hairDark $outline (Sw $Width $Height 8)
    Draw-OutlinePolygon $Graphics $bangRight $hairDark $outline (Sw $Width $Height 8)

    Draw-OutlineEllipse $Graphics (Sx $Width 190) (Sy $Height 388) (Sx $Width 124) (Sy $Height 88) $eye $outline (Sw $Width $Height 7)
    Draw-OutlineEllipse $Graphics (Sx $Width 386) (Sy $Height 388) (Sx $Width 124) (Sy $Height 88) $eye $outline (Sw $Width $Height 7)
    Draw-OutlineEllipse $Graphics (Sx $Width 220) (Sy $Height 432) (Sx $Width 13) (Sy $Height 13) $eyeShade $eyeShade 0
    Draw-OutlineEllipse $Graphics (Sx $Width 415) (Sy $Height 432) (Sx $Width 13) (Sy $Height 13) $eyeShade $eyeShade 0
    Draw-OutlineEllipse $Graphics (Sx $Width 220) (Sy $Height 426) (Sx $Width 20) (Sy $Height 8) $eyeShade $eyeShade 0
    Draw-OutlineEllipse $Graphics (Sx $Width 415) (Sy $Height 426) (Sx $Width 20) (Sy $Height 8) $eyeShade $eyeShade 0
    Draw-OutlineEllipse $Graphics (Sx $Width 334) (Sy $Height 504) (Sx $Width 8) (Sy $Height 8) (New-Color 132 95 82) (New-Color 132 95 82) 0
    Draw-OutlinedLine $Graphics (Sx $Width 320) (Sy $Height 548) (Sx $Width 346) (Sy $Height 548) (New-Color 128 87 79) $outline (Sw $Width $Height 4) (Sw $Width $Height 4)

    Draw-OutlineEllipse $Graphics (Sx $Width 80) (Sy $Height 542) (Sx $Width 26) (Sy $Height 26) $skinShade $skinShade 0
    Draw-OutlineEllipse $Graphics (Sx $Width 550) (Sy $Height 542) (Sx $Width 26) (Sy $Height 26) $skinShade $skinShade 0
}

function Draw-BackCharacter {
    param(
        [System.Drawing.Graphics]$Graphics,
        [int]$Width,
        [int]$Height
    )

    $outline = New-Color 20 18 18
    $hair = New-Color 93 68 53
    $hairDark = New-Color 73 52 40
    $skin = New-Color 222 191 150
    $apron = New-Color 83 104 96
    $jacket = New-Color 97 93 84
    $collar = New-Color 61 57 50
    $pants = New-Color 36 40 73
    $bootDark = New-Color 27 28 45
    $strap = New-Color 196 210 205

    Draw-CharacterShadow $Graphics $Width $Height

    Draw-OutlineEllipse $Graphics (Sx $Width 252) (Sy $Height 884) (Sx $Width 88) (Sy $Height 120) $pants $outline (Sw $Width $Height 10)
    Draw-OutlineEllipse $Graphics (Sx $Width 360) (Sy $Height 884) (Sx $Width 88) (Sy $Height 120) $pants $outline (Sw $Width $Height 10)
    Draw-OutlineEllipse $Graphics (Sx $Width 246) (Sy $Height 920) (Sx $Width 102) (Sy $Height 88) $bootDark $outline (Sw $Width $Height 10)
    Draw-OutlineEllipse $Graphics (Sx $Width 352) (Sy $Height 920) (Sx $Width 102) (Sy $Height 88) $bootDark $outline (Sw $Width $Height 10)

    $outerBody = @(
        (New-ScaledPoint $Width $Height 226 572),
        (New-ScaledPoint $Width $Height 224 816),
        (New-ScaledPoint $Width $Height 264 860),
        (New-ScaledPoint $Width $Height 436 860),
        (New-ScaledPoint $Width $Height 476 816),
        (New-ScaledPoint $Width $Height 474 572),
        (New-ScaledPoint $Width $Height 410 534),
        (New-ScaledPoint $Width $Height 292 534)
    )
    Draw-ClosedCurveShape $Graphics $outerBody $jacket $outline (Sw $Width $Height 11)

    $apronBack = @(
        (New-ScaledPoint $Width $Height 270 568),
        (New-ScaledPoint $Width $Height 262 846),
        (New-ScaledPoint $Width $Height 438 846),
        (New-ScaledPoint $Width $Height 430 568),
        (New-ScaledPoint $Width $Height 394 540),
        (New-ScaledPoint $Width $Height 306 540)
    )
    Draw-ClosedCurveShape $Graphics $apronBack $apron $outline (Sw $Width $Height 9)
    Draw-OutlineEllipse $Graphics (Sx $Width 301) (Sy $Height 546) (Sx $Width 100) (Sy $Height 30) $collar $outline (Sw $Width $Height 8)

    Draw-OutlinedLine $Graphics (Sx $Width 268) (Sy $Height 574) (Sx $Width 350) (Sy $Height 646) $strap $outline (Sw $Width $Height 13) (Sw $Width $Height 22)
    Draw-OutlinedLine $Graphics (Sx $Width 432) (Sy $Height 574) (Sx $Width 350) (Sy $Height 646) $strap $outline (Sw $Width $Height 13) (Sw $Width $Height 22)

    $leftSleeve = @(
        (New-ScaledPoint $Width $Height 234 583),
        (New-ScaledPoint $Width $Height 180 560),
        (New-ScaledPoint $Width $Height 150 622),
        (New-ScaledPoint $Width $Height 222 650)
    )
    $rightSleeve = @(
        (New-ScaledPoint $Width $Height 466 583),
        (New-ScaledPoint $Width $Height 520 560),
        (New-ScaledPoint $Width $Height 550 622),
        (New-ScaledPoint $Width $Height 478 650)
    )
    Draw-OutlinePolygon $Graphics $leftSleeve $jacket $outline (Sw $Width $Height 8)
    Draw-OutlinePolygon $Graphics $rightSleeve $jacket $outline (Sw $Width $Height 8)
    Draw-OutlinedLine $Graphics (Sx $Width 226) (Sy $Height 614) (Sx $Width 156) (Sy $Height 576) $jacket $outline (Sw $Width $Height 24) (Sw $Width $Height 40)
    Draw-OutlinedLine $Graphics (Sx $Width 474) (Sy $Height 614) (Sx $Width 544) (Sy $Height 576) $jacket $outline (Sw $Width $Height 24) (Sw $Width $Height 40)
    Draw-OutlineEllipse $Graphics (Sx $Width 58) (Sy $Height 496) (Sx $Width 116) (Sy $Height 116) $skin $outline (Sw $Width $Height 10)
    Draw-OutlineEllipse $Graphics (Sx $Width 526) (Sy $Height 496) (Sx $Width 116) (Sy $Height 116) $skin $outline (Sw $Width $Height 10)

    Draw-OutlineEllipse $Graphics (Sx $Width 146) (Sy $Height 402) (Sx $Width 38) (Sy $Height 56) $skin $outline (Sw $Width $Height 8)
    Draw-OutlineEllipse $Graphics (Sx $Width 516) (Sy $Height 402) (Sx $Width 38) (Sy $Height 56) $skin $outline (Sw $Width $Height 8)
    Draw-OutlineEllipse $Graphics (Sx $Width 94) (Sy $Height 52) (Sx $Width 512) (Sy $Height 466) $hair $outline (Sw $Width $Height 12)

    $hairFall = @(
        (New-ScaledPoint $Width $Height 170 438),
        (New-ScaledPoint $Width $Height 218 518),
        (New-ScaledPoint $Width $Height 288 562),
        (New-ScaledPoint $Width $Height 350 576),
        (New-ScaledPoint $Width $Height 418 562),
        (New-ScaledPoint $Width $Height 486 516),
        (New-ScaledPoint $Width $Height 528 436),
        (New-ScaledPoint $Width $Height 488 382),
        (New-ScaledPoint $Width $Height 432 344),
        (New-ScaledPoint $Width $Height 270 344),
        (New-ScaledPoint $Width $Height 212 380)
    )
    Draw-ClosedCurveShape $Graphics $hairFall $hairDark $outline (Sw $Width $Height 9) 0.45
}

function Draw-SideCharacter {
    param(
        [System.Drawing.Graphics]$Graphics,
        [int]$Width,
        [int]$Height
    )

    $outline = New-Color 20 18 18
    $hair = New-Color 95 69 54
    $hairDark = New-Color 76 54 42
    $skin = New-Color 222 191 150
    $skinShade = New-Color 205 173 136
    $apron = New-Color 83 104 96
    $jacket = New-Color 97 93 84
    $collar = New-Color 61 57 50
    $pants = New-Color 36 40 73
    $bootDark = New-Color 27 28 45
    $eye = New-Color 228 225 218
    $eyeShade = New-Color 158 150 146

    Draw-CharacterShadow $Graphics $Width $Height

    Draw-OutlineEllipse $Graphics (Sx $Width 302) (Sy $Height 884) (Sx $Width 78) (Sy $Height 120) $pants $outline (Sw $Width $Height 10)
    Draw-OutlineEllipse $Graphics (Sx $Width 372) (Sy $Height 890) (Sx $Width 86) (Sy $Height 114) $pants $outline (Sw $Width $Height 10)
    Draw-OutlineEllipse $Graphics (Sx $Width 296) (Sy $Height 922) (Sx $Width 94) (Sy $Height 86) $bootDark $outline (Sw $Width $Height 10)
    Draw-OutlineEllipse $Graphics (Sx $Width 372) (Sy $Height 928) (Sx $Width 98) (Sy $Height 80) $bootDark $outline (Sw $Width $Height 10)

    $bodyOuter = @(
        (New-ScaledPoint $Width $Height 290 570),
        (New-ScaledPoint $Width $Height 286 830),
        (New-ScaledPoint $Width $Height 330 860),
        (New-ScaledPoint $Width $Height 446 860),
        (New-ScaledPoint $Width $Height 468 816),
        (New-ScaledPoint $Width $Height 458 584),
        (New-ScaledPoint $Width $Height 398 544),
        (New-ScaledPoint $Width $Height 320 548)
    )
    Draw-ClosedCurveShape $Graphics $bodyOuter $jacket $outline (Sw $Width $Height 11)

    $apronBody = @(
        (New-ScaledPoint $Width $Height 318 576),
        (New-ScaledPoint $Width $Height 316 844),
        (New-ScaledPoint $Width $Height 438 844),
        (New-ScaledPoint $Width $Height 430 582),
        (New-ScaledPoint $Width $Height 392 550),
        (New-ScaledPoint $Width $Height 334 552)
    )
    Draw-ClosedCurveShape $Graphics $apronBody $apron $outline (Sw $Width $Height 9)
    Draw-OutlineEllipse $Graphics (Sx $Width 338) (Sy $Height 550) (Sx $Width 82) (Sy $Height 28) $collar $outline (Sw $Width $Height 8)

    Draw-OutlinedLine $Graphics (Sx $Width 320) (Sy $Height 606) (Sx $Width 190) (Sy $Height 572) $jacket $outline (Sw $Width $Height 22) (Sw $Width $Height 38)
    Draw-OutlineEllipse $Graphics (Sx $Width 88) (Sy $Height 498) (Sx $Width 112) (Sy $Height 112) $skin $outline (Sw $Width $Height 10)
    Draw-OutlineEllipse $Graphics (Sx $Width 132) (Sy $Height 544) (Sx $Width 26) (Sy $Height 24) $skinShade $skinShade 0

    $face = @(
        (New-ScaledPoint $Width $Height 272 224),
        (New-ScaledPoint $Width $Height 224 312),
        (New-ScaledPoint $Width $Height 214 424),
        (New-ScaledPoint $Width $Height 244 538),
        (New-ScaledPoint $Width $Height 314 596),
        (New-ScaledPoint $Width $Height 410 582),
        (New-ScaledPoint $Width $Height 458 520),
        (New-ScaledPoint $Width $Height 470 330),
        (New-ScaledPoint $Width $Height 418 226),
        (New-ScaledPoint $Width $Height 314 204)
    )
    Draw-ClosedCurveShape $Graphics $face $skin $outline (Sw $Width $Height 10) 0.42

    Draw-OutlineEllipse $Graphics (Sx $Width 206) (Sy $Height 82) (Sx $Width 350) (Sy $Height 438) $hair $outline (Sw $Width $Height 12)

    $frontBang = @(
        (New-ScaledPoint $Width $Height 238 218),
        (New-ScaledPoint $Width $Height 174 180),
        (New-ScaledPoint $Width $Height 178 286),
        (New-ScaledPoint $Width $Height 240 338)
    )
    $middleBang = @(
        (New-ScaledPoint $Width $Height 286 206),
        (New-ScaledPoint $Width $Height 252 132),
        (New-ScaledPoint $Width $Height 310 288),
        (New-ScaledPoint $Width $Height 286 386)
    )
    $rearHair = @(
        (New-ScaledPoint $Width $Height 430 204),
        (New-ScaledPoint $Width $Height 508 240),
        (New-ScaledPoint $Width $Height 518 420),
        (New-ScaledPoint $Width $Height 456 470)
    )
    Draw-OutlinePolygon $Graphics $frontBang $hairDark $outline (Sw $Width $Height 8)
    Draw-OutlinePolygon $Graphics $middleBang $hairDark $outline (Sw $Width $Height 8)
    Draw-OutlinePolygon $Graphics $rearHair $hairDark $outline (Sw $Width $Height 8)

    Draw-OutlineEllipse $Graphics (Sx $Width 438) (Sy $Height 396) (Sx $Width 38) (Sy $Height 56) $skin $outline (Sw $Width $Height 8)
    Draw-OutlineEllipse $Graphics (Sx $Width 246) (Sy $Height 388) (Sx $Width 108) (Sy $Height 76) $eye $outline (Sw $Width $Height 7)
    Draw-OutlineEllipse $Graphics (Sx $Width 272) (Sy $Height 432) (Sx $Width 14) (Sy $Height 14) $eyeShade $eyeShade 0
    Draw-OutlinedLine $Graphics (Sx $Width 238) (Sy $Height 548) (Sx $Width 260) (Sy $Height 548) (New-Color 128 87 79) $outline (Sw $Width $Height 4) (Sw $Width $Height 4)
    Draw-OutlineEllipse $Graphics (Sx $Width 228) (Sy $Height 504) (Sx $Width 8) (Sy $Height 8) (New-Color 132 95 82) (New-Color 132 95 82) 0
}

function Save-CharacterSprite {
    param(
        [string[]]$Paths,
        [int]$Width,
        [int]$Height,
        [scriptblock]$DrawAction
    )

    $bitmap = [System.Drawing.Bitmap]::new($Width, $Height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.Clear([System.Drawing.Color]::Transparent)

    try {
        & $DrawAction $graphics $Width $Height
        foreach ($path in $Paths) {
            $bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
        }
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

$targets = @(
    @{
        Name = 'PlayerFront.png'
        Width = 648
        Height = 1043
        Draw = ${function:Draw-FrontCharacter}
    },
    @{
        Name = 'PlayerBack.png'
        Width = 661
        Height = 1039
        Draw = ${function:Draw-BackCharacter}
    },
    @{
        Name = 'PlayerSide.png'
        Width = 733
        Height = 1049
        Draw = ${function:Draw-SideCharacter}
    }
)

foreach ($target in $targets) {
    $outputPaths = @(
        (Join-Path $generatedRoot $target.Name),
        (Join-Path $resourceRoot $target.Name)
    )

    Save-CharacterSprite -Paths $outputPaths -Width $target.Width -Height $target.Height -DrawAction $target.Draw
}

Write-Host 'Chibi player sprites generated.'
