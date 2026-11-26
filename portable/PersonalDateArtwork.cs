using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NghiLe
{
    internal sealed partial class HolidayScene
    {
        private static readonly Lazy<BitmapSource> BirthdayParty = new Lazy<BitmapSource>(() =>
        {
            using (var stream = typeof(HolidayScene).Assembly.GetManifestResourceStream("NghiLe.Birthday"))
            {
                var image = BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                image.Freeze();
                return image;
            }
        });
        private static readonly Brush CandleGlow = Glow("#C8FFB35D", "#00FFB35D");
        private static readonly Brush PartyPink = Paint("#F3A2B7");
        private static readonly Brush PartyLilac = Paint("#C9ACED");
        private static readonly Lazy<DrawingGroup> SpecialDayGarland = DeferScene(DrawSpecialDayGarland);

        private void DrawPersonalArtwork(DrawingContext drawing, bool birthday, double phase)
        {
            bool wideWidget = horizontal && !preview;
            double width = preview ? Math.Min(ActualWidth - 14, (ActualHeight - 14) * 2.08) :
                wideWidget ? 110 : pinsVisible ? (celebrating ? 158 : compact ? 128 : 138) :
                compact && !celebrating ? 150 : celebrating ? 182 : 174;
            if (width <= 0) return;
            double x = wideWidget ? 7 : (ActualWidth - width) / 2;
            double y = preview ? (ActualHeight - width / 2.08) / 2 : celebrating && !wideWidget ? 16 : 6;
            var art = new Rect(x, y, width, width / 2.08);

            if (birthday)
            {
                var flame = new Point(art.X + art.Width * .498, art.Y + art.Height * .33);
                double pulse = moving
                    ? .58 + .19 * Math.Sin(phase * 26 * Math.PI) + .12 * Math.Sin(phase * 42 * Math.PI)
                    : .54;
                drawing.PushOpacity(pulse);
                drawing.DrawEllipse(CandleGlow, null, flame, art.Width * .34, art.Height * .53);
                drawing.Pop();
                drawing.DrawImage(BirthdayParty.Value, art);
                drawing.PushOpacity(.38 * pulse);
                drawing.DrawEllipse(CandleGlow, null, flame, art.Width * .09, art.Height * .19);
                drawing.Pop();
            }
            else
            {
                drawing.PushTransform(new TranslateTransform(x, y));
                drawing.PushTransform(new ScaleTransform(width / 166, width / 166));
                drawing.DrawDrawing(SpecialDayGarland.Value);
                drawing.Pop();
                drawing.Pop();
            }

            if (moving) DrawPartyConfetti(drawing, art, phase);
            if (!wideWidget && !preview) DrawPartyStars(drawing, art, phase);
        }

        private void DrawPartyConfetti(DrawingContext drawing, Rect art, double phase)
        {
            int count = celebrating || preview ? 12 : 7;
            for (int i = 0; i < count; i++)
            {
                double progress = (phase * (i % 3 == 0 ? 2 : 1) + i * .618) % 1;
                double opacity = .77 * Math.Sin(progress * Math.PI);
                double side = i % 2 == 0 ? .11 : .89;
                double drift = .10 * Math.Sin((progress + i * .27) * Math.PI * 2);
                var point = new Point(art.X + art.Width * (side + drift),
                    art.Y + art.Height * (.12 + progress * .94));
                var brush = i % 3 == 0 ? GoldSpark : i % 3 == 1 ? PartyPink : PartyLilac;
                drawing.PushOpacity(opacity);
                drawing.PushTransform(new RotateTransform(i * 34 + progress * 180, point.X, point.Y));
                drawing.DrawRoundedRectangle(brush, null, new Rect(point.X - 1.2, point.Y - 1.7, 2.4, 3.4), .55, .55);
                drawing.Pop();
                drawing.Pop();
            }
        }

        private void DrawPartyStars(DrawingContext drawing, Rect art, double phase)
        {
            int count = celebrating ? 4 : 2;
            for (int i = 0; i < count; i++)
            {
                double x = i % 2 == 0 ? art.Left + 12 + i * 3 : art.Right - 13 - i * 3;
                double y = art.Bottom + (celebrating ? 11 + i * 29 : 26 + i * 10);
                double opacity = moving ? .66 + .29 * Math.Cos(phase * 4 * Math.PI + i * 1.7) : .72;
                drawing.PushOpacity(opacity);
                DrawStar(drawing, new Point(x, y), i % 2 == 0 ? 2.9 : 2, i % 2 == 0 ? GoldSpark : PartyPink, null);
                drawing.Pop();
            }
        }

        private static void DrawSpecialDayGarland(DrawingContext drawing)
        {
            drawing.DrawEllipse(Glow("#7ABB83DE", "#00BB83DE"), null, new Point(83, 43), 58, 36);
            drawing.DrawGeometry(null, new Pen(GoldSpark, 1.25), Geometry.Parse("M8,10 Q83,50 158,10"));
            Path(drawing, "#DF94B2", "M19,15 L38,22 26,36 Z M67,28 L86,29 77,47 Z M121,23 L139,17 134,37 Z");
            Path(drawing, "#F5CA84", "M44,24 L62,27 51,44 Z M93,29 L114,26 106,46 Z");
            DrawStar(drawing, new Point(83, 61), 13, AmberLight, null);
            DrawStar(drawing, new Point(46, 60), 6, PartyLilac, null);
            DrawStar(drawing, new Point(124, 55), 7, GoldSpark, null);
            DrawStar(drawing, new Point(21, 51), 3.5, GoldSpark, null);
            DrawStar(drawing, new Point(146, 48), 3, PartyPink, null);
            DrawStar(drawing, new Point(107, 69), 3.5, PartyPink, null);
        }
    }
}
