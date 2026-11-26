using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace NghiLe
{
    internal static partial class HolidayDiorama
    {
        private static void BuildTet(Rig rig, bool apricot)
        {
            var scene = rig.Model;
            ContactShadow(scene, new Point3D(-.63, .237, .19), 2.63, 1.16);
            ContactShadow(scene, new Point3D(1.16, .238, .19), 1.02, .75);
            BuildCalendar(scene);
            BuildVase(rig, apricot);
            if (apricot) BuildRiceRolls(scene);
            else
            {
                BuildSquareCake(scene, new Point3D(-1.51, .32, .64), -.25, .71);
                BuildSquareCake(scene, new Point3D(-1.44, .58, .59), .09, .58);
            }
        }

        private static void BuildCalendar(Model3DGroup scene)
        {
            var board = new Model3DGroup();
            Box(board, "#512D35", new Point3D(0, -.025, -.13), 1.69, 1.59, .19);
            Box(board, "#D8A981", new Point3D(.025, -.03, -.033), 1.58, 1.49, .075);
            Box(board, "#F3D6A7", new Point3D(0, 0, .017), 1.55, 1.48, .077);
            var print = Print(160, 150, drawing =>
            {
                var paper = new LinearGradientBrush(Shade("#FFF0CE"), Shade("#E6BB89"), 63);
                drawing.DrawRectangle(paper, null, new Rect(0, 0, 160, 150));
                var rule = new Pen(Ink("#BA7460"), 1);
                drawing.DrawRectangle(null, rule, new Rect(8, 8, 144, 134));
                drawing.DrawRectangle(null, new Pen(Ink("#D39777"), .7), new Rect(12, 12, 136, 126));
                PaperText(drawing, "Tết", 76, 28, new Typeface("Cambria"), "#862E41");
                PaperText(drawing, "NGUYÊN ĐÁN", 13, 112, new Typeface("Segoe UI"), "#7B3B41");
                drawing.DrawLine(rule, new Point(46, 111), new Point(114, 111));
                drawing.DrawLine(new Pen(Ink("#E9BD8F"), .75), new Point(18, 135), new Point(142, 135));
            });
            var face = new MeshGeometry3D();
            Quad(face, new Point3D(-.774, -.739, .057), new Point3D(.774, -.739, .057),
                new Point3D(.774, .739, .057), new Point3D(-.774, .739, .057));
            board.Children.Add(new GeometryModel3D(face, print));
            foreach (double pin in new[] { -.44, .44 })
            {
                Tube(board, "#BA8954", new Point3D(pin, .562, .059), new Point3D(pin, .562, .088),
                    .044, .038, Finish.Brass);
                Tube(board, "#81573A", new Point3D(pin, .562, .089), new Point3D(pin, .562, .094), .018, .018);
            }

            var pose = new Transform3DGroup();
            pose.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), -8)));
            pose.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), -9)));
            pose.Children.Add(new TranslateTransform3D(-.53, 1.0, .13));
            board.Transform = pose;
            scene.Children.Add(board);
            Box(scene, "#815048", new Point3D(-.53, .30, .105), 1.97, .16, .62);
        }

        private static void PaperText(DrawingContext drawing, string text, double size, double y, Typeface font, string ink)
        {
            var label = new FormattedText(text, System.Globalization.CultureInfo.GetCultureInfo("vi-VN"),
                FlowDirection.LeftToRight, font, size, Ink(ink), 1.0);
            drawing.DrawText(label, new Point((160 - label.Width) / 2, y));
        }

        private static void BuildVase(Rig rig, bool apricot)
        {
            var scene = rig.Model;
            var vase = new Point3D(1.12, .23, .17);
            Lathe(scene, apricot ? "#AD5A38" : "#A44A60", vase, .87,
                new[] { 0, .22, .31, .33, .31, .22, .148, .151, .161, .118, .10, 0 },
                new[] { 0, 0, .13, .27, .39, .48, .60, .65, .68, .68, .56, .51 }, Finish.Lacquer);
            Lathe(scene, "#DFAD72", vase, .87, new[] { .151, .163 }, new[] { .650, .678 }, Finish.Brass);
            Lathe(scene, "#A5785D", vase, .87, new[] { .229, .241 }, new[] { .008, .026 }, Finish.Brass);

            var branchModel = new Model3DGroup();
            var trunk = new Point3D(1.12, .86, .17);
            var fork = new Point3D(1.35, 1.24, .14);
            var crown = new Point3D(1.64, 1.68, .08);
            var tips = new[]
            {
                new[] { trunk, fork, crown, new Point3D(1.94, 2.06, -.06) },
                new[] { crown, new Point3D(1.86, 1.77, .14), new Point3D(2.09, 1.87, .28) },
                new[] { fork, new Point3D(1.12, 1.52, .17), new Point3D(.93, 1.82, .13) },
                new[] { crown, new Point3D(1.50, 1.96, .03), new Point3D(1.34, 2.19, -.12) },
                new[] { fork, new Point3D(1.59, 1.31, .35), new Point3D(1.88, 1.49, .42) }
            };

            for (int branch = 0; branch < tips.Length; branch++)
            {
                var limb = tips[branch];
                for (int step = 1; step < limb.Length; step++)
                {
                    Tube(branchModel, "#8C5845", limb[step - 1], limb[step],
                        branch == 0 ? .046 - step * .007 : .023, .012);
                    if (branch == 0 && step == 1) continue;
                    var flower = limb[step] + new Vector3D(step % 2 == 0 ? -.034 : .067, .032, .076);
                    Blossom(branchModel, flower, step == limb.Length - 1 ? .117 : .091, branch + step, apricot);
                }
            }

            Oval(branchModel, "#899056", new Point3D(1.10, 1.57, .22), .115, .038, .032, 32);
            Oval(branchModel, "#78894E", new Point3D(1.90, 1.72, .23), .12, .040, .025, 35);
            var sway = Pivot(branchModel, new Vector3D(.25, .08, 1), trunk);
            rig.Animate(phase => sway.Angle = 10 * Math.Sin(phase * 4 * Math.PI) + 2 * Math.Sin(phase * 6 * Math.PI));
            scene.Children.Add(branchModel);
        }

        private static void Blossom(Model3DGroup scene, Point3D point, double radius, int seed, bool apricot)
        {
            for (int index = 0; index < 5; index++)
            {
                var turn = index * 72 + seed * 32;
                double angle = turn * Math.PI / 180;
                var center = point + new Vector3D(Math.Cos(angle) * radius * .57, Math.Sin(angle) * radius * .57, 0);
                string tint = apricot ? (index % 2 == 0 ? "#FFEC98" : "#F7C451") :
                    (index % 2 == 0 ? "#F8CBD1" : "#E58CA9");
                Oval(scene, tint, center, radius * .57, radius * .37, radius * .24, turn);
            }
            Oval(scene, apricot ? "#DF8E40" : "#C45A78", point + new Vector3D(0, 0, radius * .22),
                radius * .29, radius * .28, radius * .16, 0);
        }

        private static void BuildSquareCake(Model3DGroup scene, Point3D point, double yaw, double size)
        {
            var cake = new Model3DGroup();
            Box(cake, "#6C7545", new Point3D(), size, .23, size);
            Box(cake, "#95A160", new Point3D(0, .12, 0), size * .95, .012, size * .96);
            foreach (double band in new[] { -.22, .22 })
            {
                Box(cake, "#E1C391", new Point3D(band * size, 0, 0), .041, .254, size + .016);
                Box(cake, "#E1C391", new Point3D(0, .004, band * size), size + .016, .256, .041);
            }
            var pose = new Transform3DGroup();
            pose.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), yaw * 180 / Math.PI)));
            pose.Children.Add(new TranslateTransform3D(point.X, point.Y, point.Z));
            cake.Transform = pose;
            scene.Children.Add(cake);
        }

        private static void BuildRiceRolls(Model3DGroup scene)
        {
            Roll(scene, new Point3D(-1.12, .41, .83), new Vector3D(-.99, .02, -.37));
            Roll(scene, new Point3D(-1.03, .39, .42), new Vector3D(-.93, .01, -.24));
            Roll(scene, new Point3D(-1.20, .68, .73), new Vector3D(-.76, -.03, -.27));
        }

        private static void Roll(Model3DGroup scene, Point3D front, Vector3D length)
        {
            Tube(scene, "#7F8549", front + length, front, .135, .141);
            foreach (var band in new[] { .20, .47, .74 })
                Tube(scene, "#DBBE82", front + length * (band + .024), front + length * band, .144, .145);
            var face = front + (front - (front + length)) * .005;
            Tube(scene, "#DCC58D", front, face, .135, .135);
            Tube(scene, "#D9A95E", face, face - length * .006, .057, .057);
        }
    }
}
