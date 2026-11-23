using System;
using System.Windows;
using System.Windows.Media.Imaging;

namespace NghiLe
{
    internal sealed class DragonClip
    {
        internal const int Width = 192, Height = 208, FrameCount = 32;
        internal static readonly DragonClip Breathe = new DragonClip("01-dung-tho", "Đứng thở", 16, 191);
        internal static readonly DragonClip Rise = new DragonClip("14-dung-day", "Đứng dậy", 18, 204);
        internal static readonly DragonClip Jump = new DragonClip("15-nhay-cat-canh", "Nhảy lên", 28, 204);
        internal static readonly DragonClip Flap = new DragonClip("16-vo-canh-cat-canh", "Vỗ cánh", 22, 204);
        internal static readonly DragonClip Fly = new DragonClip("11-bay-tai-cho", "Bay quanh chỗ đậu", 32, 208);
        internal static readonly DragonClip Land = new DragonClip("17-dap-xuong", "Đáp xuống", 20, 204);
        internal static readonly DragonClip Sulk = new DragonClip("18-u-ru-roi-ngu", "Ủ rũ", 12, 204);
        internal static readonly DragonClip Sleep = new DragonClip("12-ngu-cuon-minh", "Ngủ cuộn mình", 12, 167);
        internal static readonly DragonClip Roar = new DragonClip("13-gam", "Ngẩng đầu gầm", 22, 199);
        internal static readonly DragonClip[] All =
            { Sleep, Rise, Jump, Flap, Fly, Land, Sulk, Breathe, Roar };

        internal readonly string Title;
        internal readonly int Fps, GroundLine;
        private readonly string resource;

        private DragonClip(string slug, string title, int fps, int groundLine)
        {
            resource = "NghiLe.HacLong." + slug;
            Title = title;
            Fps = fps;
            GroundLine = groundLine;
        }

        internal int FrameAt(double seconds) { return (int)(Math.Floor(seconds * Fps) % FrameCount); }

        internal double ArtworkScale(int frame)
        {
            const double sleepingScale = .69;
            if (this == Sleep) return sleepingScale;
            if (this != Rise && this != Sulk) return 1;
            int fromSleep = this == Rise ? frame : FrameCount - 1 - frame;
            // Match the two end poses over the entire movement, including the leg extension.
            double t = (double)fromSleep / (FrameCount - 1);
            return sleepingScale + (1 - sleepingScale) * t * t * (3 - 2 * t);
        }

        internal TimeSpan NextTick(double seconds)
        {
            double remaining = 1 - (seconds * Fps) % 1;
            return TimeSpan.FromMilliseconds(Math.Max(1, Math.Ceiling(remaining * 1000 / Fps)));
        }

        internal BitmapSource[] LoadFrames()
        {
            BitmapSource sheet;
            using (var stream = typeof(DragonClip).Assembly.GetManifestResourceStream(resource))
                sheet = BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            if (sheet.PixelWidth != Width * FrameCount || sheet.PixelHeight != Height)
                throw new InvalidOperationException("Kích thước dải ảnh Hắc Long không hợp lệ: " + Title);
            sheet.Freeze();
            var frames = new BitmapSource[FrameCount];
            for (int i = 0; i < frames.Length; i++)
            {
                frames[i] = new CroppedBitmap(sheet, new Int32Rect(i * Width, 0, Width, Height));
                frames[i].Freeze();
            }
            return frames;
        }
    }

    internal struct DragonMotion
    {
        private sealed class Step
        {
            internal readonly DragonClip Clip;
            internal readonly double Duration;
            internal Step(DragonClip clip, int cycles)
            {
                Clip = clip;
                Duration = (double)(cycles * DragonClip.FrameCount) / clip.Fps;
            }
        }

        internal const int FlightWidth = 250;
        internal const int FlightCeiling = 115;
        internal const int FlyingCycles = 6;
        private const int LaunchHeight = 22, CruiseHeight = 42;
        private static readonly Step[] Routine = {
            new Step(DragonClip.Sleep, 12), new Step(DragonClip.Rise, 1), new Step(DragonClip.Jump, 1),
            new Step(DragonClip.Flap, 1), new Step(DragonClip.Fly, FlyingCycles),
            new Step(DragonClip.Land, 1), new Step(DragonClip.Sulk, 1)
        };
        internal static readonly double Duration = TotalDuration();
        internal readonly DragonClip Clip;
        internal readonly double Seconds;
        internal readonly double Progress;

        private DragonMotion(DragonClip clip, double seconds, double progress)
        {
            Clip = clip;
            Seconds = seconds;
            Progress = progress;
        }

        private static double TotalDuration()
        {
            double total = 0;
            foreach (var step in Routine) total += step.Duration;
            return total;
        }

        internal static DragonMotion At(double seconds, bool enabled)
        {
            if (enabled)
            {
                double elapsed = seconds % Duration;
                foreach (var step in Routine)
                {
                    if (elapsed < step.Duration)
                    {
                        return new DragonMotion(step.Clip, elapsed, elapsed / step.Duration);
                    }
                    elapsed -= step.Duration;
                }
            }
            return new DragonMotion(DragonClip.Sleep, 0, 0);
        }

        internal static DragonMotion Preview(DragonClip clip, double seconds)
        {
            int cycles = clip == DragonClip.Fly ? FlyingCycles : 1;
            double duration = (double)DragonClip.FrameCount * cycles / clip.Fps;
            if ((clip == DragonClip.Rise || clip == DragonClip.Sulk) && seconds >= duration)
                return new DragonMotion(clip, (DragonClip.FrameCount - 1.0) / clip.Fps, 1);
            double elapsed = seconds % duration;
            return new DragonMotion(clip, elapsed, elapsed / duration);
        }

        private static double Ease(double value)
        {
            double t = Math.Max(0, Math.Min(1, value));
            return t * t * (3 - 2 * t);
        }

        internal double FacingOutward
        {
            get
            {
                if (Clip == DragonClip.Jump) return Ease((Progress - .45) / .4);
                if (Clip == DragonClip.Flap) return 1;
                if (Clip == DragonClip.Fly) return 1 - Ease((Progress - .44) / .12);
                return 0;
            }
        }

        internal double ProfileWidth { get { return .53 + .47 * Math.Abs(2 * FacingOutward - 1); } }

        // X runs away from the perch. Y is height above it, in physical desktop pixels.
        internal Vector Offset()
        {
            if (Clip == DragonClip.Jump)
                return new Vector(0, LaunchHeight * Ease((Progress - .20) / .65));
            if (Clip == DragonClip.Flap)
                return new Vector(0, LaunchHeight + (CruiseHeight - LaunchHeight) * Ease(Progress));
            if (Clip == DragonClip.Fly)
            {
                double turn = 2 * Math.PI * Progress;
                double away = (1 - Math.Cos(turn)) / 2;
                return new Vector(FlightWidth * away, CruiseHeight + away * (50 + 40 * Math.Sin(turn)));
            }
            if (Clip == DragonClip.Land)
                return new Vector(0, CruiseHeight * (1 - Ease(Progress / .73)));
            return new Vector();
        }
    }
}
