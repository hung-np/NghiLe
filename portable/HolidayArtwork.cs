using System;
using System.Windows;
using System.Windows.Media;

namespace NghiLe
{
    internal sealed partial class HolidayScene
    {
        private static readonly Brush AmberGlow = Glow("#FFD691", "#00FFD691");
        private static readonly Brush AmberLight = Paint("#FFF1C0");
        private static readonly Brush GoldSpark = Paint("#FFE1A5");
        private static readonly Brush MoonSpark = Paint("#E3D7FF");
        private static readonly Brush Snow = Paint("#F5FCFF");
        private static readonly Lazy<DrawingGroup> Envelope = DeferScene(DrawEnvelope);
        private static readonly Lazy<DrawingGroup> NewYearSpark = DeferScene(drawing =>
            DrawStar(drawing, new Point(), 2.8, GoldSpark, null));
        private static readonly Lazy<ImageDrawing> Palace = DeferImage(DrawPalace);
        private static readonly Lazy<DrawingGroup> PalaceFlag = DeferScene(DrawPalaceFlag);
        private static readonly Lazy<ImageDrawing> Workers = DeferImage(DrawWorkers);
        private static readonly Lazy<DrawingGroup> WorkersGear = DeferScene(DrawWorkersGear);
        private static readonly Point GearHub = new Point(101, 23);
        private static readonly Lazy<ImageDrawing> BaDinh = DeferImage(DrawBaDinh);
        private static readonly Lazy<DrawingGroup> NationalFlag = DeferScene(DrawNationalFlag);
        private static readonly Lazy<ImageDrawing> OperaHouse = DeferImage(DrawOperaHouse);
        private static readonly Lazy<DrawingGroup> OperaBeam = DeferScene(DrawOperaBeam);
        private static readonly Lazy<ImageDrawing> Moon = DeferImage(DrawMoon);
        private static readonly Lazy<DrawingGroup> MoonClouds = DeferScene(DrawMoonClouds);
        private static readonly Lazy<DrawingGroup> Lantern = DeferScene(DrawLantern);
        private static readonly Lazy<DrawingGroup> Mooncakes = DeferScene(DrawMooncakes);
        private static readonly Lazy<ImageDrawing> Pine = DeferImage(DrawPine);
        private static readonly Lazy<DrawingGroup> NewYearOrnament = DeferScene(DrawNewYearOrnament);
        private static readonly Lazy<ImageDrawing> ApricotBranch = DeferImage(DrawApricotBranch);
        private static readonly Point[] ApricotHooks =
        {
            new Point(51, 19), new Point(77, 23), new Point(104, 20), new Point(117, 32)
        };
        private static readonly double[] ApricotRestAngles = { -10, 7, -6, 9 };
        private static readonly Lazy<DrawingGroup> SquareRiceCake = DeferScene(DrawSquareRiceCake);
        private static readonly Lazy<DrawingGroup> RolledRiceCake = DeferScene(DrawRolledRiceCake);
        private static readonly Lazy<DrawingGroup> SpringSide = DeferScene(DrawSpringSide);
        private static readonly Lazy<DrawingGroup> HarvestSide = DeferScene(DrawHarvestSide);
        private static readonly Brush LanternGlow = Glow("#B9FFA754", "#00FFA754");
        private static readonly Brush ApricotPetal = Paint("#FFDB79");
        private static readonly Point[] ChristmasLights =
        {
            new Point(22, 17), new Point(36, 21), new Point(54, 24),
            new Point(91, 25), new Point(100, 31), new Point(88, 37), new Point(106, 43)
        };

