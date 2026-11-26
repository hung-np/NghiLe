using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace NghiLe
{
    internal static partial class HolidayDiorama
    {
        private const int SceneWidth = 392;
        private const int SceneHeight = 202;
        private const double Aspect = SceneWidth / (double)SceneHeight;

        private enum Effect { Glints, Petals, Snow, Sparks }
        private enum Finish { Matte, Lacquer, Brass }

        private static readonly Brush TrayShadow = Backlight("#AD120A13");

        private sealed class Diorama
        {
            internal readonly string AssetName;
            internal readonly Action<Rig> Model;
            internal readonly string BaseColor;
            internal readonly string Surface;
            internal readonly Lazy<BitmapSource> Image;
            internal readonly Brush Light;
            internal readonly Brush Flecks;
            internal readonly Effect Motion;
            private WeakReference<byte[][]> animationFrames;

            internal Diorama(string assetName, Action<Rig> model, string baseColor, string surface, string glow, string accent, Effect motion)
            {
                AssetName = assetName;
                Model = model;
                BaseColor = baseColor;
                Surface = surface;
                Image = new Lazy<BitmapSource>(() => LoadStill(this));
                Light = Backlight(glow);
                Flecks = Ink(accent);
                Motion = motion;
            }

            internal byte[][] GetAnimationFrames()
            {
                byte[][] frames;
                if (animationFrames != null && animationFrames.TryGetTarget(out frames)) return frames;
                frames = ReadFrames(this, true);
                if (frames != null) animationFrames = new WeakReference<byte[][]>(frames);
                return frames;
            }
        }

        private static readonly Diorama Apricot = new Diorama("LunarNewYearApricot", scene => BuildTet(scene, true), "#6C3943",
            "#482730", "#6BBC754A", "#F7D373", Effect.Petals);

        private static readonly Dictionary<HolidayTheme, Diorama> Scenes = new Dictionary<HolidayTheme, Diorama>
        {
            { HolidayTheme.LunarNewYear, new Diorama("LunarNewYear", scene => BuildTet(scene, false), "#6C3943",
                "#482730", "#6BB66D80", "#F6ACBB", Effect.Petals) },
            { HolidayTheme.NewYear, new Diorama("NewYear", BuildNewYear, "#354A76",
                "#2B3657", "#647275DD", "#FFE1AF", Effect.Sparks) },
            { HolidayTheme.HungKings, new Diorama("HungKings", BuildHungKings, "#745032",
                "#4F3728", "#60D7A457", "#FFE6A4", Effect.Glints) },
            { HolidayTheme.Reunification, new Diorama("Reunification", BuildReunification, "#4B6554",
                "#384938", "#64C6BD87", "#FFE0A3", Effect.Glints) },
            { HolidayTheme.LabourDay, new Diorama("LabourDay", BuildLabourDay, "#50516C",
                "#35364B", "#649480C7", "#FFDA90", Effect.Glints) },
            { HolidayTheme.NationalDay, new Diorama("NationalDay", BuildNationalDay, "#7D3A40",
                "#532635", "#67DC7C5C", "#FFE38D", Effect.Sparks) },
            { HolidayTheme.VietnamCultureDay, new Diorama("VietnamCultureDay", BuildCultureDay, "#43675D",
                "#2D4846", "#63A1D6AC", "#F9D89F", Effect.Glints) },
            { HolidayTheme.MidAutumn, new Diorama("MidAutumn", BuildMidAutumn, "#574569",
                "#3C2C50", "#64AE87DB", "#FFDF9E", Effect.Glints) },
            { HolidayTheme.Christmas, new Diorama("Christmas", BuildChristmas, "#446657",
                "#334943", "#6667A78F", "#F4FCFF", Effect.Snow) }
        };

        internal static bool Supports(HolidayTheme? theme)
        {
            return theme.HasValue && Scenes.ContainsKey(theme.Value);
        }

        internal static double CountdownInset(bool compact) { return compact ? 56 : 67; }

        private static Diorama SceneFor(HolidayTheme theme, bool apricot)
        {
            return theme == HolidayTheme.LunarNewYear && apricot ? Apricot : Scenes[theme];
        }

        internal static bool Draw(DrawingContext context, HolidayTheme theme, Size size, bool horizontal,
            bool compact, bool celebrating, bool apricot, Playback playback, double phase)
        {
            var scene = SceneFor(theme, apricot);
            var image = playback == null ? scene.Image.Value : playback.Frame(theme, apricot, phase) ?? scene.Image.Value;
            if (image == null) return false;

            double top = horizontal ? 2 : celebrating ? 8 : 3;
            double height = horizontal ? 56 : celebrating ? 83 : CountdownInset(compact) - top - 2;
            double width = height * Aspect;
            double left = horizontal ? 7 : (size.Width - width) / 2;
            var bounds = new Rect(left, top, width, height);
            context.DrawEllipse(scene.Light, null,
                new Point(bounds.X + width * .54, bounds.Y + height * .52), width * .53, height * .49);
            context.DrawEllipse(TrayShadow, null, new Point(left + width * .5, top + height * .945),
                width * .462, height * .055);
            context.DrawImage(image, bounds);

            if (playback != null) DrawMotion(context, scene, bounds, phase, celebrating);
            return true;
        }

        private static void DrawMotion(DrawingContext context, Diorama scene, Rect bounds, double phase, bool celebrating)
        {
            int count = celebrating ? 7 : 4;
            for (int index = 0; index < count; index++)
            {
                double travel = (phase + index * .273) % 1;
                double sway = .055 * Math.Sin((travel + index * .31) * Math.PI * 2);
                double x = scene.Motion == Effect.Petals ? .71 + sway + (index % 3 - 1) * .089
                    : .15 + (index * .339 % .74) + sway;
                double y;
                switch (scene.Motion)
                {
                    case Effect.Glints: y = .43 + (index % 3) * .12; break;
                    case Effect.Sparks: y = .15 + (index % 3) * .14 - travel * .09; break;
                    default: y = .19 + travel * .65; break;
                }
                double pulse = Math.Sin(travel * Math.PI);
                double radius = scene.Motion == Effect.Sparks ? .6 + pulse * .65 : .88;
                context.PushOpacity(pulse * .56);
                context.DrawEllipse(scene.Flecks, null, new Point(bounds.X + bounds.Width * x, bounds.Y + bounds.Height * y),
                    scene.Motion == Effect.Petals ? 1.08 + .25 * Math.Sin(travel * 12) : radius,
                    scene.Motion == Effect.Petals ? .72 : radius);
                context.Pop();
            }
        }

        private static void BuildTray(Model3DGroup scene, string baseColor, string surface)
        {
            var center = new Point3D();
            Lathe(scene, baseColor, center, .67,
                new[] { 0, 2.05, 2.10, 2.13, 2.13, 2.08, 2.04, 0 },
                new[] { .03, .03, .068, .110, .172, .207, .229, .229 }, Finish.Lacquer);
            Lathe(scene, "#DCAD73", center, .67, new[] { 2.132, 2.132 }, new[] { .106, .128 }, Finish.Brass);
            Lathe(scene, "#BC976B", center, .67, new[] { 2.107, 2.103 }, new[] { .183, .188 }, Finish.Brass);
            Lathe(scene, surface, center, .67, new[] { 2.006, 0 }, new[] { .232, .232 }, Finish.Lacquer);
            Lathe(scene, "#896343", center, .67, new[] { 2.026, 2.006 }, new[] { .233, .234 }, Finish.Brass);
        }

        private static void Box(Model3DGroup scene, string color, Point3D center, double width, double height, double depth)
        {
            var mesh = new MeshGeometry3D();
            double x = width / 2, y = height / 2, z = depth / 2;
            var a = center + new Vector3D(-x, -y, z);
            var b = center + new Vector3D(x, -y, z);
            var c = center + new Vector3D(x, y, z);
            var d = center + new Vector3D(-x, y, z);
            var e = center + new Vector3D(-x, -y, -z);
            var f = center + new Vector3D(x, -y, -z);
            var g = center + new Vector3D(x, y, -z);
            var h = center + new Vector3D(-x, y, -z);
            Quad(mesh, a, b, c, d);
            Quad(mesh, b, f, g, c);
            Quad(mesh, f, e, h, g);
            Quad(mesh, e, a, d, h);
            Quad(mesh, d, c, g, h);
            Quad(mesh, e, f, b, a);
            Model(scene, mesh, color);
        }

        private static void Quad(MeshGeometry3D mesh, Point3D a, Point3D b, Point3D c, Point3D d)
        {
            int first = mesh.Positions.Count;
            mesh.Positions.Add(a);
            mesh.Positions.Add(b);
            mesh.Positions.Add(c);
            mesh.Positions.Add(d);
            foreach (int i in new[] { 0, 1, 2, 0, 2, 3 }) mesh.TriangleIndices.Add(first + i);
            mesh.TextureCoordinates.Add(new Point(0, 1));
            mesh.TextureCoordinates.Add(new Point(1, 1));
            mesh.TextureCoordinates.Add(new Point(1, 0));
            mesh.TextureCoordinates.Add(new Point(0, 0));
        }

        private static void Lathe(Model3DGroup scene, string color, Point3D center, double depth, double[] radii, double[] heights,
            Finish finish = Finish.Matte)
        {
            Revolve(scene, color, radii, heights, 48, (x, y, z) => center + new Vector3D(x, y, z * depth), finish);
        }

        private static void Oval(Model3DGroup scene, string color, Point3D center, double x, double y, double z,
            double degrees, Finish finish = Finish.Matte)
        {
            var radii = new double[6];
            var heights = new double[6];
            for (int ring = 0; ring < radii.Length; ring++)
            {
                var angle = ring * Math.PI / (radii.Length - 1);
                radii[ring] = Math.Sin(angle);
                heights[ring] = -Math.Cos(angle);
            }
            double turn = degrees * Math.PI / 180;
            double cos = Math.Cos(turn), sin = Math.Sin(turn);
            Revolve(scene, color, radii, heights, 10, (px, py, pz) =>
                center + new Vector3D(px * x * cos - py * y * sin, px * x * sin + py * y * cos, pz * z), finish);
        }

        private static void Tube(Model3DGroup scene, string color, Point3D start, Point3D end, double from, double to,
            Finish finish = Finish.Matte)
        {
            var axis = end - start;
            var right = Vector3D.CrossProduct(axis, Math.Abs(axis.Z) < axis.Length * .9
                ? new Vector3D(0, 0, 1) : new Vector3D(1, 0, 0));
            right.Normalize();
            var forward = Vector3D.CrossProduct(right, axis);
            forward.Normalize();
            Revolve(scene, color, new[] { 0, from, to, 0 }, new[] { 0.0, 0, 1, 1 }, 14,
                (x, y, z) => start + axis * y + right * x + forward * z, finish);
        }

        private static void Revolve(Model3DGroup scene, string color, double[] radii, double[] heights, int sides,
            Func<double, double, double, Point3D> place, Finish finish = Finish.Matte)
        {
            var mesh = new MeshGeometry3D();
            for (int ring = 0; ring < radii.Length; ring++)
            {
                for (int side = 0; side < sides; side++)
                {
                    double angle = side * 2 * Math.PI / sides;
                    mesh.Positions.Add(place(radii[ring] * Math.Cos(angle), heights[ring], -radii[ring] * Math.Sin(angle)));
                    if (ring == 0) continue;
                    int a = (ring - 1) * sides + side, b = (ring - 1) * sides + (side + 1) % sides;
                    int c = b + sides, d = a + sides;
                    mesh.TriangleIndices.Add(a);
                    mesh.TriangleIndices.Add(b);
                    mesh.TriangleIndices.Add(c);
                    mesh.TriangleIndices.Add(a);
                    mesh.TriangleIndices.Add(c);
                    mesh.TriangleIndices.Add(d);
                }
            }
            Model(scene, mesh, color, finish);
        }

        private static void Model(Model3DGroup scene, MeshGeometry3D mesh, string color, Finish finish = Finish.Matte)
        {
            Material material = new DiffuseMaterial(Ink(color));
            if (finish != Finish.Matte)
            {
                var layers = new MaterialGroup();
                layers.Children.Add(material);
                layers.Children.Add(new SpecularMaterial(Ink(finish == Finish.Brass ? "#BCFFE7AD" : "#55FFE8CD"),
                    finish == Finish.Brass ? 37 : 55));
                material = layers;
            }
            var part = new GeometryModel3D(mesh, material);
            part.Freeze();
            scene.Children.Add(part);
        }

        private static Color Shade(string value) { return (Color)ColorConverter.ConvertFromString(value); }

        private static Brush Ink(string value)
        {
            var brush = new SolidColorBrush(Shade(value));
            brush.Freeze();
            return brush;
        }

        private static Brush Backlight(string value)
        {
            var color = Shade(value);
            var brush = new RadialGradientBrush(color, Color.FromArgb(0, color.R, color.G, color.B));
            brush.Freeze();
            return brush;
        }
    }
}
