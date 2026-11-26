using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace NghiLe
{
    internal enum PetAction { Rest, Walk, Wag, Sniff, Stretch, Hop, Nap }

    internal struct PetMotion
    {
        private static readonly PetAction[] DogActions =
            { PetAction.Wag, PetAction.Sniff, PetAction.Stretch, PetAction.Hop, PetAction.Nap };
        private static readonly double[] DogStarts = { 8, 15, 24, 32, 40 };
        private static readonly double[] DogDurations = { 2, 3, 2.4, 1.2, 6 };

        internal readonly PetAction Action;
        internal readonly double Progress;

        private PetMotion(PetAction action, double progress)
        {
            Action = action;
            Progress = progress;
        }

        internal static PetMotion At(PetAnimal animal, double seconds, bool enabled)
        {
            if (!enabled) return default(PetMotion);
            if (animal == PetAnimal.HacLong) return new PetMotion(PetAction.Walk, seconds % .8 / .8);
            double phase = seconds % (animal == PetAnimal.Dog ? 48 : 40);
            if (phase < 4) return new PetMotion(PetAction.Walk, phase / 4);
            if (animal == PetAnimal.Dog)
            {
                for (int i = 0; i < DogActions.Length; i++)
                {
                    double elapsed = phase - DogStarts[i];
                    if (elapsed >= 0 && elapsed < DogDurations[i])
                        return new PetMotion(DogActions[i], elapsed / DogDurations[i]);
                }
            }
            return default(PetMotion);
        }

    }

    internal struct TaskbarRail
    {
        internal readonly Drawing.Rectangle Area;
        internal readonly bool Horizontal;
        internal readonly int Size;
        private readonly bool leadingEdge;

        internal TaskbarRail(Drawing.Rectangle screen, Drawing.Rectangle bar, bool horizontal, bool leadingEdge,
            int desiredSize, int revealGap)
        {
            Horizontal = horizontal;
            this.leadingEdge = leadingEdge;
            var visible = Drawing.Rectangle.Intersect(screen, bar);
            bool shown = (horizontal ? visible.Height : visible.Width) > revealGap;
            int left = screen.Left, top = screen.Top, right = screen.Right, bottom = screen.Bottom;
            if (horizontal && leadingEdge) top = shown ? visible.Bottom : top + revealGap;
            else if (horizontal) bottom = shown ? visible.Top : bottom - revealGap;
            else if (leadingEdge) left = shown ? visible.Right : left + revealGap;
            else right = shown ? visible.Left : right - revealGap;
            Area = Drawing.Rectangle.FromLTRB(left, top, right, bottom);
            Size = Math.Max(0, Math.Min(desiredSize, Math.Min(Area.Width, Area.Height)));
        }

        private int Start { get { return Horizontal ? Area.Left : Area.Top; } }
        private int Span { get { return (Horizontal ? Area.Width : Area.Height) - Size; } }

        internal Drawing.Rectangle Perch(bool atEnd, double? chosen, double motion)
        {
            int inset = Math.Min(3 * Size, Span / 4);
            int anchor = chosen.HasValue ? (int)Math.Round(Span * chosen.Value) : atEnd ? Span - inset : inset;
            int direction = anchor <= Span / 2 ? 1 : -1;
            int distance = Math.Min(Size * 2, direction == 1 ? Span - anchor : anchor);
            return OnRail(anchor + (int)Math.Round(motion * distance) * direction);
        }

        internal Drawing.Rectangle Patrol(double distance, double start, out bool returning)
        {
            double span = Span;
            returning = false;
            if (span <= 0) return OnRail(0);

            double phase = (distance + span * start) % (span * 2);
            returning = phase >= span;
            return OnRail((int)Math.Round(returning ? span * 2 - phase : phase));
        }

        private Drawing.Rectangle OnRail(int offset)
        {
            int along = Start + offset;
            if (Horizontal) return new Drawing.Rectangle(along, leadingEdge ? Area.Top : Area.Bottom - Size, Size, Size);
            return new Drawing.Rectangle(leadingEdge ? Area.Left : Area.Right - Size, along, Size, Size);
        }

        internal Drawing.Rectangle Clamp(int x, int y)
        {
            return new Drawing.Rectangle(Math.Max(Area.Left, Math.Min(Area.Right - Size, x)),
                Math.Max(Area.Top, Math.Min(Area.Bottom - Size, y)), Size, Size);
        }

        internal double PerchFraction(Drawing.Rectangle bounds)
        {
            int along = Horizontal ? bounds.Left : bounds.Top;
            return Span <= 0 ? 0 : (double)(along - Start) / Span;
        }
    }

    internal sealed class TaskbarPet : Window
    {
        private const int ExtendedStyle = -20;
        private const int PetStyles = 0x80 | 0x08000000; // WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE
        private const int MouseActivate = 0x0021;
        private const int LeftButtonDown = 0x0201;
        private const int LeftButtonUp = 0x0202;
        private const int DoNotActivate = 3;
        private const uint RootWindow = 2;
        private const uint GetAppBarState = 4;
        private const ulong AutoHideState = 1;
        private const uint MoveWithoutActivation = 0x0010 | 0x0200;
        private const uint KeepZOrder = 0x0004;
        private static readonly IntPtr TopmostWindow = new IntPtr(-1);
        private static readonly TimeSpan RestTick = TimeSpan.FromMilliseconds(200);
        private static readonly TimeSpan RunTick = TimeSpan.FromMilliseconds(84);
        private static readonly TimeSpan HacLongWalkTick = TimeSpan.FromMilliseconds(33);
        private static readonly TimeSpan DragTick = TimeSpan.FromMilliseconds(16);
        internal static readonly int[] DelayOptions = { 1, 5, 15 };
        internal const int DefaultDelay = 5;
        private const int DefaultSize = 52;
        internal const int MaxNameLength = 28;
        private const double PettingSeconds = 1.8;
        internal const int MinimumSizePercent = 50;
        internal const int DefaultSizePercent = 100;
        internal const int MaximumSizePercent = 200;

        private readonly Settings settings;
        private readonly DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Background);
        private readonly Stopwatch playTime = new Stopwatch();
        private readonly Stopwatch carriedTime = new Stopwatch();
        private readonly Stopwatch falling = new Stopwatch();
        private readonly Stopwatch petting = new Stopwatch();
        private readonly PetArtwork artwork = new PetArtwork { GroundAtBottom = true };
        private IntPtr handle;
        private Drawing.Rectangle lastBounds;
        private Drawing.Point? pressOrigin;
        private Drawing.Point? grabOffset;
        private Popup namePopup;
        private TextBlock nameLabel;
        private int grabHeadingX;
        private Drawing.Rectangle dropOrigin;
        private double? perch;
        private bool restAfterDrop;
        private bool facingLeft;
        private bool horizontal = true;
        private bool leadingEdge;
        private bool dpiAvailable = true;
        private bool taskbarAutoHides;
        private long nextShellStateCheck;

        internal TaskbarPet(Settings settings)
        {
            this.settings = settings;
            Title = "Thú cưng · " + Widget.ProductName;
            Width = Height = SizeInPixels(settings.PetSizePercent, 96);
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            ShowActivated = false;
            Focusable = false;
            Topmost = true;
            Cursor = Cursors.Hand;
            Content = artwork;
            timer.Tick += (s, e) => Refresh();
        }

        internal bool IsRunning { get { return timer.IsEnabled; } }

        internal static string SpeciesName(PetAnimal animal)
        {
            switch (animal)
            {
                case PetAnimal.Dog: return "Chó";
                case PetAnimal.HacLong: return "Hắc Long";
                default: return "Mèo";
            }
        }

        internal static string NormalizeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            var name = new StringBuilder(MaxNameLength);
            foreach (char character in value.Trim())
            {
                if (!char.IsControl(character)) name.Append(character);
                if (name.Length == MaxNameLength) break;
            }
            if (name.Length > 0 && char.IsHighSurrogate(name[name.Length - 1])) name.Length--;
            return name.ToString().Trim();
        }

        internal static int NormalizeDelay(int minutes)
        {
            return Array.IndexOf(DelayOptions, minutes) >= 0 ? minutes : DefaultDelay;
        }

        internal static int NormalizeSize(int percent)
        {
            return Math.Max(MinimumSizePercent, Math.Min(MaximumSizePercent, percent));
        }

        internal static int SizeInPixels(int percent, int dpi)
        {
            return (int)Math.Round(DefaultSize * NormalizeSize(percent) / 100.0 * dpi / 96);
        }

        internal void Start()
        {
            if (IsRunning) return;
            timer.Interval = RestTick;
            Refresh();
            timer.Start();
        }

        internal void Stop()
        {
            timer.Stop();
            grabOffset = null;
            Cursor = Cursors.Hand;
            carriedTime.Reset();
            falling.Reset();
            playTime.Reset();
            restAfterDrop = false;
            Conceal();
        }

        internal void ResetPerch() { perch = null; }

        internal void ResetRoute()
        {
            perch = null;
            playTime.Reset();
            falling.Reset();
            restAfterDrop = facingLeft = false;
            CancelPetting();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            handle = new WindowInteropHelper(this).Handle;
            SetWindowLong(handle, ExtendedStyle, GetWindowLong(handle, ExtendedStyle) | PetStyles);
            HwndSource.FromHwnd(handle).AddHook(WindowMessage);
        }

        private IntPtr WindowMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (message)
            {
                case MouseActivate:
                    handled = true;
                    return new IntPtr(DoNotActivate);
                case LeftButtonDown:
                    if (!IsRunning) break;
                    BeginPress(Forms.Cursor.Position);
                    handled = true;
                    break;
                case LeftButtonUp:
                    if (!pressOrigin.HasValue && !grabOffset.HasValue) break;
                    Refresh(Forms.Cursor.Position, false);
                    handled = true;
                    break;
            }
            return IntPtr.Zero;
        }

        private void Conceal()
        {
            playTime.Stop();
            CancelPetting();
            pressOrigin = null;
            grabOffset = null;
            carriedTime.Reset();
            Cursor = Cursors.Hand;
            timer.Interval = RestTick;
            if (IsVisible) Hide();
        }

        private void BeginPress(Drawing.Point pointer)
        {
            if (pressOrigin.HasValue || grabOffset.HasValue) return;
            CancelPetting();
            pressOrigin = pointer;
            falling.Reset();
            playTime.Stop();
            timer.Interval = DragTick;
        }

        private bool UpdatePress(Drawing.Point pointer, bool held)
        {
            var origin = pressOrigin.Value;
            var dragSize = Forms.SystemInformation.DragSize;
            if (Math.Abs(pointer.X - origin.X) >= Math.Max(2, dragSize.Width / 2) ||
                Math.Abs(pointer.Y - origin.Y) >= Math.Max(2, dragSize.Height / 2))
            {
                pressOrigin = null;
                BeginGrab(origin);
                return false;
            }
            if (held) return true;

            pressOrigin = null;
            if (lastBounds.Contains(pointer)) petting.Restart();
            return false;
        }

        private void ShowPetName()
        {
            if (!IsVisible) return;
            if (namePopup == null)
            {
                nameLabel = new TextBlock { FontFamily = new FontFamily("Segoe UI"), FontSize = 13,
                    FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center };
                nameLabel.SetResourceReference(TextBlock.ForegroundProperty, "PanelInk");
                var labelFrame = new Border { Child = nameLabel, MaxWidth = 200, CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(8, 3, 8, 4), BorderThickness = new Thickness(1), IsHitTestVisible = false };
                labelFrame.Resources.MergedDictionaries.Add(PanelPage.LoadTheme());
                labelFrame.SetResourceReference(Border.BackgroundProperty, "PanelCanvas");
                labelFrame.SetResourceReference(Border.BorderBrushProperty, "PanelLine");
                namePopup = new Popup { Child = labelFrame, PlacementTarget = artwork, Placement = PlacementMode.Top,
                    AllowsTransparency = true, StaysOpen = true, IsHitTestVisible = false };
            }
            var name = string.IsNullOrEmpty(settings.PetName) ? SpeciesName(settings.PetAnimal) : settings.PetName;
            if (nameLabel.Text != name) nameLabel.Text = name;
            if (!namePopup.IsOpen) namePopup.IsOpen = true;
        }

        private void CancelPetting()
        {
            petting.Reset();
            if (namePopup != null && namePopup.IsOpen) namePopup.IsOpen = false;
        }

        private void BeginGrab(Drawing.Point pointer)
        {
            if (grabOffset.HasValue) return;
            CancelPetting();
            grabOffset = new Drawing.Point(pointer.X - lastBounds.Left, pointer.Y - lastBounds.Top);
            grabHeadingX = pointer.X;
            falling.Reset();
            playTime.Stop();
            carriedTime.Restart();
            Cursor = Cursors.SizeAll;
            artwork.SetCarryPose(settings.PetAnimal, 0, facingLeft);
            timer.Interval = DragTick;
        }

        private void UpdateGrab(TaskbarRail rail, Drawing.Point pointer, bool held, bool allowMotion)
        {
            var offset = grabOffset.Value;
            var bounds = rail.Clamp(pointer.X - offset.X, pointer.Y - offset.Y);
            int horizontalTravel = pointer.X - grabHeadingX;
            if (Math.Abs(horizontalTravel) >= Math.Max(2, rail.Size / 24))
            {
                facingLeft = horizontalTravel < 0;
                grabHeadingX = pointer.X;
            }
            if (held)
            {
                artwork.SetCarryPose(settings.PetAnimal, allowMotion ? carriedTime.Elapsed.TotalSeconds : 0, facingLeft);
                Present(bounds);
                return;
            }
            grabOffset = null;
            carriedTime.Stop();
            Cursor = Cursors.Hand;
            perch = rail.PerchFraction(bounds);
            dropOrigin = bounds;
            falling.Restart();
            playTime.Reset();
            restAfterDrop = true;
        }

        private void Refresh()
        {
            // A window that leaves focus with the user's app cannot rely on retaining mouse capture.
            bool held = (pressOrigin.HasValue || grabOffset.HasValue) &&
                (GetAsyncKeyState(Forms.SystemInformation.MouseButtonsSwapped ? 2 : 1) & 0x8000) != 0;
            Refresh(Forms.Cursor.Position, held);
        }

        private void Refresh(Drawing.Point pointer, bool held)
        {
            Drawing.Rectangle screen;
            var rail = CurrentRail(out screen);
            if (rail.Size < 16)
            {
                Conceal();
                return;
            }

            bool allowMotion = settings.Motion && SystemParameters.ClientAreaAnimation;
            if (pressOrigin.HasValue && UpdatePress(pointer, held)) return;
            if (grabOffset.HasValue)
            {
                UpdateGrab(rail, pointer, held, allowMotion);
                if (grabOffset.HasValue) return;
            }

            var home = rail.Perch(settings.PetAtEnd, perch, 0);
            if (CoveredByFullscreen(screen, home))
            {
                Conceal();
                return;
            }

            if (petting.IsRunning)
            {
                double progress = petting.Elapsed.TotalSeconds / PettingSeconds;
                if (progress < 1)
                {
                    timer.Interval = allowMotion && settings.PetAnimal != PetAnimal.HacLong ? RunTick : RestTick;
                    artwork.SetPettingPose(settings.PetAnimal, allowMotion ? progress : 0.25, facingLeft);
                    ShowPetName();
                    return;
                }
                CancelPetting();
            }

            if (falling.IsRunning)
            {
                if (allowMotion && ShowFall(rail, home)) return;
                falling.Reset();
            }

            bool hover = IsVisible && lastBounds.Contains(pointer);
            if (hover) playTime.Stop();
            else playTime.Start();
            bool homeAtEnd = rail.PerchFraction(home) > .5;
            bool hacLong = settings.PetAnimal == PetAnimal.HacLong;
            double time = playTime.Elapsed.TotalSeconds;
            var motion = PetMotion.At(settings.PetAnimal, time + (restAfterDrop && !hacLong ? 4 : 0),
                settings.PetRoams && allowMotion);
            bool walking = motion.Action == PetAction.Walk;
            if (hacLong && walking)
            {
                timer.Interval = hover ? RestTick : HacLongWalkTick;
                var bounds = rail.Patrol(time * rail.Size * .9, perch.GetValueOrDefault(), out facingLeft);
                artwork.SetMotion(settings.PetAnimal, motion, facingLeft);
                Present(bounds);
                return;
            }
            timer.Interval = motion.Action != PetAction.Rest && !hover ? RunTick : RestTick;
            var boundsOnRail = walking
                ? rail.Perch(settings.PetAtEnd, perch, Math.Sin(Math.PI * motion.Progress))
                : home;
            if (walking || !restAfterDrop) facingLeft = (walking && motion.Progress >= .5) != homeAtEnd;
            artwork.SetMotion(settings.PetAnimal, motion, facingLeft);
            Present(boundsOnRail);
        }

        internal bool CheckPettingInteraction()
        {
            if (!IsVisible) return false;
            var point = new Drawing.Point(lastBounds.Left + lastBounds.Width / 2, lastBounds.Top + lastBounds.Height / 2);
            var animal = settings.PetAnimal;
            try
            {
                foreach (var species in new[] { PetAnimal.HacLong, PetAnimal.Dog, PetAnimal.Cat })
                {
                    settings.PetAnimal = species;
                    BeginPress(point);
                    Refresh(point, true);
                    if (!pressOrigin.HasValue || grabOffset.HasValue || petting.IsRunning) return false;
                    Refresh(point, false);
                    if (!petting.IsRunning || namePopup == null || !namePopup.IsOpen ||
                        nameLabel.Text != settings.PetName || grabOffset.HasValue) return false;
                }
                BeginPress(point);
                var moved = new Drawing.Point(point.X + Forms.SystemInformation.DragSize.Width + 10, point.Y);
                Refresh(moved, true);
                if (!grabOffset.HasValue || petting.IsRunning || namePopup.IsOpen) return false;
                Refresh(moved, false);
                return !grabOffset.HasValue && !petting.IsRunning;
            }
            finally
            {
                settings.PetAnimal = animal;
                CancelPetting();
            }
        }

        private bool ShowFall(TaskbarRail rail, Drawing.Rectangle home)
        {
            int distance = rail.Horizontal ? Math.Abs(home.Top - dropOrigin.Top) : Math.Abs(home.Left - dropOrigin.Left);
            double duration = Math.Min(.55, Math.Sqrt(distance / 1200.0));
            if (duration == 0) return false;
            double progress = falling.Elapsed.TotalSeconds / duration;
            if (progress >= 1) return false;
            double ease = progress * progress;
            var bounds = rail.Clamp(dropOrigin.Left + (int)Math.Round((home.Left - dropOrigin.Left) * ease),
                dropOrigin.Top + (int)Math.Round((home.Top - dropOrigin.Top) * ease));
            timer.Interval = DragTick;
            artwork.SetPose(settings.PetAnimal, left: facingLeft, lifted: true);
            Present(bounds);
            return true;
        }

        private void Present(Drawing.Rectangle bounds)
        {
            if (!IsVisible)
            {
                if (handle == IntPtr.Zero) new WindowInteropHelper(this).EnsureHandle();
                if ((GetWindowLong(handle, ExtendedStyle) & PetStyles) != PetStyles ||
                    !MoveWindow(bounds, false)) return;
                Show();
                MoveWindow(bounds, false);
            }
            else if (bounds != lastBounds) MoveWindow(bounds, true);
        }

        private bool MoveWindow(Drawing.Rectangle bounds, bool keepZOrder)
        {
            uint flags = MoveWithoutActivation | (keepZOrder ? KeepZOrder : 0);
            if (!SetWindowPos(handle, TopmostWindow, bounds.X, bounds.Y, bounds.Width, bounds.Height, flags))
                return false;
            lastBounds = bounds;
            return true;
        }

        private TaskbarRail CurrentRail(out Drawing.Rectangle screen)
        {
            var taskbar = FindWindow("Shell_TrayWnd", null);
            screen = taskbar != IntPtr.Zero ? Forms.Screen.FromHandle(taskbar).Bounds : Forms.Screen.PrimaryScreen.Bounds;
            int dpi = 96;
            if (dpiAvailable && taskbar != IntPtr.Zero)
            {
                try { dpi = (int)Math.Max(96, GetDpiForWindow(taskbar)); }
                catch (EntryPointNotFoundException) { dpiAvailable = false; }
            }

            int revealGap = (int)Math.Round(8 * dpi / 96.0);
            var bar = Drawing.Rectangle.Empty;
            NativeRectangle native;
            if (taskbar != IntPtr.Zero && GetWindowRect(taskbar, out native))
            {
                var full = native.ToRectangle();
                horizontal = full.Width >= full.Height;
                leadingEdge = horizontal ? full.Top + full.Height / 2 < screen.Top + screen.Height / 2 :
                    full.Left + full.Width / 2 < screen.Left + screen.Width / 2;
                if (IsWindowVisible(taskbar) && TaskbarAtEdge(taskbar, Drawing.Rectangle.Intersect(screen, full), revealGap))
                    bar = full;
            }
            return new TaskbarRail(screen, bar, horizontal, leadingEdge, SizeInPixels(settings.PetSizePercent, dpi), revealGap);
        }

        private bool TaskbarAtEdge(IntPtr taskbar, Drawing.Rectangle visible, int revealGap)
        {
            if ((horizontal ? visible.Height : visible.Width) <= revealGap) return false;
            long now = Stopwatch.GetTimestamp();
            if (now >= nextShellStateCheck)
            {
                var data = new AppBarData { Size = (uint)Marshal.SizeOf(typeof(AppBarData)), Window = taskbar };
                taskbarAutoHides = (SHAppBarMessage(GetAppBarState, ref data).ToUInt64() & AutoHideState) != 0;
                nextShellStateCheck = now + Stopwatch.Frequency;
            }
            if (!taskbarAutoHides) return true;

            // Some shells keep the last bounds while the auto-hidden taskbar is off-screen.
            for (int quarter = 1; quarter <= 3; quarter++)
            {
                var point = new NativePoint
                {
                    X = visible.Left + visible.Width * (horizontal ? quarter : 2) / 4,
                    Y = visible.Top + visible.Height * (horizontal ? 2 : quarter) / 4
                };
                if (GetAncestor(WindowFromPoint(point), RootWindow) == taskbar) return true;
            }
            return false;
        }

        private bool CoveredByFullscreen(Drawing.Rectangle screen, Drawing.Rectangle home)
        {
            var foreground = GetForegroundWindow();
            if (CoversScreen(foreground, screen)) return true;
            var midpoint = new NativePoint { X = home.Left + home.Width / 2, Y = home.Top + home.Height / 2 };
            var aboveRail = GetAncestor(WindowFromPoint(midpoint), RootWindow);
            return aboveRail != foreground && CoversScreen(aboveRail, screen);
        }

        private bool CoversScreen(IntPtr window, Drawing.Rectangle screen)
        {
            NativeRectangle frame;
            if (window == IntPtr.Zero || window == handle || IsZoomed(window) || !GetWindowRect(window, out frame)) return false;
            var bounds = frame.ToRectangle();
            bounds.Inflate(2, 2);
            if (!bounds.Contains(screen)) return false;
            var name = new StringBuilder(32);
            GetClassName(window, name, name.Capacity);
            string windowClass = name.ToString();
            return windowClass != "Progman" && windowClass != "WorkerW";
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AppBarData
        {
            internal uint Size;
            internal IntPtr Window;
            internal uint Callback;
            internal uint Edge;
            internal NativeRectangle Bounds;
            internal IntPtr Parameter;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint
        {
            internal int X, Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRectangle
        {
            internal int Left, Top, Right, Bottom;
            internal Drawing.Rectangle ToRectangle() { return Drawing.Rectangle.FromLTRB(Left, Top, Right, Bottom); }
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string className, string windowName);
        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr window);
        [DllImport("user32.dll")]
        private static extern bool IsZoomed(IntPtr window);
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr window, out NativeRectangle rectangle);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr window, StringBuilder name, int length);
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int key);
        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(NativePoint point);
        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr window, uint flags);
        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr window);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
        private static extern int GetWindowLong(IntPtr window, int index);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
        private static extern int SetWindowLong(IntPtr window, int index, int style);
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
        [DllImport("shell32.dll")]
        private static extern UIntPtr SHAppBarMessage(uint message, ref AppBarData data);
    }
}