        private void DrawHolidayArtwork(DrawingContext drawing, HolidayTheme holiday, double phase, bool horizontal)
        {
            switch (holiday)
            {
                case HolidayTheme.Reunification:
                    drawing.DrawDrawing(Palace.Value);
                    if (moving) DrawPalaceLight(drawing, phase);
                    DrawWavingFlag(drawing, PalaceFlag.Value, 64, 15, phase, 11);
                    break;
                case HolidayTheme.LabourDay:
                    drawing.DrawDrawing(Workers.Value);
                    if (moving) drawing.PushTransform(new RotateTransform(phase * 360, GearHub.X, GearHub.Y));
                    drawing.DrawDrawing(WorkersGear.Value);
                    if (moving) drawing.Pop();
                    break;
                case HolidayTheme.NationalDay:
                    drawing.DrawDrawing(BaDinh.Value);
                    DrawWavingFlag(drawing, NationalFlag.Value, 29, 24, phase, 8);
                    break;
                case HolidayTheme.VietnamCultureDay:
                    if (moving) DrawOperaLights(drawing, phase);
                    drawing.DrawDrawing(OperaHouse.Value);
                    break;
                case HolidayTheme.MidAutumn:
                    drawing.DrawDrawing(Moon.Value);
                    if (moving) drawing.PushTransform(new TranslateTransform(Math.Sin(phase * 2 * Math.PI) * 9, 0));
                    drawing.DrawDrawing(MoonClouds.Value);
                    if (moving) drawing.Pop();
                    if (horizontal) DrawHarvestForeground(drawing, phase);
                    break;
                case HolidayTheme.Christmas:
                    drawing.DrawDrawing(Pine.Value);
                    DrawChristmasLights(drawing, moving ? phase : 0, celebrating);
                    if (moving) DrawSnowfall(drawing, phase);
                    break;
                case HolidayTheme.LunarNewYear:
                    drawing.DrawDrawing(NewYearOrnament.Value);
                    if (celebrating) DrawNewYearSparks(drawing, moving ? phase : .22);
                    else if (moving) DrawFloatingLights(drawing, phase, 17, 98, 18, GoldSpark, 6);
                    if (apricotNewYear)
                    {
                        double movement = moving ? Math.Sin(phase * Math.PI * 2) * 2.8 : 0;
                        drawing.PushTransform(new RotateTransform(movement, 125, 13));
                        drawing.DrawDrawing(ApricotBranch.Value);
                        for (int i = 0; i < ApricotHooks.Length; i++)
                        {
                            var hook = ApricotHooks[i];
                            double sway = moving ? Math.Sin(phase * 4 * Math.PI + i * .95) * (10 - i * .8) : 0;
                            DrawHangingEnvelope(drawing, hook.X, hook.Y, ApricotRestAngles[i] + sway);
                        }
                        drawing.Pop();
                        if (moving) DrawApricotPetals(drawing, phase, celebrating);
                    }
                    else PeachScene.Draw(drawing, phase, moving, horizontal);
                    if (horizontal) drawing.DrawDrawing(apricotNewYear ? RolledRiceCake.Value : SquareRiceCake.Value);
                    break;
            }
        }

        private void DrawVerticalForeground(DrawingContext drawing, HolidayTheme holiday, double phase)
        {
            double ground = celebrating ? 183 : compact ? 153 : 137;
            if (holiday == HolidayTheme.LunarNewYear)
            {
                drawing.PushTransform(new TranslateTransform(10, ground - 4));
                drawing.DrawDrawing(SpringSide.Value);
                drawing.Pop();
                drawing.PushTransform(new TranslateTransform(ActualWidth - (apricotNewYear ? 63 : 127), ground - 39));
                drawing.DrawDrawing(apricotNewYear ? RolledRiceCake.Value : SquareRiceCake.Value);
                drawing.Pop();
            }
            else if (holiday == HolidayTheme.MidAutumn)
            {
                drawing.PushTransform(new TranslateTransform(9, ground - 4));
                drawing.DrawDrawing(HarvestSide.Value);
                drawing.Pop();
                drawing.PushTransform(new TranslateTransform(ActualWidth - 83, ground - 23));
                drawing.PushTransform(new ScaleTransform(.71, .71));
                DrawHarvestForeground(drawing, phase);
                drawing.Pop();
                drawing.Pop();
            }
        }

        private void DrawHarvestForeground(DrawingContext drawing, double phase)
        {
            double swing = moving ? Math.Sin(phase * 4 * Math.PI) * 15 : 0;
            drawing.PushTransform(new RotateTransform(swing, 44, 42));
            if (moving || celebrating)
            {
                double pulse = moving ? .74 + .26 * Math.Cos(phase * 4 * Math.PI) : 1;
                drawing.PushOpacity((celebrating ? .86 : .38) * pulse);
                drawing.DrawEllipse(LanternGlow, null, new Point(49, 49), 16, 9);
                drawing.Pop();
            }
            drawing.DrawDrawing(Lantern.Value);
            drawing.Pop();
            drawing.DrawDrawing(Mooncakes.Value);
        }

        private static void DrawSpringSide(DrawingContext drawing)
        {
            StrokePath(drawing, "#A6DEAE70", .7, "M2 15 Q11 16 15 10 Q18 5 23 8 Q27 10 24 12 Q20 15 17 12 M1 18 Q11 18 16 15 Q22 14 28 16");
            var rim = new Pen(Paint("#E9B775"), .75);
            drawing.DrawEllipse(Paint("#80523B"), rim, new Point(10, 12), 4, 3);
            drawing.DrawEllipse(Paint("#B47A4F"), rim, new Point(15, 15), 4, 3);
            Path(drawing, "#EFC18C", "M9 11 H11 V13 H9 Z M14 14 H16 V16 H14 Z");
            DrawStar(drawing, new Point(24, 5), 1.7, GoldSpark, null);
        }

        private static void DrawHarvestSide(DrawingContext drawing)
        {
            StrokePath(drawing, "#B3BAA7D4", .7, "M2 14 Q7 12 10 13 Q12 8 17 10 Q21 8 23 12 Q28 12 30 15 M5 17 Q12 15 17 17 Q22 16 28 17");
            DrawStar(drawing, new Point(19, 6), 2.1, MoonSpark, null);
        }

