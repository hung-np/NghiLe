using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

[assembly: AssemblyTitle(NghiLe.Widget.ProductName)]
[assembly: AssemblyProduct(NghiLe.Widget.ProductName)]
[assembly: AssemblyVersion("2.0.0.0")]
[assembly: AssemblyFileVersion("2.0.0.0")]

namespace NghiLe
{
    public sealed class Settings
    {
        public int Version { get; set; }
        public CountdownUnit CounterMode { get; set; }
        public bool Automatic { get; set; }
        public int Year { get; set; }
        public bool AlwaysOnTop { get; set; }
        public bool Motion { get; set; }
        public bool PauseOnBattery { get; set; }
        public bool DimOnSchedule { get; set; }
        public int DimFromHour { get; set; }
        public int DimUntilHour { get; set; }
        public bool Use3D { get; set; }
        public bool ApricotNewYear { get; set; }
        public bool Horizontal { get; set; }
        public int Glass { get; set; }
        public double Scale { get; set; }
        public double? X { get; set; }
        public double? Y { get; set; }
        public string[] SelectedHolidays { get; set; }
        public string[] PinnedOccasionIds { get; set; }
        public string DisplayedOccurrenceKey { get; set; }
        public PersonalDate[] PersonalDates { get; set; }
        public Dictionary<string, int> ReminderDays { get; set; }
        public string[] DeliveredReminders { get; set; }
        public bool PetEnabled { get; set; }
        public string PetName { get; set; }
        public PetAnimal PetAnimal { get; set; }
        public int PetDelayMinutes { get; set; }
        public bool PetRoams { get; set; }
        public bool PetAtEnd { get; set; }
        public int PetSizePercent { get; set; }
        public double SettingsWidth { get; set; }
        public double SettingsHeight { get; set; }
        public WidgetProfile[] AdditionalWidgets { get; set; }

        public Settings()
        {
            CounterMode = CountdownUnit.Days;
            Automatic = true;
            Year = 2027;
            AlwaysOnTop = true;
            Motion = SystemParameters.ClientAreaAnimation;
            DimFromHour = WidgetPower.DefaultDimFrom;
            DimUntilHour = WidgetPower.DefaultDimUntil;
            Use3D = true;
            Glass = 57;
            Scale = 1;
            SelectedHolidays = new[] { HolidaySetting.LunarNewYear };
            PersonalDates = new PersonalDate[0];
            PinnedOccasionIds = new string[0];
            ReminderDays = new Dictionary<string, int>(StringComparer.Ordinal);
            DeliveredReminders = new string[0];
            PetDelayMinutes = TaskbarPet.DefaultDelay;
            PetRoams = true;
            PetSizePercent = TaskbarPet.DefaultSizePercent;
            SettingsWidth = SettingsWindow.DefaultWidth;
            SettingsHeight = SettingsWindow.DefaultHeight;
            AdditionalWidgets = new WidgetProfile[0];
        }

        internal Settings Copy() { return (Settings)MemberwiseClone(); }
    }

    [Flags]
    internal enum WidgetEdge { None = 0, Left = 1, Top = 2, Right = 4, Bottom = 8 }

    internal static class WidgetSize
    {
        private const double EdgeInset = 7;
        private const double CornerInset = 16;
        internal const double MinimumScale = .6;
        internal const double MaximumScale = 1.8;
        internal const double Width = 200;
        internal const double Height = 250;
        internal const double HorizontalWidth = 360;
        internal const double HorizontalHeight = 84;
        internal const double CompactScale = .8;

        internal static double Clamp(double scale)
        {
            if (double.IsNaN(scale) || double.IsInfinity(scale) || scale <= 0) return 1;
            return LimitScale(scale);
        }

        private static double LimitScale(double scale)
        {
            return Math.Max(MinimumScale, Math.Min(MaximumScale, scale));
        }

        internal static Size Clock(bool horizontal)
        {
            return horizontal ? new Size(HorizontalWidth, HorizontalHeight) : new Size(Width, Height);
        }

        internal static double FromDrag(double scale, double dx, double dy, bool horizontal = false)
        {
            var size = Clock(horizontal);
            return LimitScale(scale + (dx * size.Width + dy * size.Height) /
                (size.Width * size.Width + size.Height * size.Height));
        }

        internal static WidgetEdge EdgeAt(Point point, Size size)
        {
            if (point.X < 0 || point.Y < 0 || point.X > size.Width || point.Y > size.Height)
                return WidgetEdge.None;

            double right = size.Width - point.X, bottom = size.Height - point.Y;
            var side = point.X <= CornerInset ? WidgetEdge.Left : right <= CornerInset ? WidgetEdge.Right : WidgetEdge.None;
            var end = point.Y <= CornerInset ? WidgetEdge.Top : bottom <= CornerInset ? WidgetEdge.Bottom : WidgetEdge.None;
            if (side != WidgetEdge.None && end != WidgetEdge.None) return side | end;
            if (Math.Min(point.X, right) <= EdgeInset) return side;
            if (Math.Min(point.Y, bottom) <= EdgeInset) return end;
            return WidgetEdge.None;
        }

        internal static double ResizeScale(double scale, Vector delta, WidgetEdge edge, bool horizontal)
        {
            int x = Direction(edge, WidgetEdge.Left, WidgetEdge.Right);
            int y = Direction(edge, WidgetEdge.Top, WidgetEdge.Bottom);
            if (x != 0 && y != 0) return FromDrag(scale, x * delta.X, y * delta.Y, horizontal);
            var size = Clock(horizontal);
            return LimitScale(scale + x * delta.X / size.Width + y * delta.Y / size.Height);
        }

        internal static Point ResizeOrigin(Rect start, Size target, WidgetEdge edge)
        {
            double x = (1 - Direction(edge, WidgetEdge.Left, WidgetEdge.Right)) / 2.0;
            double y = (1 - Direction(edge, WidgetEdge.Top, WidgetEdge.Bottom)) / 2.0;
            return new Point(start.X + (start.Width - target.Width) * x,
                start.Y + (start.Height - target.Height) * y);
        }

        private static int Direction(WidgetEdge edge, WidgetEdge first, WidgetEdge last)
        {
            return (edge & first) != 0 ? -1 : (edge & last) != 0 ? 1 : 0;
        }
    }

    internal static class Calendar
    {
        internal static readonly DateTimeOffset[] Holidays = Load("NghiLe.Calendar");
        private static readonly string[] Stems = { "Giáp", "Ất", "Bính", "Đinh", "Mậu", "Kỷ", "Canh", "Tân", "Nhâm", "Quý" };
        private static readonly string[] Branches = { "Tý", "Sửu", "Dần", "Mão", "Thìn", "Tỵ", "Ngọ", "Mùi", "Thân", "Dậu", "Tuất", "Hợi" };

