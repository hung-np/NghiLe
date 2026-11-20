using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Path = System.Windows.Shapes.Path;

namespace NhipNgay
{
    internal sealed class PeachScene : Grid
    {
        internal const int FramesPerSecond = 16;

        public sealed class BranchArtwork
        {
            public string Wood { get; set; }
            public string Grain { get; set; }
            public string Texture { get; set; }
            public TipArtwork[] Tips { get; set; }
        }

        public sealed class TipArtwork
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Strength { get; set; }
            public double Delay { get; set; }
            public string Stem { get; set; }
            public string Leaves { get; set; }
            public double[][] Flowers { get; set; }
            public double[][] Buds { get; set; }
        }

        private sealed class BranchLayer : FrameworkElement
        {
            private readonly DrawingVisual visual = new DrawingVisual();

            internal BranchLayer(Action<DrawingContext> draw)
            {
                using (var context = visual.RenderOpen()) draw(context);
                AddVisualChild(visual);
            }

            protected override int VisualChildrenCount { get { return 1; } }

            protected override Visual GetVisualChild(int index)
            {
                if (index != 0) throw new ArgumentOutOfRangeException("index");
                return visual;
            }
        }

        private const string PetalShape = "M10 10 C8.7 9.2 5.45 7.5 5.55 4.75 C5.6 2.4 7.55 1.05 10 1.2 C12.45 1.05 14.4 2.4 14.45 4.75 C14.55 7.5 11.3 9.2 10 10 Z";
        private const string BudShape = "M0 .5 Q-1.5 -.4 -1.1 -1.9 Q-.7 -3.1 .4 -2.6 Q1.6 -2.1 1 -1 Q.8 -.1 0 .5 Z";
        private const string FallingPetal = "M.5,1.5 C3,-1 6,1.7 5.5,4 C5.1,5.8 3.6,7.1 2,7.8 C2.3,5.2 -.8,4 .5,1.5 Z";
        private static readonly PropertyPath TipAngle = new PropertyPath("(0).(1)", RenderTransformProperty, RotateTransform.AngleProperty);
        private static readonly PropertyPath PetalAngle = new PropertyPath("(0).(1)[0].(2)", RenderTransformProperty, TransformGroup.ChildrenProperty, RotateTransform.AngleProperty);
        private static readonly PropertyPath PetalX = new PropertyPath("(0).(1)[1].(2)", RenderTransformProperty, TransformGroup.ChildrenProperty, TranslateTransform.XProperty);
        private static readonly PropertyPath PetalY = new PropertyPath("(0).(1)[1].(2)", RenderTransformProperty, TransformGroup.ChildrenProperty, TranslateTransform.YProperty);
        private static readonly PropertyPath RainX = new PropertyPath("(0).(1)", RenderTransformProperty, TranslateTransform.XProperty);
        private static readonly PropertyPath RainY = new PropertyPath("(0).(1)", RenderTransformProperty, TranslateTransform.YProperty);
        private static readonly PropertyPath ParticleOpacity = new PropertyPath(OpacityProperty);
        private static readonly BranchArtwork artwork = ReadArtwork();
        private static readonly Brush flowerPink = FlowerPaint("#FFF4EE", "#FFD1DE", "#EF94AB");
        private static readonly Brush flowerRose = FlowerPaint("#FFECDF", "#FAAEC4", "#DB6D94");
        private static readonly Pen flowerEdge = Outline(Brush("#C67899"), .38);
        private static readonly Pen stamen = Outline(Brush("#B56386"), .42);
        private static readonly Brush flowerCenter = Brush("#A24D72");
        private static readonly Brush pollen = Brush("#FFF0C9");
        private static readonly Geometry flowerPetal = Geometry.Parse(PetalShape);
        private static readonly Geometry filament = Geometry.Parse("M10 9.8 Q9.4 8.4 9.7 7.2");
        private static readonly Geometry budShape = Geometry.Parse(BudShape);
        private static readonly DrawingGroup pinkBlossom = CreateBlossom(flowerPink);
        private static readonly DrawingGroup roseBlossom = CreateBlossom(flowerRose);
        private static readonly BitmapCache movingBranchCache = CreateBranchCache();
        private readonly Canvas particles = new Canvas();
        private readonly Canvas branch;
        private readonly List<BranchLayer> tips = new List<BranchLayer>();
        private readonly List<BranchLayer> ornaments = new List<BranchLayer>();
        private Storyboard animation;
        private bool horizontal;