        private static void DrawPalace(DrawingContext drawing)
        {
            drawing.DrawEllipse(Glow("#89DCB263", "#00DCB263"), null, new Point(70, 31), 57, 26);
            drawing.DrawEllipse(Glow("#FFE7AC", "#D89452"), null, new Point(95, 18), 12, 12);
            Path(drawing, "#527054", "M8 53 Q12 34 21 34 Q28 24 35 37 L46 51 Z M90 52 L103 36 Q109 30 113 37 Q121 33 128 52 Z");
            Path(drawing, "#DFBE86", "M26 26 H104 V48 H26 Z M22 46 H108 V51 H22 Z");
            Path(drawing, "#303F35", "M28 29 H102 V34 H28 Z M28 37 H102 V43 H28 Z M58 35 H71 V48 H58 Z");
            Path(drawing, "#B29367", "M26 30 H104 V31 H26 Z M26 39 H104 V40 H26 Z M22 48 H108 V49 H22 Z");
            var columns = new Pen(Paint("#F5D499"), 1.3);
            for (int i = 0; i < 16; i++)
                drawing.DrawLine(columns, new Point(29 + i * 4.75, 28), new Point(29 + i * 4.75, 45));
            Path(drawing, "#FFDA95", "M24 25 H106 V28 H24 Z M24 34 H106 V37 H24 Z M25 43 H105 V46 H25 Z M19 51 H111 V53 H19 Z");
            StrokePath(drawing, "#D79E61", .55, "M20 52 H110 M24 46 H104 M28 26 H102");
            StrokePath(drawing, "#98A475", .7, "M8 52 Q13 42 18 42 M14 53 Q19 37 24 38 M119 51 Q116 40 110 39 M124 53 Q121 45 117 44");
            Path(drawing, "#D7B679", "M63 10 H64 V25 H63 Z");
        }

        private static void DrawPalaceFlag(DrawingContext drawing)
        {
            Path(drawing, "#F76153", "M64 11 Q71 8.5 82 11 V20 Q75 17 64 20 Z");
            DrawStar(drawing, new Point(73, 14.8), 3.2, GoldSpark, null);
        }

        private void DrawWavingFlag(DrawingContext drawing, DrawingGroup flag, double poleX, double poleY,
            double phase, double amplitude)
        {
            if (moving)
                drawing.PushTransform(new SkewTransform(0, Math.Sin(phase * 4 * Math.PI) * amplitude, poleX, poleY));
            drawing.DrawDrawing(flag);
            if (moving) drawing.Pop();
        }

        private static void DrawPalaceLight(DrawingContext drawing, double phase)
        {
            double x = 18 + 96 * phase;
            drawing.PushOpacity(Math.Sin(phase * Math.PI) * .84);
            drawing.DrawEllipse(AmberGlow, null, new Point(x, 28), 18, 11);
            drawing.DrawEllipse(AmberGlow, null, new Point(x - 5, 39), 14, 6);
            drawing.Pop();
        }

        private static void DrawOperaHouse(DrawingContext drawing)
        {
            drawing.DrawEllipse(Glow("#9EDEAA5E", "#00DEAA5E"), null, new Point(65, 28), 57, 27);
            Path(drawing, "#C2245C54", "M7 49 Q4 42 11 39 Q8 32 15 31 Q16 25 21 28 Q27 31 24 36 Q30 38 28 47 L101 47 Q97 37 105 35 Q101 29 109 27 Q116 26 116 34 Q123 37 120 43 Q125 45 121 49 Z");
            Path(drawing, "#55736A", "M19 24 L24 18 H41 L49 25 H81 L89 18 H106 L111 24 V31 H19 Z");
            Path(drawing, "#C59356", "M18 25 H112 V46 H18 Z");
            Path(drawing, "#EAC58B", "M39 24 H91 V47 H39 Z M17 25 H40 V27 H17 Z M90 25 H113 V27 H90 Z");
            Path(drawing, "#33504A", "M25 36 V32 Q27 28 29 32 V36 Z M32 36 V32 Q34 28 36 32 V36 Z M94 36 V32 Q96 28 98 32 V36 Z M101 36 V32 Q103 28 105 32 V36 Z M23 40 H36 V44 H23 Z M94 40 H107 V44 H94 Z");
            Path(drawing, "#3D5148", "M48 30 H82 V45 H48 Z");
            var column = new Pen(Paint("#F7DA9D"), 2.5);
            for (int i = 0; i < 6; i++)
            {
                double x = 43 + i * 8.8;
                drawing.DrawLine(column, new Point(x, 29), new Point(x, 45));
            }
            Path(drawing, "#F3D299", "M37 25 L65 13 93 25 V29 H37 Z M15 46 H115 V48 H15 Z M21 48 H109 V50 H21 Z M27 50 H103 V51 H27 Z");
            StrokePath(drawing, "#987042", .65, "M43 24 L65 15.5 87 24 M39 26 H91 M40 45 H90");
            drawing.DrawEllipse(Paint("#B88850"), null, new Point(65, 21.4), 2, 1.8);
            StrokePath(drawing, "#A8D6B474", .65, "M11 54 H48 M82 54 H119 M57 54 Q61 54 65 57 Q69 54 73 54");
        }

