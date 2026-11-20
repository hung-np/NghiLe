using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace NghiLe
{
    internal sealed partial class HolidayScene : FrameworkElement
    {
        private static readonly Lazy<DrawingGroup> NightSky = DeferScene(DrawNightSky);
        private static readonly Lazy<DrawingGroup> Skyline = DeferScene(DrawSkyline);
        private static readonly Lazy<DrawingGroup> BronzeDrum = DeferScene(DrawBronzeDrum);
        private static readonly Point[] BurstCenters = { new Point(47, 24), new Point(93, 18), new Point(104, 36) };
        private static readonly Lazy<DrawingGroup[]> Bursts = new Lazy<DrawingGroup[]>(() => new[]
        {
            DrawScene(d => Burst(d, BurstCenters[0], 23, 22, "#ECF6FF", "#8FAEFF")),
            DrawScene(d => Burst(d, BurstCenters[1], 16, 16, "#FFEEE1", "#B6A6F7")),
            DrawScene(d => Burst(d, BurstCenters[2], 11, 11, "#D2EAFF", "#8099E6"))
        });
        private static readonly Point DrumCenter = new Point(87, 28);
        private static readonly EllipseGeometry DrumFace = new EllipseGeometry(DrumCenter, 24, 24);
        private static readonly Lazy<Brush> DrumShine = new Lazy<Brush>(CreateDrumShine);
        private static readonly DoubleAnimation PhaseCycle = CreatePhaseCycle();
        private static readonly DependencyProperty PhaseProperty = DependencyProperty.Register(
            "Phase", typeof(double), typeof(HolidayScene), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
        private Size artworkSize = Size.Empty;
        private Transform placement = Transform.Identity;
        private HolidayTheme? theme;
        private bool moving;
        private bool celebrating;
        private bool apricotNewYear;
        private bool compact;
        private bool horizontal;

        internal HolidayScene() { IsHitTestVisible = false; }

        private void UpdatePlacement()
        {
            var size = RenderSize;
            if (artworkSize == size) return;
            artworkSize = size;
            horizontal = size.Width > size.Height;
            double scale = horizontal ? 1 : .93;
            double x = horizontal ? 0 : (size.Width - 130 * scale) / 2;
            var transform = new MatrixTransform(scale, 0, 0, scale, x, horizontal ? 1 : 0);
            transform.Freeze();
            placement = transform;
        }

        private static DoubleAnimation CreatePhaseCycle()
        {
            var cycle = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(8)) { RepeatBehavior = RepeatBehavior.Forever };
            Timeline.SetDesiredFrameRate(cycle, PeachScene.FramesPerSecond);
            cycle.Freeze();
            return cycle;
        }

        internal void ShowTheme(HolidayTheme? value, bool apricot)
        {
            if (theme == value && apricotNewYear == apricot) return;
            theme = value;
            apricotNewYear = apricot;
            InvalidateVisual();
        }

        internal bool IsAnimating { get { return moving && HasAnimatedProperties; } }

        internal void SetCompact(bool value)
        {
            if (compact == value) return;
            compact = value;
            InvalidateVisual();
        }

        internal void SetCelebration(bool value)
        {
            if (celebrating == value) return;
            celebrating = value;
            InvalidateVisual();
        }

        internal void SetMotion(bool enabled)
        {
            enabled = enabled && theme.HasValue;
            if (enabled == moving) return;
            moving = enabled;
            BeginAnimation(PhaseProperty, moving ? PhaseCycle : null);
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawing)
        {
            if (!theme.HasValue) return;
            UpdatePlacement();
            drawing.PushTransform(placement);
            double phase = (double)GetValue(PhaseProperty);
            if (theme == HolidayTheme.NewYear)
            {
                drawing.DrawDrawing(NightSky.Value);
                var bursts = Bursts.Value;
                for (int i = 0; i < bursts.Length; i++)
                {
                    if (!moving)
                    {
                        drawing.DrawDrawing(bursts[i]);
                        continue;
                    }
                    double elapsed = (phase * 8 + 8 - i * 1.65) % 8;
                    if (elapsed >= 3.7) continue;
                    double expansion = Math.Min(1, elapsed / 1.35);
                    double size = 1.10 - .93 * Math.Pow(1 - expansion, 2);
                    double opacity = Math.Min(1, elapsed / .5) * Math.Min(1, (3.7 - elapsed) / 1.9);
                    drawing.PushOpacity(opacity);
                    drawing.PushTransform(new ScaleTransform(size, size, BurstCenters[i].X, BurstCenters[i].Y));
                    drawing.DrawDrawing(bursts[i]);
                    drawing.Pop();
                    drawing.Pop();
                }
                drawing.DrawDrawing(Skyline.Value);
            }
            else if (theme == HolidayTheme.HungKings)
            {
                drawing.DrawDrawing(BronzeDrum.Value);
                if (moving && phase < .68) DrawDrumShine(drawing, phase / .68);
            }
            else DrawHolidayArtwork(drawing, theme.Value, phase, horizontal);
            drawing.Pop();
            if (!horizontal) DrawVerticalForeground(drawing, theme.Value, phase);
        }

        private static DrawingGroup DrawScene(Action<DrawingContext> draw)
        {
            var scene = new DrawingGroup();
            using (var context = scene.Open()) draw(context);
            scene.Freeze();
            return scene;
        }

        private static Lazy<DrawingGroup> DeferScene(Action<DrawingContext> draw)
        {
            return new Lazy<DrawingGroup>(() => DrawScene(draw));
        }

        private static void DrawNightSky(DrawingContext drawing)
        {
            drawing.DrawEllipse(Glow("#404779E8", "#004779E8"), null, new Point(57, 29), 56, 28);
            drawing.DrawEllipse(Glow("#335D67CB", "#005D67CB"), null, new Point(98, 24), 30, 22);

            var stars = Paint("#A4B2D4FF");
            foreach (var point in new[] { new Point(12, 23), new Point(21, 31), new Point(31, 6), new Point(66, 8),
                new Point(75, 35), new Point(77, 13), new Point(112, 9), new Point(120, 19), new Point(124, 36) })
                drawing.DrawEllipse(stars, null, point, .5, .5);
        }

        private static void DrawSkyline(DrawingContext drawing)
        {
            Path(drawing, "#995A72A0", "M4 49 H11 V44 H17 V49 H23 V40 H29 V48 H38 V46 H44 V49 H54 V39 H60 V44 H70 V48 H78 V42 H85 V46 H91 V40 H97 V47 H103 V44 H109 V49 H117 V43 H123 V49 H127 V54 H4 Z");
            Path(drawing, "#D3101B34", "M3 53 H9 V50 H16 V53 H22 V48 H30 V53 H36 V40 H40 V35 H41 V30 H42 V35 H43 V40 H47 V53 H53 V49 H58 V53 H66 V47 H75 V53 H81 V45 H88 V42 H89 V45 H93 V53 H100 V49 H107 V53 H114 V48 H119 V53 H127 V56 H3 Z");
            Path(drawing, "#AEA6C8F7", "M38 42 H40 V43 H38 Z M42 42 H44 V43 H42 Z M38 46 H40 V47 H38 Z M42 46 H44 V47 H42 Z M70 49 H72 V50 H70 Z M85 48 H87 V49 H85 Z M89 48 H91 V49 H89 Z M25 50 H27 V51 H25 Z M115 50 H117 V51 H115 Z");
            drawing.DrawRectangle(new LinearGradientBrush(PaintColor("#006F9EE2"), PaintColor("#006F9EE2"), 0)
            {
                GradientStops = { new GradientStop(PaintColor("#876F9EE2"), .48) }
            }, null, new Rect(4, 55, 122, 1));
        }

        private static void Burst(DrawingContext drawing, Point center, double radius, int rays, string light, string muted)
        {
            drawing.DrawEllipse(Glow(muted, "#0085A1FF", .43), null, center, 6, 6);
            var bright = new Pen(Paint(light), .74) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
            var dim = new Pen(Paint(muted), .55) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
            var spark = Paint(light);
            for (int i = 0; i < rays; i++)
            {
                double angle = i * 2 * Math.PI / rays + .04 * Math.Sin(i * 3);
                double length = radius * (.71 + .085 * (i % 4));
                Point start = Polar(center, length * .19, angle);
                Point bend = Polar(center, length * .54, angle + .035);
                Point tip = Polar(center, length * .89, angle + .08);
                tip.Y += 1.1;
                var trail = new StreamGeometry();
                using (var path = trail.Open())
                {
                    path.BeginFigure(start, false, false);
                    path.QuadraticBezierTo(bend, tip, true, false);
                }
                drawing.DrawGeometry(null, i % 3 == 0 ? dim : bright, trail);
                var end = Polar(center, length, angle + .10);
                end.Y += 1.4;
                drawing.DrawEllipse(spark, null, end, i % 4 == 0 ? .85 : .48, .52);
                if (i % 3 == 0) drawing.DrawEllipse(dim.Brush, null, Polar(center, length * 1.09, angle - .055), .48, .48);
            }
            drawing.DrawEllipse(spark, null, center, 1.1, 1.1);
        }

        private static void DrawBronzeDrum(DrawingContext drawing)
        {
            drawing.DrawEllipse(Glow("#509D6033", "#009D6033"), null, new Point(81, 28), 46, 28);
            Path(drawing, "#795E4F3B", "M1 48 Q11 44 18 36 Q21 32 25 35 L37 43 Q42 38 48 36 Q54 32 64 44 L78 49 Q91 41 101 34 L110 31 Q118 40 129 43 V54 H1 Z");
            Path(drawing, "#A43D352A", "M1 49 Q17 42 29 45 T59 48 Q69 42 79 45 Q94 51 107 43 L129 47 V56 H1 Z");
            Path(drawing, "#748A7354", "M4 53 Q20 47 35 50 T69 52 Q47 51 34 54 T4 54 Z M94 52 Q108 48 124 51 Q112 51 105 54 Z");
            DrawDrumFace(drawing, new Point(87, 28));
            DrawCeremonialFigure(drawing);
        }

        private static Brush CreateDrumShine()
        {
            var shine = new LinearGradientBrush(PaintColor("#00FFDFA8"), PaintColor("#00FFDFA8"), 0)
            {
                MappingMode = BrushMappingMode.Absolute,
                StartPoint = new Point(-12, 15),
                EndPoint = new Point(12, 33)
            };
            shine.GradientStops.Insert(1, new GradientStop(PaintColor("#48FFE0A5"), .5));
            shine.Freeze();
            return shine;
        }

        private static void DrawDrumShine(DrawingContext drawing, double progress)
        {
            double x = 46 + 82 * progress;
            drawing.PushClip(DrumFace);
            drawing.PushTransform(new TranslateTransform(x, 0));
            drawing.DrawRectangle(DrumShine.Value, null, new Rect(63 - x, 4, 48, 48));
            drawing.Pop();
            drawing.Pop();
        }

        private static void DrawDrumFace(DrawingContext drawing, Point center)
        {
            var gold = Paint("#F1CB89");
            var rim = new Pen(Paint("#D9AF70"), .76);
            var fine = new Pen(Paint("#A0D7A167"), .50);
            var shadow = new Pen(Paint("#C640291F"), 1);
            var finish = new LinearGradientBrush(PaintColor("#A87148"), PaintColor("#4C3129"), 112);
            finish.GradientStops.Insert(1, new GradientStop(PaintColor("#69462F"), .46));
            drawing.DrawEllipse(Paint("#9E221A18"), null, new Point(center.X + 1, center.Y + 2), 26, 26);
            drawing.DrawEllipse(finish, rim, center, 25, 25);
            drawing.DrawEllipse(null, shadow, center, 23.9, 23.9);
            foreach (double radius in new[] { 23.1, 20.8, 19.3, 13.8, 12.4, 10.5 })
                drawing.DrawEllipse(null, fine, center, radius, radius);

            for (int i = 0; i < 30; i++)
            {
                double angle = i * Math.PI / 15;
                Point crest = Polar(center, 22.75, angle + .075);
                drawing.DrawLine(fine, Polar(center, 21.1, angle), crest);
                drawing.DrawLine(fine, crest, Polar(center, 21.1, angle + .18));
                drawing.DrawEllipse(gold, null, Polar(center, 13.15, angle), .31, .31);
            }
            for (int i = 0; i < 6; i++)
            {
                double angle = i * Math.PI / 3;
                var bird = Polar(center, 16.6, angle);
                drawing.PushTransform(new TranslateTransform(bird.X, bird.Y));
                drawing.PushTransform(new RotateTransform(i * 60 + 90));
                Path(drawing, "#E8D1A36F", "M-3.8 1.6 L-2 -.2 -1 -1.8 -.5 -.4 1 -.5 2 -1 3.7 -1.1 2.3 -.2 1.4 1 H-1.4 L-3.5 2 Z");
                drawing.Pop();
                drawing.Pop();
            }
            var sun = new StreamGeometry();
            using (var path = sun.Open())
            {
                path.BeginFigure(Polar(center, 9.6, -Math.PI / 2), true, true);
                for (int i = 1; i < 24; i++)
                    path.LineTo(Polar(center, i % 2 == 0 ? 9.6 : 3, (i - 6) * Math.PI / 12), true, false);
            }
            drawing.DrawGeometry(gold, null, sun);
            drawing.DrawEllipse(Paint("#8B593D"), null, center, 1.25, 1.25);
        }

        private static void DrawCeremonialFigure(DrawingContext drawing)
        {
            Path(drawing, "#36251D", "M20 54 Q29 50 36 51 Q46 49 59 53 L60 55 H20 Z");
            Path(drawing, "#AC7348", "M29 20 Q25 15 24 9 Q30 12 34 16 Q29 10 29 4 Q34 8 37 15 Q38 9 42 7 L41 18 45 16 43 22 Z");
            Path(drawing, "#ECC78C", "M29 19 Q26 14 25 11 L34 19 H36 L30 7 Q35 10 37 17 L41 9 39 18 43 18 42 21 H29 Z");
            Path(drawing, "#DCAA72", "M32 22 Q37 20 41 22 L42 25 45 26 42 28 Q42 31 38 31 L35 30 35 33 31 31 33 27 Z");
            Path(drawing, "#B67E52", "M33 23 Q37 20 41 22 L39 23 H34 L34 26 36 29 35 31 32 29 Z");
            Path(drawing, "#AF754C", "M30 32 Q36 30 39 32 L45 34 53 41 50 44 43 39 46 48 49 52 Q36 54 25 52 L29 41 25 44 21 42 Z");
            Path(drawing, "#694631", "M34 33 L38 33 42 44 45 51 38 52 35 43 33 51 27 52 29 45 Z");
            Path(drawing, "#EBC286", "M30 32 L33 32 30 44 26 50 25 52 29 51 34 43 35 38 40 35 39 32 36 36 34 36 Z M43 36 L45 37 52 41 50 42 Z M24 40 L27 37 28 38 25 43 23 42 Z M37 44 H43 L43 45 H38 Z");
            Path(drawing, "#D1A16C", "M32 48 L32 47 34 46 35 47 33 48 Z M39 48 L40 47 42 48 42 49 40 49 Z M36 50 H42 V51 H36 Z");
        }

        private static Point Polar(Point center, double radius, double angle)
        {
            return new Point(center.X + radius * Math.Cos(angle), center.Y + radius * Math.Sin(angle));
        }

        private static void Path(DrawingContext drawing, string color, string path)
        {
            drawing.DrawGeometry(Paint(color), null, Geometry.Parse(path));
        }

        private static Brush Paint(string color)
        {
            return new SolidColorBrush(PaintColor(color));
        }

        private static Color PaintColor(string value) { return (Color)ColorConverter.ConvertFromString(value); }

        private static Brush Glow(string from, string to, double opacity = 1)
        {
            return new RadialGradientBrush(PaintColor(from), PaintColor(to)) { Opacity = opacity };
        }
    }
}