        internal bool IsAnimating { get { return animation != null &&
            tips.Concat(ornaments).All(layer => ((RotateTransform)layer.RenderTransform).HasAnimatedProperties); } }

        internal PeachScene()
        {
            IsHitTestVisible = false;
            Children.Add(particles);
            branch = new Canvas { Width = 147, Height = 61, Margin = new Thickness(0, 4, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top };
            Children.Add(branch);

            var twigInk = Outline(Brush("#AC7770"), .68);
            var leafInk = Brush("#41252C");
            var budPaint = Gradient("#FBC4D7", "#C56D96");
            foreach (var tip in artwork.Tips)
            {
                var layer = new BranchLayer(drawing =>
                {
                    drawing.DrawGeometry(null, twigInk, Geometry.Parse(tip.Stem));
                    drawing.DrawGeometry(leafInk, null, Geometry.Parse(tip.Leaves));
                    foreach (var bud in tip.Buds)
                    {
                        drawing.PushTransform(new TranslateTransform(bud[0], bud[1]));
                        drawing.PushTransform(new RotateTransform(bud[2]));
                        drawing.DrawGeometry(budPaint, null, budShape);
                        drawing.Pop();
                        drawing.Pop();
                    }
                    for (int i = 0; i < tip.Flowers.Length; i++)
                    {
                        var flower = tip.Flowers[i];
                        Blossom(drawing, flower[0], flower[1], flower[2], flower[3], i % 3 == 2);
                    }
                }) { RenderTransform = new RotateTransform(0, tip.X, tip.Y) };
                tips.Add(layer);
                branch.Children.Add(layer);
            }

            var bark = new LinearGradientBrush(Brush("#533237").Color, Brush("#29191E").Color, new Point(0, 0), new Point(1, 1));
            branch.Children.Add(new BranchLayer(drawing =>
            {
                drawing.DrawGeometry(bark, Outline(Brush("#B07C514C"), .42), Geometry.Parse("F1 " + artwork.Wood));
                drawing.DrawGeometry(null, Outline(Brush("#BF9A6D5E"), .55), Geometry.Parse(artwork.Grain));
                drawing.DrawGeometry(Brush("#BA79544E"), null, Geometry.Parse(artwork.Texture));
            }));
            AddOrnament(25, 30, drawing => HolidayScene.DrawHangingEnvelope(drawing, 25, 30, 10, .94));
            AddOrnament(61, 24, drawing => HolidayScene.DrawHangingEnvelope(drawing, 61, 24, -9, .94));
            AddOrnament(116, 24, drawing =>
            {
                drawing.DrawLine(Outline(Brush("#FFCE78"), .72), new Point(116, 24), new Point(118, 32));
                HolidayScene.DrawHangingEnvelope(drawing, 118, 32, 8, .98);
            });
            AddOrnament(95, 10, drawing =>
            {
                drawing.DrawLine(Outline(Brush("#FFCE78"), .72), new Point(95, 10), new Point(101, 17));
                HolidayScene.DrawDiamondString(drawing, 101, 17);
            });
        }

        private void AddOrnament(double x, double y, Action<DrawingContext> draw)
        {
            var ornament = new BranchLayer(draw) { RenderTransform = new RotateTransform(0, x, y) };
            ornaments.Add(ornament);
            branch.Children.Add(ornament);
        }

        private static BitmapCache CreateBranchCache()
        {
            // Keep a high-resolution copy of each moving layer so sway only needs a transform.
            var cache = new BitmapCache(2);
            cache.Freeze();
            return cache;
        }

        internal void SetHorizontal(bool value)
        {
            if (horizontal == value) return;
            bool animating = animation != null;
            SetMotion(false);
            horizontal = value;
            branch.HorizontalAlignment = value ? HorizontalAlignment.Left : HorizontalAlignment.Right;
            branch.RenderTransform = value ? new MatrixTransform(-.8, 0, 0, .8, 122, 0) : Transform.Identity;
            SetMotion(animating);
        }

        private static BranchArtwork ReadArtwork()
        {
            using (var reader = new StreamReader(typeof(PeachScene).Assembly.GetManifestResourceStream("NhipNgay.Peach")))
                return new JavaScriptSerializer().Deserialize<BranchArtwork>(reader.ReadToEnd());
        }

        private static SolidColorBrush Brush(string color)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
            brush.Freeze();
            return brush;
        }