        private static void DrawOperaBeam(DrawingContext drawing)
        {
            var light = new LinearGradientBrush(PaintColor("#20FFDCA1"), PaintColor("#D7FFDCA1"),
                new Point(.5, 0), new Point(.5, 1));
            light.GradientStops.Insert(1, new GradientStop(PaintColor("#88FFDCA1"), .7));
            drawing.DrawGeometry(light, null, Geometry.Parse("M0 0 Q-4 -24 -14 -63 Q0 -68 14 -63 Q4 -24 0 0 Z"));
            drawing.DrawEllipse(AmberGlow, null, new Point(), 6, 3);
        }

        private static void DrawOperaLights(DrawingContext drawing, double phase)
        {
            double sweep = Math.Sin(phase * 2 * Math.PI);
            drawing.PushTransform(new TranslateTransform(17, 48));
            drawing.PushTransform(new RotateTransform(29 + sweep * 23));
            drawing.DrawDrawing(OperaBeam.Value);
            drawing.Pop();
            drawing.Pop();
            drawing.PushTransform(new TranslateTransform(113, 48));
            drawing.PushTransform(new RotateTransform(-29 + sweep * 23));
            drawing.DrawDrawing(OperaBeam.Value);
            drawing.Pop();
            drawing.Pop();
        }

        private static void DrawWorkers(DrawingContext drawing)
        {
            Path(drawing, "#D46642", "M12 54 L58 6 H75 L29 54 Z M35 54 L82 6 H90 L44 54 Z M100 54 L123 30 H129 L108 54 Z");
            Path(drawing, "#365C76", "M68 54 V37 H76 V32 H82 V41 L95 33 V42 L107 34 V54 Z M112 54 V20 H117 V54 Z");
            StrokePath(drawing, "#9FA7A5", .6, "M74 47 H79 M86 47 H91 M96 47 H101 M74 50 H79 M86 50 H91 M96 50 H101");
            Path(drawing, "#27435D", "M26 56 Q28 42 39 39 L48 34 62 38 Q69 43 73 56 Z");
            Path(drawing, "#F1B584", "M45 23 L61 21 61 31 57 37 49 36 45 31 Z");
            Path(drawing, "#F4CE86", "M40 25 Q41 14 50 13 L53 11 56 13 Q63 14 65 24 L70 25 V28 H38 V26 Z");
            Path(drawing, "#DB8051", "M50 13 H54 L55 22 H52 Z M40 26 H66 V27 H40 Z");
            Path(drawing, "#ED9B63", "M39 39 L44 37 47 43 53 46 59 42 62 38 66 40 60 49 53 52 45 49 Z M39 49 H46 V52 H39 Z");
            Path(drawing, "#AB665A", "M58 27 H64 L64 30 61 30 60 34 56 36 51 35 48 32 52 33 57 33 58 30 Z");
            StrokePath(drawing, "#FFE6AE", .7, "M43 22 Q49 15 55 15 M46 31 Q49 28 51 30 M31 54 Q34 45 39 43 M63 42 Q68 45 70 53");
        }

        private static void DrawWorkersGear(DrawingContext drawing)
        {
            drawing.DrawEllipse(Paint("#B330465A"), new Pen(Paint("#D69965"), 1.4), GearHub, 10, 10);
            var teeth = new Pen(Paint("#F5BE7F"), 2.6) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
            for (int i = 0; i < 8; i++)
            {
                double angle = i * Math.PI / 4;
                drawing.DrawLine(teeth, Polar(GearHub, 9, angle), Polar(GearHub, 12.1, angle));
            }
            var spokes = new Pen(Paint("#BA9C8A"), 1.1);
            for (int i = 0; i < 4; i++)
                drawing.DrawLine(spokes, GearHub, Polar(GearHub, 7, .35 + i * Math.PI / 2));
            drawing.DrawEllipse(GoldSpark, null, GearHub, 2, 2);
        }

        private static void DrawBaDinh(DrawingContext drawing)
        {
            drawing.DrawEllipse(Glow("#7099564A", "#0099564A"), null, new Point(97, 35), 30, 20);
            Path(drawing, "#91694E", "M46 50 Q50 42 56 44 Q62 41 69 48 L76 52 H46 Z");
            Path(drawing, "#805447", "M76 49 V34 H122 V49 H128 V53 H69 V49 Z");
            Path(drawing, "#E8B681", "M71 34 H127 V37 H71 Z M69 49 H129 V51 H69 Z M67 53 H129 V55 H67 Z");
            Path(drawing, "#4B2427", "M79 39 H119 V47 H79 Z M97 43 H103 V48 H97 Z");
            StrokePath(drawing, "#B49877", 1, "M83 38 V48 M88 38 V48 M93 38 V48 M104 38 V48 M109 38 V48 M114 38 V48 M118 38 V48");
            Path(drawing, "#D9B289", "M27 7 H29 V55 H27 Z");
            drawing.DrawEllipse(GoldSpark, null, new Point(28, 6), 1.7, 1.7);
        }

