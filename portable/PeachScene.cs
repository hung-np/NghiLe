using System;
using System.IO;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Media;

namespace NghiLe
{
    internal static class PeachScene
    {
        public sealed class BranchArtwork
        {
            public string Wood { get; set; }
            public string Grain { get; set; }
            public string Texture { get; set; }
            public TipArtwork[] Tips { get; set; }
        }

        public sealed class TipArtwork
        {
            public string Stem { get; set; }
            public string Leaves { get; set; }
            public double[][] Flowers { get; set; }
            public double[][] Buds { get; set; }
        }

        private static readonly Lazy<ImageDrawing> Branch = new Lazy<ImageDrawing>(BuildBranch);
        private static readonly Point[] Hooks = { new Point(25, 30), new Point(61, 24), new Point(116, 24), new Point(95, 10) };
        private static readonly Lazy<DrawingGroup[]> Ornaments = new Lazy<DrawingGroup[]>(BuildOrnaments);
        private static readonly Transform HorizontalBranch = Freeze(new MatrixTransform(-.8, 0, 0, .8, 122, 0));
        private static readonly Transform VerticalBranch = Freeze(new TranslateTransform(7, 0));
        private static readonly Geometry FallingPetal = Freeze(Geometry.Parse("M.5,1.5 C3,-1 6,1.7 5.5,4 C5.1,5.8 3.6,7.1 2,7.8 C2.3,5.2 -.8,4 .5,1.5 Z"));
        private static readonly Brush PetalInk = Freeze(new LinearGradientBrush(ColorOf("#FFDDED"), ColorOf("#D78BAD"), 90));
        private static readonly double[] PetalOrigins = { 21, 54, 91, 116 };

        internal static void Draw(DrawingContext drawing, double phase, bool moving, bool horizontal)
        {
            drawing.PushTransform(horizontal ? HorizontalBranch : VerticalBranch);
            drawing.DrawDrawing(Branch.Value);
            for (int i = 0; i < Hooks.Length; i++)
            {
                if (moving)
                {
                    var hook = Hooks[i];
                    double sway = Math.Sin((phase * 2 + i * .13) * Math.PI * 2) * (10 - i);
                    drawing.PushTransform(new RotateTransform(sway, hook.X, hook.Y));
                }
                drawing.DrawDrawing(Ornaments.Value[i]);
                if (moving) drawing.Pop();
            }
            drawing.Pop();

            if (!moving) return;
            for (int i = 0; i < PetalOrigins.Length; i++)
            {
                double fall = (phase + i * .24) % 1;
                var matrix = Matrix.Identity;
                matrix.RotateAt(fall * 230 + i * 32, 3, 4);
                matrix.Translate(PetalOrigins[i] + Math.Sin(fall * Math.PI * 2) * 6, 9 + fall * 51);
                drawing.PushOpacity(Math.Sin(fall * Math.PI) * .85);
                drawing.PushTransform(new MatrixTransform(matrix));
                drawing.DrawGeometry(PetalInk, null, FallingPetal);
                drawing.Pop();
                drawing.Pop();
            }
        }