        private static LinearGradientBrush Gradient(string from, string to)
        {
            var brush = new LinearGradientBrush(Brush(from).Color, Brush(to).Color, 115);
            brush.Freeze();
            return brush;
        }

        private static Brush FlowerPaint(string first, string middle, string last)
        {
            var fill = new LinearGradientBrush { StartPoint = new Point(.25, 0), EndPoint = new Point(.7, 1) };
            fill.GradientStops.Add(new GradientStop(Brush(first).Color, 0));
            fill.GradientStops.Add(new GradientStop(Brush(middle).Color, .55));
            fill.GradientStops.Add(new GradientStop(Brush(last).Color, 1));
            fill.Freeze();
            return fill;
        }

        private static Path Filled(string data, Brush fill)
        {
            return new Path { Data = Geometry.Parse(data), Fill = fill };
        }

        private static Path Stroke(string data, Brush brush, double thickness)
        {
            return new Path { Data = Geometry.Parse(data), Stroke = brush, StrokeThickness = thickness,
                StrokeLineJoin = PenLineJoin.Round, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round };
        }

        private static Pen Outline(Brush brush, double thickness)
        {
            var pen = new Pen(brush, thickness) { LineJoin = PenLineJoin.Round,
                StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
            pen.Freeze();
            return pen;
        }

        private static DrawingGroup CreateBlossom(Brush fill)
        {
            var blossom = new DrawingGroup();
            using (var drawing = blossom.Open())
            {
                for (int i = 0; i < 5; i++)
                {
                    drawing.PushTransform(new RotateTransform(i * 72, 10, 10));
                    drawing.DrawGeometry(fill, flowerEdge, flowerPetal);
                    drawing.DrawGeometry(null, stamen, filament);
                    drawing.Pop();
                }
                drawing.DrawEllipse(flowerCenter, null, new Point(10, 10), 1.35, 1.35);
                drawing.DrawEllipse(pollen, null, new Point(10, 9.9), .55, .55);
                drawing.DrawEllipse(pollen, null, new Point(8.65, 9.2), .36, .36);
                drawing.DrawEllipse(pollen, null, new Point(11.3, 9.05), .34, .34);
                drawing.DrawEllipse(pollen, null, new Point(10.65, 11.3), .375, .375);
            }
            blossom.Freeze();
            return blossom;
        }

        private static void Blossom(DrawingContext drawing, double x, double y, double scale, double rotation, bool shade)
        {
            var matrix = Matrix.Identity;
            matrix.ScaleAt(scale, scale, 10, 10);
            matrix.RotateAt(rotation, 10, 10);
            matrix.Translate(x - 10, y - 10);
            var placement = new MatrixTransform(matrix);
            placement.Freeze();
            drawing.PushTransform(placement);
            drawing.DrawDrawing(shade ? roseBlossom : pinkBlossom);
            drawing.Pop();
        }

        internal void SetMotion(bool enabled)
        {
            if (enabled == (animation != null)) return;
            foreach (var layer in tips.Concat(ornaments)) layer.CacheMode = enabled ? movingBranchCache : null;
            if (animation != null)
            {
                animation.Remove(this);
                animation = null;
                particles.Children.Clear();
            }
            if (!enabled) return;

            animation = new Storyboard();
            Timeline.SetDesiredFrameRate(animation, FramesPerSecond);
            for (int i = 0; i < tips.Count; i++)
            {
                var tip = artwork.Tips[i];
                Animate(tips[i], TipAngle, 8.6, tip.Delay, true,
                    0, 0, .22, tip.Strength, .41, -tip.Strength * .42, .54, tip.Strength * .24, .72, 0, 1, 0);
            }
            for (int i = 0; i < ornaments.Count; i++)
            {
                double swing = i == 3 ? 4.8 : 7;
                Animate(ornaments[i], TipAngle, 4.1 + .34 * i, .18 * i, true,
                    0, 0, .21, swing, .52, -swing, .79, swing * .43, 1, 0);
            }
            if (horizontal)
            {
                AddPetal(25, 23, 10, 62, 9, .1, .56);
                AddPetal(108, 29, 13, 57, 11, 3.1, .55);
            }
            else
            {
                AddPetal(159, 33, 23, 198, 13, .1, .64);
                AddPetal(179, 44, 9, 184, 16, 3.1, .62);
                AddPetal(15, 103, 8, 144, 17, 5.2, .5);
            }
            int[] rainX = horizontal ? new[] { 48, 119, 189, 275, 343 } : new[] { 29, 57, 105, 142, 187 };
            for (int i = 0; i < rainX.Length; i++) AddRain(rainX[i], 5.8 + i * .62, i * .77, horizontal ? 110 : 278);
            animation.Begin(this, true);
        }

        private void AddPetal(double x, double y, double drift, double fall, double duration, double delay, double alpha)
        {
            var petal = Filled(FallingPetal, Gradient("#FFE1EB", "#DC87AA"));
            var transform = new TransformGroup();
            transform.Children.Add(new RotateTransform(-18, 3, 4));
            transform.Children.Add(new TranslateTransform());
            petal.RenderTransform = transform;
            AddParticle(petal, x, y);
            Animate(petal, PetalX, duration, delay, true, 0, 0, .28, drift * .78, .55, drift * .3, .79, drift, 1, drift * .7);
            Animate(petal, PetalY, duration, delay, false, 0, 0, 1, fall);
            Animate(petal, PetalAngle, duration, delay, true, 0, -18, .28, 94, .56, 31, .84, 161, 1, 212);
            Animate(petal, ParticleOpacity, duration, delay, false, 0, 0, .12, alpha, .62, alpha * .8, .92, 0, 1, 0);
        }

        private void AddRain(double x, double duration, double delay, double distance)
        {
            var rain = Stroke("M4,0 L0,16", Gradient("#00F3D3D8", "#FFF3D3D8"), .75);
            rain.RenderTransform = new TranslateTransform();
            AddParticle(rain, x, -17);
            Animate(rain, RainX, duration, delay, false, 0, 0, 1, -distance * .12);
            Animate(rain, RainY, duration, delay, false, 0, 0, 1, distance);
            Animate(rain, ParticleOpacity, duration, delay, false, 0, 0, .15, .23, .62, .18, .92, 0, 1, 0);
        }

        private void AddParticle(Shape shape, double x, double y)
        {
            shape.Opacity = 0;
            Canvas.SetLeft(shape, x);
            Canvas.SetTop(shape, y);
            particles.Children.Add(shape);
        }

        private void Animate(UIElement target, PropertyPath property, double duration, double delay, bool eased, params double[] frames)
        {
            var track = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromSeconds(duration),
                BeginTime = TimeSpan.FromSeconds(delay), RepeatBehavior = RepeatBehavior.Forever };
            for (int i = 0; i < frames.Length; i += 2)
            {
                var time = KeyTime.FromPercent(frames[i]);
                if (eased) track.KeyFrames.Add(new EasingDoubleKeyFrame(frames[i + 1], time, new SineEase { EasingMode = EasingMode.EaseInOut }));
                else track.KeyFrames.Add(new LinearDoubleKeyFrame(frames[i + 1], time));
            }
            Storyboard.SetTarget(track, target);
            Storyboard.SetTargetProperty(track, property);
            animation.Children.Add(track);
        }
    }
}