        private static void DrawNationalFlag(DrawingContext drawing)
        {
            drawing.DrawGeometry(new LinearGradientBrush(PaintColor("#FF7663"), PaintColor("#B6293B"), 22), null,
                Geometry.Parse("M29 9 Q45 5 58 10 Q73 15 89 9 V36 Q72 42 58 38 Q43 34 29 38 Z"));
            Path(drawing, "#AD2A37", "M29 29 Q43 23 58 28 Q74 34 89 28 V36 Q72 42 58 38 Q43 34 29 38 Z");
            StrokePath(drawing, "#D9FFA591", .65, "M30 10 Q44 6 58 11 Q75 16 88 10 M31 28 Q46 25 58 29 Q72 34 86 29");
            DrawStar(drawing, new Point(59, 23), 10, Paint("#FFE086"), null);
        }

        private static void DrawMoon(DrawingContext drawing)
        {
            var center = new Point(100, 17);
            drawing.DrawEllipse(Glow("#72DFBEA4", "#00DFBEA4"), null, center, 28, 21);
            drawing.DrawEllipse(Glow("#FFF1D0", "#CFA994"), null, center, 13.5, 13.5);
            Path(drawing, "#32926882", "M92 13 Q94 11 97 12 L99 16 96 19 92 18 Z M102 19 L105 16 109 19 107 23 103 24 Z");
            var craters = Paint("#21A76E64");
            drawing.DrawEllipse(craters, null, new Point(96, 25), 2.4, 1.4);
            drawing.DrawEllipse(craters, null, new Point(104, 10), 1.7, 1.2);
            DrawStar(drawing, new Point(119, 11), 1.6, MoonSpark, null);
            DrawStar(drawing, new Point(71, 13), 1.4, MoonSpark, null);
            Path(drawing, "#A54C466D", "M52 47 Q64 41 75 36 Q80 33 93 41 Q103 36 111 40 L128 43 V50 H52 Z");
            Path(drawing, "#A982759F", "M62 43 Q69 39 76 41 Q87 37 92 41 Q101 39 109 42 L121 44 Q89 47 62 45 Z");
        }

        private static void DrawMoonClouds(DrawingContext drawing)
        {
            Path(drawing, "#DC7D6D99", "M69 27 Q78 21 83 26 Q89 20 94 25 Q104 22 110 27 Q115 21 121 26 L124 28 Q100 32 69 29 Z");
            Path(drawing, "#E0C8B7DF", "M76 27 Q85 23 90 26 Q99 23 105 27 L115 27 Q97 30 76 28 Z");
        }

        private static void DrawLantern(DrawingContext drawing)
        {
            var center = new Point(49, 48);
            drawing.DrawEllipse(Paint("#86402538"), null, new Point(49, 53), 15, 3);
            StrokePath(drawing, "#DEAC7A", .7, "M37 48 Q32 47 31 49 Q30 51 34 52 L38 52");
            Path(drawing, "#B96066", "M56 51 Q61 53 65 53 L63 56 Q58 56 54 53 Z");
            drawing.PushTransform(new ScaleTransform(1, .62, center.X, center.Y));
            drawing.PushTransform(new RotateTransform(-30, center.X, center.Y));
            DrawStar(drawing, center, 12.4, Glow("#FFD98A", "#D34459"), new Pen(Paint("#FFE2A4"), 1));
            StrokePath(drawing, "#F6D4A1", .53, "M37.2 44.2 L60.8 44.2 41.7 58 49 35.6 56.3 58 Z");
            drawing.DrawEllipse(AmberLight, null, center, 1.3, 1.3);
            drawing.Pop();
            drawing.Pop();
        }

        private static void DrawMooncakes(DrawingContext drawing)
        {
            drawing.PushTransform(new TranslateTransform(-3, 0));
            drawing.DrawEllipse(Paint("#84452340"), null, new Point(84, 54), 23, 3.5);
            Path(drawing, "#A65240", "M64 46 Q77 38 88 46 V52 Q77 59 64 52 Z");
            Path(drawing, "#D69059", "M64 46 Q64 44 67 44 Q67 42 71 42 Q73 40 77 41 Q81 40 84 42 Q88 42 88 45 Q91 46 87 49 Q87 51 82 52 Q78 54 75 52 Q71 53 69 51 Q65 51 64 48 Z");
            StrokePath(drawing, "#F2C180", .65, "M69 46 Q77 42 84 46 Q79 50 70 48 Z M77 43 V50 M71 46 H83 M68 51 V53 M73 52 V55 M81 52 V55");
            Path(drawing, "#8F473D", "M88 52 L96 45 106 49 V54 L97 58 88 56 Z");
            Path(drawing, "#F0AB66", "M88 52 L96 45 106 49 97 54 Z");
            Path(drawing, "#FCD289", "M89 53 L97 55 105 51 V53 L97 57 89 55 Z");
            drawing.Pop();
        }