        private static ImageDrawing BuildBranch()
        {
            BranchArtwork artwork;
            using (var reader = new StreamReader(typeof(PeachScene).Assembly.GetManifestResourceStream("NghiLe.Peach")))
                artwork = new JavaScriptSerializer().Deserialize<BranchArtwork>(reader.ReadToEnd());

            var twig = new Pen(Paint("#AC7770"), .68);
            var leaf = Paint("#41252C");
            var budPaint = new LinearGradientBrush(ColorOf("#FBC4D7"), ColorOf("#C56D96"), 90);
            var budShape = Geometry.Parse("M0 .5 Q-1.5 -.4 -1.1 -1.9 Q-.7 -3.1 .4 -2.6 Q1.6 -2.1 1 -1 Q.8 -.1 0 .5 Z");
            var pink = BuildBlossom("#FFF4EE", "#EF94AB");
            var rose = BuildBlossom("#FFECDF", "#DB6D94");
            var branch = HolidayScene.DrawScene(drawing =>
            {
                foreach (var tip in artwork.Tips)
                {
                    drawing.DrawGeometry(null, twig, Geometry.Parse(tip.Stem));
                    drawing.DrawGeometry(leaf, null, Geometry.Parse(tip.Leaves));
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
                        var matrix = Matrix.Identity;
                        matrix.ScaleAt(flower[2], flower[2], 10, 10);
                        matrix.RotateAt(flower[3], 10, 10);
                        matrix.Translate(flower[0] - 10, flower[1] - 10);
                        drawing.PushTransform(new MatrixTransform(matrix));
                        drawing.DrawDrawing(i % 3 == 2 ? rose : pink);
                        drawing.Pop();
                    }
                }
                var bark = new LinearGradientBrush(ColorOf("#533237"), ColorOf("#29191E"), 45);
                drawing.DrawGeometry(bark, new Pen(Paint("#B07C514C"), .42), Geometry.Parse("F1 " + artwork.Wood));
                drawing.DrawGeometry(null, new Pen(Paint("#BF9A6D5E"), .55), Geometry.Parse(artwork.Grain));
                drawing.DrawGeometry(Paint("#BA79544E"), null, Geometry.Parse(artwork.Texture));
            });
            return HolidayScene.Rasterize(branch);
        }

        private static DrawingGroup BuildBlossom(string first, string last)
        {
            var petal = Geometry.Parse("M10 10 C8.7 9.2 5.45 7.5 5.55 4.75 C5.6 2.4 7.55 1.05 10 1.2 C12.45 1.05 14.4 2.4 14.45 4.75 C14.55 7.5 11.3 9.2 10 10 Z");
            var filament = Geometry.Parse("M10 9.8 Q9.4 8.4 9.7 7.2");
            var rim = new Pen(Paint("#C67899"), .38);
            var stamen = new Pen(Paint("#B56386"), .42);
            var fill = new LinearGradientBrush(ColorOf(first), ColorOf(last), 90);
            return HolidayScene.DrawScene(drawing =>
            {
                for (int i = 0; i < 5; i++)
                {
                    drawing.PushTransform(new RotateTransform(i * 72, 10, 10));
                    drawing.DrawGeometry(fill, rim, petal);
                    drawing.DrawGeometry(null, stamen, filament);
                    drawing.Pop();
                }
                drawing.DrawEllipse(Paint("#A24D72"), null, new Point(10, 10), 1.35, 1.35);
                drawing.DrawEllipse(Paint("#FFF0C9"), null, new Point(10, 9.9), .65, .65);
            });
        }

        private static DrawingGroup[] BuildOrnaments()
        {
            var cord = new Pen(Paint("#FFCE78"), .72);
            return new[]
            {
                HolidayScene.DrawScene(drawing => HolidayScene.DrawHangingEnvelope(drawing, 25, 30, 10, .94)),
                HolidayScene.DrawScene(drawing => HolidayScene.DrawHangingEnvelope(drawing, 61, 24, -9, .94)),
                HolidayScene.DrawScene(drawing =>
                {
                    drawing.DrawLine(cord, Hooks[2], new Point(118, 32));
                    HolidayScene.DrawHangingEnvelope(drawing, 118, 32, 8, .98);
                }),
                HolidayScene.DrawScene(drawing =>
                {
                    drawing.DrawLine(cord, Hooks[3], new Point(101, 17));
                    HolidayScene.DrawDiamondString(drawing, 101, 17);
                })
            };
        }

        private static Color ColorOf(string value) { return (Color)ColorConverter.ConvertFromString(value); }
        private static Brush Paint(string value) { return new SolidColorBrush(ColorOf(value)); }

        private static T Freeze<T>(T resource) where T : Freezable
        {
            resource.Freeze();
            return resource;
        }
    }
}