        internal static DateTimeOffset[] Load(string resource)
        {
            using (var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)))
            {
                return reader.ReadToEnd().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(day => DateTimeOffset.ParseExact(day + " +07:00", "yyyy-MM-dd zzz", CultureInfo.InvariantCulture)).ToArray();
            }
        }

        internal static string Name(int year) { return Stems[(year - 4) % 10] + " " + Branches[(year - 4) % 12]; }

        internal static long Seconds(DateTimeOffset target, DateTimeOffset now)
        {
            return Math.Max(0, (long)Math.Ceiling((target - now).TotalSeconds));
        }
    }

    internal sealed class SettingsFile
    {
        internal const int SettingsVersion = 9;
        private readonly string path;
        private readonly string legacyPath;
        private readonly JavaScriptSerializer json = new JavaScriptSerializer();
        internal SettingsFile(string path, string legacyPath = null)
        {
            this.path = path;
            this.legacyPath = legacyPath;
        }

        internal Settings Read()
        {
            try
            {
                var source = !File.Exists(path) && legacyPath != null ? legacyPath : path;
                return Normalize(json.Deserialize<Settings>(File.ReadAllText(source)));
            }
            catch (IOException) { return new Settings(); }
            catch (UnauthorizedAccessException) { return new Settings(); }
            catch (ArgumentException) { return new Settings(); }
            catch (InvalidOperationException) { return new Settings(); }
        }

        internal static Settings Normalize(Settings s)
        {
            if (s == null) return new Settings();
            if (!Enum.IsDefined(typeof(CountdownUnit), s.CounterMode)) s.CounterMode = CountdownUnit.Days;
            if (!Calendar.Holidays.Any(d => d.Year == s.Year)) s.Year = Calendar.Holidays[0].Year;
            s.Glass = s.Version < 2 && s.Glass == 88 ? 100 : Math.Max(42, Math.Min(100, s.Glass));
            s.Scale = WidgetSize.Clamp(s.Scale);
            if (s.SelectedHolidays == null) s.SelectedHolidays = new[] { HolidaySetting.LunarNewYear };
            s.PersonalDates = PersonalDate.Normalize(s.PersonalDates);
            s.PinnedOccasionIds = PinnedOccasions.Normalize(s);
            var reminderIds = new HashSet<string>(HolidaySetting.All.Select(item => item.Id)
                .Concat(s.PersonalDates.Select(item => item.Id.ToString("N"))), StringComparer.Ordinal);
            s.ReminderDays = (s.ReminderDays ?? new Dictionary<string, int>())
                .Where(item => reminderIds.Contains(item.Key) && item.Value != OccasionReminders.Off &&
                    OccasionReminders.Choices.Contains(item.Value))
                .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
            s.DeliveredReminders = (s.DeliveredReminders ?? new string[0]).Where(key => !string.IsNullOrEmpty(key)).ToArray();
            if (!Enum.IsDefined(typeof(PetAnimal), s.PetAnimal)) s.PetAnimal = PetAnimal.Cat;
            s.PetName = TaskbarPet.NormalizeName(s.PetName);
            s.PetDelayMinutes = TaskbarPet.NormalizeDelay(s.PetDelayMinutes);
            s.PetSizePercent = TaskbarPet.NormalizeSize(s.PetSizePercent);
            if (s.Version < 3 && s.SettingsWidth == 680 && s.SettingsHeight == 680)
            {
                s.SettingsWidth = SettingsWindow.DefaultWidth;
                s.SettingsHeight = SettingsWindow.DefaultHeight;
            }
            s.DimFromHour = WidgetPower.ValidHour(s.DimFromHour, WidgetPower.DefaultDimFrom);
            s.DimUntilHour = WidgetPower.ValidHour(s.DimUntilHour, WidgetPower.DefaultDimUntil);
            s.AdditionalWidgets = WidgetProfile.Normalize(s.AdditionalWidgets);
            return s;
        }

        internal bool IsSettingsPath(string candidate)
        {
            string full = System.IO.Path.GetFullPath(candidate);
            return string.Equals(full, System.IO.Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase) ||
                (legacyPath != null && string.Equals(full, System.IO.Path.GetFullPath(legacyPath), StringComparison.OrdinalIgnoreCase));
        }

        internal bool Save(Settings s)
        {
            s.Version = SettingsVersion;
            return WriteJson(path, json.Serialize(s));
        }

        internal static bool WriteJson(string path, string payload)
        {
            string target = System.IO.Path.GetFullPath(path);
            string temp = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(target));
                File.WriteAllText(temp, payload);
                if (File.Exists(target)) File.Replace(temp, target, null); else File.Move(temp, target);
                return true;
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
            finally
            {
                try { File.Delete(temp); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }

    internal sealed partial class Widget : Window
    {
        internal const string ProductName = "Nghỉ Lễ";
        private const double HorizontalArtworkWidth = 124;
        internal static readonly FontFamily CounterFont = new FontFamily("Bahnschrift, Segoe UI");
        internal static readonly FontFamily DisplayFont = new FontFamily("Georgia, Segoe UI");
        private static readonly string[] clockNumbers = Enumerable.Range(0, 60)
            .Select(value => value.ToString("00", CultureInfo.InvariantCulture)).ToArray();
        internal static void ConfigureRendering()
        {
            // This small layered window doesn't need a hardware rendering device.
            RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
        }

        private readonly SettingsFile file;
        private Settings config;
        private readonly Border glass;
        private readonly Grid surface;
        private readonly Border inlay = new Border { Margin = new Thickness(4), BorderThickness = new Thickness(1),
            IsHitTestVisible = false };
        private HolidayPalette currentPalette = Palette(null);
        private SettingsWindow settingsWindow;
        private ResourceDictionary settingsTheme;
        private DateTime? completedReminderDay;
        private readonly HolidayScene holidayScene = new HolidayScene();
        private readonly Grid face = new Grid { IsHitTestVisible = false };
        private readonly StackPanel greeting = new StackPanel { IsHitTestVisible = false, Visibility = Visibility.Collapsed,
            VerticalAlignment = VerticalAlignment.Center };
        private readonly TextBlock greetingText = Label("", 24, true);
        private readonly TextBlock greetingDetail = Label("", 11, false);
        private readonly StackPanel dayCount = new StackPanel();
        private readonly Grid divider = new Grid();
        private readonly Border dividerRule = new Border { Height = 1, VerticalAlignment = VerticalAlignment.Center };
        private readonly Border dividerSeal = new Border { Width = 5, Height = 5, HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center, RenderTransformOrigin = new Point(.5, .5),
            RenderTransform = new RotateTransform(45) };
        private readonly StackPanel controls = new StackPanel { Orientation = Orientation.Horizontal };
        private ScrollViewer optionsPage;
        private MusicView music;
        private Playlist playlist;
        private Slider sizeOption;
        private WidgetEdge resizeEdge;
        private Point resizeStart;
        private Rect resizeBounds;
        private Matrix resizeFromDevice;
        private double resizeScale;
        private Point? pendingResizePoint;
        private bool resizing { get { return resizeEdge != WidgetEdge.None; } }
        private bool movingWindow;
        private readonly TextBlock year = Label("", 14, false);
        private readonly DockPanel titleLabel = new DockPanel();

        private readonly TextBlock days = Label("", 70, true);
        private readonly TextBlock dayUnit = Label("NGÀY", 10, false);
        private readonly Grid clock = new Grid();
        private readonly TextBlock dateStatus = Label("", 11, false);
        private readonly TextBlock[] time = { Label("", 24, true), Label("", 24, true), Label("", 24, true) };
        private readonly TextBlock[] clockUnits = new TextBlock[3];
        private readonly DispatcherTimer tick = new DispatcherTimer();
        private readonly DispatcherTimer reminderTick = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        private readonly DispatcherTimer petDelay = new DispatcherTimer(DispatcherPriority.Background);
        private readonly DispatcherTimer save = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        private readonly Forms.NotifyIcon tray;
        private readonly Forms.ContextMenu trayMenu;
        private readonly Forms.MenuItem showPetItem;
        private readonly Forms.MenuItem showBothItem;
        private TaskbarPet pet;
        private readonly Drawing.Icon icon;
        private bool quitting;
        private bool ready;
        private bool compact;
        private DateTimeOffset? displayedTarget;
        private CountdownUnit? displayedUnit;
        private long displayedExtraDays;
        private HolidayTheme? displayedTheme;
        private string displayedName;
        private bool celebrating;
        private string unavailableStatus;
        private long displayedSeconds = long.MinValue;
        private DateTime holidayLookupDate;
        private string[] holidayLookupSelection;
        private PersonalDate[] holidayLookupPersonalDates;
        private int holidayLookupFixedYear;
        private HolidayOccurrence? holidayLookupResult;
        private string holidayLookupKey;
        private const int WheelPauseMilliseconds = 220;
        private int wheelRemainder;
        private int? lastWheelTimestamp;

        internal Widget(SettingsFile file) : this(file, null, null) { }

        private Widget(SettingsFile file, Widget host, WidgetProfile profile)
        {
            this.file = file;
            this.host = host;
            this.profile = profile;
            config = IsCompanion ? host.SettingsForWidget(profile) : file.Read();
            onBattery = WidgetPower.RunsOnBattery();
            Title = IsCompanion ? host.CompanionTitle(profile) : ProductName;
            ResizeMode = ResizeMode.NoResize;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            Topmost = config.AlwaysOnTop;
            controls.IsKeyboardFocusWithinChanged += (s, e) => UpdateControls();

            surface = new Grid { ClipToBounds = true };
            glass = new Border { BorderThickness = new Thickness(1), Child = surface };
            Content = new Viewbox { Stretch = Stretch.Uniform, Child = glass };
            surface.Children.Add(holidayScene);
            surface.Children.Add(inlay);
            BuildFace();
            BuildControls();
            surface.Children.Add(dimLayer);
            ApplyOrientation();
            SetScale(config.Scale);
            if (!IsCompanion)
            {
                icon = Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
                tray = new Forms.NotifyIcon { Icon = icon, Text = ProductName + " · Đếm ngược ngày lễ", Visible = true };
                trayMenu = new Forms.ContextMenu();
                trayMenu.MenuItems.Add("Mở widget", (s, e) => ShowWidget());
                trayMenu.MenuItems.Add("Thêm widget", (s, e) => AddCompanion());
                companionMenu = trayMenu.MenuItems.Add("Widget bổ sung");
                trayMenu.Popup += (s, e) => RefreshCompanionMenu();
                RefreshCompanionMenu();
                trayMenu.MenuItems.Add("Ẩn khỏi màn hình", (s, e) => HideAllWidgets());
                showPetItem = trayMenu.MenuItems.Add("Hiện thú cưng ngay", (s, e) => ShowPet(false));
                showBothItem = trayMenu.MenuItems.Add("Hiện cả hai", (s, e) => ShowPet(true));
                showPetItem.Enabled = config.PetEnabled;
                showBothItem.Enabled = config.PetEnabled;
                trayMenu.MenuItems.Add("Dịp sắp tới", (s, e) => OpenUpcoming());
                trayMenu.MenuItems.Add("Cài đặt", (s, e) => { if (!IsVisible) ShowWidget(); OpenSettings(); });
                trayMenu.MenuItems.Add("Thoát " + ProductName, (s, e) => Quit());
                tray.ContextMenu = trayMenu;
                tray.MouseClick += (s, e) => { if (e.Button == Forms.MouseButtons.Left) ShowWidget(); };
                tray.BalloonTipClicked += (s, e) => OpenUpcoming();
            }

            PreviewMouseLeftButtonDown += (s, e) =>
            {
                if (InControl(e.OriginalSource as DependencyObject)) return;
                var point = e.GetPosition(this);
                var edge = WidgetSize.EdgeAt(point, RenderSize);
                if (edge != WidgetEdge.None)
                {
                    StartEdgeResize(edge, PointToScreen(point));
                    e.Handled = true;
                    return;
                }
                movingWindow = true;
                try { DragMove(); }
                finally { movingWindow = false; }
                ScheduleSave();
                ResetPetDelay();
            };
            PreviewMouseMove += (s, e) =>
            {
                var point = e.GetPosition(this);
                if (resizing)
                {
                    QueueResizeAt(PointToScreen(point));
                    e.Handled = true;
                }
                else
                {
                    var edge = WidgetSize.EdgeAt(point, RenderSize);
                    SetResizeCursor(edge != WidgetEdge.None && InControl(e.OriginalSource as DependencyObject)
                        ? WidgetEdge.None : edge);
                }
            };
            PreviewMouseLeftButtonUp += (s, e) =>
            {
                if (!resizing) return;
                QueueResizeAt(PointToScreen(e.GetPosition(this)));
                EndEdgeResize();
                e.Handled = true;
            };
            LostMouseCapture += (s, e) => { if (e.OriginalSource == this) EndEdgeResize(); };
            PreviewMouseDown += (s, e) => ResetPetDelay();
            PreviewMouseWheel += (s, e) =>
            {
                ResetPetDelay();
                if (resizing || movingWindow || e.Delta == 0) return;
                // Treat closely spaced wheel events as one gesture, including inertial scrolling.
                if (lastWheelTimestamp.HasValue &&
                    unchecked((uint)(e.Timestamp - lastWheelTimestamp.Value)) < WheelPauseMilliseconds)
                {
                    lastWheelTimestamp = e.Timestamp;
                    e.Handled = true;
                    return;
                }

                lastWheelTimestamp = null;
                wheelRemainder -= e.Delta;
                int steps = wheelRemainder / Mouse.MouseWheelDeltaForOneLine;
                wheelRemainder %= Mouse.MouseWheelDeltaForOneLine;
                if (steps != 0 && BrowseOccasions(Math.Sign(steps)))
                {
                    wheelRemainder = 0;
                    lastWheelTimestamp = e.Timestamp;
                    e.Handled = true;
                }
            };
            PreviewKeyDown += (s, e) => ResetPetDelay();
            MouseEnter += (s, e) => UpdateControls();
            MouseLeave += (s, e) =>
            {
                if (!resizing) SetResizeCursor(WidgetEdge.None);
                UpdateControls();
            };
            KeyDown += (s, e) =>
            {
                if (e.Key == Key.F2) OpenSettings();
                if (e.Key == Key.F3) OpenUpcoming();
                if (e.Key == Key.PageDown || e.Key == Key.PageUp)
                    e.Handled = BrowseOccasions(e.Key == Key.PageDown ? 1 : -1);
            };
            LocationChanged += (s, e) => { if (ready && !resizing) ScheduleSave(); };
            Loaded += (s, e) =>
            {
                PlaceOnScreen();
                ready = true;
                ScheduleSave();
                if (IsCompanion) return;
                UpdateReminderTimer();
                ResetPetDelay();
                Dispatcher.BeginInvoke(new Action(RestoreCompanions), DispatcherPriority.Loaded);
            };
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
            Microsoft.Win32.SystemEvents.TimeChanged += OnTimeChanged;
            Microsoft.Win32.SystemEvents.PowerModeChanged += OnPowerModeChanged;
            tick.Tick += (s, e) => TickClock();
            reminderTick.Tick += (s, e) => CheckReminders();
            petDelay.Tick += (s, e) => EnterPetModeAfterDelay();
            save.Tick += (s, e) => Persist();
            IsVisibleChanged += (s, e) =>
            {
                if (!IsVisible)
                {
                    EndEdgeResize();
                    wheelRemainder = 0;
                    lastWheelTimestamp = null;
                }
                UpdateClockTimer();
                ResetPetDelay();
                if (!IsVisible && !quitting)
                {
                    if (companionSettings != null) companionSettings.Close();
                    else if (settingsWindow != null) settingsWindow.Close();
                    else Persist();
                }
                UpdateMotion();
            };
            Closing += (s, e) =>
            {
                if (!quitting) { e.Cancel = true; HideWidget(); }
                else if (resizing) EndEdgeResize();
                else Persist();
            };
            Closed += (s, e) =>
            {
                Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
                Microsoft.Win32.SystemEvents.TimeChanged -= OnTimeChanged;
                Microsoft.Win32.SystemEvents.PowerModeChanged -= OnPowerModeChanged;
                tick.Stop(); reminderTick.Stop(); petDelay.Stop(); save.Stop();
                if (pendingResizePoint.HasValue) CompositionTarget.Rendering -= ResizeOnRender;
                if (pet != null) { pet.Stop(); pet.Close(); }
                if (music != null) music.Dispose();
                if (playlist != null) playlist.Dispose();
                if (!IsCompanion)
                {
                    CloseCompanions();
                    tray.Dispose(); trayMenu.Dispose(); icon.Dispose();
                    System.Windows.Application.Current.Shutdown();
                }
            };
            UpdateClock();
        }

        internal void ShowWidget() { ShowWidget(true); }

        private void ShowWidget(bool activate)
        {
            if (pet != null) pet.Stop();
            if (IsCompanion) profile.Hidden = false;
            ShowActivated = activate;
            Show();
            PlaceOnScreen(IsCompanion);
            if (activate) Activate();
            if (IsCompanion) Persist();
            ResetPetDelay();
        }

        private void HideWidget()
        {
            if (IsCompanion) profile.Hidden = true;
            petDelay.Stop();
            if (pet != null) pet.Stop();
            Hide();
        }

        private void ShowPet(bool keepClock)
        {
            if (quitting || !config.PetEnabled) return;
            if (pet == null) pet = new TaskbarPet(config);
            if (keepClock)
            {
                if (!IsVisible) { Show(); PlaceOnScreen(); }
                if (settingsWindow != null) settingsWindow.Close();
            }
            else Hide();
            pet.Start();
            ResetPetDelay();
        }

        private void ResetPetDelay()
        {
            petDelay.Stop();
            if (IsCompanion || quitting || !ready || !IsVisible || settingsWindow != null || !config.PetEnabled ||
                (pet != null && pet.IsRunning)) return;
            petDelay.Interval = TimeSpan.FromMinutes(config.PetDelayMinutes);
            petDelay.Start();
        }

        private void EnterPetModeAfterDelay()
        {
            petDelay.Stop();
            if (!IsVisible || settingsWindow != null) return;
            if (movingWindow || resizing || IsMouseCaptureWithin) ResetPetDelay();
            else ShowPet(false);
        }

        private void OnDisplaySettingsChanged(object sender, EventArgs args)
        {
            Dispatcher.BeginInvoke(new Action(() => { if (IsVisible) PlaceOnScreen(); }));
        }

        private void OnTimeChanged(object sender, EventArgs args)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (quitting) return;
                if (IsVisible) UpdateClock();
                if (!IsCompanion)
                {
                    completedReminderDay = null;
                    CheckReminders();
                }
            }));
        }

        private void PlaceOnScreen(bool preservePosition = false)
        {
            var source = PresentationSource.FromVisual(this);
            if (source == null) return;
            var hwnd = new WindowInteropHelper(this).Handle;
            var inverse = source.CompositionTarget.TransformFromDevice;
            var screen = Forms.Screen.FromHandle(hwnd);
            var point = inverse.Transform(new Point(screen.WorkingArea.Right, screen.WorkingArea.Bottom));
            if (!ready)
            {
                Left = config.X ?? point.X - Width - 22;
                Top = config.Y ?? point.Y - Height - 22;
            }
            foreach (var monitor in Forms.Screen.AllScreens)
            {
                var area = monitor.WorkingArea;
                var start = inverse.Transform(new Point(area.Left, area.Top));
                var end = inverse.Transform(new Point(area.Right, area.Bottom));
                if (Left >= start.X && Top >= start.Y && Left + Width <= end.X && Top + Height <= end.Y) return;
            }
            if (preservePosition)
            {
                var start = inverse.Transform(new Point(screen.WorkingArea.Left, screen.WorkingArea.Top));
                Left = Math.Max(start.X, Math.Min(Left, point.X - Width));
                Top = Math.Max(start.Y, Math.Min(Top, point.Y - Height));
                return;
            }
            Left = point.X - Width - 22;
            Top = point.Y - Height - 22;
        }

        private void ScheduleSave() { save.Stop(); save.Start(); }
        private bool Persist()
        {
            save.Stop();
            if (IsCompanion) return PersistCompanion();
            if (changingCompanionVisibility) return true;
            CollectCompanionPlacement();
            if (ready)
            {
                var size = WidgetSize.Clock(config.Horizontal);
                config.X = Left + Width - size.Width * config.Scale;
                config.Y = Top + Height - size.Height * config.Scale;
            }
            bool saved = file.Save(config);
            if (saved && !quitting)
                foreach (var window in companions.Values.Where(item => config.AdditionalWidgets.Contains(item.profile)))
                    window.SyncFromHost();
            return saved;
        }

        private void SetScale(double scale)
        {
            config.Scale = WidgetSize.Clamp(scale);
            ResizeWidget(false);
            bool useCompact = !config.Horizontal && config.Scale <= WidgetSize.CompactScale;
            if (useCompact != compact) ApplyFaceTypography(useCompact);
            if (sizeOption != null) sizeOption.Value = config.Scale * 100;
        }

        private void ResizeWidget(bool keepCorner)
        {
            var size = WidgetSize.Clock(config.Horizontal);
            double dx = Width - size.Width * config.Scale;
            double dy = Height - size.Height * config.Scale;
            Width = size.Width * config.Scale;
            Height = size.Height * config.Scale;
            if (keepCorner && (dx != 0 || dy != 0))
            {
                Left += dx;
                Top += dy;
                PlaceOnScreen(true);
            }
        }
        private static SolidColorBrush ColorBrush(string value) { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(value)); }

        private static TextBlock Label(string value, double size, bool serif)
        {
            var text = new TextBlock { Text = value, FontSize = size,
                FontFamily = serif ? DisplayFont : new FontFamily("Segoe UI"),
                Foreground = ColorBrush("#F5DCA8"), TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center };
            Typography.SetNumeralAlignment(text, FontNumeralAlignment.Tabular);
            Typography.SetNumeralStyle(text, FontNumeralStyle.Lining);
            return text;
        }

        private static void SetCounterTypeface(TextBlock number)
        {
            number.FontFamily = CounterFont;
            number.FontWeight = FontWeights.SemiBold;
        }

        private void ApplyGlass()
        {
            glass.Background = HolidayGlassPaint(currentPalette, config.Horizontal, config.Glass / 100.0);
        }

        private sealed class HolidayPalette
        {
            internal readonly string From, Middle, To, Ink, Accent;

            internal HolidayPalette(string from, string middle, string to, string ink, string accent)
            {
                From = from;
                Middle = middle;
                To = to;
                Ink = ink;
                Accent = accent;
            }
        }

        private static HolidayPalette Palette(HolidayTheme? theme)
        {
            switch (theme)
            {
                case HolidayTheme.LunarNewYear: return new HolidayPalette("#763A45", "#451E2B", "#26151E", "#FAEEE2", "#E6BE8A");
                case HolidayTheme.NewYear: return new HolidayPalette("#263C60", "#172541", "#121C34", "#E8F2FF", "#BBD0F3");
                case HolidayTheme.HungKings: return new HolidayPalette("#69472E", "#422C25", "#281F21", "#FFE3B5", "#E7BA85");
                case HolidayTheme.Reunification: return new HolidayPalette("#4B6756", "#2E463E", "#1B302C", "#FFF1CE", "#EAC583");
                case HolidayTheme.LabourDay: return new HolidayPalette("#3E5870", "#293D56", "#1B2B3D", "#FFEDDA", "#F4B184");
                case HolidayTheme.NationalDay: return new HolidayPalette("#9A3239", "#60262E", "#3C1C29", "#FFF1CB", "#F7CD78");
                case HolidayTheme.VietnamCultureDay: return new HolidayPalette("#2C625C", "#224B4B", "#162D33", "#FFF2D2", "#F1C98B");
                case HolidayTheme.MidAutumn: return new HolidayPalette("#524269", "#342B51", "#27223F", "#FFF1CB", "#F6BD8B");
                case HolidayTheme.Christmas: return new HolidayPalette("#346352", "#244841", "#172E2C", "#FFF5DE", "#EBC58B");
                case HolidayTheme.Birthday: return new HolidayPalette("#582D4D", "#3D2038", "#221322", "#FFF1DF", "#FFDC96");
                case HolidayTheme.PersonalDay: return new HolidayPalette("#44385F", "#332747", "#221A31", "#FFF2E8", "#F6D297");
                default: return new HolidayPalette("#4A333E", "#2F222B", "#1C151A", "#FAEEE2", "#E6BE8A");
            }
        }

        private static LinearGradientBrush HolidayGlassPaint(HolidayPalette palette, bool horizontal, double glass)
        {
            var start = new Point(0, 0);
            var end = horizontal ? new Point(1, .12) : new Point(.2, 1);
            // The illustrated end follows the glass slider. The text end needs contrast on light desktops.
            var brush = new LinearGradientBrush(Tint(palette.From, .26 + .74 * glass), Tint(palette.To, .78 + .22 * glass), start, end);
            brush.GradientStops.Insert(1, new GradientStop(Tint(palette.Middle, .67 + .33 * glass), horizontal ? .32 : .29));
            brush.Freeze();
            return brush;
        }

        internal static Brush HolidayGlassPaint(HolidayTheme theme)
        {
            return HolidayGlassPaint(Palette(theme), false, 1);
        }

        private static Color Tint(string value, double opacity)
        {
            var color = (Color)ColorConverter.ConvertFromString(value);
            color.A = (byte)Math.Round(255 * opacity);
            return color;
        }

        private void ApplyHolidayTheme(HolidayTheme? theme)
        {
            currentPalette = theme == HolidayTheme.LunarNewYear && config.ApricotNewYear
                ? new HolidayPalette("#8C4A36", "#58282B", "#301923", "#FFF0D9", "#EDC689")
                : Palette(theme);
            var palette = currentPalette;
            var caption = ColorBrush(palette.Ink);
            var small = ColorBrush(palette.Accent);
            glass.BorderBrush = new SolidColorBrush(Tint(palette.Accent, .70));
            inlay.BorderBrush = new SolidColorBrush(Tint(palette.Accent, .31));
            year.Foreground = caption;
            dayUnit.Foreground = small;
            dateStatus.Foreground = small;
            days.Foreground = caption;
            greetingText.Foreground = caption;
            greetingDetail.Foreground = small;
            for (int i = 0; i < time.Length; i++)
            {
                time[i].Foreground = caption;
                clockUnits[i].Foreground = small;
            }
            var dividerPaint = new LinearGradientBrush(Tint(palette.Accent, 0), Tint(palette.Accent, 0), 0);
            dividerPaint.GradientStops.Insert(1, new GradientStop(Tint(palette.Accent, .63), .5));
            dividerPaint.Freeze();
            dividerRule.Background = dividerPaint;
            dividerSeal.Background = small;
            var buttonFill = ColorBrush(palette.To);
            foreach (var button in controls.Children.OfType<Button>())
            {
                button.Foreground = caption;
                button.Background = buttonFill;
                button.BorderBrush = glass.BorderBrush;
            }
            ApplyGlass();
            ApplyPinPalette();
            holidayScene.ShowTheme(theme, config.ApricotNewYear, config.Use3D);
            UpdateMotion();
        }

        private void BuildFace()
        {
            year.TextTrimming = TextTrimming.CharacterEllipsis;
            year.FontFamily = DisplayFont;
            year.TextWrapping = TextWrapping.NoWrap;
            dayUnit.FontWeight = FontWeights.SemiBold;
            titleLabel.Children.Add(new Viewbox { Child = year, StretchDirection = StretchDirection.DownOnly,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
            face.Children.Add(titleLabel);
            dayCount.Children.Add(days);
            dayCount.Children.Add(dayUnit);
            dayCount.VerticalAlignment = VerticalAlignment.Center;
            face.Children.Add(dayCount);
            divider.Children.Add(dividerRule);
            divider.Children.Add(dividerSeal);
            face.Children.Add(divider);
            string[] units = { "GIỜ", "PHÚT", "GIÂY" };
            for (int i = 0; i < time.Length; i++)
            {
                clock.ColumnDefinitions.Add(new ColumnDefinition());
                var pair = new StackPanel();
                SetCounterTypeface(time[i]);
                pair.Children.Add(time[i]);
                var unit = Label(units[i], 8.75, false);
                clockUnits[i] = unit;
                pair.Children.Add(unit);
                Grid.SetColumn(pair, i);
                clock.Children.Add(pair);
            }
            face.Children.Add(clock);
            dateStatus.FontSize = 10;
            dateStatus.Margin = new Thickness(0, 6, 0, 0);

            face.Children.Add(dateStatus);
            surface.Children.Add(face);
            greetingText.TextWrapping = TextWrapping.Wrap;
            greetingDetail.Margin = new Thickness(0, 5, 0, 0);
            greetingDetail.TextTrimming = TextTrimming.CharacterEllipsis;
            greeting.Children.Add(greetingText);
            greeting.Children.Add(greetingDetail);
            surface.Children.Add(greeting);
            surface.Children.Add(pinnedBar);
        }

        private void ApplyOrientation()
        {
            bool horizontal = config.Horizontal;
            var size = WidgetSize.Clock(horizontal);
            double radius = horizontal ? 14 : 18;
            glass.Width = size.Width;
            glass.Height = size.Height;
            glass.CornerRadius = new CornerRadius(radius);
            inlay.CornerRadius = new CornerRadius(radius - 5);
            surface.Clip = new RectangleGeometry(new Rect(0, 0, size.Width - 2, size.Height - 2), radius - 1, radius - 1);
            ApplyGlass();

            face.RowDefinitions.Clear();
            face.ColumnDefinitions.Clear();
            if (horizontal)
            {
                face.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(HorizontalArtworkWidth) });
                face.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(94) });
                face.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
                face.ColumnDefinitions.Add(new ColumnDefinition());
            }
            else
            {
                for (int i = 0; i < face.Children.Count; i++)
                    face.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }
            for (int i = 0; i < face.Children.Count; i++) Grid.SetRow(face.Children[i], horizontal ? 0 : i);
            Grid.SetColumn(dayCount, horizontal ? 1 : 0);
            Grid.SetColumn(divider, horizontal ? 2 : 0);
            Grid.SetColumn(clock, horizontal ? 3 : 0);

            titleLabel.VerticalAlignment = horizontal ? VerticalAlignment.Bottom : VerticalAlignment.Center;
            titleLabel.Margin = horizontal ? new Thickness(8, 0, 4, 6) : new Thickness(0, 0, 0, 3);
            divider.HorizontalAlignment = HorizontalAlignment.Center;
            divider.VerticalAlignment = VerticalAlignment.Center;
            divider.LayoutTransform = horizontal ? new RotateTransform(90) : Transform.Identity;
            clock.VerticalAlignment = VerticalAlignment.Center;
            ApplyFaceTypography(!horizontal && config.Scale <= WidgetSize.CompactScale);
            if (ready) ResizeWidget(true);
        }

        private void ApplyFaceTypography(bool useCompact)
        {
            compact = useCompact;
            holidayScene.SetCompact(useCompact);
            holidayScene.SetPinsVisible(PinsVisible);
            bool horizontal = config.Horizontal;
            pinnedBar.Margin = new Thickness(horizontal ? HorizontalArtworkWidth + 8 : 11, 0, 11, PinBarBottom);
            double bottomSpace = horizontal && PinsVisible ? PinBarHeight + 10 : 0;
            clock.Margin = new Thickness(0, 0, 0, bottomSpace);
            divider.Margin = clock.Margin;
            divider.Width = horizontal ? (PinsVisible ? 35 : 52) : 146;
            bool personal = PersonalDate.IsTheme(displayedTheme);
            bool personalVertical = personal && !horizontal;
            bool model = config.Use3D && HolidayDiorama.Supports(displayedTheme);
            face.VerticalAlignment = horizontal ? VerticalAlignment.Stretch : VerticalAlignment.Top;
            double artworkHeight = model ? HolidayDiorama.CountdownInset(compact) :
                personal ? (compact ? 82 : 92) : (compact ? 44 : 51);
            face.Margin = horizontal ? new Thickness(0, 0, 14, 0) : new Thickness(15, artworkHeight, 15, 0);
            titleLabel.Height = horizontal || personalVertical ? 17 : compact ? 23 : 19;
            year.FontSize = horizontal ? 12 : personalVertical ? 13 : compact ? 17 : 14;
            double minTitleSize = horizontal ? 9.5 : compact ? 13 : 11;
            double titleWidth = horizontal ? HorizontalArtworkWidth :
                glass.Width - glass.BorderThickness.Left - glass.BorderThickness.Right - face.Margin.Left - face.Margin.Right;
            titleWidth -= titleLabel.Margin.Left + titleLabel.Margin.Right;
            // At the smallest font size, extra text is trimmed inside the single-line slot.
            year.MaxWidth = titleWidth * year.FontSize / minTitleSize;
            UpdateGreetingTypography();
            greeting.Margin = horizontal ? new Thickness(128, 0, 19, 0) : new Thickness(16, personal ? 97 : 42, 16, 0);
            days.Height = horizontal ? 49 : personalVertical ? (compact ? 71 : 54) : compact ? 91 : 73;
            dayUnit.FontSize = horizontal ? 9 : compact ? 11 : 9.5;
            dayCount.Margin = horizontal ? new Thickness(2, 0, 2, 0) : new Thickness(2, 1, 2, personalVertical || model ? 2 : 8);
            divider.Height = personalVertical || (model && !horizontal) ? 6 : 11;
            foreach (var number in time) number.FontSize = horizontal ? 23 : personalVertical ? 21 : 25;
            dateStatus.FontSize = personalVertical ? 9 : 10;
            dateStatus.Margin = new Thickness(0, model || personalVertical ? 3 : 6, 0, 0);
            dateStatus.Visibility = horizontal || compact ? Visibility.Collapsed : Visibility.Visible;
            if (PinsVisible)
            {
                greeting.Margin = new Thickness(greeting.Margin.Left, greeting.Margin.Top, greeting.Margin.Right,
                    horizontal ? bottomSpace : PinBarHeight + 4);
                days.Height = horizontal ? 31 : compact ? 67 : 48;
                dayCount.Margin = new Thickness(2, horizontal ? 0 : 1, 2, bottomSpace);
                if (personalVertical) face.Margin = new Thickness(15, compact ? 72 : 78, 15, 0);
                if (!horizontal) divider.Height = 6;
                foreach (var number in time) number.FontSize = personalVertical ? 19 : 20;
                dateStatus.Margin = new Thickness(0, 1, 0, 0);
            }
            if (unavailableStatus != null) year.Text = horizontal || compact ? unavailableStatus : ProductName;
            UpdateDayPresentation();
        }

        private void UpdateGreetingTypography()
        {
            bool cultureDay = displayedTheme == HolidayTheme.VietnamCultureDay;
            greetingText.FontSize = config.Horizontal ? (cultureDay ? 17.5 : 20) : 26;
        }

        private void UpdateDayPresentation()
        {
            bool abbreviated = compact || config.Horizontal;
            bool wideNumber = days.Text.Length >= 4;
            if (PinsVisible)
                days.FontSize = config.Horizontal ? (wideNumber ? 25 : days.Text.Length == 3 ? 29 : 32) :
                    wideNumber ? (compact ? 48 : 38) : compact ? 61 : 45;
            else if (config.Horizontal)
                days.FontSize = wideNumber ? 32 : days.Text.Length == 3 ? 40 : 46;
            else if (PersonalDate.IsTheme(displayedTheme))
                days.FontSize = wideNumber ? 47 : compact ? 64 : 55;
            else days.FontSize = wideNumber ? 62 : compact ? 87 : 70;
            string symbol = displayedSeconds < 0 ? "NGÀY" : CountdownUnits.Symbol(config.CounterMode);
            if (displayedExtraDays > 0 && displayedSeconds > 0)
                symbol += " · " + displayedExtraDays.ToString(CultureInfo.InvariantCulture) + " NGÀY";
            dayUnit.Text = abbreviated && days.Text == "–" ? (unavailableStatus == "Hết dữ liệu lịch" ? "HẾT DỮ LIỆU" : "NGÀY") :
                abbreviated && displayedSeconds == 0 ? (displayedTheme == HolidayTheme.LunarNewYear ? "TẾT ĐÃ ĐẾN" : "HÔM NAY") : symbol;
        }

        private void UpdateClockTimer()
        {
            if (!IsVisible)
            {
                tick.Stop();
                return;
            }
            TickClock();
            tick.Start();
        }

        private void TickClock()
        {
            var now = DateTimeOffset.UtcNow;
            UpdateClock(now);
            UpdateDim(now.LocalDateTime);
            var period = displayedSeconds > 0 ? TimeSpan.TicksPerSecond : TimeSpan.TicksPerMinute;
            var remaining = period - now.UtcTicks % period;
            tick.Interval = TimeSpan.FromMilliseconds(Math.Ceiling((double)remaining / TimeSpan.TicksPerMillisecond));
        }

        private void UpdateClock() { TickClock(); }

        private bool BrowseOccasions(int steps)
        {
            if (!IsVisible) return false;
            var now = DateTimeOffset.UtcNow;
            var local = now.ToOffset(HolidayCountdown.VietnamOffset);
            var current = GetHoliday(local);
            var choices = HolidayCountdown.UpcomingTracked(config, local).ToArray();
            if (!current.HasValue || choices.Length < 2) return false;

            int index = Array.FindIndex(choices, item => item.Id == current.Value.Id);
            int next = ((index + steps) % choices.Length + choices.Length) % choices.Length;
            config.DisplayedOccurrenceKey = choices[next].Key;
            TickClock();
            ScheduleSave();
            return true;
        }

        private HolidayOccurrence? GetHoliday(DateTimeOffset local)
        {
            int fixedYear = config.Automatic ? 0 : config.Year;
            if (holidayLookupDate != local.Date || holidayLookupSelection != config.SelectedHolidays ||
                holidayLookupPersonalDates != config.PersonalDates || holidayLookupFixedYear != fixedYear ||
                holidayLookupKey != config.DisplayedOccurrenceKey || holidayLookupPinIds != config.PinnedOccasionIds)
            {
                holidayLookupResult = HolidayCountdown.FindForDay(config, local);
                if (config.DisplayedOccurrenceKey != null &&
                    (!holidayLookupResult.HasValue || holidayLookupResult.Value.LastDay.Date < local.Date ||
                    holidayLookupResult.Value.Key != config.DisplayedOccurrenceKey))
                {
                    config.DisplayedOccurrenceKey = null;
                    ScheduleSave();
                }
                holidayLookupKey = config.DisplayedOccurrenceKey;
                holidayLookupDate = local.Date;
                holidayLookupSelection = config.SelectedHolidays;
                holidayLookupPersonalDates = config.PersonalDates;
                holidayLookupFixedYear = fixedYear;
                holidayLookupPinIds = config.PinnedOccasionIds;
                ReadSecondaryPins(local, holidayLookupResult);
            }
            return holidayLookupResult;
        }

        private void UpdateClock(DateTimeOffset now)
        {
            var local = now.ToOffset(HolidayCountdown.VietnamOffset);
            if (settingsWindow != null) settingsWindow.RefreshUpcoming(local);
            var unit = config.CounterMode;
            bool unitChanged = unit != displayedUnit;
            var occurrence = GetHoliday(local);
            bool pinLayoutChanged = RefreshPinnedBar(local.Date);
            var target = occurrence.HasValue ? (DateTimeOffset?)occurrence.Value.Date : null;
            var theme = occurrence.HasValue ? (HolidayTheme?)occurrence.Value.Theme : null;
            var name = occurrence.HasValue ? occurrence.Value.Name : null;
            string unavailable = target.HasValue ? null :
                config.SelectedHolidays.Length == 0 && !config.PersonalDates.Any(date => date.Enabled)
                    ? "Chưa chọn ngày nghỉ" : "Hết dữ liệu lịch";
            var seconds = target.HasValue ? Calendar.Seconds(target.Value, now) : -1;
            string greetingTitle = occurrence.HasValue && seconds == 0 && local.Date <= occurrence.Value.LastDay.Date ? GreetingFor(theme) : null;
            bool showGreeting = greetingTitle != null;
            bool themeChanged = theme != displayedTheme || unavailable != unavailableStatus;
            bool targetChanged = target != displayedTarget || themeChanged || name != displayedName;
            if (!targetChanged && !unitChanged && !pinLayoutChanged && seconds == displayedSeconds && celebrating == showGreeting) return;

            bool dateChanged = targetChanged || (seconds == 0) != (displayedSeconds == 0);
            bool quantityChanged = targetChanged || unitChanged || seconds / 86400 != displayedSeconds / 86400;
            displayedTarget = target;
            displayedSeconds = seconds;
            displayedTheme = theme;
            displayedName = name;
            displayedUnit = unit;
            unavailableStatus = unavailable;
            if (themeChanged) ApplyHolidayTheme(theme);
            if (themeChanged || pinLayoutChanged) ApplyFaceTypography(compact);
            if (dateChanged && settingsWindow != null)
                settingsWindow.ShowOccurrence(name, target, seconds == 0, unavailable);
            if (targetChanged || celebrating != showGreeting) UpdateGreeting(greetingTitle, theme, target, local);
            if (!target.HasValue)
            {
                year.Text = config.Horizontal || compact ? unavailable : ProductName;
                displayedExtraDays = 0;
                days.Text = "–";
                foreach (var label in time) label.Text = "–";
                dateStatus.Text = unavailable;
                ToolTip = unavailable;
                UpdateDayPresentation();
                return;
            }
            var day = target.Value;
            if (targetChanged)
            {
                year.Text = theme == HolidayTheme.LunarNewYear ? Calendar.Name(day.Year) + " · " + day.Year : name;
                ToolTip = year.Text + " · " + day.ToString("dd/MM/yyyy") + " · Giờ Việt Nam\nCuộn hoặc nhấn Page Down / Page Up để xem các dịp đang theo dõi.";
            }
            if (quantityChanged)
            {
                var quantity = CountdownUnits.Measure(unit, day, seconds);
                displayedExtraDays = quantity.ExtraDays;
                SetNumber(days, quantity.WholeUnits.ToString(CultureInfo.InvariantCulture));
            }
            SetNumber(time[0], clockNumbers[seconds / 3600 % 24]);
            SetNumber(time[1], clockNumbers[seconds / 60 % 60]);
            SetNumber(time[2], clockNumbers[seconds % 60]);
            if (dateChanged)
                dateStatus.Text = seconds == 0 ? (theme == HolidayTheme.LunarNewYear ? "Tết này đã đến" : "Hôm nay") :
                    day.ToString("dd/MM/yyyy") + " · Giờ Việt Nam";
            if (quantityChanged || dateChanged) UpdateDayPresentation();
        }

        private static string GreetingFor(HolidayTheme? theme)
        {
            switch (theme)
            {
                case HolidayTheme.LunarNewYear: return "Chúc mừng năm mới";
                case HolidayTheme.MidAutumn: return "Vui Tết Trung thu";
                case HolidayTheme.Christmas: return "Giáng sinh an lành";
                case HolidayTheme.VietnamCultureDay: return HolidaySetting.VietnamCultureDayName;
                case HolidayTheme.Birthday: return "Chúc mừng sinh nhật!";
                case HolidayTheme.PersonalDay: return "Mừng ngày đặc biệt!";
                default: return null;
            }
        }

        private void UpdateGreeting(string title, HolidayTheme? theme, DateTimeOffset? target, DateTimeOffset now)
        {
            celebrating = title != null;
            if (celebrating)
            {
                greetingText.Text = title;
                greetingDetail.Text = theme == HolidayTheme.LunarNewYear ? Calendar.Name(target.Value.Year) + " · " + target.Value.Year :
                    now.ToString("dd/MM");
                if ((theme == HolidayTheme.Birthday && displayedName != PersonalDate.BirthdayName) ||
                    (theme == HolidayTheme.PersonalDay && displayedName != PersonalDate.SpecialDayName))
                    greetingDetail.Text = displayedName + " · " + greetingDetail.Text;
                greetingDetail.ToolTip = greetingDetail.Text;
            }
            greeting.Visibility = celebrating ? Visibility.Visible : Visibility.Collapsed;
            face.Visibility = celebrating ? Visibility.Hidden : Visibility.Visible;
            holidayScene.SetCelebration(celebrating);
            UpdateMotion();
        }

        private static void SetNumber(TextBlock view, string value)
        {
            if (view.Text != value) view.Text = value;
        }

        private bool InControl(DependencyObject item)
        {
            while (item != null && item != surface)
            {
                if (item is System.Windows.Controls.Primitives.ButtonBase) return true;
                item = item is Visual ? VisualTreeHelper.GetParent(item) : null;
            }
            return false;
        }

        private Button MakeButton(string name, string tooltip, Action action)
        {
            var button = new Button { Content = name, ToolTip = tooltip, FontSize = 11,
                Foreground = ColorBrush("#F5DCA8"), Background = ColorBrush("#BF341A25"),
                BorderBrush = ColorBrush("#9AD6AA80"), Padding = new Thickness(5, 2, 5, 2),
                Margin = new Thickness(0, 0, 4, 0), MinHeight = 22, Cursor = Cursors.Hand };
            AutomationProperties.SetName(button, tooltip);
            if (action != null) button.Click += (s, e) => action();
            return button;
        }

        private void BuildControls()
        {
            controls.Margin = new Thickness(7, 6, 0, 0);
            controls.VerticalAlignment = VerticalAlignment.Top;
            controls.HorizontalAlignment = HorizontalAlignment.Left;
            controls.Opacity = 0;
            controls.Children.Add(MakeButton("⚙", "Cài đặt (F2)", OpenSettings));
            controls.Children.Add(MakeButton("×", "Ẩn widget", HideWidget));
            surface.Children.Add(controls);
        }

        private void BuildCountdownSettings(StackPanel fields)
        {
            fields.Children.Add(PanelPage.SectionHeading("Đếm ngược", settingsWindow, 20));
            var units = new ComboBox { Name = "CounterUnit", Width = 184, MinHeight = 30, FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Right };
            System.Windows.Automation.AutomationProperties.SetName(units, "Đơn vị hiển thị");
            foreach (CountdownUnit unit in Enum.GetValues(typeof(CountdownUnit)))
                units.Items.Add(new ComboBoxItem { Content = CountdownUnits.Label(unit), Tag = unit });
            units.SelectedIndex = (int)config.CounterMode;
            AddOptionRow(fields, units, "Đơn vị hiển thị");

            var select = new ComboBox { Width = 184, MinHeight = 30, FontSize = 14, HorizontalAlignment = HorizontalAlignment.Right };
            System.Windows.Automation.AutomationProperties.SetName(select, "Năm Tết");
            select.SetBinding(ToolTipProperty, new Binding("SelectionBoxItem") { Source = select });
            select.Items.Add(new ComboBoxItem { Content = "Tết kế tiếp" });
            foreach (var d in Calendar.Holidays)
                select.Items.Add(new ComboBoxItem { Content = d.Year + " · " + Calendar.Name(d.Year), Tag = d.Year });
            select.SelectedIndex = config.Automatic ? 0 : Math.Max(0, 1 + Array.FindIndex(Calendar.Holidays, day => day.Year == config.Year));
            select.SelectionChanged += (s, e) =>
            {
                var choice = select.SelectedItem as ComboBoxItem;
                if (choice == null) return;
                config.Automatic = choice.Tag == null;
                if (!config.Automatic) config.Year = (int)choice.Tag;
                completedReminderDay = null;
                UpdateClock();
                Persist();
            };
            AddOptionRow(fields, select, "Sự kiện");
            units.SelectionChanged += (s, e) =>
            {
                var choice = units.SelectedItem as ComboBoxItem;
                if (choice == null) return;
                config.CounterMode = (CountdownUnit)choice.Tag;
                UpdateClock();
                Persist();
            };
        }

        private FrameworkElement BuildSettings()
        {
            var muted = (Brush)settingsWindow.FindResource("PanelMuted");
            var fields = new StackPanel();
            AddCompanionSetting(fields);
            BuildCountdownSettings(fields);
            fields.Children.Add(PanelPage.SectionHeading("Hiển thị", settingsWindow, 20));
            var layout = new ComboBox { Width = 112, MinHeight = 30, FontSize = 14, HorizontalAlignment = HorizontalAlignment.Right };
            System.Windows.Automation.AutomationProperties.SetName(layout, "Bố cục");
            layout.Items.Add(new ComboBoxItem { Content = "Dọc" });
            layout.Items.Add(new ComboBoxItem { Content = "Ngang" });
            layout.SelectedIndex = config.Horizontal ? 1 : 0;
            layout.SelectionChanged += (s, e) =>
            {
                config.Horizontal = layout.SelectedIndex == 1;
                ApplyOrientation();
                Persist();
            };
            AddOptionRow(fields, layout, "Bố cục");
            AddSizeOption(fields);
            var glassControl = new DockPanel();
            var valueLabel = new TextBlock { Text = config.Glass + "%", FontSize = 13, Foreground = muted,
                Width = 48, TextAlignment = TextAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            DockPanel.SetDock(valueLabel, Dock.Right);
            glassControl.Children.Add(valueLabel);
            var slider = new Slider { Minimum = 42, Maximum = 100, Value = config.Glass, TickFrequency = 1,
                IsSnapToTickEnabled = true, IsMoveToPointEnabled = true, VerticalAlignment = VerticalAlignment.Center };
            System.Windows.Automation.AutomationProperties.SetName(slider, "Độ đậm kính");
            slider.ValueChanged += (s, e) =>
            {
                config.Glass = (int)e.NewValue;
                valueLabel.Text = config.Glass + "%";
                ApplyGlass();
                ScheduleSave();
            };
            glassControl.Children.Add(slider);
            AddOptionRow(fields, glassControl, "Độ đậm kính");

            var model = MakeSwitch("Mô hình 3D", "Bật cảnh 3D, tắt để dùng hình 2D", config.Use3D, value =>
            {
                config.Use3D = value;
                holidayScene.ShowTheme(displayedTheme, config.ApricotNewYear, value);
                ApplyFaceTypography(compact);
            });
            model.Name = "Use3D";
            AutomationProperties.SetName(model, "Mô hình 3D");
            AutomationProperties.SetHelpText(model, "Bật cảnh 3D, tắt để dùng hình 2D");
            AddOptionRow(fields, model);
            AddOptionRow(fields, MakeSwitch("Chuyển động", "Hiệu ứng chuyển động", config.Motion,
                value => { config.Motion = value; UpdateMotion(); }));
            AddOptionRow(fields, MakeSwitch("Luôn nổi", "Luôn nổi trên màn hình", config.AlwaysOnTop,
                value => { config.AlwaysOnTop = value; Topmost = value; }));

            AddPowerSettings(fields);
            fields.Children.Add(PanelPage.SectionHeading("Khởi động", settingsWindow, 20));
            AddStartupSetting(fields);
            AddBackupSettings(fields);
            optionsPage = PanelPage.Scroll(fields);
            return optionsPage;
        }

        private FrameworkElement BuildPetSettings()
        {
            var fields = new StackPanel();
            fields.Children.Add(PanelPage.SectionHeading("Thú cưng trên thanh tác vụ", settingsWindow, 0));
            double artSize = TaskbarPet.SizeInPixels(config.PetSizePercent, 96) * .72;
            var artwork = new PetArtwork { Width = artSize, Height = artSize };
            artwork.SetPose(config.PetAnimal);
            var artFrame = new Grid { Width = 78, Height = 78, Margin = new Thickness(0, 0, 12, 0) };
            artFrame.Children.Add(artwork);
            Func<PetAnimal, string> previewHint = animal => animal == PetAnimal.HacLong
                ? "Hắc Long đi qua lại dọc thanh tác vụ. Bấm nhẹ để xem tên, giữ chuột rồi kéo để đổi chỗ."
                : "Bấm nhẹ vào thú cưng để vuốt ve và xem tên. Giữ chuột rồi kéo để nhấc thú cưng lên.";
            var description = new TextBlock { Text = previewHint(config.PetAnimal),
                FontSize = 14, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
            description.SetResourceReference(TextBlock.ForegroundProperty, "PanelMuted");
            var preview = new DockPanel();
            preview.Children.Add(artFrame);
            preview.Children.Add(description);
            var card = new Border { Child = preview, CornerRadius = new CornerRadius(5), Padding = new Thickness(12, 3, 12, 3),
                Margin = new Thickness(0, 3, 0, 8) };
            card.SetResourceReference(Border.BackgroundProperty, "PanelField");
            fields.Children.Add(card);

            var status = new TextBlock { Margin = new Thickness(0, 8, 0, 0), FontSize = 13, TextWrapping = TextWrapping.Wrap,
                Visibility = Visibility.Collapsed };
            status.SetResourceReference(TextBlock.ForegroundProperty, "PanelError");
            AutomationProperties.SetLiveSetting(status, AutomationLiveSetting.Polite);
            Action<bool> showFailure = failed =>
            {
                status.Visibility = failed ? Visibility.Visible : Visibility.Collapsed;
                status.Text = failed ? "Không lưu được tùy chọn thú cưng. Hãy thử lại." : "";
                var peer = System.Windows.Automation.Peers.UIElementAutomationPeer.FromElement(status);
                if (failed && peer != null) peer.RaiseAutomationEvent(System.Windows.Automation.Peers.AutomationEvents.LiveRegionChanged);
            };

            var showNow = new Button { Name = "ShowPetNow", Content = "Hiện thú cưng ngay", MinHeight = 34,
                Margin = new Thickness(0, 0, 8, 4), IsEnabled = config.PetEnabled };
            showNow.Click += (s, e) => ShowPet(false);
            var showBoth = new Button { Name = "ShowPetAndClock", Content = "Hiện cả hai", MinHeight = 34,
                Margin = new Thickness(0, 0, 0, 4), IsEnabled = config.PetEnabled };
            showBoth.SetResourceReference(StyleProperty, "PanelPrimary");
            showBoth.Click += (s, e) => ShowPet(true);

            var enable = CreateSwitch("Bật chế độ thú cưng", "Bật chế độ thú cưng", config.PetEnabled);
            enable.Name = "EnableTaskbarPet";
            enable.Click += (s, e) =>
            {
                bool saved = SavePetOption(enable.IsChecked == true, config.PetEnabled, value => config.PetEnabled = value);
                enable.IsChecked = showNow.IsEnabled = showBoth.IsEnabled = config.PetEnabled;
                showFailure(!saved);
            };
            AddOptionRow(fields, enable);
            ComboBox movement = null;
            Func<PetAnimal, string> walkingLabel = animal => animal == PetAnimal.HacLong ? "Đi qua lại" : "Thỉnh thoảng";
            var animals = new[] { PetAnimal.Cat, PetAnimal.Dog, PetAnimal.HacLong };
            AddPetChoice(fields, "PetSpecies", "Loại thú cưng", config.PetAnimal,
                animals, animals.Select(TaskbarPet.SpeciesName).ToArray(), next =>
                {
                    var previous = config.PetAnimal;
                    if (!SavePetOption(next, previous, value => config.PetAnimal = value)) return false;
                    artwork.SetPose(next);
                    description.Text = previewHint(next);
                    ((ComboBoxItem)movement.Items[0]).Content = walkingLabel(next);
                    if (pet != null && (previous == PetAnimal.HacLong || next == PetAnimal.HacLong)) pet.ResetRoute();
                    return true;
                }, showFailure);
            var nameInput = new TextBox { Name = "PetNameInput", Text = config.PetName ?? "",
                MaxLength = TaskbarPet.MaxNameLength, MinWidth = 110, Margin = new Thickness(0, 0, 8, 0) };
            nameInput.SetResourceReference(StyleProperty, "SettingsTextInput");
            AutomationProperties.SetName(nameInput, "Tên thú cưng");
            var saveName = new Button { Name = "SavePetName", Content = "Lưu tên", MinHeight = 32 };
            saveName.Click += (s, e) =>
            {
                string name = TaskbarPet.NormalizeName(nameInput.Text);
                bool saved = SavePetOption(name, config.PetName, value => config.PetName = value);
                if (saved) nameInput.Text = config.PetName;
                showFailure(!saved);
            };
            var nameRow = new DockPanel { Width = 230, HorizontalAlignment = HorizontalAlignment.Right };
            DockPanel.SetDock(saveName, Dock.Right);
            nameRow.Children.Add(saveName);
            nameRow.Children.Add(nameInput);
            AddOptionRow(fields, nameRow, "Tên thú cưng");
            AddPetChoice(fields, "PetDelayMinutes", "Thời gian chờ", config.PetDelayMinutes,
                TaskbarPet.DelayOptions, TaskbarPet.DelayOptions.Select(value => value + " phút").ToArray(),
                next => SavePetOption(next, config.PetDelayMinutes, value => config.PetDelayMinutes = value), showFailure);
            movement = AddPetChoice(fields, "PetMovement", "Chuyển động", config.PetRoams,
                new[] { true, false }, new[] { walkingLabel(config.PetAnimal), "Ở yên" },
                next => SavePetOption(next, config.PetRoams, value => config.PetRoams = value), showFailure);
            AddPetChoice(fields, "PetPosition", "Vị trí đậu", config.PetAtEnd,
                new[] { false, true }, new[] { "Gần đầu thanh", "Gần cuối thanh" },
                next =>
                {
                    if (!SavePetOption(next, config.PetAtEnd, value => config.PetAtEnd = value)) return false;
                    if (pet != null) pet.ResetPerch();
                    return true;
                }, showFailure);
            var sizeControl = new DockPanel { Width = 230, HorizontalAlignment = HorizontalAlignment.Right };
            var percent = new TextBlock { Width = 48, FontSize = 13, TextAlignment = TextAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center, Text = config.PetSizePercent + "%" };
            percent.SetResourceReference(TextBlock.ForegroundProperty, "PanelMuted");
            DockPanel.SetDock(percent, Dock.Right);
            sizeControl.Children.Add(percent);
            var size = new Slider { Name = "PetSizePercent", Minimum = TaskbarPet.MinimumSizePercent,
                Maximum = TaskbarPet.MaximumSizePercent, Value = config.PetSizePercent,
                TickFrequency = 5, SmallChange = 5, LargeChange = 25, IsSnapToTickEnabled = true, IsMoveToPointEnabled = true,
                VerticalAlignment = VerticalAlignment.Center };
            AutomationProperties.SetName(size, "Kích thước thú cưng theo phần trăm");
            size.ValueChanged += (s, e) =>
            {
                config.PetSizePercent = (int)e.NewValue;
                percent.Text = config.PetSizePercent + "%";
                artwork.Width = artwork.Height = TaskbarPet.SizeInPixels(config.PetSizePercent, 96) * .72;
                ScheduleSave();
            };
            sizeControl.Children.Add(size);
            AddOptionRow(fields, sizeControl, "Kích thước thú cưng");
            fields.Children.Add(status);
            var actions = new WrapPanel { Margin = new Thickness(0, 14, 0, 0) };
            actions.Children.Add(showNow);
            actions.Children.Add(showBoth);
            fields.Children.Add(actions);
            return PanelPage.Scroll(fields);
        }

        private static ComboBox AddPetChoice<T>(StackPanel fields, string name, string label, T current, T[] values, string[] labels,
            Func<T, bool> apply, Action<bool> showFailure)
        {
            var picker = new ComboBox { Name = name, Width = 170, MinHeight = 32, FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Right };
            AutomationProperties.SetName(picker, label);
            foreach (string title in labels) picker.Items.Add(new ComboBoxItem { Content = title });
            int savedIndex = Array.IndexOf(values, current);
            picker.SelectedIndex = savedIndex;
            picker.SelectionChanged += (s, e) =>
            {
                int index = picker.SelectedIndex;
                if (index < 0 || index == savedIndex) return;
                bool saved = apply(values[index]);
                if (saved) savedIndex = index;
                else picker.SelectedIndex = savedIndex;
                showFailure(!saved);
            };
            AddOptionRow(fields, picker, label);
            return picker;
        }

        private bool SavePetOption<T>(T next, T previous, Action<T> apply)
        {
            apply(next);
            if (!Persist())
            {
                apply(previous);
                return false;
            }
            showPetItem.Enabled = config.PetEnabled;
            showBothItem.Enabled = config.PetEnabled;
            if (!config.PetEnabled && pet != null) pet.Stop();
            ResetPetDelay();
            return true;
        }

        private static void AddOptionRow(StackPanel fields, FrameworkElement control, string label = null)
        {
            FrameworkElement content = control;
            if (label != null)
            {
                var row = new DockPanel { Margin = new Thickness(12, 0, 12, 0), MinHeight = 44 };
                var caption = new TextBlock { Text = label, FontSize = 14, MinWidth = 132,
                    VerticalAlignment = VerticalAlignment.Center };
                caption.SetResourceReference(TextBlock.ForegroundProperty, "PanelInk");
                row.Children.Add(caption);
                control.VerticalAlignment = VerticalAlignment.Center;
                row.Children.Add(control);
                content = row;
            }

            var divider = new Border { Child = content, BorderThickness = new Thickness(0, 0, 0, 1) };
            divider.SetResourceReference(Border.BorderBrushProperty, "PanelDivider");
            fields.Children.Add(divider);
        }

        private void StartEdgeResize(WidgetEdge edge, Point screenPoint)
        {
            var source = PresentationSource.FromVisual(this);
            if (source == null || !CaptureMouse()) return;
            resizeEdge = edge;
            resizeStart = screenPoint;
            resizeBounds = new Rect(Left, Top, Width, Height);
            resizeFromDevice = source.CompositionTarget.TransformFromDevice;
            resizeScale = config.Scale;
            save.Stop();
            SetResizeCursor(edge);
        }

        private void QueueResizeAt(Point screenPoint)
        {
            // Only the latest pointer position matters for the next rendered frame.
            if (!pendingResizePoint.HasValue) CompositionTarget.Rendering += ResizeOnRender;
            pendingResizePoint = screenPoint;
        }

        private void ResizeOnRender(object sender, EventArgs args) { ApplyPendingResize(); }

        private void ApplyPendingResize()
        {
            if (!pendingResizePoint.HasValue) return;
            var point = pendingResizePoint.Value;
            pendingResizePoint = null;
            CompositionTarget.Rendering -= ResizeOnRender;
            ResizeAt(point);
        }

        private void ResizeAt(Point screenPoint)
        {
            var delta = resizeFromDevice.Transform(screenPoint - resizeStart);
            double scale = WidgetSize.ResizeScale(resizeScale, delta, resizeEdge, config.Horizontal);
            if (scale == config.Scale) return;
            SetScale(scale);
            var origin = WidgetSize.ResizeOrigin(resizeBounds, new Size(Width, Height), resizeEdge);
            Left = origin.X;
            Top = origin.Y;
        }

        private void EndEdgeResize()
        {
            if (!resizing) return;
            ApplyPendingResize();
            resizeEdge = WidgetEdge.None;
            if (IsMouseCaptured) ReleaseMouseCapture();
            SetResizeCursor(IsMouseOver ? WidgetSize.EdgeAt(Mouse.GetPosition(this), RenderSize) : WidgetEdge.None);
            if (IsVisible) PlaceOnScreen(true);
            Persist();
            ResetPetDelay();
        }

        private void SetResizeCursor(WidgetEdge edge)
        {
            Cursor next;
            switch (edge)
            {
                case WidgetEdge.Left:
                case WidgetEdge.Right: next = Cursors.SizeWE; break;
                case WidgetEdge.Top:
                case WidgetEdge.Bottom: next = Cursors.SizeNS; break;
                case WidgetEdge.Left | WidgetEdge.Top:
                case WidgetEdge.Right | WidgetEdge.Bottom: next = Cursors.SizeNWSE; break;
                case WidgetEdge.Right | WidgetEdge.Top:
                case WidgetEdge.Left | WidgetEdge.Bottom: next = Cursors.SizeNESW; break;
                default: next = null; break;
            }
            if (Cursor != next) Cursor = next;
        }

        private void AddSizeOption(StackPanel fields)
        {
            var value = new TextBlock { Width = 48, TextAlignment = TextAlignment.Right, FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center };
            value.SetResourceReference(TextBlock.ForegroundProperty, "PanelMuted");
            var slider = new Slider
            {
                Name = "WidgetScale",
                Minimum = WidgetSize.MinimumScale * 100,
                Maximum = WidgetSize.MaximumScale * 100,
                Value = config.Scale * 100,
                SmallChange = 5,
                LargeChange = 10,
                IsMoveToPointEnabled = true,
                VerticalAlignment = VerticalAlignment.Center
            };
            sizeOption = slider;
            AutomationProperties.SetName(slider, "Kích thước widget");
            value.SetBinding(TextBlock.TextProperty, new Binding("Value") { Source = slider, StringFormat = "{0:0}%" });
            var row = new DockPanel();
            DockPanel.SetDock(value, Dock.Right);
            row.Children.Add(value);
            row.Children.Add(slider);
            slider.ValueChanged += (s, e) =>
            {
                double scale = e.NewValue / 100;
                if (Math.Abs(scale - config.Scale) < .00001) return;
                SetScale(scale);
                PlaceOnScreen(true);
                ScheduleSave();
            };
            AddOptionRow(fields, row, "Kích thước widget");
        }

        private CheckBox MakeSwitch(string name, string tooltip, bool value, Action<bool> changed)
        {
            var input = CreateSwitch(name, tooltip, value);
            RoutedEventHandler update = (s, e) => { changed(input.IsChecked == true); Persist(); };
            input.Checked += update;
            input.Unchecked += update;
            return input;
        }

        private static CheckBox CreateSwitch(string name, string tooltip, bool value)
        {
            var input = new CheckBox { Content = new TextBlock { Text = name, TextWrapping = TextWrapping.Wrap },
                ToolTip = tooltip, IsChecked = value };
            System.Windows.Automation.AutomationProperties.SetName(input, tooltip);
            return input;
        }

        private static void AddStartupSetting(StackPanel fields)
        {
            var startup = StartupShortcut.ForCurrentUser();
            var input = CreateSwitch("Mở cùng Windows", "Tự mở " + ProductName + " khi đăng nhập Windows", false);
            AddOptionRow(fields, input);
            var message = new TextBlock { FontSize = 13, Foreground = ColorBrush("#FFD2D6"), Margin = new Thickness(12, 6, 12, 0),
                TextWrapping = TextWrapping.Wrap, Visibility = Visibility.Collapsed };
            fields.Children.Add(message);

            bool refreshing = false;
            Action<bool> refresh = failed =>
            {
                var enabled = startup.ReadEnabled();
                refreshing = true;
                input.IsChecked = enabled.GetValueOrDefault();
                input.IsEnabled = enabled.HasValue;
                refreshing = false;
                message.Text = failed ? "Không đổi được chế độ mở cùng Windows." : "Không đọc được cài đặt khởi động Windows.";
                message.Visibility = failed || !enabled.HasValue ? Visibility.Visible : Visibility.Collapsed;
            };
            RoutedEventHandler update = (s, e) =>
            {
                if (!refreshing) refresh(!startup.SetEnabled(input.IsChecked == true));
            };
            input.IsVisibleChanged += (s, e) => { if (input.IsVisible) refresh(false); };
            input.Checked += update;
            input.Unchecked += update;
        }

        private void OpenSettings() { OpenSettings(false); }

        private void OpenUpcoming()
        {
            if (IsCompanion) { host.OpenUpcoming(); return; }
            if (!IsVisible) ShowWidget();
            OpenSettings(true);
        }

        private void OpenSettings(bool upcoming)
        {
            if (IsCompanion) { OpenCompanionSettings(); return; }
            petDelay.Stop();
            wheelRemainder = 0;
            if (settingsWindow == null)
            {
                if (settingsTheme == null) settingsTheme = PanelPage.LoadTheme();
                settingsWindow = new SettingsWindow(this, config, settingsTheme, () =>
                    {
                        completedReminderDay = null;
                        PrunePins();
                        UpdateClock();
                        Persist();
                    }, ScheduleSave, BuildSettings, BuildPetSettings, CreateMusic, SetSpringStyle, SavePersonalDates, SaveReminder, SavePin);
                settingsWindow.Closed += (s, e) =>
                {
                    if (music != null) music.Dispose();
                    music = null;
                    optionsPage = null;
                    sizeOption = null;
                    settingsWindow = null;
                    if (!quitting) Persist();
                    ResetPetDelay();
                };
            }
            settingsWindow.ShowOccurrence(displayedName, displayedTarget, displayedSeconds == 0, unavailableStatus);
            settingsWindow.Open(upcoming);
        }

        private bool SavePersonalDates(PersonalDate[] dates)
        {
            var previous = config.PersonalDates;
            var previousReminders = config.ReminderDays;
            var previousPins = config.PinnedOccasionIds;
            if (dates.Length < previous.Length)
            {
                config.ReminderDays = new Dictionary<string, int>(previousReminders, StringComparer.Ordinal);
                foreach (var deleted in previous.Where(date => !dates.Any(item => item.Id == date.Id)))
                    config.ReminderDays.Remove(deleted.Id.ToString("N"));
            }
            config.PersonalDates = dates;
            PrunePins();
            if (Persist())
            {
                UpdateClock();
                UpdateReminderTimer();
                return true;
            }
            config.PersonalDates = previous;
            config.ReminderDays = previousReminders;
            config.PinnedOccasionIds = previousPins;
            return false;
        }

        private bool SaveReminder(string id, int days)
        {
            var previous = config.ReminderDays;
            var updated = new Dictionary<string, int>(previous, StringComparer.Ordinal);
            if (days == OccasionReminders.Off) updated.Remove(id);
            else updated[id] = days;
            config.ReminderDays = updated;
            if (!Persist())
            {
                config.ReminderDays = previous;
                return false;
            }
            UpdateReminderTimer();
            return true;
        }

        private void UpdateReminderTimer()
        {
            completedReminderDay = null;
            if (config.ReminderDays.Count == 0) reminderTick.Stop();
            else
            {
                CheckReminders();
                reminderTick.Start();
            }
        }

        private void CheckReminders()
        {
            var now = DateTimeOffset.UtcNow;
            var local = now.ToOffset(HolidayCountdown.VietnamOffset);
            if (local.TimeOfDay < OccasionReminders.ReminderTime || completedReminderDay == local.Date ||
                config.ReminderDays.Count == 0) return;

            string message;
            if (!TryRecordDueReminders(now, out message)) return;
            completedReminderDay = local.Date;
            if (message != null)
                tray.ShowBalloonTip(10000, ProductName + " · Nhắc ngày", message, Forms.ToolTipIcon.None);
        }

        private bool TryRecordDueReminders(DateTimeOffset now, out string message)
        {
            message = null;
            var local = now.ToOffset(HolidayCountdown.VietnamOffset);
            var due = OccasionReminders.Due(config, now);
            if (due.Count == 0) return true;
            var previous = config.DeliveredReminders;
            config.DeliveredReminders = OccasionReminders.Record(previous, due.Keys, local.Date);
            if (!Persist())
            {
                config.DeliveredReminders = previous;
                return false;
            }

            var lines = due.Values.Take(2)
                .Select(occasion => OccasionReminders.CountdownLabel(occasion, local.Date) + " · " + occasion.Name);
            message = string.Join(Environment.NewLine, lines);
            if (due.Count > 2) message += Environment.NewLine + "Và " + (due.Count - 2) + " dịp khác.";
            return true;
        }

        private void SetSpringStyle(bool apricot)
        {
            if (config.ApricotNewYear == apricot) return;
            config.ApricotNewYear = apricot;
            if (displayedTheme == HolidayTheme.LunarNewYear) ApplyHolidayTheme(displayedTheme);
            Persist();
        }

        private FrameworkElement CreateMusic(ResourceDictionary theme)
        {
            if (playlist == null)
                playlist = new Playlist(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nhac"));
            music = new MusicView(playlist, theme);
            return music;
        }

        private void UpdateControls()
        {
            controls.Opacity = IsMouseOver || controls.IsKeyboardFocusWithin ? 1 : 0;
        }

        private void UpdateMotion()
        {
            holidayScene.SetMotion(WidgetPower.CanAnimate(config, onBattery, IsVisible));
        }

        private bool CheckCountdownUnits()
        {
            var target = new DateTimeOffset(2027, 2, 6, 0, 0, 0, HolidayCountdown.VietnamOffset);
            var sample = target.AddDays(-77).AddSeconds(-30810);
            UpdateClock(sample);
            if (config.CounterMode != CountdownUnit.Days || days.Text != "77" || dayUnit.Text != "NGÀY") return false;
            var title = year.Text;
            var background = glass.Background;
            var margin = face.Margin;

            config.CounterMode = CountdownUnit.Weeks;
            UpdateClock(sample);
            if (days.Text != "11" || dayUnit.Text != "TUẦN") return false;
            config.CounterMode = CountdownUnit.Months;
            UpdateClock(sample);
            if (days.Text != "2" || dayUnit.Text != "THÁNG · 16 NGÀY" || displayedTarget != target ||
                displayedTheme != HolidayTheme.LunarNewYear || year.Text != title || !ReferenceEquals(glass.Background, background) ||
                face.Margin != margin || time[0].Text != "08" || time[1].Text != "33" || time[2].Text != "30") return false;

            config.CounterMode = CountdownUnit.Weeks;
            UpdateClock(sample.AddDays(1));
            if (days.Text != "10" || dayUnit.Text != "TUẦN · 6 NGÀY") return false;
            config.CounterMode = CountdownUnit.Days;
            UpdateClock(sample);
            return days.Text == "77" && dayUnit.Text == "NGÀY" && displayedTarget == target;
        }

        private bool CheckTitleLayout()
        {
            var original = year.Text;
            try
            {
                year.Text = "Tết";
                UpdateLayout();
                var titleSize = titleLabel.RenderSize;
                var artworkPosition = holidayScene.TranslatePoint(new Point(), glass);
                var artworkSize = holidayScene.RenderSize;
                var countPosition = dayCount.TranslatePoint(new Point(), glass);
                var shortTitle = year.TransformToAncestor(titleLabel).TransformBounds(new Rect(year.RenderSize));

                year.Text = "Ngày kỷ niệm thành lập và phát triển văn hóa truyền thống Việt Nam";
                UpdateLayout();
                var longTitle = year.TransformToAncestor(titleLabel).TransformBounds(new Rect(year.RenderSize));
                return longTitle.Height < shortTitle.Height && longTitle.Right <= titleSize.Width + .1 &&
                    titleLabel.RenderSize == titleSize && dayCount.TranslatePoint(new Point(), glass) == countPosition &&
                    holidayScene.TranslatePoint(new Point(), glass) == artworkPosition && holidayScene.RenderSize == artworkSize;
            }
            finally
            {
                year.Text = original;
                UpdateLayout();
            }
        }

        private int CheckPersonalDatesPage()
        {
            var tab = (TabItem)LogicalTreeHelper.FindLogicalNode(settingsWindow, "PersonalDatesTab");
            if (tab == null) return 92;
            tab.IsSelected = true;
            settingsWindow.UpdateLayout();
            var page = (PersonalDatesPage)((ScrollViewer)tab.Content).Content;
            var preview = (HolidayScene)LogicalTreeHelper.FindLogicalNode(page, "PersonalDatePreview");
            var name = (TextBox)LogicalTreeHelper.FindLogicalNode(page, "PersonalDateName");
            var day = (ComboBox)LogicalTreeHelper.FindLogicalNode(page, "PersonalDateDay");
            var month = (ComboBox)LogicalTreeHelper.FindLogicalNode(page, "PersonalDateMonth");
            var add = (Button)LogicalTreeHelper.FindLogicalNode(page, "SavePersonalDate");
            var status = (TextBlock)LogicalTreeHelper.FindLogicalNode(page, "PersonalDateStatus");
            Action<string> click = controlName =>
            {
                var button = (Button)LogicalTreeHelper.FindLogicalNode(page, controlName);
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            };
            if (!preview.IsAnimating || preview.ActualWidth < 100 || !add.IsVisible) return 93;
            name.Text = "Sinh nhật An";
            day.SelectedIndex = 31;
            month.SelectedIndex = 4;
            click("SavePersonalDate");
            if (config.PersonalDates.Length != 0 || string.IsNullOrEmpty(status.Text) || name.Text != "Sinh nhật An") return 94;

            day.SelectedIndex = 15;
            month.SelectedIndex = 12;
            click("SavePersonalDate");
            var saved = file.Read().PersonalDates.SingleOrDefault();
            if (saved == null || saved.Name != "Sinh nhật An" || !saved.Birthday || !saved.Enabled || saved.Day != 15 || saved.Month != 12)
                return 95;

            var birthday = new DateTimeOffset(2026, 12, 15, 0, 0, 0, HolidayCountdown.VietnamOffset);
            UpdateClock(birthday.AddSeconds(-1));
            SetScale(1);
            UpdateLayout();
            if (displayedTheme != HolidayTheme.Birthday || year.Text != saved.Name || time[2].Text != "01" || celebrating ||
                !holidayScene.IsAnimating || dateStatus.TranslatePoint(new Point(), glass).Y + dateStatus.ActualHeight > 249) return 96;
            SetScale(.6);
            UpdateClock(birthday.ToUniversalTime());
            if (!celebrating || greetingText.Text != "Chúc mừng sinh nhật!" || greetingDetail.Text != "Sinh nhật An · 15/12") return 97;
            UpdateClock(birthday.AddDays(1));
            if (celebrating || displayedTheme == HolidayTheme.Birthday) return 98;

            click("EditPersonalDate");
            name.Text = "Sinh nhật của An";
            click("SavePersonalDate");
            var edited = file.Read().PersonalDates.Single();
            if (edited.Id != saved.Id || edited.Name != "Sinh nhật của An") return 99;
            var track = (CheckBox)LogicalTreeHelper.FindLogicalNode(page, "TrackPersonalDate");
            track.IsChecked = false;
            track.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            if (config.PersonalDates[0].Enabled || file.Read().PersonalDates.Single().Enabled) return 100;
            click("EditPersonalDate");
            click("DeletePersonalDate");
            click("KeepPersonalDate");
            if (config.PersonalDates.Length != 1) return 101;
            click("DeletePersonalDate");
            click("ConfirmDeletePersonalDate");
            return config.PersonalDates.Length == 0 && file.Read().PersonalDates.Length == 0 ? 0 : 102;
        }

        private int CheckQuickBrowse()
        {
            var selection = config.SelectedHolidays;
            try
            {
                config.SelectedHolidays = new[] { HolidaySetting.NewYear, "christmas" };
                UpdateClock();
                var primary = displayedTarget;
                foreach (bool horizontal in new[] { false, true })
                {
                    config.Horizontal = horizontal;
                    ApplyOrientation();
                    int timestamp = Environment.TickCount;
                    var wheel = new MouseWheelEventArgs(Mouse.PrimaryDevice, timestamp, horizontal ? -240 : 120)
                        { RoutedEvent = Mouse.PreviewMouseWheelEvent };
                    glass.RaiseEvent(wheel);
                    var chosen = displayedTarget;
                    if (!wheel.Handled || !chosen.HasValue || chosen == primary) return 132;
                    foreach (int offset in new[] { 100, 260 })
                    {
                        var burst = new MouseWheelEventArgs(Mouse.PrimaryDevice, unchecked(timestamp + offset), -120)
                            { RoutedEvent = Mouse.PreviewMouseWheelEvent };
                        glass.RaiseEvent(burst);
                        if (!burst.Handled || displayedTarget != chosen) return 146;
                    }
                    if (settingsWindow != null)
                    {
                        var stamp = (TextBlock)LogicalTreeHelper.FindLogicalNode(settingsWindow, "OccurrenceDate");
                        if (stamp.Text != chosen.Value.ToString("dd/MM", CultureInfo.InvariantCulture)) return 142;
                        stamp.RaiseEvent(new MouseWheelEventArgs(Mouse.PrimaryDevice, Environment.TickCount, -120)
                            { RoutedEvent = Mouse.PreviewMouseWheelEvent });
                        if (displayedTarget != chosen) return 143;
                    }
                    else
                    {
                        OpenSettings();
                        if (displayedTarget != chosen) return 144;
                        settingsWindow.Close();
                        HideWidget();
                        ShowWidget();
                        var saved = HolidayCountdown.Find(file.Read(), DateTimeOffset.UtcNow);
                        if (!saved.HasValue || saved.Value.Date != chosen || displayedTarget != chosen) return 145;
                    }
                    UpdateClock(DateTimeOffset.UtcNow.AddSeconds(15));
                    if (displayedTarget != chosen) return 133;
                    var nextGesture = new MouseWheelEventArgs(Mouse.PrimaryDevice,
                        unchecked(timestamp + 260 + WheelPauseMilliseconds), -120)
                        { RoutedEvent = Mouse.PreviewMouseWheelEvent };
                    glass.RaiseEvent(nextGesture);
                    if (!nextGesture.Handled || displayedTarget != primary) return 135;
                    lastWheelTimestamp = null;
                }
                var other = displayedTheme == HolidayTheme.NewYear ? "christmas" : HolidaySetting.NewYear;
                config.SelectedHolidays = new[] { other };
                UpdateClock();
                if (BrowseOccasions(1) || config.DisplayedOccurrenceKey != null || displayedTarget == primary) return 136;
                return 0;
            }
            finally
            {
                config.Horizontal = true;
                config.SelectedHolidays = selection;
                ApplyOrientation();
                UpdateClock();
            }
        }

        private int CheckMonthCalendar(UpcomingPage page)
        {
            var switchView = (Button)LogicalTreeHelper.FindLogicalNode(page, "SwitchUpcomingView");
            var selection = config.SelectedHolidays;
            var personal = config.PersonalDates;
            var date = new DateTimeOffset(2027, 2, 1, 8, 0, 0, HolidayCountdown.VietnamOffset);
            try
            {
                config.SelectedHolidays = new[] { HolidaySetting.LunarNewYear };
                config.PersonalDates = new[] { new PersonalDate { Id = Guid.NewGuid(), Enabled = true, Birthday = true,
                    Name = "Sinh nhật ngày Tết", Month = 2, Day = 6 } };
                page.Refresh(date);
                switchView.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var day = (Button)LogicalTreeHelper.FindLogicalNode(page, "CalendarDay6");
                settingsWindow.UpdateLayout();
                if (day == null || day.ActualWidth < 40 || !AutomationProperties.GetName(day).Contains("ngày 1 tháng 1 âm lịch")
                    || !AutomationProperties.GetName(day).Contains("Sinh nhật ngày Tết")) return 137;
                day.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var reminder = (ComboBox)LogicalTreeHelper.FindLogicalNode(page, "OccasionReminder");
                if (reminder == null || !AutomationProperties.GetName(reminder).Contains("Tết Nguyên đán")) return 138;
                reminder.SelectedIndex = 2;
                if (file.Read().ReminderDays[HolidaySetting.LunarNewYear] != 1) return 139;
                switchView.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                reminder = (ComboBox)LogicalTreeHelper.FindLogicalNode(page, "OccasionReminder");
                if (reminder == null || reminder.SelectedIndex != 2) return 140;
                reminder.SelectedIndex = 0;
                return 0;
            }
            finally
            {
                config.PersonalDates = personal;
                config.SelectedHolidays = selection;
                page.Refresh(DateTimeOffset.UtcNow.ToOffset(HolidayCountdown.VietnamOffset));
            }
        }

        private int CheckUpcomingPage()
        {
            var selection = config.SelectedHolidays;
            config.SelectedHolidays = new string[0];
            var target = DateTimeOffset.UtcNow.ToOffset(HolidayCountdown.VietnamOffset).AddMonths(3);
            var personal = new PersonalDate { Id = Guid.NewGuid(), Name = "Kỷ niệm gia đình", Enabled = true,
                Day = target.Day, Month = target.Month };
            config.PersonalDates = new[] { personal };
            if (controls.Children.OfType<Button>().Any(button => (string)button.Content == "Sắp tới")) return 103;
            trayMenu.MenuItems.Cast<Forms.MenuItem>().Single(item => item.Text == "Dịp sắp tới").PerformClick();
            var tab = (TabItem)LogicalTreeHelper.FindLogicalNode(settingsWindow, "UpcomingTab");
            if (tab == null || !tab.IsSelected) return 103;
            settingsWindow.Width = settingsWindow.MinWidth;
            settingsWindow.Height = settingsWindow.MinHeight;
            settingsWindow.UpdateLayout();
            var page = (UpcomingPage)((ScrollViewer)tab.Content).Content;
            var picker = (ComboBox)LogicalTreeHelper.FindLogicalNode(page, "OccasionReminder");
            if (picker == null || !picker.IsVisible || picker.Items.Count != 4 || picker.SelectedIndex != 0 ||
                !AutomationProperties.GetName(picker).Contains(personal.Name)) return 104;
            var pin = (Button)LogicalTreeHelper.FindLogicalNode(page, "OccasionPin");
            if (pin == null || !pin.IsVisible || !Equals(pin.Content, "Ghim")) return 161;
            pin.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            if (!Equals(pin.Content, "Bỏ ghim") || !file.Read().PinnedOccasionIds.SequenceEqual(new[] { personal.Id.ToString("N") }) ||
                PinsVisible) return 162;
            picker.Focus();
            picker.IsDropDownOpen = true;
            settingsWindow.UpdateLayout();
            if (!picker.IsDropDownOpen || picker.ActualWidth < 130) return 105;
            picker.IsDropDownOpen = false;
            picker.SelectedIndex = 1;
            int days;
            if (!file.Read().ReminderDays.TryGetValue(personal.Id.ToString("N"), out days) || days != 7 ||
                !reminderTick.IsEnabled) return 106;
            Hide();
            if (tick.IsEnabled || !reminderTick.IsEnabled) return 107;
            var dueAt = new DateTimeOffset(target.Year, target.Month, target.Day, 9, 0, 0, target.Offset).AddDays(-7);
            string reminder;
            if (!TryRecordDueReminders(dueAt.AddSeconds(-1), out reminder) || reminder != null ||
                !TryRecordDueReminders(dueAt.ToUniversalTime(), out reminder) || reminder != "Còn 7 ngày · " + personal.Name ||
                file.Read().DeliveredReminders.Length != 1 ||
                !TryRecordDueReminders(dueAt.AddHours(1), out reminder) || reminder != null) return 114;
            trayMenu.MenuItems.Cast<Forms.MenuItem>().Single(item => item.Text == "Dịp sắp tới").PerformClick();
            tab = (TabItem)LogicalTreeHelper.FindLogicalNode(settingsWindow, "UpcomingTab");
            if (!IsVisible || !tab.IsSelected) return 108;
            page = (UpcomingPage)((ScrollViewer)tab.Content).Content;
            picker = (ComboBox)LogicalTreeHelper.FindLogicalNode(page, "OccasionReminder");
            if (picker.SelectedIndex != 1 || !SavePersonalDates(new PersonalDate[0]) || reminderTick.IsEnabled ||
                file.Read().ReminderDays.Count != 0 || file.Read().PinnedOccasionIds.Length != 0) return 109;
            page.Refresh(DateTimeOffset.UtcNow.ToOffset(HolidayCountdown.VietnamOffset));
            if (LogicalTreeHelper.FindLogicalNode(page, "OccasionReminder") != null) return 110;
            int calendarResult = CheckMonthCalendar(page);
            if (calendarResult != 0) return calendarResult;
            config.SelectedHolidays = selection;
            UpdateClock();
            return 0;
        }

        private int CheckPetSettingsAndMode()
        {
            if (config.PetEnabled || petDelay.IsEnabled || showPetItem.Enabled || showBothItem.Enabled) return 115;
            OpenSettings();
            var tab = (TabItem)LogicalTreeHelper.FindLogicalNode(settingsWindow, "PetTab");
            if (tab == null) return 116;
            tab.IsSelected = true;
            settingsWindow.Width = settingsWindow.MinWidth;
            settingsWindow.Height = settingsWindow.MinHeight;
            settingsWindow.UpdateLayout();
            var page = ((ScrollViewer)tab.Content).Content as FrameworkElement;
            var enable = (CheckBox)LogicalTreeHelper.FindLogicalNode(page, "EnableTaskbarPet");
            var species = (ComboBox)LogicalTreeHelper.FindLogicalNode(page, "PetSpecies");
            var delay = (ComboBox)LogicalTreeHelper.FindLogicalNode(page, "PetDelayMinutes");
            var movement = (ComboBox)LogicalTreeHelper.FindLogicalNode(page, "PetMovement");
            var position = (ComboBox)LogicalTreeHelper.FindLogicalNode(page, "PetPosition");
            var showNow = (Button)LogicalTreeHelper.FindLogicalNode(page, "ShowPetNow");
            var showBoth = (Button)LogicalTreeHelper.FindLogicalNode(page, "ShowPetAndClock");
            var size = (Slider)LogicalTreeHelper.FindLogicalNode(page, "PetSizePercent");
            if (!species.IsVisible || species.Items.Count != 3 || species.SelectedIndex != 0 || delay.SelectedIndex != 1 ||
                movement.SelectedIndex != 0 || position.SelectedIndex != 0 || showNow.IsEnabled || showBoth.IsEnabled ||
                size.Minimum != 50 || size.Maximum != 200 || size.Value != 100) return 117;
            if (species.ActualWidth < 160) return 118;
            species.SelectedIndex = 2;
            if (file.Read().PetAnimal != PetAnimal.HacLong ||
                !Equals(((ComboBoxItem)movement.Items[0]).Content, "Đi qua lại")) return 151;
            var petName = (TextBox)LogicalTreeHelper.FindLogicalNode(page, "PetNameInput");
            var savePetName = (Button)LogicalTreeHelper.FindLogicalNode(page, "SavePetName");
            if (petName.Text != "") return 147;
            petName.Text = "  Bông  ";
            savePetName.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            if (petName.Text != "Bông" || file.Read().PetName != "Bông") return 148;
            species.SelectedIndex = 1;
            delay.SelectedIndex = 0;
            movement.SelectedIndex = 1;
            position.SelectedIndex = 1;
            enable.IsChecked = true;
            enable.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var restored = file.Read();
            if (!showNow.IsEnabled || !showBoth.IsEnabled || !showPetItem.Enabled || !showBothItem.Enabled ||
                !restored.PetEnabled || restored.PetAnimal != PetAnimal.Dog ||
                restored.PetDelayMinutes != 1 || restored.PetRoams || !restored.PetAtEnd || petDelay.IsEnabled) return 119;

            size.Value = 50;
            if (config.PetSizePercent != 50) return 128;
            size.Value = 200;
            showBoth.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            if (settingsWindow != null || !IsVisible || pet == null || !pet.IsRunning || petDelay.IsEnabled ||
                !tick.IsEnabled || file.Read().PetSizePercent != 200) return 129;
            if (!pet.CheckPettingInteraction()) return 149;
            ShowWidget();
            if (!petDelay.IsEnabled || petDelay.Interval != TimeSpan.FromMinutes(1) || !tick.IsEnabled) return 120;
            config.ReminderDays[HolidaySetting.NewYear] = 7;
            UpdateReminderTimer();
            EnterPetModeAfterDelay();
            if (pet == null || !pet.IsRunning || IsVisible || tick.IsEnabled || petDelay.IsEnabled || !reminderTick.IsEnabled)
                return 121;
            trayMenu.MenuItems[0].PerformClick();
            if (!IsVisible || pet.IsRunning || !petDelay.IsEnabled || !tick.IsEnabled) return 122;

            showPetItem.PerformClick();
            if (!pet.IsRunning || IsVisible) return 123;
            showBothItem.PerformClick();
            if (!pet.IsRunning || !IsVisible || !tick.IsEnabled || petDelay.IsEnabled) return 130;
            HideWidget();
            if (pet.IsVisible || pet.IsRunning || petDelay.IsEnabled || IsVisible) return 124;
            ShowWidget();
            config.PetAnimal = PetAnimal.HacLong;
            config.PetRoams = true;
            pet.ResetRoute();
            showBothItem.PerformClick();
            if (!IsVisible || !pet.IsVisible || !pet.IsRunning) return 152;
            ShowWidget();
            return 0;
        }

        internal int CheckWindowLifecycle()
        {
            if (!IsVisible || !AllowsTransparency || ShowInTaskbar || !tray.Visible ||
                !tray.Text.StartsWith(ProductName, StringComparison.Ordinal) ||
                !trayMenu.MenuItems.Cast<Forms.MenuItem>().Any(item => item.Text == "Thoát " + ProductName)) return 21;
            if (settingsWindow != null || optionsPage != null || music != null || !tick.IsEnabled) return 36;
            if (Math.Abs(ActualWidth - 200) > 1 || Math.Abs(ActualHeight - 250) > 1) return 22;
            if (!CheckCountdownUnits()) return 83;
            config.Glass = 100;
            ApplyGlass();
            var fill = (LinearGradientBrush)glass.Background;
            if (fill.GradientStops.Any(stop => stop.Color.A != 255)) return 27;
            if (Title != ProductName || dateStatus.Visibility != Visibility.Visible ||
                year.FontSize != 14 || days.FontSize != 70 || dateStatus.TranslatePoint(new Point(), glass).Y + dateStatus.ActualHeight > 245) return 30;
            if (!CheckTitleLayout()) return 89;
            SetScale(.8);
            UpdateLayout();
            if (dateStatus.Visibility != Visibility.Collapsed || year.FontSize != 17 || days.FontSize != 87 ||
                clock.Visibility != Visibility.Visible || clock.TranslatePoint(new Point(), glass).Y + clock.ActualHeight > 250) return 31;
            if (!CheckTitleLayout()) return 90;
            config.Automatic = false;
            var midnight = new DateTimeOffset(2027, 2, 6, 0, 0, 0, TimeSpan.FromHours(7));
            var reference = new DateTimeOffset(2026, 11, 20, 0, 0, 0, midnight.Offset);
            UpdateClock(reference);
            if (days.Text != "78" || time.Any(label => label.Text != "00") ||
                dateStatus.Text != "06/02/2027 · Giờ Việt Nam" || celebrating) return 75;
            UpdateClock(reference.AddHours(8).AddMinutes(15).AddSeconds(34));
            if (days.Text != "77" || time[0].Text != "15" || time[1].Text != "44" || time[2].Text != "26") return 76;
            UpdateClock(midnight.AddSeconds(-1));
            if (days.Text != "0" || time[2].Text != "01" || dayUnit.Text != "NGÀY") return 53;
            UpdateClock(midnight);
            if (time[2].Text != "00" || dayUnit.Text != "TẾT ĐÃ ĐẾN" || !celebrating ||
                greetingText.Text != "Chúc mừng năm mới") return 54;
            config.ApricotNewYear = true;
            ApplyHolidayTheme(displayedTheme);
            if (!holidayScene.UsesApricot || !celebrating ||
                (config.Motion && !holidayScene.IsAnimating)) return 73;
            config.ApricotNewYear = false;
            ApplyHolidayTheme(displayedTheme);
            if (holidayScene.UsesApricot) return 74;
            UpdateClock(midnight.AddSeconds(-2));
            if (time[2].Text != "02" || dayUnit.Text != "NGÀY") return 55;
            config.Automatic = true;
            config.SelectedHolidays = new[] { HolidaySetting.NewYear, HolidaySetting.HungKings };
            config.Motion = true;
            var newYear = new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.FromHours(7));
            UpdateClock(newYear.AddSeconds(-1));
            if (displayedTheme != HolidayTheme.NewYear || year.Text != "Tết Dương lịch" ||
                time[2].Text != "01" ||
                !holidayScene.IsAnimating) return 60;
            UpdateClock(newYear.AddDays(1));
            if (displayedTheme != HolidayTheme.HungKings || year.Text != "Giỗ Tổ Hùng Vương" ||
                displayedTarget != new DateTimeOffset(2027, 4, 16, 0, 0, 0, TimeSpan.FromHours(7)) ||
                !holidayScene.IsAnimating) return 61;
            config.Motion = false;
            UpdateMotion();
            if (holidayScene.IsAnimating) return 66;
            config.Motion = true;
            UpdateMotion();
            if (!holidayScene.IsAnimating) return 67;
            foreach (var holiday in HolidaySetting.All)
            {
                config.SelectedHolidays = new[] { holiday.Id };
                UpdateClock(newYear.AddSeconds(-1));
                if (displayedTheme != holiday.Theme || !holidayScene.IsAnimating) return 72;
            }
            config.SelectedHolidays = new[] { "mid-autumn" };
            UpdateClock(new DateTimeOffset(2027, 9, 15, 0, 0, 0, TimeSpan.FromHours(7)));
            if (!celebrating || greetingText.Text != "Vui Tết Trung thu") return 68;
            config.SelectedHolidays = new[] { "christmas" };
            UpdateClock(new DateTimeOffset(2027, 12, 25, 0, 0, 0, TimeSpan.FromHours(7)));
            if (!celebrating || greetingText.Text != "Giáng sinh an lành") return 69;
            var cultureDay = new DateTimeOffset(2026, 11, 24, 0, 0, 0, HolidayCountdown.VietnamOffset);
            config.SelectedHolidays = new[] { HolidaySetting.VietnamCultureDay };
            UpdateClock(cultureDay.AddSeconds(-1));
            if (displayedTheme != HolidayTheme.VietnamCultureDay || year.Text != HolidaySetting.VietnamCultureDayName ||
                !holidayScene.IsAnimating || celebrating || time[2].Text != "01") return 79;
            UpdateLayout();
            UpdateClock(cultureDay);
            if (!celebrating || greetingText.Text != HolidaySetting.VietnamCultureDayName || greetingDetail.Text != "24/11") return 80;
            UpdateClock(cultureDay.AddDays(1));
            if (celebrating || displayedTarget != cultureDay.AddYears(1)) return 81;
            config.SelectedHolidays = new string[0];
            UpdateClock(newYear);
            if (displayedTarget.HasValue || days.Text != "–" || dateStatus.Text != "Chưa chọn ngày nghỉ" ||
                holidayScene.IsAnimating) return 62;
            config.SelectedHolidays = new[] { HolidaySetting.LunarNewYear };
            UpdateClock();
            SetScale(.81);
            if (dateStatus.Visibility != Visibility.Visible || days.FontSize != 70) return 32;
            SetScale(WidgetSize.FromDrag(1, -80, -100));
            UpdateLayout();
            if (Math.Abs(ActualWidth - 120) > 1 || Math.Abs(ActualHeight - 150) > 1 ||
                Math.Abs(glass.ActualWidth - 200) > 1) return 28;
            SetScale(WidgetSize.FromDrag(.6, 500, 625));
            UpdateLayout();
            if (Math.Abs(ActualWidth - 360) > 1 || Math.Abs(ActualHeight - 450) > 1) return 29;
            SetScale(1);
            config.Motion = true;
            UpdateMotion();
            if (!holidayScene.IsAnimating || !holidayScene.IsDescendantOf(glass)) return 23;
            config.Motion = false;
            UpdateMotion();
            if (holidayScene.IsAnimating || !tick.IsEnabled || !holidayScene.IsVisible) return 35;
            config.Motion = true;
            UpdateMotion();
            SetScale(.6);
            var widgetPosition = new Point(Left, Top);
            OpenSettings();
            UpdateLayout();
            settingsWindow.UpdateLayout();
            if (!settingsWindow.IsVisible || settingsWindow.Title != "Cài đặt · " + ProductName ||
                !holidayScene.IsAnimating || !tick.IsEnabled || optionsPage != null || music != null) return 24;
            if (settingsWindow.ActualWidth < settingsWindow.MinWidth) return 38;
            if (Math.Abs(ActualWidth - 120) > 1 || Math.Abs(ActualHeight - 150) > 1) return 56;
            if (widgetPosition != new Point(Left, Top)) return 57;
            var occurrenceStatus = (TextBlock)LogicalTreeHelper.FindLogicalNode(settingsWindow, "OccurrenceStatus");
            var occurrenceDate = (TextBlock)LogicalTreeHelper.FindLogicalNode(settingsWindow, "OccurrenceDate");
            UpdateClock(reference);
            if (occurrenceStatus.Text != "ĐANG ĐẾM NGƯỢC" || occurrenceDate.Text != "06/02" || !occurrenceDate.IsVisible) return 86;
            UpdateClock(midnight);
            if (occurrenceStatus.Text != "ĐANG DIỄN RA" || !occurrenceDate.IsVisible) return 87;
            config.SelectedHolidays = new string[0];
            UpdateClock(midnight);
            if (occurrenceStatus.Text != "CHƯA CHỌN DỊP" || occurrenceDate.IsVisible) return 88;
            config.SelectedHolidays = new[] { HolidaySetting.LunarNewYear };
            UpdateClock();
            var optionsTab = (TabItem)LogicalTreeHelper.FindLogicalNode(settingsWindow, "OptionsTab");
            var musicTab = (TabItem)LogicalTreeHelper.FindLogicalNode(settingsWindow, "MusicTab");
            if (optionsTab == null || musicTab == null) return 78;
            int personalDatesResult = CheckPersonalDatesPage();
            if (personalDatesResult != 0) return personalDatesResult;
            musicTab.IsSelected = true;
            settingsWindow.UpdateLayout();
            if (music == null || !music.IsVisible || music.ActualWidth < 200 ||
                music.ActualHeight > settingsWindow.ActualHeight || !holidayScene.IsAnimating || optionsPage != null) return 34;
            optionsTab.IsSelected = true;
            settingsWindow.UpdateLayout();
            if (optionsPage == null || !optionsPage.IsVisible || music.IsVisible) return 78;
            var size = (Slider)LogicalTreeHelper.FindLogicalNode(optionsPage, "WidgetScale");
            if (size == null || size.Value != 60 || surface.Children.OfType<Button>().Any(button => Equals(button.Content, "↘"))) return 157;
            size.Value = 95;
            UpdateLayout();
            if (Math.Abs(ActualWidth - 190) > 1 || Math.Abs(ActualHeight - 237.5) > 1 ||
                config.Scale != .95) return 158;
            SetScale(.6);
            UpdateLayout();
            if (size.Value != 60 || Math.Abs(ActualWidth - 120) > 1 || Math.Abs(ActualHeight - 150) > 1) return 159;
            var model =  (CheckBox)LogicalTreeHelper.FindLogicalNode(optionsPage, "Use3D");
            if (model == null || model.IsChecked != true) return 153;
            var modelInset = face.Margin.Top;
            var modelTarget = displayedTarget;
            model.IsChecked = false;
            if (config.Use3D || file.Read().Use3D || face.Margin.Top >= modelInset ||
                displayedTarget != modelTarget || !holidayScene.IsAnimating) return 154;
            model.IsChecked = true;
            if (!config.Use3D || !file.Read().Use3D || face.Margin.Top != modelInset ||
                displayedTarget != modelTarget || !holidayScene.IsAnimating) return 155;
            var units = (ComboBox)LogicalTreeHelper.FindLogicalNode(optionsPage, "CounterUnit");
            if (units == null || units.Items.Count != 3 || units.SelectedIndex != (int)CountdownUnit.Days) return 84;
            var activeTarget = displayedTarget;
            foreach (var unit in new[] { CountdownUnit.Weeks, CountdownUnit.Months, CountdownUnit.Days })
            {
                units.SelectedIndex = (int)unit;
                if (config.CounterMode != unit || displayedTarget != activeTarget ||
                    !tick.IsEnabled || file.Read().CounterMode != unit || !holidayScene.IsAnimating) return 85;
            }

            OpenSettings();
            config.Horizontal = true;
            ApplyOrientation();
            var lastSettings = settingsWindow;
            var lastPlaylist = playlist;
            settingsWindow.Close();
            UpdateLayout();
            if (settingsWindow != null || music != null || optionsPage != null || playlist != lastPlaylist ||
                Math.Abs(ActualWidth - 216) > 1 || Math.Abs(ActualHeight - 50.4) > 1 || !holidayScene.IsAnimating) return 50;
            var horizontalScale = WidgetSize.FromDrag(.6, 144, 33.6, true);
            SetScale(horizontalScale);
            UpdateLayout();
            if (Math.Abs(ActualWidth - 360) > 1 || Math.Abs(ActualHeight - 84) > 1 || dateStatus.Visibility != Visibility.Collapsed ||
                clock.TranslatePoint(new Point(clock.ActualWidth, 0), glass).X > glass.ActualWidth ||
                days.TranslatePoint(new Point(), glass).X >= clock.TranslatePoint(new Point(), glass).X) return 51;
            if (!CheckTitleLayout()) return 91;
            int browseResult = CheckQuickBrowse();
            if (browseResult != 0) return browseResult;

            OpenSettings();
            UpdateLayout();
            if (!settingsWindow.IsVisible || settingsWindow == lastSettings || Math.Abs(ActualWidth - 360) > 1 ||
                Math.Abs(ActualHeight - 84) > 1 || !tick.IsEnabled || !holidayScene.IsAnimating) return 52;
            browseResult = CheckQuickBrowse();
            if (browseResult != 0) return browseResult;
            int upcomingResult = CheckUpcomingPage();
            if (upcomingResult != 0) return upcomingResult;
            config.Horizontal = false;
            ApplyOrientation();
            settingsWindow.Close();
            if (!tick.IsEnabled) return 37;
            Close();
            if (IsVisible || tick.IsEnabled || holidayScene.IsAnimating) return 25;
            ShowWidget();
            if (!IsVisible || !tick.IsEnabled || !holidayScene.IsAnimating) return 26;
            return CheckPetSettingsAndMode();
        }

        internal void Quit() { quitting = true; Close(); }
    }

    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            if (args.Length == 1 && args[0] == "--self-test") return SelfTest();
            if (args.Length == 1 && args[0] == "--smoke-test") return SmokeTest();
            if (args.Length == 1 && args[0] == "--startup-test") return StartupShortcut.Check();
            if (args.Length == 1 && args[0] == "--music-test") return MusicChecks.Run();
            if (args.Length == 2 && args[0] == "--verify-music") return MusicChecks.VerifyFolder(args[1]);
            bool first;
            using (var instance = new Mutex(true, "Local\\NocturnePortable-v1", out first))
            using (var reveal = new EventWaitHandle(false, EventResetMode.AutoReset, "Local\\NocturnePortableShow-v1"))
            {
                if (!first) { reveal.Set(); return 0; }
                Widget.ConfigureRendering();
                var app = new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                // Keep the settings directory used by builds released as Hẹn Tết.
                var path = System.IO.Path.Combine(appData, "HenTet", "settings.json");
                var legacy = System.IO.Path.Combine(appData, "Nocturne", "settings.json");
                var widget = new Widget(new SettingsFile(path, legacy));
                app.SessionEnding += (s, e) => widget.Quit();
                StartupShortcut.ForCurrentUser().UpgradeLegacyTarget();
                var waiter = ThreadPool.RegisterWaitForSingleObject(reveal,
                    (s, timeout) => widget.Dispatcher.BeginInvoke(new Action(widget.ShowWidget)), null, Timeout.Infinite, false);
                try { return app.Run(widget); }
                finally { waiter.Unregister(null); }
            }
        }

        private static int SmokeTest()
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "NghiLe-ui-test-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                Widget.ConfigureRendering();
                var app = new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                var window = new Widget(new SettingsFile(path));
                var result = 20;
                window.ContentRendered += (s, e) => window.Dispatcher.BeginInvoke(new Action(() =>
                {
                    result = window.CheckWindowLifecycle();
                    window.Quit();
                }), DispatcherPriority.ApplicationIdle);
                app.Run(window);
                return result;
            }
            finally { File.Delete(path); }
        }

        private static bool CheckUnits()
        {
            var offset = HolidayCountdown.VietnamOffset;
            var months = new[]
            {
                Tuple.Create(new DateTimeOffset(2027, 5, 1, 0, 0, 0, offset), 30),
                Tuple.Create(new DateTimeOffset(2027, 2, 1, 0, 0, 0, offset), 31),
                Tuple.Create(new DateTimeOffset(2027, 3, 1, 0, 0, 0, offset), 28),
                Tuple.Create(new DateTimeOffset(2028, 3, 1, 0, 0, 0, offset), 29),
                Tuple.Create(new DateTimeOffset(2029, 1, 1, 0, 0, 0, offset), 31)
            };
            foreach (var month in months)
            {
                var target = month.Item1;
                long seconds = month.Item2 * 86400L;
                var wholeMonth = CountdownUnits.Measure(CountdownUnit.Months, target, seconds);
                var partialMonth = CountdownUnits.Measure(CountdownUnit.Months, target, seconds - 1);
                if (wholeMonth.WholeUnits != 1 || wholeMonth.ExtraDays != 0 ||
                    partialMonth.WholeUnits != 0 || partialMonth.ExtraDays != month.Item2 - 1) return false;
            }
            var endOfFebruary = new DateTimeOffset(2027, 2, 28, 0, 0, 0, offset);
            var clampedMonth = CountdownUnits.Measure(CountdownUnit.Months, endOfFebruary, 28 * 86400L);
            var weeks = CountdownUnits.Measure(CountdownUnit.Weeks, endOfFebruary, 79 * 86400L);
            return clampedMonth.WholeUnits == 1 && clampedMonth.ExtraDays == 0 && weeks.WholeUnits == 11 && weeks.ExtraDays == 2;
        }

        private static bool CheckPersonalCalendar()
        {
            var settings = new Settings();
            var reminder = new PersonalDate { Id = Guid.NewGuid(), Name = "Sinh nhật", Birthday = true, Enabled = true, Day = 29, Month = 2 };
            settings.PersonalDates = new[] { reminder };
            var offset = HolidayCountdown.VietnamOffset;
            var leapDay = new DateTimeOffset(2028, 2, 29, 0, 0, 0, offset);
            if (!PersonalDate.IsValidDay(2, 29) || PersonalDate.IsValidDay(4, 31)) return false;
            if (HolidayCountdown.Find(settings, leapDay.AddSeconds(-1)).Value.Date != leapDay ||
                HolidayCountdown.Find(settings, leapDay.AddHours(23)).Value.Theme != HolidayTheme.Birthday) return false;
            settings.SelectedHolidays = new string[0];
            if (HolidayCountdown.Find(settings, leapDay.AddDays(1)).Value.Date != leapDay.AddYears(4)) return false;
            settings.PersonalDates = new[] { reminder.WithEnabled(false) };
            if (HolidayCountdown.Find(settings, leapDay).HasValue) return false;
            settings.SelectedHolidays = new[] { HolidaySetting.LunarNewYear };
            settings.PersonalDates = new[] { new PersonalDate { Id = Guid.NewGuid(), Name = "Kỷ niệm", Enabled = true, Day = 8, Month = 2 } };
            var date = new DateTimeOffset(2027, 2, 8, 0, 0, 0, offset);
            var occurrence = HolidayCountdown.Find(settings, date);
            return occurrence.HasValue && occurrence.Value.Theme == HolidayTheme.PersonalDay && occurrence.Value.Name == "Kỷ niệm";
        }

        private static bool CheckOccasionReminders()
        {
            var tet = new DateTimeOffset(2027, 2, 6, 9, 0, 0, HolidayCountdown.VietnamOffset);
            var birthday = new PersonalDate { Id = Guid.NewGuid(), Name = "Sinh nhật", Birthday = true,
                Enabled = true, Day = 6, Month = 2 };
            var another = new PersonalDate { Id = Guid.NewGuid(), Name = "Kỷ niệm", Enabled = true, Day = 7, Month = 2 };
            var settings = new Settings { PersonalDates = new[] { another, birthday } };
            string birthdayId = birthday.Id.ToString("N");
            string anotherId = another.Id.ToString("N");
            settings.ReminderDays[HolidaySetting.LunarNewYear] = 7;
            settings.ReminderDays[birthdayId] = 0;
            settings.ReminderDays[anotherId] = 1;
            // A manual selection survives a nearer occasion and the whole holiday, but not its next recurrence.
            var selectedTet = HolidayCountdown.UpcomingTracked(settings, tet).First(item => item.Theme == HolidayTheme.LunarNewYear);
            settings.DisplayedOccurrenceKey = selectedTet.Key;
            if (HolidayCountdown.Find(settings, tet).Value.Id != selectedTet.Id ||
                HolidayCountdown.Find(settings, tet.AddDays(4)).Value.Key != selectedTet.Key ||
                HolidayCountdown.Find(settings, tet.AddDays(5)).Value.Key == selectedTet.Key) return false;
            var beforeTet = tet.AddDays(-7).ToUniversalTime();
            if (OccasionReminders.Due(settings, beforeTet.AddSeconds(-1)).Count != 0) return false;
            if (OccasionReminders.Due(settings, beforeTet).Values.Single().Id != HolidaySetting.LunarNewYear) return false;
            if (!HolidayCountdown.UpcomingTracked(settings, tet).Select(occasion => occasion.Id)
                .SequenceEqual(new[] { HolidaySetting.LunarNewYear, birthdayId, anotherId })) return false;
            var due = OccasionReminders.Due(settings, tet.AddHours(12));
            if (due.Count != 2 || !due.Values.Any(item => item.Id == birthdayId) || !due.Values.Any(item => item.Id == anotherId))
                return false;
            settings.DeliveredReminders = OccasionReminders.Record(new[] { "20200101|old" }, due.Keys, tet.Date);
            if (settings.DeliveredReminders.Length != 2 || OccasionReminders.Due(settings, tet.AddHours(13)).Count != 0 ||
                OccasionReminders.Due(settings, tet.AddDays(1)).Count != 0) return false;
            settings.DeliveredReminders = new string[0];
            settings.PersonalDates = new[] { birthday.WithEnabled(false) };
            if (OccasionReminders.Due(settings, tet).Count != 0) return false;
            settings.Automatic = false;
            settings.Year = 2026;
            return !HolidayCountdown.UpcomingTracked(settings, tet).Any() && HolidayCountdown.Find(settings, tet).HasValue;
        }

        private static bool CheckLunarMonths()
        {
            foreach (var holiday in HolidaySetting.All.Where(item => item.IsLunar))
            {
                var settings = new Settings { SelectedHolidays = new[] { holiday.Id } };
                for (int year = 2026; year <= 2036; year++)
                {
                    var start = new DateTimeOffset(year, 1, 1, 0, 0, 0, HolidayCountdown.VietnamOffset);
                    var occasion = HolidayCountdown.Find(settings, start).Value;
                    var lunar = VietnameseCalendar.Convert(occasion.Date.Date);
                    if (lunar.Day != holiday.Day || lunar.Month != holiday.Month || lunar.Leap) return false;
                }
            }
            var leap = VietnameseCalendar.Convert(new DateTime(2033, 12, 22));
            return leap.Day == 1 && leap.Month == 11 && leap.Leap &&
                VietnameseCalendar.Convert(new DateTime(2026, 1, 1)).ShortLabel == "13/11" &&
                VietnameseCalendar.Convert(new DateTime(2036, 12, 31)).ShortLabel == "15/11";
        }

        private static bool CheckEdgeResize()
        {
            foreach (bool horizontal in new[] { false, true })
            {
                var size = WidgetSize.Clock(horizontal);
                var edges = new[]
                {
                    Tuple.Create(new Point(3, size.Height / 2), WidgetEdge.Left, -1, 0),
                    Tuple.Create(new Point(size.Width - 3, size.Height / 2), WidgetEdge.Right, 1, 0),
                    Tuple.Create(new Point(size.Width / 2, 3), WidgetEdge.Top, 0, -1),
                    Tuple.Create(new Point(size.Width / 2, size.Height - 3), WidgetEdge.Bottom, 0, 1),
                    Tuple.Create(new Point(9, 9), WidgetEdge.Left | WidgetEdge.Top, -1, -1),
                    Tuple.Create(new Point(size.Width - 9, 9), WidgetEdge.Right | WidgetEdge.Top, 1, -1),
                    Tuple.Create(new Point(9, size.Height - 9), WidgetEdge.Left | WidgetEdge.Bottom, -1, 1),
                    Tuple.Create(new Point(size.Width - 9, size.Height - 9), WidgetEdge.Right | WidgetEdge.Bottom, 1, 1)
                };
                var center = new Point(size.Width / 2, size.Height / 2);
                if (WidgetSize.EdgeAt(center, size) != WidgetEdge.None ||
                    WidgetSize.EdgeAt(new Point(-1, 0), size) != WidgetEdge.None) return false;
                foreach (var edge in edges)
                {
                    var delta = new Vector(edge.Item3 * size.Width / 4, edge.Item4 * size.Height / 4);
                    if (WidgetSize.EdgeAt(edge.Item1, size) != edge.Item2 ||
                        WidgetSize.ResizeScale(1, delta, edge.Item2, horizontal) != 1.25 ||
                        WidgetSize.ResizeScale(1, -delta * 10, edge.Item2, horizontal) != WidgetSize.MinimumScale ||
                        WidgetSize.ResizeScale(1, delta * 10, edge.Item2, horizontal) != WidgetSize.MaximumScale) return false;
                    var start = new Rect(new Point(120, 140), size);
                    var target = new Size(size.Width * 1.25, size.Height * 1.25);
                    var origin = WidgetSize.ResizeOrigin(start, target, edge.Item2);
                    var before = start.TopLeft + new Vector(size.Width * (1 - edge.Item3) / 2, size.Height * (1 - edge.Item4) / 2);
                    var after = origin + new Vector(target.Width * (1 - edge.Item3) / 2, target.Height * (1 - edge.Item4) / 2);
                    if (before != after) return false;
                }
            }
            return true;
        }

        private static bool CheckPinnedOccasions()
        {
            var local = new DateTimeOffset(2026, 11, 20, 12, 0, 0, HolidayCountdown.VietnamOffset);
            var trip = new PersonalDate { Id = Guid.NewGuid(), Name = "Chuyến đi", Enabled = true, Day = 15, Month = 3 };
            string id = trip.Id.ToString("N");
            var selected = new[] { HolidaySetting.LunarNewYear, HolidaySetting.NewYear, "christmas" };
            var settings = new Settings { SelectedHolidays = selected, PersonalDates = new[] { trip },
                PinnedOccasionIds = new[] { HolidaySetting.LunarNewYear, "missing", HolidaySetting.LunarNewYear,
                    HolidaySetting.NewYear, id, "christmas" } };
            settings.PinnedOccasionIds = PinnedOccasions.Normalize(settings);
            if (!settings.PinnedOccasionIds.SequenceEqual(new[] { HolidaySetting.LunarNewYear, HolidaySetting.NewYear, id }) ||
                HolidayCountdown.FindForDay(settings, local).Value.Id != HolidaySetting.LunarNewYear) return false;
            var chosen = HolidayCountdown.UpcomingTracked(settings, local).Single(item => item.Id == "christmas");
            settings.DisplayedOccurrenceKey = chosen.Key;
            if (HolidayCountdown.FindForDay(settings, local).Value.Id != "christmas") return false;
            settings.DisplayedOccurrenceKey = null;
            var birthday = new PersonalDate { Id = Guid.NewGuid(), Name = "Sinh nhật", Enabled = true, Birthday = true,
                Day = local.Day, Month = local.Month };
            settings.PersonalDates = new[] { trip, birthday };
            if (HolidayCountdown.FindForDay(settings, local).Value.Id != birthday.Id.ToString("N")) return false;
            settings.PersonalDates = new[] { trip.WithEnabled(false) };
            return PinnedOccasions.Normalize(settings).SequenceEqual(new[] { HolidaySetting.LunarNewYear, HolidaySetting.NewYear });
        }

        private static bool CheckPowerOptions()
        {
            var settings = new Settings { Motion = true, PauseOnBattery = true, DimOnSchedule = true };
            if (WidgetPower.CanAnimate(settings, true, true) || !WidgetPower.CanAnimate(settings, false, true) ||
                WidgetPower.CanAnimate(settings, false, false) || !WidgetPower.DimAt(settings, 22) ||
                !WidgetPower.DimAt(settings, 0) || !WidgetPower.DimAt(settings, 5) ||
                WidgetPower.DimAt(settings, 6) || WidgetPower.DimAt(settings, 21)) return false;
            settings.DimFromHour = 8;
            settings.DimUntilHour = 15;
            if (!WidgetPower.DimAt(settings, 8) || WidgetPower.DimAt(settings, 15) || WidgetPower.DimAt(settings, 6)) return false;
            settings.DimUntilHour = 8;
            settings.PauseOnBattery = false;
            return !WidgetPower.DimAt(settings, 8) && WidgetPower.CanAnimate(settings, true, true);
        }

        private static bool CheckSettingsBackup()
        {
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "NghiLe-backup-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var date = new PersonalDate { Id = Guid.NewGuid(), Name = "Kỷ niệm", Day = 10, Month = 3, Enabled = true };
                var original = new Settings { Scale = 1.15, Horizontal = true, Glass = 88, X = 250, Y = 120,
                    Motion = false, PauseOnBattery = true, DimOnSchedule = true, DimFromHour = 20, DimUntilHour = 8,
                    PersonalDates = new[] { date }, PinnedOccasionIds = new[] { date.Id.ToString("N") } };
                original.ReminderDays[date.Id.ToString("N")] = 7;
                original.DeliveredReminders = new[] { "local history" };
                var widgetId = Guid.NewGuid();
                original.AdditionalWidgets = new[] { new WidgetProfile { Id = widgetId, Scale = .95,
                    X = 150, Y = 80, OccasionId = date.Id.ToString("N") } };
                if (!SettingsBackup.Export(path, original)) return false;
                var restored = SettingsBackup.Import(path);
                if (restored == null || restored.PersonalDates.Length != 1 || restored.PersonalDates[0].Id != date.Id ||
                    restored.PinnedOccasionIds.Length != 1 || restored.ReminderDays[date.Id.ToString("N")] != 7 ||
                    restored.Scale != 1.15 || !restored.Horizontal || restored.Glass != 88 || restored.Motion ||
                    !restored.PauseOnBattery || !restored.DimOnSchedule || restored.DimFromHour != 20 ||
                    restored.DimUntilHour != 8 || restored.X.HasValue || restored.DeliveredReminders.Length != 0) return false;
                if (restored.AdditionalWidgets.Length != 1 || restored.AdditionalWidgets[0].Id != widgetId ||
                    restored.AdditionalWidgets[0].X.HasValue || restored.AdditionalWidgets[0].OccasionId != date.Id.ToString("N")) return false;
                var local = new Settings { X = 64, Y = 32, SettingsWidth = 710, DeliveredReminders = new[] { "delivery" },
                    AdditionalWidgets = new[] { new WidgetProfile { Id = widgetId, X = 180, Y = 45, Scale = 1.3 } } };
                var merged = SettingsBackup.ForThisDevice(restored, local);
                if (merged.X != 64 || merged.Y != 32 || merged.SettingsWidth != 710 || merged.DeliveredReminders[0] != "delivery" ||
                    !merged.Horizontal || original.X != 250 || merged.AdditionalWidgets[0].X != 180 ||
                    merged.AdditionalWidgets[0].Scale != .95) return false;
                var json = new JavaScriptSerializer();
                var legacy = (Dictionary<string, object>)json.DeserializeObject(File.ReadAllText(path));
                ((Dictionary<string, object>)legacy["settings"]).Remove("AdditionalWidgets");
                File.WriteAllText(path, json.Serialize(legacy));
                var legacyBackup = SettingsBackup.Import(path);
                if (legacyBackup == null || SettingsBackup.ForThisDevice(legacyBackup, local).AdditionalWidgets[0].Scale != 1.3) return false;
                File.WriteAllText(path, "{\"format\":\"vn.nghile.backup\",\"version\":1,\"settings\":{}}");
                if (SettingsBackup.Import(path) != null) return false;
                File.WriteAllText(path, "{bad json");
                return SettingsBackup.Import(path) == null;
            }
            finally { File.Delete(path); }
        }

        private static bool CheckWidgetProfiles()
        {
            var first = new WidgetProfile { Id = Guid.NewGuid(), Scale = 3, X = double.NaN, Y = 0 };
            var second = new WidgetProfile { Id = Guid.NewGuid(), Scale = .4, Horizontal = true, X = 14, Y = 28 };
            var list = WidgetProfile.Normalize(new[] { null, first, first.Copy(), new WidgetProfile(), second });
            return list.Length == 2 && list[0] == first && list[1] == second &&
                first.Scale == 1.8 && !first.X.HasValue && !first.Y.HasValue &&
                second.Scale == .6 && second.X == 14 && second.Y == 28 && second.Horizontal;
        }

        private static int SelfTest()
        {
            if (!CheckWidgetProfiles()) return 166;
            if (!CheckPowerOptions()) return 163;
            if (!CheckSettingsBackup()) return 164;
            if (!CheckPinnedOccasions()) return 160;
            if (!CheckEdgeResize()) return 156;
            if (!CheckUnits()) return 87;
            if (!CheckLunarMonths()) return 141;
            if (!CheckPersonalCalendar()) return 92;
            if (!CheckOccasionReminders()) return 111;
            if (!CheckTaskbarRoute()) return 125;
            if (!CheckPetMotion()) return 131;
            var s = new Settings { Automatic = false, Year = 2027 };
            var eve = new DateTimeOffset(2027, 2, 5, 17, 0, 0, TimeSpan.Zero);
            var d = HolidayCountdown.Find(s, eve);
            if (!d.HasValue || d.Value.Date != eve || Calendar.Name(2027) != "Đinh Mùi") return 10;
            if (Calendar.Seconds(d.Value.Date, eve.AddMilliseconds(-1)) != 1 || Calendar.Seconds(d.Value.Date, eve) != 0 ||
                Calendar.Seconds(d.Value.Date, eve.AddSeconds(1)) != 0) return 11;
            s.Automatic = true;
            if (HolidayCountdown.Find(s, eve.AddTicks(-1)).Value.Date.Year != 2027 ||
                HolidayCountdown.Find(s, eve.AddDays(7)).Value.Date.Year != 2028 ||
                HolidayCountdown.Find(s, new DateTimeOffset(2040, 1, 1, 0, 0, 0, TimeSpan.Zero)) != null) return 12;
            if (Calendar.Holidays.Single(item => item.Year == 2030).ToString("MM-dd") != "02-02") return 13;
            var newYear = new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.FromHours(7));
            var hungKings = new DateTimeOffset(2027, 4, 16, 0, 0, 0, TimeSpan.FromHours(7));
            s.SelectedHolidays = new[] { HolidaySetting.LunarNewYear, HolidaySetting.NewYear, HolidaySetting.HungKings };
            if (HolidayCountdown.Find(s, newYear.ToUniversalTime().AddSeconds(-1)).Value.Date != newYear ||
                HolidayCountdown.Find(s, newYear.AddHours(23)).Value.Date != newYear ||
                HolidayCountdown.Find(s, newYear.AddDays(1)).Value.Theme != HolidayTheme.LunarNewYear) return 63;
            s.SelectedHolidays = new[] { HolidaySetting.HungKings };
            if (HolidayCountdown.Find(s, newYear).Value.Date != hungKings ||
                HolidayCountdown.Find(s, hungKings.AddHours(23)).Value.Date != hungKings ||
                HolidayCountdown.Find(s, hungKings.AddDays(1)).Value.Date != new DateTimeOffset(2028, 4, 4, 0, 0, 0, TimeSpan.FromHours(7))) return 64;
            var offset = newYear.Offset;
            var tet2027 = new DateTimeOffset(2027, 2, 6, 0, 0, 0, offset);
            var reference = new DateTimeOffset(2026, 11, 20, 0, 0, 0, offset);
            var cultureDay = new DateTimeOffset(2026, 11, 24, 0, 0, 0, offset);
            var expectedNext = new[]
            {
                Tuple.Create(HolidayTheme.NewYear, newYear),
                Tuple.Create(HolidayTheme.LunarNewYear, tet2027),
                Tuple.Create(HolidayTheme.HungKings, hungKings),
                Tuple.Create(HolidayTheme.Reunification, new DateTimeOffset(2027, 4, 30, 0, 0, 0, offset)),
                Tuple.Create(HolidayTheme.LabourDay, new DateTimeOffset(2027, 5, 1, 0, 0, 0, offset)),
                Tuple.Create(HolidayTheme.NationalDay, new DateTimeOffset(2027, 9, 2, 0, 0, 0, offset)),
                Tuple.Create(HolidayTheme.VietnamCultureDay, cultureDay),
                Tuple.Create(HolidayTheme.MidAutumn, new DateTimeOffset(2027, 9, 15, 0, 0, 0, offset)),
                Tuple.Create(HolidayTheme.Christmas, new DateTimeOffset(2026, 12, 25, 0, 0, 0, offset))
            };
            foreach (var expected in expectedNext)
            {
                var theme = expected.Item1;
                s.SelectedHolidays = new[] { HolidaySetting.All.Single(holiday => holiday.Theme == theme).Id };
                var actual = HolidayCountdown.Find(s, reference.ToOffset(TimeSpan.FromHours(-5)));
                if (!actual.HasValue || actual.Value.Theme != theme || actual.Value.Date != expected.Item2 ||
                    actual.Value.Date.Offset != offset) return 77;
            }
            s.SelectedHolidays = new[] { HolidaySetting.VietnamCultureDay };
            if (HolidayCountdown.Find(s, cultureDay.AddYears(-1)).Value.Date != cultureDay ||
                HolidayCountdown.Find(s, cultureDay.AddHours(23)).Value.LastDay != cultureDay ||
                HolidayCountdown.Find(s, cultureDay.AddDays(1)).Value.Date != cultureDay.AddYears(1)) return 82;
            s.SelectedHolidays = HolidaySetting.All.Select(holiday => holiday.Id).ToArray();
            if (HolidayCountdown.Find(s, reference).Value.Theme != HolidayTheme.VietnamCultureDay ||
                HolidayCountdown.Find(s, cultureDay.AddDays(1)).Value.Theme != HolidayTheme.Christmas ||
                HolidayCountdown.Find(s, newYear.AddDays(1)).Value.Date != tet2027 ||
                HolidayCountdown.Find(s, tet2027.AddDays(-2)).Value.Date != tet2027 ||
                HolidayCountdown.Find(s, tet2027.AddDays(4).AddHours(23)).Value.Date != tet2027 ||
                HolidayCountdown.Find(s, tet2027.AddDays(5)).Value.Theme != HolidayTheme.HungKings) return 70;
            var april = new DateTimeOffset(2027, 4, 30, 0, 0, 0, offset);
            var september = new DateTimeOffset(2027, 9, 2, 0, 0, 0, offset);
            if (HolidayCountdown.Find(s, april).Value.Theme != HolidayTheme.Reunification ||
                HolidayCountdown.Find(s, april.AddDays(1)).Value.Theme != HolidayTheme.LabourDay ||
                HolidayCountdown.Find(s, september.AddDays(3)).Value.Date != september ||
                HolidayCountdown.Find(s, september.AddDays(4)).Value.Date != new DateTimeOffset(2027, 9, 15, 0, 0, 0, offset) ||
                HolidayCountdown.Find(s, new DateTimeOffset(2027, 12, 25, 0, 0, 0, offset)).Value.Theme != HolidayTheme.Christmas) return 71;
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "NghiLe-test-" + Guid.NewGuid().ToString("N") + ".json");
            var legacyPath = path + ".legacy";
            try
            {
                var file = new SettingsFile(path);
                if (file.Read().CounterMode != CountdownUnit.Days) return 88;
                s.X = -200; s.Y = 10; s.Glass = 100; s.Scale = .6; s.Motion = false; s.AlwaysOnTop = false;
                s.SelectedHolidays = new[] { HolidaySetting.LunarNewYear, "mid-autumn", HolidaySetting.VietnamCultureDay };
                s.SettingsWidth = 740;
                s.SettingsHeight = 700;
                s.ApricotNewYear = true;
                s.PinnedOccasionIds = new[] { HolidaySetting.VietnamCultureDay, HolidaySetting.LunarNewYear };
                s.Use3D = false;
                s.CounterMode = CountdownUnit.Weeks;
                s.ReminderDays[HolidaySetting.LunarNewYear] = 7;
                s.ReminderDays[HolidaySetting.NewYear] = 1;
                s.DeliveredReminders = new[] { "20270130|lunar-new-year|20270206" };
                if (!file.Save(s)) return 14;
                s.Year = 2030; s.Automatic = false; s.Horizontal = true;
                s.CounterMode = CountdownUnit.Months;
                if (!file.Save(s)) return 15;
                var restored = file.Read();
                if (restored.ReminderDays.Count != 2 || restored.ReminderDays[HolidaySetting.LunarNewYear] != 7 ||
                    restored.ReminderDays[HolidaySetting.NewYear] != 1 ||
                    !restored.DeliveredReminders.SequenceEqual(s.DeliveredReminders)) return 112;
                if (restored.Year != 2030 || restored.Automatic || restored.Motion || restored.AlwaysOnTop ||
                    restored.Glass != 100 || restored.Scale != .6 || restored.X != -200 || restored.Y != 10 || !restored.Horizontal ||
                    restored.CounterMode != CountdownUnit.Months ||
                    !restored.SelectedHolidays.SequenceEqual(s.SelectedHolidays) ||
                    !restored.PinnedOccasionIds.SequenceEqual(s.PinnedOccasionIds) ||
                    restored.SettingsWidth != 740 || restored.SettingsHeight != 700 || !restored.ApricotNewYear || restored.Use3D) return 16;
                File.WriteAllText(path, "{\"Glass\":88}");
                var legacyDefaults = file.Read();
                if (legacyDefaults.ReminderDays.Count != 0 || legacyDefaults.DeliveredReminders.Length != 0 ||
                    legacyDefaults.PinnedOccasionIds.Length != 0) return 113;
                if (legacyDefaults.PetEnabled || legacyDefaults.PetAnimal != PetAnimal.Cat || legacyDefaults.PetDelayMinutes != 5 ||
                    !legacyDefaults.PetRoams || legacyDefaults.PetAtEnd || legacyDefaults.PetSizePercent != 100) return 126;
                if (legacyDefaults.PauseOnBattery || legacyDefaults.DimOnSchedule ||
                    legacyDefaults.DimFromHour != 22 || legacyDefaults.DimUntilHour != 6) return 165;
                if (legacyDefaults.Scale != 1 || legacyDefaults.Glass != 100 || legacyDefaults.Horizontal ||
                    legacyDefaults.CounterMode != CountdownUnit.Days || !legacyDefaults.Use3D ||
                    !legacyDefaults.SelectedHolidays.SequenceEqual(new[] { HolidaySetting.LunarNewYear })) return 18;
                restored.Glass = 88;
                restored.SelectedHolidays = new string[0];
                if (!file.Save(restored) || file.Read().Glass != 88 || file.Read().SelectedHolidays.Length != 0) return 19;
                File.Move(path, legacyPath);
                var migrated = new SettingsFile(path, legacyPath).Read();
                if (migrated.Scale != restored.Scale || migrated.Year != restored.Year || migrated.Glass != 88 ||
                    migrated.CounterMode != CountdownUnit.Months) return 33;
                File.WriteAllText(path, "{\"Version\":4,\"CounterMode\":3}");
                if (file.Read().CounterMode != CountdownUnit.Days) return 89;
                File.WriteAllText(path, "{\"Version\":6,\"PetEnabled\":true,\"PetAnimal\":99,\"PetDelayMinutes\":0}");
                var invalidPet = file.Read();
                if (!invalidPet.PetEnabled || invalidPet.PetAnimal != PetAnimal.Cat ||
                    invalidPet.PetDelayMinutes != 5 || invalidPet.PetSizePercent != 100) return 127;
                File.WriteAllText(path, "{\"Version\":7,\"PetEnabled\":true,\"PetAnimal\":2,\"PetSizePercent\":150}");
                var removedPet = file.Read();
                if (!removedPet.PetEnabled || removedPet.PetAnimal != PetAnimal.Cat || removedPet.PetSizePercent != 150)
                    return 132;
                File.WriteAllText(path, "{oops!");
                return file.Read().Automatic ? 0 : 17;
            }
            finally { File.Delete(path); File.Delete(legacyPath); }
        }

        private static bool CheckTaskbarRoute()
        {
            var screen = new Drawing.Rectangle(-1920, 0, 1920, 1080);
            var bottom = new Drawing.Rectangle(-1920, 1032, 1920, 48);
            var rail = new TaskbarRail(screen, bottom, true, false, 52, 8);
            var start = rail.Perch(false, null, 0);
            var end = rail.Perch(true, null, 0);
            if (!screen.Contains(start) || start.Bottom != bottom.Top || end.Bottom != bottom.Top ||
                start.Right >= end.Left || rail.Perch(false, null, 1).X <= start.X) return false;
            bool returning;
            if (rail.Patrol(0, 0, out returning).Left != screen.Left || returning ||
                rail.Patrol(1868, 0, out returning).Right != screen.Right || !returning ||
                rail.Patrol(1888, 0, out returning).Right != screen.Right - 20 || !returning ||
                rail.Patrol(3736, 0, out returning).Left != screen.Left || returning ||
                rail.Patrol(0, .5, out returning).Bottom != bottom.Top) return false;
            var held = rail.Clamp(-630, -5);
            var dropped = rail.Perch(false, rail.PerchFraction(held), 0);
            if (held.Top != 0 || dropped.X != -630 || dropped.Bottom != bottom.Top) return false;

            var hidden = new TaskbarRail(screen, new Drawing.Rectangle(-1920, 1078, 1920, 2), true, false, 52, 8);
            if (hidden.Perch(false, hidden.PerchFraction(hidden.Clamp(-630, 10)), 0).Bottom != 1072) return false;
            var top = new TaskbarRail(screen, new Drawing.Rectangle(-1920, 0, 1920, 48), true, true, 52, 8);
            var left = new TaskbarRail(screen, new Drawing.Rectangle(-1920, 0, 48, 1080), false, true, 52, 8);
            var right = new TaskbarRail(screen, new Drawing.Rectangle(-48, 0, 48, 1080), false, false, 52, 8);
            return top.Perch(false, null, 0).Top == 48 && left.Perch(true, null, 0).Left == -1872 &&
                right.Perch(false, null, 0).Right == -48;
        }

        private static bool CheckPetMotion()
        {
            var times = new[] { 1d, 5d, 9d, 16d, 25d, 32.6, 43d, 47d, 49d };
            var actions = new[] { PetAction.Walk, PetAction.Rest, PetAction.Wag, PetAction.Sniff, PetAction.Stretch,
                PetAction.Hop, PetAction.Nap, PetAction.Rest, PetAction.Walk };
            for (int i = 0; i < times.Length; i++)
            {
                if (PetMotion.At(PetAnimal.Dog, times[i], true).Action != actions[i] ||
                    PetMotion.At(PetAnimal.Dog, times[i], false).Action != PetAction.Rest) return false;
            }
            return PetMotion.At(PetAnimal.Cat, 16, true).Action == PetAction.Rest &&
                PetMotion.At(PetAnimal.Cat, 41, true).Action == PetAction.Walk &&
                PetMotion.At(PetAnimal.HacLong, 41, true).Action == PetAction.Walk &&
                PetMotion.At(PetAnimal.HacLong, 41, false).Action == PetAction.Rest &&
                TaskbarPet.SizeInPixels(50, 96) == 26 && TaskbarPet.SizeInPixels(100, 96) == 52 &&
                TaskbarPet.SizeInPixels(200, 96) == 104 && TaskbarPet.SizeInPixels(200, 144) == 156 &&
                TaskbarPet.NormalizeSize(49) == 50 && TaskbarPet.NormalizeSize(201) == 200;
        }


    }
}