        private static void DrawPine(DrawingContext drawing)
        {
            Path(drawing, "#436D57", "M3 8 Q36 8 57 25 Q26 11 3 10 Z M14 4 Q41 4 65 15 Q32 9 14 8 Z M3 20 Q32 15 54 24 Q29 22 3 24 Z");
            var needles = new Pen(Paint("#85A87C"), .65);
            var newGrowth = new Pen(Paint("#C4BE85"), .37);
            for (int i = 0; i < 13; i++)
            {
                double x = 5 + i * 4, y = 9 + i * .78;
                drawing.DrawLine(needles, new Point(x, y), new Point(x + 9, y - 4));
                drawing.DrawLine(needles, new Point(x, y + 1), new Point(x + 7, y + 7));
                drawing.DrawLine(newGrowth, new Point(x + 2, y), new Point(x + 9, y + 2));
            }
            Path(drawing, "#A99B68", "M50 12 H51 V33 H50 Z M48 31 H53 V36 H48 Z");
            drawing.DrawEllipse(Glow("#F5A887", "#9D283A"), new Pen(Paint("#DD7B6F"), .7), new Point(50.5, 43), 9, 9);
            Path(drawing, "#FFE1A8", "M45 41 Q49 38 55 41 L55 42 Q50 40 45 42 Z");
            StrokePath(drawing, "#FFC59D", .55, "M44 45 Q51 50 57 45 M46 48 Q50 51 54 48");
            DrawStar(drawing, new Point(51, 44), 2.2, AmberLight, null);
            StrokePath(drawing, "#8C9B69", .72, "M16 14 Q40 26 66 23");

            drawing.DrawEllipse(Paint("#8A152A28"), null, new Point(97, 55), 29, 3);
            Path(drawing, "#AB765A", "M94 45 H100 V55 H94 Z");
            drawing.DrawGeometry(new LinearGradientBrush(PaintColor("#6F9F77"), PaintColor("#214A3E"), 75), null,
                Geometry.Parse("M97 11 Q95 18 88 22 L92 22 Q88 27 83 30 L90 30 Q83 37 78 39 L85 40 Q80 45 74 48 Q96 54 119 48 Q112 44 107 39 L114 39 Q106 34 103 30 L110 30 Q103 25 101 22 H106 Q99 18 97 11 Z"));
            StrokePath(drawing, "#AAC58C", .65,
                "M88 22 Q96 24 106 22 M84 30 Q97 34 108 30 M79 39 Q94 44 113 39 M77 48 Q94 52 117 48");
            StrokePath(drawing, "#CDB47F", .85, "M92 25 Q95 31 104 32 M87 37 Q94 44 106 43");
            DrawStar(drawing, new Point(97, 10), 4.5, AmberLight, null);
            drawing.DrawEllipse(Paint("#CB5260"), null, new Point(95, 34), 1.7, 1.7);
            drawing.DrawEllipse(Paint("#F5B285"), null, new Point(101, 45), 1.5, 1.5);
            DrawGift(drawing, 74, 46, 13, 9, "#AD4357", "#FFD58E");
            DrawGift(drawing, 86, 50, 12, 7, "#A390BA", "#F7D6AC");
            DrawGift(drawing, 105, 47, 15, 10, "#BE864E", "#EED1EA");
        }

        private static void DrawGift(DrawingContext drawing, double x, double y, double width, double height,
            string paper, string ribbon)
        {
            var wrap = Paint(paper);
            var trim = Paint(ribbon);
            drawing.DrawRectangle(wrap, null, new Rect(x, y, width, height));
            drawing.DrawRectangle(wrap, new Pen(trim, .38), new Rect(x - .45, y, width + .9, 2.25));
            double center = x + width / 2;
            drawing.DrawRectangle(trim, null, new Rect(center - .85, y, 1.7, height));
            var bow = new Pen(trim, .8);
            drawing.DrawEllipse(null, bow, new Point(center - 1.5, y - .9), 1.45, .8);
            drawing.DrawEllipse(null, bow, new Point(center + 1.5, y - .9), 1.45, .8);
        }

        private static void DrawChristmasLights(DrawingContext drawing, double phase, bool celebration)
        {
            for (int i = 0; i < ChristmasLights.Length; i++)
            {
                double flicker = .69 + .27 * Math.Cos((phase * 2 - i * .12) * 2 * Math.PI);
                drawing.PushOpacity(celebration ? Math.Min(1, flicker + .25) : flicker);
                drawing.DrawEllipse(AmberGlow, null, ChristmasLights[i], celebration ? 7 : 4, celebration ? 7 : 4);
                drawing.DrawEllipse(AmberLight, null, ChristmasLights[i], 1.65, 1.8);
                drawing.Pop();
            }
        }

        private static void DrawSnowfall(DrawingContext drawing, double phase)
        {
            for (int i = 0; i < 12; i++)
            {
                double fall = (phase + i * .17) % 1;
                double x = 13 + i * 9 + Math.Sin(fall * 2 * Math.PI + i) * 5;
                double size = i % 3 == 0 ? 1.65 : .95;
                drawing.PushOpacity(Math.Sin(fall * Math.PI) * .92);
                drawing.DrawEllipse(Snow, null, new Point(x, 3 + fall * 51), size, size);
                drawing.Pop();
            }
        }

