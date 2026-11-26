using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NghiLe
{
    // Value 2 belonged to a retired pet in older settings.
    public enum PetAnimal { Cat = 0, Dog = 1, HacLong = 3 }

    internal sealed class PetArtwork : FrameworkElement
    {
        private const double CanvasWidth = 36;
        private const double CanvasHeight = 30;
        private static readonly Brush Outline = Paint("#282536");
        private static readonly Brush Shadow = Paint("#69999AAE");
        private static readonly Brush DogCoat = Tint("#FFD092", "#CE7F44");
        private static readonly Brush DogEar = Tint("#B5754F", "#8E4A37");
        private static readonly Brush Cream = Tint("#FFF5D9", "#FFDAA8");
        private static readonly Brush CatCoat = Tint("#C4CFE9", "#8294BA");
        private static readonly Brush CatShade = Paint("#66799F");
        private static readonly Brush CatCream = Tint("#FFFFFF", "#D7DDF4");
        private static readonly Brush Pink = Paint("#F2AAB6");
        private static readonly Brush Collar = Tint("#DD7776", "#B04652");
        private static readonly Brush CollarTag = Paint("#FFD28D");
        private static readonly Brush Glint = Paint("#FFFDF7");
        private static readonly Brush CatEye = Paint("#BFE6A6");
        private static readonly Pen Edge = Stroke(Outline, 1.05);
        private static readonly Pen Fine = Stroke(Outline, .48);
        private static readonly Pen Fur = Stroke(Paint("#B48361"), .6);
        private static readonly Pen Stripe = Stroke(CatShade, 1.05);
        private static readonly Pen TailEdge = Stroke(Outline, 4.7);
        private static readonly Pen CatTail = Stroke(CatShade, 3.25);
        private static readonly Pen DogTail = Stroke(DogCoat, 3.25);
        private static readonly DrawingGroup[][] Poses = { Frames(false), Frames(true) };
        private static readonly DrawingGroup[][] Carried = { CarryFrames(false), CarryFrames(true) };
        private static readonly DrawingGroup[][] DogActions =
        {
            ActionFrames(PetAction.Wag, 24), ActionFrames(PetAction.Sniff, 30),
            ActionFrames(PetAction.Stretch, 28), ActionFrames(PetAction.Hop, 14), ActionFrames(PetAction.Nap, 48)
        };

        private static readonly Lazy<DrawingGroup[]> CatNuzzle = new Lazy<DrawingGroup[]>(CreateNuzzleFrames);

        private static readonly Lazy<DrawingGroup[]> HacLongWalk = new Lazy<DrawingGroup[]>(LoadHacLongWalk);

        private DrawingGroup drawing = Poses[0][0];
        private double canvasHeight = CanvasHeight;
        private bool mirrorFrame;

        internal bool GroundAtBottom { get; set; }

        internal void SetPose(PetAnimal nextAnimal, bool moving = false, int frame = 0, bool left = false, bool lifted = false)
        {
            if (nextAnimal == PetAnimal.HacLong)
            {
                SetDrawing(HacLongWalk.Value[0], left, CanvasWidth);
                return;
            }
            int nextPose = lifted ? 5 : moving ? 1 + frame % 4 : 0;
            SetDrawing(Poses[(int)nextAnimal][nextPose], left);
        }

        internal void SetMotion(PetAnimal animal, PetMotion motion, bool left)
        {
            if (animal == PetAnimal.HacLong)
            {
                // The original walk repeats an 80 ms, 40 ms, 40 ms cadence.
                int beat = (int)(motion.Progress * 20);
                int frame = (beat / 4) * 3 + Math.Max(0, beat % 4 - 1);
                SetDrawing(HacLongWalk.Value[frame], left, CanvasWidth);
                return;
            }
            if (animal != PetAnimal.Dog || motion.Action <= PetAction.Walk)
            {
                SetPose(animal, motion.Action == PetAction.Walk, (int)(motion.Progress * 40), left);
                return;
            }
            var frames = DogActions[(int)motion.Action - (int)PetAction.Wag];
            SetDrawing(frames[Math.Min(frames.Length - 1, (int)(motion.Progress * frames.Length))], left);
        }

        internal void SetPettingPose(PetAnimal animal, double progress, bool left)
        {
            if (animal == PetAnimal.HacLong)
            {
                SetPose(animal, left: left);
                return;
            }
            var frames = animal == PetAnimal.Dog ? DogActions[0] : CatNuzzle.Value;
            SetDrawing(frames[Math.Min(frames.Length - 1, (int)(progress * frames.Length))], left);
        }

        private static DrawingGroup[] CreateNuzzleFrames()
        {
            var frames = new DrawingGroup[18];
            for (int index = 0; index < frames.Length; index++)
            {
                var frame = new DrawingGroup();
                double phase = index / (double)(frames.Length - 1);
                using (var context = frame.Open())
                {
                    context.DrawEllipse(Shadow, null, new Point(18, 28.8), 14.3, 1);
                    DrawPet(context, false, 0, nuzzle: Math.Sin(Math.PI * phase) * Math.Sin(4 * Math.PI * phase), contented: true);
                }
                frame.Freeze();
                frames[index] = frame;
            }
            return frames;
        }

        internal void SetCarryPose(PetAnimal animal, double seconds, bool left)
        {
            if (animal == PetAnimal.HacLong)
            {
                SetPose(animal, left: left);
                return;
            }
            var frames = Carried[(int)animal];
            // The first frame pulls the paws inward before the alternating kicks.
            int index = seconds < .12 ? 0 : 1 + (int)((seconds - .12) * 12) % (frames.Length - 1);
            SetDrawing(frames[index], left);
        }

        private static DrawingGroup[] LoadHacLongWalk()
        {
            using (var stream = typeof(PetArtwork).Assembly.GetManifestResourceStream("NghiLe.HacLongWalk"))
            {
                var atlas = BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                const int cell = 256;
                const int columns = 5;
                var frames = new DrawingGroup[15];
                for (int index = 0; index < frames.Length; index++)
                {
                    var region = new Int32Rect(index % columns * cell, index / columns * cell, cell, cell);
                    var image = new CroppedBitmap(atlas, region);
                    var frame = new DrawingGroup();
                    using (var context = frame.Open())
                        context.DrawImage(image, new Rect(0, 0, CanvasWidth, CanvasWidth));
                    frame.Freeze();
                    frames[index] = frame;
                }
                return frames;
            }
        }

        private void SetDrawing(DrawingGroup frame, bool mirror, double height = CanvasHeight)
        {
            if (drawing == frame && mirrorFrame == mirror) return;
            drawing = frame;
            mirrorFrame = mirror;
            canvasHeight = height;
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext context)
        {
            double scale = Math.Min(RenderSize.Width / CanvasWidth, RenderSize.Height / canvasHeight);
            if (scale <= 0) return;
            double spareHeight = RenderSize.Height - canvasHeight * scale;
            context.PushTransform(new TranslateTransform((RenderSize.Width - CanvasWidth * scale) / 2,
                GroundAtBottom ? spareHeight : spareHeight / 2));
            context.PushTransform(new ScaleTransform(scale, scale));
            if (mirrorFrame) context.PushTransform(new ScaleTransform(-1, 1, CanvasWidth / 2, 0));
            context.DrawDrawing(drawing);
            if (mirrorFrame) context.Pop();
            context.Pop();
            context.Pop();
        }

        private static DrawingGroup[] Frames(bool dog)
        {
            var frames = new DrawingGroup[6];
            for (int index = 0; index < frames.Length; index++)
            {
                var drawing = new DrawingGroup();
                using (var context = drawing.Open())
                {
                    if (index != 5) context.DrawEllipse(Shadow, null, new Point(18, 28.8), 14.3, 1);
                    if (index == 0 && !dog) SleepingCat(context);
                    else DrawPet(context, dog, index);
                }
                drawing.Freeze();
                frames[index] = drawing;
            }
            return frames;
        }

        private static DrawingGroup[] ActionFrames(PetAction action, int count)
        {
            var frames = new DrawingGroup[count];
            for (int index = 0; index < count; index++)
            {
                double phase = (double)index / (count - 1);
                var frame = new DrawingGroup();
                using (var context = frame.Open())
                {
                    context.DrawEllipse(Shadow, null, new Point(18, 28.8), 14.3, 1);
                    DrawDogAction(context, action, phase);
                }
                frame.Freeze();
                frames[index] = frame;
            }
            return frames;
        }

        private static DrawingGroup[] CarryFrames(bool dog)
        {
            var frames = new DrawingGroup[9];
            for (int index = 0; index < frames.Length; index++)
            {
                double phase = (index - 1) * 2 * Math.PI / (frames.Length - 1);
                double paddle = index == 0 ? 0 : Math.Sin(phase);
                var frame = new DrawingGroup();
                using (var context = frame.Open())
                    DrawPet(context, dog, 5, wag: paddle * 1.7, paddle: paddle, tuck: index == 0 ? 2.4 : .4);
                frame.Freeze();
                frames[index] = frame;
            }
            return frames;
        }

        private static void DrawDogAction(DrawingContext context, PetAction action, double phase)
        {
            if (action == PetAction.Wag)
            {
                DrawPet(context, true, 0, 2.8 * Math.Sin(8 * Math.PI * phase));
                return;
            }
            if (action == PetAction.Hop)
            {
                double height = 4.4 * Math.Sin(Math.PI * phase);
                context.PushTransform(new TranslateTransform(0, -height));
                DrawPet(context, true, height > 1 ? 5 : 0);
                context.Pop();
                return;
            }

            double lower = Math.Min(1, Math.Sin(Math.PI * phase) * 3);
            if (lower < .15)
            {
                DrawPet(context, true, 0);
                return;
            }
            bool asleep = action == PetAction.Nap;
            bool stretch = action == PetAction.Stretch;
            double breath = Math.Sin((asleep ? 6 : 9) * Math.PI * phase);
            DrawDogLow(context, lower, breath, stretch, asleep);
        }

        private static void DrawDogLow(DrawingContext context, double lower, double breath, bool stretch, bool asleep)
        {
            double back = stretch ? 16.8 : asleep ? 22.3 : 19.1;
            var tail = new[] { new Point(4.2, back + 3), new Point(2.9, back + 1),
                new Point(5.7, back + (stretch ? -6 : 1)) };
            Curve(context, TailEdge, new Point(11, back + 2), tail);
            Curve(context, DogTail, new Point(11, back + 2), tail);
            context.DrawEllipse(DogEar, Edge, new Point(10.4, 25.9), 4.3, 2.1);
            context.DrawEllipse(DogCoat, Edge, new Point(14.7, back), 9.3, 5.7 + (asleep ? breath * .24 : 0));
            context.DrawEllipse(Cream, null, new Point(18, 23.4), 5.4, 3.0);
            var shoulder = new Point(20.4, stretch ? 22.4 : 23.9);
            var paw = new Point(stretch ? 30.3 : 25.3, 27.1);
            context.DrawLine(Stroke(Outline, 3.9), shoulder, paw);
            context.DrawLine(Stroke(Cream, 2.45), shoulder, paw);
            context.DrawEllipse(Cream, Edge, paw, 2.75, 1.48);
            context.DrawLine(Fine, new Point(paw.X + .7, 27.1), new Point(paw.X + .7, 28));

            double headY = 15 + lower * (asleep ? 7.4 : 6.9) + (asleep ? 0 : breath * .52);
            context.DrawEllipse(DogCoat, Edge, new Point(24.7, headY), 7.1, 5.5);
            context.DrawEllipse(DogEar, Edge, new Point(20.7, headY + .7), 3.1, 4.6);
            context.DrawEllipse(Cream, Fine, new Point(29.9, headY + 2), 4.05, 2.5);
            context.DrawEllipse(Outline, null, new Point(32.8, headY + 1.65), 1.31, .91);
            context.DrawRoundedRectangle(Collar, Edge, new Rect(19, headY + 2.2, 4.6, 1.6), .8, .8);
            context.DrawEllipse(CollarTag, Fine, new Point(22.6, headY + 4.2), .82, .92);
            if (asleep || stretch)
                Curve(context, Fine, new Point(26.1, headY + .1), new Point(27, headY + .85),
                    new Point(27.8, headY + .65), new Point(28.2, headY));
            else
            {
                context.DrawEllipse(Outline, null, new Point(27, headY - .05), 1.05, 1.2);
                context.DrawEllipse(Glint, null, new Point(26.75, headY - .46), .35, .34);
            }
        }

        private static void SleepingCat(DrawingContext context)
        {
            context.DrawEllipse(CatCoat, Edge, new Point(14.7, 21.3), 10.9, 7);
            context.DrawEllipse(CatCream, null, new Point(17, 24.6), 6.2, 2.8);
            Curve(context, Stripe, new Point(9, 16), new Point(8.5, 18), new Point(8.7, 19.9), new Point(9.6, 21.2));
            Curve(context, Stripe, new Point(12.3, 15), new Point(11.7, 17.3), new Point(11.9, 18.9), new Point(12.6, 20));
            Shape(context, CatCoat, Edge, new Point(18, 19), new Point(18.6, 10.4), new Point(24, 15),
                new Point(28, 15), new Point(31.8, 10.9), new Point(32.2, 20));
            Shape(context, Pink, null, new Point(19.6, 17), new Point(19.7, 13), new Point(23.2, 16.2));
            Shape(context, Pink, null, new Point(28.8, 16.5), new Point(31, 13), new Point(31.4, 17.1));
            context.DrawEllipse(CatCoat, Edge, new Point(25.2, 21.1), 7.9, 5.7);
            context.DrawEllipse(CatCream, null, new Point(27.8, 22.9), 4, 2.65);
            Curve(context, Fine, new Point(22.2, 20.2), new Point(23.1, 21.3), new Point(24.5, 21.3), new Point(25.5, 20.3));
            context.DrawEllipse(Pink, Fine, new Point(31, 22), 1.15, .73);
            context.DrawLine(Fine, new Point(28.9, 22.1), new Point(25.5, 21.7));
            context.DrawLine(Fine, new Point(28.8, 23), new Point(25.4, 23.5));
            Curve(context, TailEdge, new Point(5.8, 22.4), new Point(6.6, 30), new Point(20, 28.1), new Point(22.1, 26));
            Curve(context, CatTail, new Point(5.8, 22.4), new Point(6.6, 30), new Point(20, 28.1), new Point(22.1, 26));
            context.DrawLine(Fine, new Point(11.1, 26.7), new Point(10.7, 27.9));
            context.DrawLine(Fine, new Point(14.1, 27.1), new Point(14, 28.3));
        }

        private static void DrawPet(DrawingContext context, bool dog, int pose, double wag = 0,
            double paddle = 0, double tuck = 0, double nuzzle = 0, bool contented = false)
        {
            bool lifted = pose == 5;
            bool moving = pose != 0 && !lifted;
            double bob = pose == 2 || pose == 4 ? -1 : 0;
            if (lifted) bob = -1.5 - Math.Abs(paddle) * .5;
            Brush coat = dog ? DogCoat : CatCoat;
            Brush shade = dog ? DogEar : CatShade;
            Brush cream = dog ? Cream : CatCream;

            double tailTip = pose == 1 || pose == 4 ? 1.5 : 0;
            if (dog)
            {
                var tail = new[] { new Point(4.6, 19 + bob), new Point(2.6, 12 + bob), new Point(7.6 + wag, 11.4 + bob + tailTip),
                    new Point(10.1 + wag, 11.7 + bob + tailTip), new Point(10.1 + wag, 14.8 + bob), new Point(8.1 + wag, 15 + bob) };
                Curve(context, TailEdge, new Point(11.2, 20 + bob), tail);
                Curve(context, DogTail, new Point(11.2, 20 + bob), tail);
                context.DrawEllipse(cream, null, new Point(8.25 + wag, 14.85 + bob), 1.3, 1.35);
            }
            else
            {
                var tail = new[] { new Point(5.4, 19.5 + bob), new Point(3.4, 15.4 + bob),
                    new Point(6.2 + wag, 11 + bob + tailTip + Math.Abs(wag)) };
                Curve(context, TailEdge, new Point(11.4, 21 + bob), tail);
                Curve(context, CatTail, new Point(11.4, 21 + bob), tail);
                context.DrawLine(Fine, new Point(5.1, 14.1 + bob), new Point(7, 14.5 + bob));
            }

            double shift = pose == 1 ? 3 : pose == 3 ? -3 : 0;
            if (moving || lifted)
            {
                Leg(context, shade, shade, 12.8, lifted ? -2 - paddle : -shift, 1.7 + tuck);
                Leg(context, shade, shade, 22, shift + 1 + paddle, 2.1 + tuck);
                context.DrawEllipse(coat, Edge, new Point(16, 20.3 + bob), 9.5, 5.1);
                context.DrawEllipse(cream, null, new Point(19.8, 22.3 + bob), 5.1, 2.2);
                Leg(context, coat, cream, 10.1, lifted ? -2.6 + paddle * 2 : shift,
                    lifted ? tuck + Math.Max(0, paddle) * 1.9 : 1.3);
                Leg(context, coat, cream, 20.4, lifted ? 2.5 - paddle * 2 : -shift,
                    lifted ? .4 + tuck + Math.Max(0, -paddle) * 1.9 : 0);
                if (!dog)
                {
                    Curve(context, Stripe, new Point(12.3, 16.5 + bob), new Point(12.6, 17.6 + bob),
                        new Point(12.6, 18.9 + bob), new Point(11.8, 19.6 + bob));
                    Curve(context, Stripe, new Point(15.4, 16 + bob), new Point(15.8, 17.5 + bob),
                        new Point(15.6, 18.3 + bob), new Point(15.2, 18.9 + bob));
                }
            }
            else
            {
                context.DrawEllipse(coat, Edge, new Point(15, 20.8), 8.3, 7.1);
                context.DrawEllipse(cream, null, new Point(20.3, 22.7), 3.4, 4.2);
                Curve(context, Fur, new Point(12.3, 20.4), new Point(10.1, 21.1), new Point(10, 23.9), new Point(12.7, 25));
                context.DrawEllipse(cream, Edge, new Point(22.1, 27.1), 4.5, 1.65);
                context.DrawLine(Fine, new Point(23.5, 27.4), new Point(23.5, 28.3));
                context.DrawLine(Fine, new Point(25, 27.2), new Point(25, 28.1));
            }

            if (contented) context.PushTransform(new RotateTransform(nuzzle * 17, 22, 19));
            if (!dog)
            {
                Shape(context, coat, Edge, new Point(17, 12 + bob), new Point(18, 4 + bob), new Point(23, 8.5 + bob),
                    new Point(27, 8.5 + bob), new Point(30, 4.5 + bob), new Point(31.5, 13 + bob));
                Shape(context, Pink, null, new Point(18.7, 10.2 + bob), new Point(19, 6.9 + bob), new Point(22, 9.2 + bob));
                Shape(context, Pink, null, new Point(27.5, 9.4 + bob), new Point(29.5, 6.8 + bob), new Point(30.4, 11 + bob));
            }

            context.DrawEllipse(coat, Edge, new Point(24.5, 14.2 + bob), 8.4, dog ? 7.5 : 6);
            if (dog)
            {
                context.PushTransform(new RotateTransform(17, 18.8, 12 + bob));
                context.DrawEllipse(shade, Edge, new Point(18.6, 12.8 + bob), 3.85, 5.9);
                Curve(context, Fur, new Point(17.6, 9 + bob), new Point(16.2, 11.4 + bob),
                    new Point(16.3, 14.6 + bob), new Point(17.2, 15.8 + bob));
                context.Pop();
                context.DrawRoundedRectangle(Collar, Edge, new Rect(19.8, 20 + bob, 7.5, 2.45), 1.1, 1.1);
                context.DrawEllipse(CollarTag, Fine, new Point(25, 23.1 + bob), 1.25, 1.28);
                context.DrawEllipse(Glint, null, new Point(24.6, 22.75 + bob), .32, .3);
                Curve(context, Fur, new Point(25.2, 10.6 + bob), new Point(26.1, 10 + bob),
                    new Point(27.2, 10 + bob), new Point(27.8, 10.5 + bob));
            }
            else
            {
                context.DrawLine(Stripe, new Point(22.2, 8.8 + bob), new Point(23.2, 11.1 + bob));
                context.DrawLine(Stripe, new Point(24.6, 8.6 + bob), new Point(24.9, 10.7 + bob));
                context.DrawLine(Stripe, new Point(27, 8.9 + bob), new Point(26.6, 10.7 + bob));
            }
            context.DrawEllipse(cream, dog ? Fine : null, new Point(29.4, 17.3 + bob), 4.75, 2.85);
            if (contented) Curve(context, Fine, new Point(25.6, 13.4), new Point(26.2, 12.5),
                new Point(27.5, 12.5), new Point(28.3, 13.4));
            else
            {
                context.DrawEllipse(dog ? Outline : CatEye, Fine, new Point(27, 13.2 + bob), lifted ? 1.57 : 1.35, 1.68);
                if (!dog) context.DrawEllipse(Outline, null, new Point(27.35, 13.2 + bob), .62, 1.15);
                context.DrawEllipse(Glint, null, new Point(26.65, 12.55 + bob), .46, .48);
            }
            context.DrawEllipse(dog ? Outline : Pink, Fine, new Point(32.8, 16.35 + bob), 1.42, .96);
            if (lifted) context.DrawEllipse(Outline, null, new Point(30.6, 19.1 + bob), .62, .76);
            else Curve(context, Fine, new Point(32.45, 17.16 + bob), new Point(32.1, 19.15 + bob),
                new Point(30.25, 19.75 + bob), new Point(29.2, 18.55 + bob));
            if (dog)
            {
                context.DrawEllipse(Fur.Brush, null, new Point(30, 16.8 + bob), .36, .37);
                context.DrawEllipse(Fur.Brush, null, new Point(28.7, 17.3 + bob), .34, .36);
            }
            else
            {
                context.DrawLine(Fine, new Point(29, 17.3 + bob), new Point(24.8, 16.4 + bob));
                context.DrawLine(Fine, new Point(29, 18.2 + bob), new Point(24.5, 18.7 + bob));
            }
            if (contented) context.Pop();
        }

        private static void Leg(DrawingContext context, Brush coat, Brush paw, double origin, double shift, double lift)
        {
            Shape(context, coat, Edge, new Point(origin - 1.5, 21.7), new Point(origin + 1, 21.2),
                new Point(origin + shift + 2.25, 27.1 - lift), new Point(origin + shift - .65, 27.3 - lift));
            context.DrawEllipse(paw, Edge, new Point(origin + shift + .9, 27.15 - lift), 1.75, 1.22);
            context.DrawLine(Fine, new Point(origin + shift + 1.6, 27.15 - lift),
                new Point(origin + shift + 1.66, 28.02 - lift));
        }

        private static void Shape(DrawingContext context, Brush color, Pen border, params Point[] points)
        {
            var shape = new StreamGeometry();
            using (var line = shape.Open())
            {
                line.BeginFigure(points[0], true, true);
                for (int index = 1; index < points.Length; index++) line.LineTo(points[index], true, false);
            }
            shape.Freeze();
            context.DrawGeometry(color, border, shape);
        }

        private static Brush Paint(string color)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
            brush.Freeze();
            return brush;
        }

        private static Brush Tint(string light, string dark)
        {
            var brush = new LinearGradientBrush((Color)ColorConverter.ConvertFromString(light),
                (Color)ColorConverter.ConvertFromString(dark), new Point(.18, 0), new Point(.8, 1));
            brush.Freeze();
            return brush;
        }

        private static void Curve(DrawingContext context, Pen pen, Point start, params Point[] bends)
        {
            var shape = new StreamGeometry();
            using (var line = shape.Open())
            {
                line.BeginFigure(start, false, false);
                for (int index = 0; index < bends.Length; index += 3)
                    line.BezierTo(bends[index], bends[index + 1], bends[index + 2], true, false);
            }
            shape.Freeze();
            context.DrawGeometry(null, pen, shape);
        }

        private static Pen Stroke(Brush brush, double size)
        {
            var pen = new Pen(brush, size) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round };
            pen.Freeze();
            return pen;
        }
    }
}
