using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace NghiLe
{
    internal static partial class HolidayDiorama
    {
        private static void BuildNewYear(Rig rig)
        {
            var scene = rig.Model;
            ContactShadow(scene, new Point3D(-.02, .237, .02), 3.67, 1.29);
            var heights = new[] { .46, .72, .54, .93, .61, .77, .45 };
            for (int i = 0; i < heights.Length; i++)
            {
                double x = (i - 3) * .44;
                double y = .23 + heights[i] / 2;
                Box(scene, i % 2 == 0 ? "#3C4D7E" : "#515F8C", new Point3D(x, y, -.10), .36, heights[i], .48);
                Box(scene, "#7185AB", new Point3D(x, y + heights[i] / 2, -.10), .39, .048, .51);
                for (int row = 0; row < (int)(heights[i] / .16); row++)
                    for (int column = -1; column <= 1; column++)
                        Box(scene, (i + row + column) % 4 == 0 ? "#A8CCE7" : "#FFE3A0",
                            new Point3D(x + column * .092, .34 + row * .15, .149), .052, .082, .018);
            }
            Box(scene, "#6B7EAC", new Point3D(0, .268, .36), 3.18, .068, .13);
            Tube(scene, "#BFCFFF", new Point3D(0, 1.16, -.1), new Point3D(0, 1.37, -.1), .021, .012);
            Burst(rig, new Point3D(-1.15, 1.49, -.37), .56, "#FBD28E", "#FFF1BD", .04);
            Burst(rig, new Point3D(.77, 1.62, -.39), .53, "#A1BBFA", "#EBDAFF", .43);
            Burst(rig, new Point3D(1.52, 1.27, -.12), .25, "#E6A7C3", "#FFE4E5", .68);
        }

        private static void Burst(Rig rig, Point3D center, double size, string ray, string tip, double offset)
        {
            var burst = new Model3DGroup();
            Firework(burst, center, size, ray, tip);
            var grow = new ScaleTransform3D(1, 1, 1, center.X, center.Y, center.Z);
            burst.Transform = grow;
            rig.Animate(phase =>
            {
                var beat = Math.Sin((phase * 2 + offset) * 2 * Math.PI) * .5 + .5;
                grow.ScaleX = grow.ScaleY = grow.ScaleZ = .40 + .66 * Math.Pow(beat, .72);
            });
            rig.Model.Children.Add(burst);
        }

        private static void Firework(Model3DGroup scene, Point3D center, double size, string ray, string tip)
        {
            Oval(scene, tip, center, .046, .046, .031, 0);
            int spokes = size > .3 ? 13 : 8;
            for (int i = 0; i < spokes; i++)
            {
                double angle = (i + .27) * 2 * Math.PI / spokes;
                var direction = new Vector3D(Math.Cos(angle), Math.Sin(angle), Math.Sin(angle * 3) * .21);
                double reach = size * (.82 + .18 * Math.Sin(i * 4));
                Tube(scene, ray, center + direction * reach * .23, center + direction * reach * .78, .018, .012);
                Oval(scene, tip, center + direction * reach, .026, .026, .024, 0);
            }
        }

        private static void BuildHungKings(Rig rig)
        {
            var scene = rig.Model;
            ContactShadow(scene, new Point3D(-.36, .237, .05), 2.16, 1.13);
            var drum = new Model3DGroup();
            var center = new Point3D();
            Lathe(drum, "#AB6937", center, 1,
                new[] { 0, .62, .69, .64, .52, .51, .71, .85, .88, .87, 0 },
                new[] { 0, 0, .07, .15, .31, .46, .63, .68, .75, .82, .82 });
            Lathe(drum, "#E1AA67", center, 1, new[] { .876, .884, .872 }, new[] { .708, .75, .788 }, Finish.Brass);
            Lathe(drum, "#CF8B4C", center, 1, new[] { .649, .652 }, new[] { .126, .154 }, Finish.Brass);
            DrumFace(drum);

            var tilt = new Transform3DGroup();
            tilt.Children.Add(Turn(1, 0, 0, 59));
            var turn = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0);
            tilt.Children.Add(new RotateTransform3D(turn, new Point3D(0, .4, 0)));
            tilt.Children.Add(new TranslateTransform3D(-.43, 1.06, -.11));
            rig.Animate(phase => turn.Angle = 16 * Math.Sin(phase * 2 * Math.PI));
            drum.Transform = tilt;
            scene.Children.Add(drum);
            Box(scene, "#795443", new Point3D(-.44, .44, -.02), 1.15, .38, .61);
            for (int step = 0; step < 3; step++)
                Box(scene, "#BC8E66", new Point3D(1.04, .28 + step * .104, -.16 - step * .19),
                    1.15, .104, .97 - step * .22);
            for (int pole = 0; pole < 2; pole++)
            {
                double x = .64 + .76 * pole;
                Box(scene, "#855343", new Point3D(x, .87, -.57), .092, .80, .13);
                Box(scene, "#D0A174", new Point3D(x, .52, -.57), .18, .085, .21);
            }
            Roof(scene, "#A6543A", new Point3D(1.02, 1.26, -.56), 1.22, .38, .67);
        }

        private static void DrumFace(Model3DGroup scene)
        {
            const double radius = .855;
            var mesh = new MeshGeometry3D();
            mesh.Positions.Add(new Point3D(0, .822, 0));
            mesh.TextureCoordinates.Add(new Point(.5, .5));
            for (int i = 0; i < 48; i++)
            {
                double angle = i * 2 * Math.PI / 48;
                double x = Math.Cos(angle), z = -Math.Sin(angle);
                mesh.Positions.Add(new Point3D(radius * x, .822, radius * z));
                mesh.TextureCoordinates.Add(new Point(.5 + x / 2, .5 + z / 2));
                mesh.TriangleIndices.Add(0);
                mesh.TriangleIndices.Add(i + 1);
                mesh.TriangleIndices.Add((i + 1) % 48 + 1);
            }
            var material = Print(160, 160, drawing =>
            {
                var center = new Point(80, 80);
                drawing.DrawEllipse(new RadialGradientBrush(Shade("#F5BE76"), Shade("#C28147")), null, center, 80, 80);
                foreach (int ring in new[] { 32, 37, 50, 54, 66, 72, 76 })
                    drawing.DrawEllipse(null, new Pen(Ink("#845032"), ring % 2 == 0 ? 1.6 : 1.0), center, ring, ring);
                for (int mark = 0; mark < 30; mark++)
                {
                    double theta = mark * 2 * Math.PI / 30;
                    drawing.DrawEllipse(Ink("#995831"), null,
                        new Point(80 + Math.Cos(theta) * 60, 80 + Math.Sin(theta) * 60), 2, 2);
                }
                drawing.DrawGeometry(Ink("#88502F"), null, Star(center, 30, 9, 12));
            });
            scene.Children.Add(new GeometryModel3D(mesh, material));
        }

        private static void BuildReunification(Rig rig)
        {
            var scene = rig.Model;
            Box(scene, "#8CA486", new Point3D(0, .268, .08), 3.47, .07, 1.58);
            Box(scene, "#D6C6A7", new Point3D(0, .36, -.19), 2.89, .23, 1.16);
            Box(scene, "#A5AB9A", new Point3D(0, .83, -.27), 2.62, .78, .96);
            Box(scene, "#566759", new Point3D(0, .82, .225), 2.52, .62, .052);
            Box(scene, "#E6D6B5", new Point3D(0, 1.21, -.24), 2.87, .13, 1.15);
            Box(scene, "#BCB598", new Point3D(0, 1.31, -.29), 1.79, .092, .79);
            for (int column = -8; column <= 8; column++)
            {
                double x = column * .154;
                Box(scene, "#E7DABB", new Point3D(x, .833, .296), .053, .65, .11);
            }
            Box(scene, "#D7C9AB", new Point3D(0, .81, .25), 2.61, .064, .13);
            for (int step = 0; step < 3; step++)
                Box(scene, "#D2C8AE", new Point3D(0, .257 + step * .054, .61 - step * .10), .91, .061, .35);
            for (int i = 0; i < 4; i++)
            {
                double x = (i < 2 ? -1 : 1) * (1.12 + i % 2 * .30);
                Oval(scene, i % 2 == 0 ? "#71906A" : "#4C7255", new Point3D(x, .40, .58), .20, .14, .18, 0);
            }
            Flag(rig, new Point3D(-.16, 1.35, -.18), .69, .78, .40);
        }

        private static void BuildLabourDay(Rig rig)
        {
            var scene = rig.Model;
            ContactShadow(scene, new Point3D(-.46, .237, .03), 2.12, .98);
            ContactShadow(scene, new Point3D(.76, .238, .44), 1.89, 1.0);
            var gear = new Model3DGroup();
            Tube(gear, "#9C683F", new Point3D(0, 0, -.075), new Point3D(0, 0, .075), .69, .69);
            Tube(gear, "#CB945B", new Point3D(0, 0, .075), new Point3D(0, 0, .103), .64, .62, Finish.Brass);
            for (int tooth = 0; tooth < 12; tooth++)
            {
                var node = new Model3DGroup();
                Box(node, "#D59E68", new Point3D(0, .685, 0), .21, .202, .153);
                node.Transform = Turn(0, 0, 1, tooth * 30);
                gear.Children.Add(node);
            }
            Tube(gear, "#6A5054", new Point3D(0, 0, .10), new Point3D(0, 0, .126), .37, .34);
            Tube(gear, "#493B48", new Point3D(0, 0, .125), new Point3D(0, 0, .137), .16, .16);
            for (int bolt = 0; bolt < 6; bolt++)
            {
                var angle = bolt * Math.PI / 3;
                var p = new Point3D(.48 * Math.Cos(angle), .48 * Math.Sin(angle), .124);
                Tube(gear, "#F0C18A", p, p + new Vector3D(0, 0, .042), .048, .042, Finish.Brass);
            }
            var pose = new Transform3DGroup();
            var spin = new AxisAngleRotation3D(new Vector3D(0, 0, 1), 0);
            pose.Children.Add(new RotateTransform3D(spin));
            rig.Animate(phase => spin.Angle = -360 * phase);
            pose.Children.Add(Turn(1, 0, 0, -14));
            pose.Children.Add(Turn(0, 1, 0, -16));
            pose.Children.Add(new TranslateTransform3D(-.50, 1.03, -.13));
            gear.Transform = pose;
            scene.Children.Add(gear);

            var helmet = new Point3D(.84, .28, .45);
            Lathe(scene, "#E8A536", helmet, .82,
                new[] { 0, .82, .86, .80, .64, .58, .48, .34, .16, 0 },
                new[] { 0, 0, .07, .13, .17, .38, .56, .67, .73, .745 }, Finish.Lacquer);
            Lathe(scene, "#F7C05E", helmet, .82, new[] { .812, .864, .821 }, new[] { .058, .072, .116 });
            Oval(scene, "#FDD076", new Point3D(.83, .995, .44), .083, .045, .30, 0);
            Box(scene, "#D69746", new Point3D(1.46, .32, -.36), .43, .17, .18);
        }

        private static void BuildNationalDay(Rig rig)
        {
            var scene = rig.Model;
            Box(scene, "#BFA194", new Point3D(.62, .31, -.07), 2.07, .16, 1.06);
            Box(scene, "#D2B7A5", new Point3D(.62, .43, -.10), 1.93, .11, .95);
            Box(scene, "#A38D89", new Point3D(.62, .72, -.24), 1.62, .56, .67);
            for (int pillar = -3; pillar <= 3; pillar++)
                Box(scene, "#D4BBAA", new Point3D(.62 + pillar * .226, .725, .129), .12, .52, .13);
            Box(scene, "#D6B4A1", new Point3D(.62, 1.04, -.18), 1.91, .15, .89);
            Box(scene, "#AC8D86", new Point3D(.62, 1.155, -.18), 1.79, .08, .83);
            Flag(rig, new Point3D(-1.46, .24, .44), 1.89, 1.51, .92);
            Box(scene, "#C9A370", new Point3D(-1.46, .277, .44), .37, .092, .32);
            for (int i = 0; i < 3; i++)
                Oval(scene, i % 2 == 0 ? "#B15655" : "#E6AD71", new Point3D(.73 + i * .24, .33, .57), .105, .10, .109, 0);
        }

        private static void Flag(Rig rig, Point3D foot, double height, double width, double size)
        {
            var scene = rig.Model;
            double radius = height > 1 ? .030 : .015;
            Tube(scene, "#D6AF83", foot, foot + new Vector3D(0, height, 0), radius, radius * .72, Finish.Brass);
            Oval(scene, "#EEC183", foot + new Vector3D(0, height + radius, 0), radius * 1.4, radius * 1.4, radius * 1.4, 0);
            var mesh = new MeshGeometry3D();
            for (int i = 0; i <= 14; i++)
            {
                double span = i / 14.0;
                mesh.TextureCoordinates.Add(new Point(span, 1));
                mesh.TextureCoordinates.Add(new Point(span, 0));
                if (i == 0) continue;
                int a = (i - 1) * 2;
                foreach (int index in new[] { a, a + 2, a + 3, a, a + 3, a + 1 }) mesh.TriangleIndices.Add(index);
            }
            Action<double> wave = phase =>
            {
                var positions = new Point3DCollection(30);
                for (int strip = 0; strip <= 14; strip++)
                {
                    double span = strip / 14.0;
                    double sweep = span * 2.8 * Math.PI - phase * 4 * Math.PI;
                    double depth = .31 * size * span * Math.Sin(sweep);
                    double drop = size * span * (-.08 + .19 * Math.Sin(sweep + .65));
                    positions.Add(foot + new Vector3D(width * span, height + drop - size, depth * 1.22));
                    positions.Add(foot + new Vector3D(width * span, height + drop, depth));
                }
                positions.Freeze();
                mesh.Positions = positions;
            };
            wave(0);
            rig.Animate(wave);
            var cloth = Print(150, 100, drawing =>
            {
                drawing.DrawRectangle(new LinearGradientBrush(Shade("#DE4A47"), Shade("#AB252F"), 90),
                    null, new Rect(0, 0, 150, 100));
                drawing.DrawGeometry(Ink("#FFE589"), null, Star(new Point(75, 50), 25, 9.54, 5));
            });
            scene.Children.Add(new GeometryModel3D(mesh, cloth) { BackMaterial = cloth });
        }

        private static void BuildCultureDay(Rig rig)
        {
            var scene = rig.Model;
            var beam = new SpotLight
            {
                Color = Color.FromRgb(176, 138, 93),
                Range = 4,
                InnerConeAngle = 18,
                OuterConeAngle = 36,
                ConstantAttenuation = 1
            };
            Action<double> aim = phase =>
            {
                double x = 1.22 * Math.Sin(phase * 2 * Math.PI);
                beam.Position = new Point3D(x - .24, 1.88, 2.24);
                beam.Direction = new Point3D(x, .83, .45) - beam.Position;
            };
            aim(0);
            rig.Animate(aim);
            scene.Children.Add(beam);
            Box(scene, "#D0AD85", new Point3D(0, .29, .02), 3.48, .14, 1.38);
            Box(scene, "#E1C18F", new Point3D(0, .79, -.19), 3.15, .91, .96);
            Box(scene, "#F1D5A7", new Point3D(0, .90, .30), 1.73, 1.12, .59);
            Box(scene, "#DFBE94", new Point3D(0, 1.28, -.18), 3.25, .16, 1.03);
            foreach (int wing in new[] { -1, 1 })
            {
                Roof(scene, "#648A7D", new Point3D(wing * 1.13, 1.34, -.18), 1.18, .30, 1.05);
                Arch(scene, "#A78064", wing * 1.19, .46, .300, .44, .59);
                Arch(scene, "#4F655F", wing * 1.19, .48, .306, .31, .52);
                Box(scene, "#86674D", new Point3D(wing * 1.19, 1.12, .301), .21, .15, .026);
            }
            Roof(scene, "#F3D3A2", new Point3D(0, 1.45, .38), 1.86, .42, .66);
            Roof(scene, "#7C9C89", new Point3D(0, 1.84, .24), 1.50, .18, .59);
            Box(scene, "#DEAC7C", new Point3D(0, 1.42, .613), 1.96, .13, .13);
            for (int arch = -1; arch <= 1; arch++)
            {
                double x = arch * .43;
                Arch(scene, "#AD8764", x, .46, .603, .33, .73);
                Arch(scene, "#5C7266", x, .49, .608, .24, .64);
            }
            for (int i = 0; i < 4; i++)
            {
                double x = (i - 1.5) * .43;
                Box(scene, "#F1D5AD", new Point3D(x, .91, .683), .096, .97, .16);
                Box(scene, "#C7A77F", new Point3D(x, 1.33, .69), .154, .10, .18);
            }
            for (int i = 0; i < 3; i++)
                Box(scene, "#BD9D7E", new Point3D(0, .25 + i * .058, .83 - i * .10), 1.64, .061, .23);
            foreach (int side in new[] { -1, 1 })
                Oval(scene, "#80A072", new Point3D(1.67 * side, .39, .57), .21, .16, .19, 0);
        }

        private static void Roof(Model3DGroup scene, string tint, Point3D origin, double width, double height, double depth)
        {
            var mesh = new MeshGeometry3D();
            var half = width / 2;
            var left = origin + new Vector3D(-half, 0, depth / 2);
            var right = origin + new Vector3D(half, 0, depth / 2);
            var crest = origin + new Vector3D(0, height, depth / 2);
            var back = new Vector3D(0, 0, -depth);
            Quad(mesh, left, right, crest, crest);
            Quad(mesh, right + back, left + back, crest + back, crest + back);
            Quad(mesh, left, crest, crest + back, left + back);
            Quad(mesh, crest, right, right + back, crest + back);
            Quad(mesh, right, left, left + back, right + back);
            Model(scene, mesh, tint);
        }

        private static void Arch(Model3DGroup scene, string tint, double x, double y, double z, double width, double height)
        {
            var mesh = new MeshGeometry3D();
            double radius = width / 2;
            double spring = y + height - radius;
            Quad(mesh, new Point3D(x - radius, y, z), new Point3D(x + radius, y, z),
                new Point3D(x + radius, spring, z), new Point3D(x - radius, spring, z));
            var center = new Point3D(x, spring, z);
            for (int i = 0; i < 10; i++)
            {
                double a = i * Math.PI / 10, b = (i + 1) * Math.PI / 10;
                Quad(mesh, center, center + new Vector3D(radius * Math.Cos(a), radius * Math.Sin(a), 0),
                    center + new Vector3D(radius * Math.Cos(b), radius * Math.Sin(b), 0), center);
            }
            Model(scene, mesh, tint);
        }

        private static void BuildMidAutumn(Rig rig)
        {
            var scene = rig.Model;
            var moon = new Model3DGroup();
            Oval(moon, "#F4D691", new Point3D(.69, 1.42, -.50), .62, .62, .26, 0);
            Oval(moon, "#CEA572", new Point3D(.54, 1.50, -.27), .17, .12, .028, -22);
            Oval(moon, "#DBB37D", new Point3D(.82, 1.26, -.26), .12, .085, .025, 24);
            var rise = new TranslateTransform3D();
            moon.Transform = rise;
            rig.Animate(phase => rise.OffsetY = .09 * Math.Sin(phase * 2 * Math.PI));
            scene.Children.Add(moon);
            var lanternModel = new Model3DGroup();
            var lantern = new Point3D(-.73, .45, .25);
            var heights = new[] { 0, .07, .22, .45, .68, .85, .93 };
            var radii = new[] { .22, .30, .42, .48, .43, .30, .20 };
            Lathe(lanternModel, "#B85462", lantern, .81, radii, heights, Finish.Lacquer);
            for (int rib = 0; rib < 12; rib++)
            {
                double angle = rib * 2 * Math.PI / 12;
                for (int i = 1; i < radii.Length; i++)
                {
                    var a = lantern + new Vector3D(radii[i - 1] * Math.Cos(angle), heights[i - 1],
                        radii[i - 1] * Math.Sin(angle) * .81);
                    var b = lantern + new Vector3D(radii[i] * Math.Cos(angle), heights[i],
                        radii[i] * Math.Sin(angle) * .81);
                    Tube(lanternModel, "#F0B278", a, b, .018, .016, Finish.Brass);
                }
            }
            Lathe(lanternModel, "#E6B777", lantern, .81, new[] { 0, .24, .23, 0 }, new[] { -.02, -.02, .061, .061 }, Finish.Brass);
            Lathe(lanternModel, "#DBA46C", lantern, .81, new[] { 0, .24, .22, 0 }, new[] { .878, .878, .965, .965 }, Finish.Brass);
            Tube(lanternModel, "#F2BA78", new Point3D(-.73, .43, .25), new Point3D(-.70, .24, .25), .019, .045);
            var pole = new Point3D(-1.58, .24, -.15);
            Tube(scene, "#916552", pole, pole + new Vector3D(.05, 1.59, 0), .047, .043);
            Tube(scene, "#BE8C64", pole + new Vector3D(.05, 1.59, 0), new Point3D(-.67, 1.75, .21), .044, .026);
            var hook = new Point3D(-.71, 1.75, .22);
            Tube(lanternModel, "#DDB772", hook, new Point3D(-.73, 1.38, .25), .022, .020);
            var swing = Pivot(lanternModel, new Vector3D(.1, .15, 1), hook);
            rig.Animate(phase => swing.Angle = 12 * Math.Sin(phase * 4 * Math.PI));
            scene.Children.Add(lanternModel);
            Mooncake(scene, new Point3D(.72, .24, .51), .40);
            Mooncake(scene, new Point3D(1.42, .24, .31), .33);
        }

        private static void Mooncake(Model3DGroup scene, Point3D point, double radius)
        {
            Revolve(scene, "#BB7445", new[] { 0, radius * .87, radius, radius, radius * .86, 0 },
                new[] { 0, 0, .06, .20, .25, .25 }, 28, (x, y, z) => point + new Vector3D(x, y, z));
            for (int mark = 0; mark < 10; mark++)
            {
                double angle = mark * 2 * Math.PI / 10;
                Oval(scene, "#EDB575", point + new Vector3D(Math.Cos(angle) * radius * .57, .251,
                    Math.Sin(angle) * radius * .57), .047, .014, .042, 0);
                var side = new Vector3D(Math.Cos(angle) * radius, 0, Math.Sin(angle) * radius);
                Tube(scene, "#DB9963", point + side + new Vector3D(0, .073, 0),
                    point + side + new Vector3D(0, .20, 0), .025, .026);
            }
        }

        private static void BuildChristmas(Rig rig)
        {
            var scene = rig.Model;
            var pine = new Model3DGroup();
            var tree = new Point3D(-.39, .23, -.04);
            ContactShadow(scene, new Point3D(-.31, .237, .02), 2.38, 1.06);
            Lathe(pine, "#836452", tree, 1, new[] { 0, .14, .13, 0 }, new[] { 0, 0, .46, .46 });
            for (int tier = 0; tier < 3; tier++)
            {
                double baseY = .34 + tier * .39, width = .82 - tier * .20;
                Lathe(pine, tier == 1 ? "#62947A" : "#4C8367", tree, 1,
                    new[] { 0, width, width * .87, width * .51, .03 }, new[] { baseY, baseY, baseY + .12, baseY + .40, baseY + .74 });
                for (int ball = 0; ball < 7; ball++)
                {
                    double angle = (ball + tier * .44) * 2 * Math.PI / 7;
                    Oval(pine, ball % 3 == 0 ? "#E9B872" : ball % 3 == 1 ? "#DB817D" : "#DFE6D0",
                        tree + new Vector3D(Math.Cos(angle) * width * .88, baseY + .13, Math.Sin(angle) * width * .88),
                        .066, .068, .065, 0, ball % 3 == 0 ? Finish.Brass : Finish.Lacquer);
                }
            }
            var turn = Pivot(pine, new Vector3D(0, 1, 0), tree);
            rig.Animate(phase => turn.Angle = 30 * Math.Sin(phase * 2 * Math.PI));
            scene.Children.Add(pine);
            Burst(rig, new Point3D(-.39, 1.98, .02), .20, "#F4BD66", "#FFE7A2", .23);
            Gift(scene, new Point3D(.76, .391, .40), "#B95D58", .61);
            Gift(scene, new Point3D(1.39, .363, .17), "#527992", .46);
            Gift(scene, new Point3D(-1.38, .344, .57), "#C79666", .44);
        }

        private static void Gift(Model3DGroup scene, Point3D position, string color, double size)
        {
            Box(scene, color, position, size, size * .56, size);
            Box(scene, "#EACEA3", position + new Vector3D(0, .005, 0), size * .16, size * .59, size + .014);
            Box(scene, "#EACEA3", position + new Vector3D(0, .01, 0), size + .016, size * .59, size * .14);
            Oval(scene, "#F1D3A8", position + new Vector3D(0, size * .31, 0), size * .19, size * .047, size * .086, 34);
        }

        private static RotateTransform3D Turn(double x, double y, double z, double angle)
        {
            return new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(x, y, z), angle));
        }

        private static DiffuseMaterial Print(double width, double height, Action<DrawingContext> draw)
        {
            var brush = new DrawingBrush(HolidayScene.DrawScene(draw))
            {
                ViewboxUnits = BrushMappingMode.Absolute,
                Viewbox = new Rect(0, 0, width, height),
                Stretch = Stretch.Fill
            };
            return new DiffuseMaterial(brush);
        }

        private static Geometry Star(Point center, double outside, double inside, int points)
        {
            var star = new StreamGeometry();
            using (var path = star.Open())
            {
                for (int point = 0; point < points * 2; point++)
                {
                    double angle = point * Math.PI / points - Math.PI / 2;
                    double radius = point % 2 == 0 ? outside : inside;
                    var location = new Point(center.X + Math.Cos(angle) * radius, center.Y + Math.Sin(angle) * radius);
                    if (point == 0) path.BeginFigure(location, true, true);
                    else path.LineTo(location, true, false);
                }
            }
            star.Freeze();
            return star;
        }
    }
}