        private static void DrawNewYearOrnament(DrawingContext drawing)
        {
            drawing.DrawEllipse(Glow("#65FFA67F", "#00FFA67F"), null, new Point(69, 24), 55, 28);
            StrokePath(drawing, "#89E9AC70", .65,
                "M32 39 Q39 33 46 39 T60 39 M29 43 Q36 37 43 43 T57 43 M81 47 Q90 42 98 46 T112 46");
            StrokePath(drawing, "#C7D9A168", .6,
                "M16 33 Q19 28 24 31 Q24 25 30 26 Q35 26 35 30 H42 M86 41 Q89 37 93 39 Q97 33 102 37 Q107 36 111 40 H119");
            DrawStar(drawing, new Point(114, 18), 2.3, GoldSpark, null);
            DrawStar(drawing, new Point(20, 19), 1.7, GoldSpark, null);
        }

        private static void DrawApricotBranch(DrawingContext drawing)
        {
            StrokePath(drawing, "#654F36", 3.4, "M127 43 Q103 36 83 30 Q63 23 35 28");
            StrokePath(drawing, "#BC955A", 1.05,
                "M126 42 Q103 35 83 29 Q64 22 35 28 M100 34 Q88 19 73 12 L62 12 M83 29 Q65 36 53 36 M73 26 Q63 21 49 18 M111 37 Q105 28 102 20 L96 15 M90 31 Q79 34 76 39");
            StrokePath(drawing, "#B58B53", .64,
                "M62 24 Q57 15 51 9 L43 8 M80 18 Q71 8 65 6 M96 23 Q100 18 107 13 L117 13 M110 35 Q121 31 125 27 M53 27 Q47 33 39 34");
            Path(drawing, "#858651", "M65 22 Q57 11 47 12 Q51 19 65 22 Z M79 32 Q70 40 64 39 Q69 34 79 32 Z M94 24 Q97 14 103 13 Q101 20 94 24 Z M110 32 Q111 23 118 20 Q117 26 110 32 Z");
            var flowers = new[]
            {
                new Point(36, 27), new Point(41, 34), new Point(43, 8), new Point(48, 17), new Point(55, 12),
                new Point(55, 35), new Point(62, 20), new Point(63, 11), new Point(67, 6), new Point(73, 14),
                new Point(78, 19), new Point(80, 31), new Point(86, 23), new Point(91, 32), new Point(96, 15),
                new Point(101, 18), new Point(107, 13), new Point(111, 32), new Point(116, 13), new Point(123, 27),
                new Point(124, 40)
            };
            for (int i = 0; i < flowers.Length; i++)
                DrawApricotBlossom(drawing, flowers[i], 1.9 + .53 * (i % 3));
        }

        internal static void DrawHangingEnvelope(DrawingContext drawing, double x, double y, double angle, double size = 1)
        {
            double radians = angle * Math.PI / 180;
            double cosine = Math.Cos(radians) * size;
            double sine = Math.Sin(radians) * size;
            drawing.PushTransform(new MatrixTransform(cosine, sine, -sine, cosine, x, y));
            drawing.DrawDrawing(Envelope.Value);
            drawing.Pop();
        }

        private static void DrawEnvelope(DrawingContext drawing)
        {
            StrokePath(drawing, "#FFCE78", .67, "M0 0 V3 M0 13 V17 M-1 16 L0 19 1 16");
            drawing.DrawRoundedRectangle(Paint("#BF2630"), new Pen(Paint("#FFC675"), .65),
                new Rect(-3.35, 3, 6.7, 10), .65, .65);
            StrokePath(drawing, "#F4B76C", .4, "M-2 4.5 H2 M-2 11.5 H2 M0 6.2 L1.3 8 0 9.8 -1.3 8 Z");
        }

        internal static void DrawDiamondString(DrawingContext drawing, double x, double y)
        {
            drawing.PushTransform(new TranslateTransform(x, y));
            drawing.PushTransform(new RotateTransform(-5));
            StrokePath(drawing, "#F5C176", .8, "M0 0 V34");
            var edge = new Pen(Paint("#FFD78C"), .8);
            var face = new LinearGradientBrush(PaintColor("#F04945"), PaintColor("#9B152A"), 115);
            for (int i = 0; i < 4; i++)
            {
                double center = 6 + i * 8;
                drawing.PushTransform(new TranslateTransform(0, center));
                drawing.DrawGeometry(face, edge, Geometry.Parse("M0 -3.7 L3.7 0 0 3.7 -3.7 0 Z"));
                Path(drawing, "#FFE39C", "M0 -1.8 L.55 -.55 1.65 0 .55 .55 0 1.8 -.55 .55 -1.65 0 -.55 -.55 Z");
                drawing.Pop();
            }
            Path(drawing, "#F04949", "M-.75 33 H.75 L1.55 39 H-1.55 Z");
            StrokePath(drawing, "#FFCB77", .55, "M-1.1 33 H1.1 M0 34 V38.5");
            drawing.Pop();
            drawing.Pop();
        }

        private static void DrawApricotBlossom(DrawingContext drawing, Point center, double size)
        {
            var petal = Glow("#FFF3A6", "#F3B43B");
            for (int i = 0; i < 5; i++)
            {
                double angle = (i * 72 - 90) * Math.PI / 180;
                var tip = Polar(center, size * .86, angle);
                drawing.PushTransform(new RotateTransform(angle * 180 / Math.PI, tip.X, tip.Y));
                drawing.DrawEllipse(petal, null, tip, size, size * .65);
                drawing.Pop();
            }
            drawing.DrawEllipse(Paint("#D97D32"), null, center, size * .39, size * .39);
            drawing.DrawEllipse(AmberLight, null, new Point(center.X - .15, center.Y - .3), .45, .45);
        }

        private static void DrawApricotPetals(DrawingContext drawing, double phase, bool celebration)
        {
            int count = celebration ? 10 : 7;
            for (int i = 0; i < count; i++)
            {
                double fall = (phase + i / (double)count) % 1;
                var point = new Point(39 + i * (78.0 / count) + Math.Sin(fall * 7 + i) * 7, 15 + fall * 39);
                drawing.PushOpacity(Math.Sin(fall * Math.PI) * .92);
                drawing.PushTransform(new RotateTransform(42 + fall * 230 + i * 19, point.X, point.Y));
                drawing.DrawEllipse(ApricotPetal, null, point, 2.15, 1.1);
                drawing.Pop();
                drawing.Pop();
            }
        }

        private static void DrawSquareRiceCake(DrawingContext drawing)
        {
            drawing.PushTransform(new ScaleTransform(.84, .84, 106, 54));
            drawing.DrawEllipse(Paint("#95291820"), null, new Point(106, 52), 19, 4.4);
            Path(drawing, "#244838", "M89 43 L102 35 124 42 V50 L111 57 89 51 Z");
            Path(drawing, "#3C6846", "M89 43 L102 35 124 42 111 49 Z M111 49 L124 42 V50 L111 57 Z");
            StrokePath(drawing, "#7B8F55", .6, "M90 44 L111 50 123 43 M91 49 L111 55 123 49 M95 42 L112 46 M100 39 L117 44");
            StrokePath(drawing, "#E5C38D", 1.25, "M94 40 L117 46 V53 M99 37 L121 44 V52 M94 45 L107 37 M103 47 L117 40 M96 46 V53 M103 48 V55");
            drawing.Pop();
        }

        private static void DrawRolledRiceCake(DrawingContext drawing)
        {
            drawing.PushTransform(new ScaleTransform(.84, .84, 41, 53));
            drawing.DrawEllipse(Paint("#8D22271F"), null, new Point(41, 53), 22, 3.5);
            Path(drawing, "#335E40", "M23 40 L47 34 Q55 33 58 40 Q58 45 52 48 L28 54 Z");
            Path(drawing, "#6C9554", "M23 41 L48 35 Q53 33 56 37 L29 45 Z");
            StrokePath(drawing, "#D3BA7F", 1.3, "M34 37 Q42 43 39 50 M43 35 Q51 40 48 48 M52 35 Q58 39 55 46 M25 46 L54 39");
            drawing.DrawEllipse(Paint("#D8D997"), new Pen(Paint("#456747"), 1.1), new Point(25.5, 47.5), 5.4, 6.6);
            drawing.DrawEllipse(Paint("#E8B864"), null, new Point(25.5, 47.5), 3.25, 4.1);
            drawing.DrawEllipse(Paint("#B98055"), null, new Point(25.5, 47.5), 1.35, 1.9);
            drawing.Pop();
        }

        private static void DrawFloatingLights(DrawingContext drawing, double phase, double startX, double width,
            double bottom, Brush color, int count)
        {
            for (int i = 0; i < count; i++)
            {
                double progress = (phase + i / (double)count) % 1;
                double x = startX + width * ((i * .37) % 1) + Math.Sin(progress * 5 + i) * 4;
                var center = new Point(x, bottom + 27 * (1 - progress));
                drawing.PushOpacity(Math.Sin(progress * Math.PI) * .74);
                drawing.DrawEllipse(color, null, center, i % 3 == 0 ? 1.15 : .65, .75);
                drawing.Pop();
            }
        }

        private static void DrawNewYearSparks(DrawingContext drawing, double phase)
        {
            for (int i = 0; i < 8; i++)
            {
                double lift = (phase * 2 + i * .17) % 1;
                var center = new Point(17 + i * 14 + Math.Sin(lift * 5 + i) * 4, 50 - lift * 43);
                drawing.PushOpacity(Math.Sin(lift * Math.PI) * .9);
                drawing.PushTransform(new TranslateTransform(center.X, center.Y));
                drawing.DrawDrawing(NewYearSpark.Value);
                drawing.Pop();
                drawing.Pop();
            }
        }

        private static void DrawStar(DrawingContext drawing, Point center, double radius, Brush brush, Pen edge)
        {
            var star = new StreamGeometry();
            using (var points = star.Open())
            {
                points.BeginFigure(Polar(center, radius, -Math.PI / 2), true, true);
                for (int i = 1; i < 10; i++)
                    points.LineTo(Polar(center, i % 2 == 0 ? radius : radius * .43, (i - 2.5) * Math.PI / 5), true, false);
            }
            drawing.DrawGeometry(brush, edge, star);
        }

        private static void StrokePath(DrawingContext drawing, string color, double width, string path)
        {
            drawing.DrawGeometry(null, new Pen(Paint(color), width), Geometry.Parse(path));
        }
    }
}
