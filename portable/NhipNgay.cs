using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

[assembly: AssemblyTitle(NhipNgay.Widget.ProductName)]
[assembly: AssemblyProduct(NhipNgay.Widget.ProductName)]
[assembly: AssemblyVersion("0.1.10.0")]
[assembly: AssemblyFileVersion("0.1.10.0")]

namespace NhipNgay
{
    public sealed class Settings
    {
        public int Version { get; set; }
        public bool Automatic { get; set; }
        public int Year { get; set; }
        public bool AlwaysOnTop { get; set; }
        public bool Motion { get; set; }
        public bool ApricotNewYear { get; set; }
        public bool Horizontal { get; set; }
        public int Glass { get; set; }
        public double Scale { get; set; }
        public double? X { get; set; }
        public double? Y { get; set; }
        public string[] SelectedHolidays { get; set; }
        public double SettingsWidth { get; set; }
        public double SettingsHeight { get; set; }

        public Settings()
        {
            Automatic = true;
            Year = 2027;
            AlwaysOnTop = true;
            Motion = SystemParameters.ClientAreaAnimation;
            Glass = 57;
            Scale = 1;
            SelectedHolidays = new[] { HolidaySetting.LunarNewYear };
            SettingsWidth = SettingsWindow.DefaultWidth;
            SettingsHeight = SettingsWindow.DefaultHeight;
        }
    }

    internal static class WidgetSize
    {
        internal const double Width = 200;
        internal const double Height = 250;
        internal const double HorizontalWidth = 360;
        internal const double HorizontalHeight = 84;
        internal const double CompactScale = .8;

        internal static double Clamp(double scale)
        {
            if (double.IsNaN(scale) || double.IsInfinity(scale) || scale <= 0) return 1;
            return Math.Max(.6, Math.Min(1.8, scale));
        }

        internal static Size Clock(bool horizontal)
        {
            return horizontal ? new Size(HorizontalWidth, HorizontalHeight) : new Size(Width, Height);
        }

        internal static double FromDrag(double scale, double dx, double dy, bool horizontal = false)
        {
            var size = Clock(horizontal);
            return Math.Max(.6, Math.Min(1.8, scale +
                (dx * size.Width + dy * size.Height) / (size.Width * size.Width + size.Height * size.Height)));
        }
    }

    internal static class Calendar
    {
        internal static readonly DateTimeOffset[] Holidays = Load("NhipNgay.Calendar");
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
                var s = json.Deserialize<Settings>(File.ReadAllText(source));
                if (s == null) return new Settings();
                if (!Calendar.Holidays.Any(d => d.Year == s.Year)) s.Year = Calendar.Holidays[0].Year;
                s.Glass = s.Version < 2 && s.Glass == 88 ? 100 : Math.Max(42, Math.Min(100, s.Glass));
                s.Scale = WidgetSize.Clamp(s.Scale);
                if (s.SelectedHolidays == null) s.SelectedHolidays = new[] { HolidaySetting.LunarNewYear };
                if (s.Version < 3 && s.SettingsWidth == 680 && s.SettingsHeight == 680)
                {
                    s.SettingsWidth = SettingsWindow.DefaultWidth;
                    s.SettingsHeight = SettingsWindow.DefaultHeight;
                }
                return s;
            }
            catch (IOException) { return new Settings(); }
            catch (UnauthorizedAccessException) { return new Settings(); }
            catch (ArgumentException) { return new Settings(); }
            catch (InvalidOperationException) { return new Settings(); }
        }

        internal bool Save(Settings s)
        {
            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                string temp = path + ".tmp";
                s.Version = 3;
                File.WriteAllText(temp, json.Serialize(s));
                if (File.Exists(path)) File.Replace(temp, path, null); else File.Move(temp, path);
                return true;
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }
    }

    internal sealed class Widget : Window
    {
        internal const string ProductName = "Nhịp Ngày";
        private static readonly DoubleAnimation numberFade = CreateNumberFade();
        private static readonly string[] clockNumbers = Enumerable.Range(0, 60)
            .Select(value => value.ToString("00", CultureInfo.InvariantCulture)).ToArray();
        internal static void ConfigureRendering()
        {
            // This small layered window doesn't need a hardware rendering device.
            RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
        }

        private readonly SettingsFile file;
        private readonly Settings config;
        private readonly Border glass;
        private readonly Border innerRim;
        private readonly Grid surface;
        private SettingsWindow settingsWindow;
        private PeachScene weather;
        private readonly HolidayScene holidayScene = new HolidayScene();
        private readonly Grid face = new Grid { IsHitTestVisible = false };
        private readonly StackPanel greeting = new StackPanel { IsHitTestVisible = false, Visibility = Visibility.Collapsed,
            VerticalAlignment = VerticalAlignment.Center };
        private readonly TextBlock greetingText = Label("", 24, true);
        private readonly TextBlock greetingDetail = Label("", 11, false);
        private readonly StackPanel dayCount = new StackPanel();
        private readonly Grid divider = new Grid();
        private readonly StackPanel controls = new StackPanel { Orientation = Orientation.Horizontal };
        private PanelPage panel;
        private MusicView music;
        private Button resizeGrip;
        private Point resizeStart;
        private double resizeScale;
        private bool resizing;
        private readonly TextBlock year = Label("", 14, false);
        private readonly TextBlock days = Label("", 70, true);
        private readonly TextBlock dayUnit = Label("NGÀY", 10, false);
        private readonly Grid clock = new Grid();
        private readonly TextBlock dateStatus = Label("", 11, false);
        private readonly TextBlock[] time = { Label("", 25, true), Label("", 25, true), Label("", 25, true) };
        private readonly TextBlock[] clockUnits = new TextBlock[3];
        private readonly DispatcherTimer tick = new DispatcherTimer();
        private readonly DispatcherTimer save = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        private readonly Forms.NotifyIcon tray;
        private readonly Drawing.Icon icon;
        private bool quitting;
        private bool ready;
        private bool compact;
        private DateTimeOffset? displayedTarget;
        private HolidayTheme? displayedTheme;
        private bool celebrating;
        private string unavailableStatus;
        private long displayedSeconds = long.MinValue;
        private DateTime holidayLookupDate;
        private string[] holidayLookupSelection;
        private int holidayLookupFixedYear;
        private HolidayOccurrence? holidayLookupResult;

        internal Widget(SettingsFile file)
        {
            this.file = file;
            config = file.Read();
            Title = ProductName;
            ResizeMode = ResizeMode.NoResize;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            Topmost = config.AlwaysOnTop;
            controls.IsKeyboardFocusWithinChanged += (s, e) => UpdateControls();

            surface = new Grid { ClipToBounds = true };
            glass = new Border { BorderThickness = new Thickness(1),
                BorderBrush = GoldLine("#D6D9AA7A", "#6D8E4F4C", "#A0EDC894", 125),
                Background = HolidayGlassPaint(Palette(null)), Child = surface };
            Content = new Viewbox { Stretch = Stretch.Uniform, Child = glass };
            innerRim = new Border { Margin = new Thickness(3),
                BorderThickness = new Thickness(.65), BorderBrush = GoldLine("#57F9CF9F", "#126F343E", "#4BD7A471", 135),
                IsHitTestVisible = false };
            surface.Children.Add(innerRim);
            surface.Children.Add(holidayScene);
            BuildFace();
            BuildControls();
            BuildResizeGrip();
            ApplyOrientation();
            SetScale(config.Scale);
            ApplyGlass();
            icon = Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
            tray = new Forms.NotifyIcon { Icon = icon, Text = ProductName + " · Đếm ngược ngày lễ", Visible = true };
            var menu = new Forms.ContextMenuStrip();
            menu.Items.Add("Mở widget", null, (s, e) => ShowWidget());
            menu.Items.Add("Ẩn widget", null, (s, e) => Hide());
            menu.Items.Add("Cài đặt", null, (s, e) => { if (!IsVisible) ShowWidget(); OpenSettings(); });
            menu.Items.Add("Thoát " + ProductName, null, (s, e) => Quit());
            tray.ContextMenuStrip = menu;
            tray.MouseClick += (s, e) => { if (e.Button == Forms.MouseButtons.Left) ShowWidget(); };

            PreviewMouseLeftButtonDown += (s, e) =>
            {
                if (InControl(e.OriginalSource as DependencyObject)) return;
                DragMove();
                ScheduleSave();
            };
            MouseEnter += (s, e) => UpdateControls();
            MouseLeave += (s, e) => UpdateControls();
            KeyDown += (s, e) =>
            {
                if (e.Key == Key.F2) OpenSettings();
            };
            LocationChanged += (s, e) => { if (ready) ScheduleSave(); };
            Loaded += (s, e) => { PlaceOnScreen(); ready = true; Persist(); };
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
            tick.Tick += (s, e) => TickClock();
            save.Tick += (s, e) => Persist();
            IsVisibleChanged += (s, e) =>
            {
                UpdateClockTimer();
                if (!IsVisible && !quitting)
                {
                    if (settingsWindow != null) settingsWindow.Hide();
                    Persist();
                }
                UpdateMotion();
            };
            Closing += (s, e) =>
            {
                if (quitting) Persist();
                else { e.Cancel = true; Hide(); }
            };
            Closed += (s, e) =>
            {
                Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
                tick.Stop(); save.Stop();
                if (music != null) music.Dispose();
                tray.Dispose(); icon.Dispose();
                System.Windows.Application.Current.Shutdown();
            };
            UpdateClock();
        }

        internal void ShowWidget() { Show(); PlaceOnScreen(); Activate(); }

        private void OnDisplaySettingsChanged(object sender, EventArgs args)
        {
            Dispatcher.BeginInvoke(new Action(() => { if (IsVisible) PlaceOnScreen(); }));
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
        private void Persist()
        {
            save.Stop();
            if (ready)
            {
                var size = WidgetSize.Clock(config.Horizontal);
                config.X = Left + Width - size.Width * config.Scale;
                config.Y = Top + Height - size.Height * config.Scale;
            }
            file.Save(config);
        }

        private void SetScale(double scale)
        {
            config.Scale = WidgetSize.Clamp(scale);
            ResizeWidget(false);
            bool useCompact = !config.Horizontal && config.Scale <= WidgetSize.CompactScale;
            if (useCompact != compact) ApplyFaceTypography(useCompact);
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

        private static LinearGradientBrush GoldLine(string first, string middle, string last, double angle)
        {
            var brush = new LinearGradientBrush(ColorBrush(first).Color, ColorBrush(last).Color, angle);
            brush.GradientStops.Insert(1, new GradientStop(ColorBrush(middle).Color, .55));
            return brush;
        }

        private static TextBlock Label(string value, double size, bool serif)
        {
            var text = new TextBlock { Text = value, FontSize = size,
                FontFamily = new FontFamily(serif ? "Georgia" : "Segoe UI"),
                Foreground = ColorBrush("#F5DCA8"), TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center };
            Typography.SetNumeralAlignment(text, FontNumeralAlignment.Tabular);
            Typography.SetNumeralStyle(text, FontNumeralStyle.Lining);
            return text;
        }

        private void ApplyGlass()
        {
            glass.Background.Opacity = config.Glass / 100.0;
        }

        private sealed class HolidayPalette
        {
            internal readonly string From, Middle, To, Ink, Accent, Glow;

            internal HolidayPalette(string from, string middle, string to, string ink, string accent, string glow)
            {
                From = from;
                Middle = middle;
                To = to;
                Ink = ink;
                Accent = accent;
                Glow = glow;
            }
        }

        private static HolidayPalette Palette(HolidayTheme? theme)
        {
            switch (theme)
            {
                case HolidayTheme.LunarNewYear: return new HolidayPalette("#842C3B", "#4D1828", "#27121D", "#FFF0CD", "#F2BB83", "#A9A64355");
                case HolidayTheme.NewYear: return new HolidayPalette("#223254", "#122138", "#10152A", "#E8F2FF", "#A8C2EE", "#57486DB5");
                case HolidayTheme.HungKings: return new HolidayPalette("#543827", "#38251F", "#20191A", "#F8D7A0", "#D7A76F", "#63895B38");
                case HolidayTheme.Reunification: return new HolidayPalette("#53634C", "#2A4135", "#192C27", "#FFF1CE", "#EAC583", "#659B8D57");
                case HolidayTheme.LabourDay: return new HolidayPalette("#344F67", "#24384E", "#192839", "#FFEDDA", "#EE9F72", "#75755A58");
                case HolidayTheme.NationalDay: return new HolidayPalette("#9B2733", "#671C28", "#361522", "#FFF1CB", "#F7CD78", "#739B413D");
                case HolidayTheme.MidAutumn: return new HolidayPalette("#45385F", "#2D244B", "#201B39", "#FFF1CB", "#F6BD8B", "#75684D8D");
                case HolidayTheme.Christmas: return new HolidayPalette("#2E574A", "#1B3934", "#112924", "#FFF5DE", "#EBC58B", "#68548467");
                default: return new HolidayPalette("#511B2C", "#361725", "#200F19", "#FFE4B9", "#EDC79E", "#50883F50");
            }
        }

        private static DrawingBrush HolidayGlassPaint(HolidayPalette palette)
        {
            var bounds = new RectangleGeometry(new Rect(0, 0, WidgetSize.Width, WidgetSize.Height));
            var fill = GoldLine(palette.From, palette.Middle, palette.To, 126);
            var highlight = ColorBrush(palette.Glow).Color;
            var glow = new RadialGradientBrush(highlight, Color.FromArgb(0, highlight.R, highlight.G, highlight.B))
            {
                Center = new Point(.26, .14), GradientOrigin = new Point(.26, .10), RadiusX = .72, RadiusY = .87
            };
            var layers = new DrawingGroup();
            layers.Children.Add(new GeometryDrawing(fill, null, bounds));
            layers.Children.Add(new GeometryDrawing(glow, null, bounds));
            return new DrawingBrush(layers);
        }

        private void ApplyHolidayTheme(HolidayTheme? theme)
        {
            var palette = theme == HolidayTheme.LunarNewYear && config.ApricotNewYear
                ? new HolidayPalette("#8D361F", "#641F22", "#341619", "#FFF0C4", "#EDBE78", "#A7CE6243")
                : Palette(theme);
            var accent = palette.Accent.Substring(1);
            glass.Background = HolidayGlassPaint(palette);
            glass.BorderBrush = GoldLine(palette.Ink, "#88" + accent, palette.Accent, 125);
            innerRim.BorderBrush = GoldLine("#60" + accent, "#18" + accent, "#60" + accent, 135);
            var caption = ColorBrush(palette.Ink);
            var small = ColorBrush(palette.Accent);
            year.Foreground = caption;
            dayUnit.Foreground = small;
            dateStatus.Foreground = small;
            days.Foreground = GoldLine("#FFFFFF", palette.Ink, palette.Accent, 90);
            greetingText.Foreground = caption;
            greetingDetail.Foreground = small;
            for (int i = 0; i < time.Length; i++)
            {
                time[i].Foreground = caption;
                clockUnits[i].Foreground = small;
            }
            ((Border)divider.Children[0]).Background = GoldLine("#00" + accent, "#B0" + accent, "#00" + accent, 0);
            ((Rectangle)divider.Children[1]).Fill = small;
            foreach (var separator in clock.Children.OfType<Border>())
                separator.Background = GoldLine("#00" + accent, "#50" + accent, "#00" + accent, 90);
            ApplyGlass();
            bool showPeach = theme == HolidayTheme.LunarNewYear && !config.ApricotNewYear;
            if (showPeach && weather == null)
            {
                weather = new PeachScene();
                weather.SetHorizontal(config.Horizontal);
                surface.Children.Insert(surface.Children.IndexOf(holidayScene) + 1, weather);
            }
            if (weather != null) weather.Visibility = showPeach ? Visibility.Visible : Visibility.Collapsed;
            holidayScene.ShowTheme(theme, config.ApricotNewYear);
            UpdateMotion();
        }

        private void BuildFace()
        {
            year.Foreground = ColorBrush("#FFE4B9");
            year.TextTrimming = TextTrimming.CharacterEllipsis;
            days.Foreground = GoldLine("#FFF0CD", "#F9DDAB", "#E4AE73", 90);
            dayUnit.Foreground = ColorBrush("#EDC79E");
            face.Children.Add(year);
            dayCount.Children.Add(days);
            dayCount.Children.Add(dayUnit);
            face.Children.Add(dayCount);
            divider.Children.Add(new Border { Height = .7, VerticalAlignment = VerticalAlignment.Center,
                Background = GoldLine("#00E8B583", "#ACE8B583", "#00E8B583", 0) });
            divider.Children.Add(new Rectangle { Width = 4, Height = 4, Fill = ColorBrush("#ECC698"),
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                RenderTransform = new RotateTransform(45), RenderTransformOrigin = new Point(.5, .5) });
            face.Children.Add(divider);
            string[] units = { "GIỜ", "PHÚT", "GIÂY" };
            for (int i = 0; i < time.Length; i++)
            {
                clock.ColumnDefinitions.Add(new ColumnDefinition());
                var pair = new StackPanel();
                pair.Children.Add(time[i]);
                var unit = Label(units[i], 8.75, false);
                clockUnits[i] = unit;
                unit.Foreground = ColorBrush("#EDC79E");
                pair.Children.Add(unit);
                Grid.SetColumn(pair, i);
                clock.Children.Add(pair);
                if (i == 0) continue;
                var separator = new Border { Width = .65, Height = 23, VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Left, Background = GoldLine("#00DAA373", "#5BDAA373", "#00DAA373", 90) };
                Grid.SetColumn(separator, i);
                clock.Children.Add(separator);
            }
            face.Children.Add(clock);
            dateStatus.FontSize = 10;
            dateStatus.Margin = new Thickness(0, 8, 0, 0);
            dateStatus.Foreground = ColorBrush("#E7BF93");
            face.Children.Add(dateStatus);
            surface.Children.Add(face);
            greetingText.TextWrapping = TextWrapping.Wrap;
            greetingDetail.Margin = new Thickness(0, 5, 0, 0);
            greeting.Children.Add(greetingText);
            greeting.Children.Add(greetingDetail);
            surface.Children.Add(greeting);
        }

        private void ApplyOrientation()
        {
            bool horizontal = config.Horizontal;
            var size = WidgetSize.Clock(horizontal);
            double radius = horizontal ? 14 : 18;
            glass.Width = size.Width;
            glass.Height = size.Height;
            glass.CornerRadius = new CornerRadius(radius);
            innerRim.CornerRadius = new CornerRadius(radius - 4);
            surface.Clip = new RectangleGeometry(new Rect(0, 0, size.Width - 2, size.Height - 2), radius - 1, radius - 1);
            if (weather != null) weather.SetHorizontal(horizontal);

            face.RowDefinitions.Clear();
            face.ColumnDefinitions.Clear();
            if (horizontal)
            {
                face.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
                face.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(81) });
                face.ColumnDefinitions.Add(new ColumnDefinition());
            }
            else
            {
                for (int i = 0; i < face.Children.Count; i++)
                    face.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }
            for (int i = 0; i < face.Children.Count; i++) Grid.SetRow(face.Children[i], horizontal ? 0 : i);
            Grid.SetColumn(dayCount, horizontal ? 1 : 0);
            Grid.SetColumn(divider, horizontal ? 1 : 0);
            Grid.SetColumn(clock, horizontal ? 2 : 0);

            year.VerticalAlignment = horizontal ? VerticalAlignment.Bottom : VerticalAlignment.Center;
            year.Margin = horizontal ? new Thickness(5, 0, 5, 8) : new Thickness(0);
            dayCount.VerticalAlignment = VerticalAlignment.Center;
            divider.Width = horizontal ? 50 : 152;
            divider.Height = 11;
            divider.HorizontalAlignment = horizontal ? HorizontalAlignment.Left : HorizontalAlignment.Center;
            divider.VerticalAlignment = VerticalAlignment.Center;
            divider.Margin = horizontal ? new Thickness(-5, 0, 0, 0) : new Thickness(0, 3, 0, 0);
            divider.LayoutTransform = horizontal ? new RotateTransform(90) : Transform.Identity;
            clock.VerticalAlignment = VerticalAlignment.Center;
            foreach (var number in time) number.FontSize = horizontal ? 24 : 25;
            ApplyFaceTypography(!horizontal && config.Scale <= WidgetSize.CompactScale);
            if (ready) ResizeWidget(true);
        }

        private void ApplyFaceTypography(bool useCompact)
        {
            compact = useCompact;
            holidayScene.SetCompact(useCompact);
            bool horizontal = config.Horizontal;
            face.VerticalAlignment = horizontal ? VerticalAlignment.Stretch : VerticalAlignment.Top;
            face.Margin = horizontal ? new Thickness(0, 0, 18, 0) : new Thickness(12, compact ? 49 : 51, 12, 0);
            year.FontSize = horizontal ? 12 : compact ? 17 : 14;
            greeting.Margin = horizontal ? new Thickness(128, 0, 19, 0) : new Thickness(16, 42, 16, 0);
            greetingText.FontSize = horizontal ? 20 : 26;
            days.Height = horizontal ? 53 : compact ? 97 : 77;
            dayUnit.FontSize = horizontal ? 9 : compact ? 11 : 9.5;
            clock.Margin = horizontal ? new Thickness(0, 1, 0, 0) : new Thickness(0, 2, 0, 0);
            dateStatus.Visibility = horizontal || compact ? Visibility.Collapsed : Visibility.Visible;
            if (unavailableStatus != null) year.Text = horizontal || compact ? unavailableStatus : ProductName;
            UpdateDayPresentation();
        }

        private void UpdateDayPresentation()
        {
            bool abbreviated = compact || config.Horizontal;
            bool wideNumber = days.Text.Length >= 4;
            days.FontSize = config.Horizontal ? (wideNumber ? 32 : days.Text.Length == 3 ? 41 : 46) : wideNumber ? 62 : compact ? 87 : 70;
            dayUnit.Text = abbreviated && days.Text == "–" ? (unavailableStatus == "Hết dữ liệu lịch" ? "HẾT DỮ LIỆU" : "NGÀY") :
                abbreviated && displayedSeconds == 0 ? (displayedTheme == HolidayTheme.LunarNewYear ? "TẾT ĐÃ ĐẾN" : "HÔM NAY") : "NGÀY";
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
            var untilNextSecond = TimeSpan.TicksPerSecond - now.UtcTicks % TimeSpan.TicksPerSecond;
            tick.Interval = TimeSpan.FromMilliseconds(Math.Ceiling((double)untilNextSecond / TimeSpan.TicksPerMillisecond));
            UpdateClock(now);
        }

        private void UpdateClock() { UpdateClock(DateTimeOffset.UtcNow); }

        private void UpdateClock(DateTimeOffset now)
        {
            var local = now.ToOffset(HolidayCountdown.VietnamOffset);
            int fixedYear = config.Automatic ? 0 : config.Year;
            if (holidayLookupDate != local.Date || holidayLookupSelection != config.SelectedHolidays || holidayLookupFixedYear != fixedYear)
            {
                holidayLookupResult = HolidayCountdown.FindForDay(config, local);
                holidayLookupDate = local.Date;
                holidayLookupSelection = config.SelectedHolidays;
                holidayLookupFixedYear = fixedYear;
            }
            var occurrence = holidayLookupResult;
            var target = occurrence.HasValue ? (DateTimeOffset?)occurrence.Value.Date : null;
            var theme = occurrence.HasValue ? (HolidayTheme?)occurrence.Value.Theme : null;
            string unavailable = target.HasValue ? null : config.SelectedHolidays.Length == 0 ? "Chưa chọn ngày nghỉ" : "Hết dữ liệu lịch";
            var seconds = target.HasValue ? Calendar.Seconds(target.Value, now) : -1;
            bool showGreeting = seconds == 0 && local.Date <= occurrence.Value.LastDay.Date &&
                (theme == HolidayTheme.LunarNewYear || theme == HolidayTheme.MidAutumn || theme == HolidayTheme.Christmas);
            bool themeChanged = theme != displayedTheme || unavailable != unavailableStatus;
            bool targetChanged = target != displayedTarget || themeChanged;
            if (!targetChanged && seconds == displayedSeconds && celebrating == showGreeting) return;

            bool dateChanged = targetChanged || (seconds == 0) != (displayedSeconds == 0);
            bool daysChanged = targetChanged || seconds / 86400 != displayedSeconds / 86400;
            displayedTarget = target;
            displayedSeconds = seconds;
            displayedTheme = theme;
            unavailableStatus = unavailable;
            if (themeChanged) ApplyHolidayTheme(theme);
            if (targetChanged || celebrating != showGreeting) UpdateGreeting(showGreeting, theme, target, local);
            if (!target.HasValue)
            {
                year.Text = config.Horizontal || compact ? unavailable : ProductName;
                days.Text = "–";
                foreach (var unit in time) unit.Text = "–";
                dateStatus.Text = unavailable;
                ToolTip = unavailable;
                UpdateDayPresentation();
                return;
            }
            var day = target.Value;
            if (targetChanged)
            {
                year.Text = theme == HolidayTheme.LunarNewYear ? Calendar.Name(day.Year) + " · " + day.Year :
                    HolidaySetting.All.First(holiday => holiday.Theme == theme).Name;
                ToolTip = year.Text + " · " + day.ToString("dd/MM/yyyy") + " · Giờ Việt Nam";
            }
            if (daysChanged) SetNumber(days, (seconds / 86400).ToString(CultureInfo.InvariantCulture));
            SetNumber(time[0], clockNumbers[seconds / 3600 % 24]);
            SetNumber(time[1], clockNumbers[seconds / 60 % 60]);
            SetNumber(time[2], clockNumbers[seconds % 60]);
            if (dateChanged)
                dateStatus.Text = seconds == 0 ? (theme == HolidayTheme.LunarNewYear ? "Tết này đã đến" : "Hôm nay") :
                    day.ToString("dd/MM/yyyy") + " · Giờ Việt Nam";
            if (daysChanged || dateChanged) UpdateDayPresentation();
        }

        private void UpdateGreeting(bool show, HolidayTheme? theme, DateTimeOffset? target, DateTimeOffset now)
        {
            celebrating = show;
            if (show)
            {
                greetingText.Text = theme == HolidayTheme.LunarNewYear ? "Chúc mừng năm mới" :
                    theme == HolidayTheme.MidAutumn ? "Vui Tết Trung thu" : "Giáng sinh an lành";
                greetingDetail.Text = theme == HolidayTheme.LunarNewYear ? Calendar.Name(target.Value.Year) + " · " + target.Value.Year :
                    now.ToString("dd/MM");
            }
            greeting.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            face.Visibility = show ? Visibility.Hidden : Visibility.Visible;
            holidayScene.SetCelebration(show);
            UpdateMotion();
        }

        private void SetNumber(TextBlock view, string value)
        {
            if (view.Text == value) return;
            view.Text = value;
            if (config.Motion && IsVisible)
                view.BeginAnimation(OpacityProperty, numberFade);
        }

        private static DoubleAnimation CreateNumberFade()
        {
            var fade = new DoubleAnimation(.9, 1, TimeSpan.FromMilliseconds(170)) { FillBehavior = FillBehavior.Stop };
            Timeline.SetDesiredFrameRate(fade, PeachScene.FramesPerSecond);
            fade.Freeze();
            return fade;
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
                Margin = new Thickness(0, 0, 4, 0), MinHeight = 22 };
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
            controls.Children.Add(MakeButton("×", "Ẩn widget", Hide));
            surface.Children.Add(controls);
        }

        private void BuildSettings()
        {
            panel = new PanelPage("Tùy chọn", "Về ngày lễ và sự kiện", OpenSettings);
            panel.MaxWidth = 480;
            panel.Margin = new Thickness(24);
            var muted = (Brush)panel.FindResource("PanelMuted");
            var fields = new StackPanel();
            AddStartupSetting(fields);
            fields.Children.Add(new TextBlock { Text = "ĐẾM NGƯỢC", Foreground = muted, FontSize = 9,
                FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 1, 0, 4) });
            var select = new ComboBox { Margin = new Thickness(0, 0, 0, 4) };
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
                UpdateClock();
                Persist();
            };
            fields.Children.Add(select);
            fields.Children.Add(new TextBlock { Text = "HIỂN THỊ", Foreground = muted, FontSize = 9, FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 2, 0, 2) });
            var layout = new ComboBox { Width = 60, MinHeight = 24, Margin = new Thickness(4, 0, 0, 0) };
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
            var layoutRow = new DockPanel();
            DockPanel.SetDock(layout, Dock.Right);
            layoutRow.Children.Add(layout);
            layoutRow.Children.Add(new TextBlock { Text = "Bố cục", FontSize = 11, VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap });
            fields.Children.Add(layoutRow);
            fields.Children.Add(MakeSwitch("Luôn nổi", "Luôn nổi trên màn hình", config.AlwaysOnTop,
                value => { config.AlwaysOnTop = value; Topmost = value; }));
            fields.Children.Add(MakeSwitch("Chuyển động", "Hiệu ứng chuyển động", config.Motion,
                value => { config.Motion = value; UpdateMotion(); }));

            var glassHeading = new DockPanel { Margin = new Thickness(0, 2, 0, 0) };
            var valueLabel = new TextBlock { Text = config.Glass + "%", FontSize = 10, Foreground = muted };
            DockPanel.SetDock(valueLabel, Dock.Right);
            glassHeading.Children.Add(valueLabel);
            glassHeading.Children.Add(new TextBlock { Text = "Độ đậm kính", FontSize = 11 });
            fields.Children.Add(glassHeading);
            var slider = new Slider { Minimum = 42, Maximum = 100, Value = config.Glass, TickFrequency = 1,
                IsSnapToTickEnabled = true, IsMoveToPointEnabled = true };
            System.Windows.Automation.AutomationProperties.SetName(slider, "Độ đậm kính");
            slider.ValueChanged += (s, e) =>
            {
                config.Glass = (int)e.NewValue;
                valueLabel.Text = config.Glass + "%";
                ApplyGlass();
                ScheduleSave();
            };
            fields.Children.Add(slider);

            var scroller = new ScrollViewer { Content = fields, VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, PanningMode = PanningMode.VerticalOnly, Focusable = false };
            var musicButton = new Button { Content = "Nhạc  ›", Style = (Style)panel.FindResource("PanelPrimary") };
            musicButton.Click += (s, e) => ShowMusic();
            panel.SetContent(scroller, musicButton);
        }

        private void BuildResizeGrip()
        {
            resizeGrip = MakeButton("↘", "Kéo để thu nhỏ hoặc phóng to. Dùng các phím mũi tên để chỉnh.", null);
            resizeGrip.Width = resizeGrip.Height = 23;
            resizeGrip.Padding = new Thickness(0);
            resizeGrip.Margin = new Thickness(0, 0, 5, 5);
            resizeGrip.HorizontalAlignment = HorizontalAlignment.Right;
            resizeGrip.VerticalAlignment = VerticalAlignment.Bottom;
            resizeGrip.Cursor = Cursors.SizeNWSE;
            resizeGrip.Opacity = 0;
            System.Windows.Automation.AutomationProperties.SetName(resizeGrip, "Đổi kích thước widget");
            resizeGrip.IsKeyboardFocusWithinChanged += (s, e) => UpdateControls();
            resizeGrip.PreviewMouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                resizeGrip.Focus();
                resizeStart = e.GetPosition(this);
                resizeScale = config.Scale;
                resizing = resizeGrip.CaptureMouse();
                UpdateControls();
            };
            resizeGrip.PreviewMouseMove += (s, e) =>
            {
                if (!resizing) return;
                Vector delta = e.GetPosition(this) - resizeStart;
                SetScale(WidgetSize.FromDrag(resizeScale, delta.X, delta.Y, config.Horizontal));
            };
            resizeGrip.PreviewMouseLeftButtonUp += (s, e) =>
            {
                if (!resizing) return;
                e.Handled = true;
                resizeGrip.ReleaseMouseCapture();
            };
            resizeGrip.LostMouseCapture += (s, e) =>
            {
                if (!resizing) return;
                resizing = false;
                PlaceOnScreen();
                Persist();
                UpdateControls();
            };
            resizeGrip.PreviewKeyDown += (s, e) =>
            {
                int direction = e.Key == Key.Up || e.Key == Key.Right ? 1 :
                    e.Key == Key.Down || e.Key == Key.Left ? -1 : 0;
                if (direction == 0) return;
                e.Handled = true;
                SetScale(WidgetSize.Clamp(config.Scale + direction * .05));
                PlaceOnScreen();
                Persist();
            };
            surface.Children.Add(resizeGrip);
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
            var input = new CheckBox { Content = new TextBlock { Text = name, TextWrapping = TextWrapping.Wrap }, MinHeight = 23,
                ToolTip = tooltip, IsChecked = value };
            System.Windows.Automation.AutomationProperties.SetName(input, tooltip);
            return input;
        }

        private static void AddStartupSetting(StackPanel fields)
        {
            var startup = StartupShortcut.ForCurrentUser();
            var input = CreateSwitch("Mở cùng Windows", "Tự mở " + ProductName + " khi đăng nhập Windows", false);
            fields.Children.Add(input);
            var message = new TextBlock { FontSize = 10, Foreground = ColorBrush("#FFD2D6"),
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

        private void OpenSettings()
        {
            if (settingsWindow == null)
                settingsWindow = new SettingsWindow(this, config, () => { UpdateClock(); Persist(); },
                    ScheduleSave, ShowOptions, SetSpringStyle);
            settingsWindow.OpenHolidays();
        }

        private void ShowOptions()
        {
            if (panel == null) BuildSettings();
            settingsWindow.OpenPage(panel);
        }

        private void SetSpringStyle(bool apricot)
        {
            if (config.ApricotNewYear == apricot) return;
            config.ApricotNewYear = apricot;
            if (displayedTheme == HolidayTheme.LunarNewYear) ApplyHolidayTheme(displayedTheme);
            Persist();
        }

        private void ShowMusic()
        {
            if (music == null)
            {
                music = new MusicView(new Playlist(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nhac")),
                    ShowOptions);
                music.MaxWidth = 520;
                music.Margin = new Thickness(24);
            }
            settingsWindow.OpenPage(music);
        }

        private void UpdateControls()
        {
            controls.Opacity = IsMouseOver || controls.IsKeyboardFocusWithin ? 1 : 0;
            if (resizeGrip != null)
                resizeGrip.Opacity = IsMouseOver || resizing || resizeGrip.IsKeyboardFocusWithin ? 1 : 0;
        }

        private void UpdateMotion()
        {
            bool enabled = config.Motion && IsVisible;
            if (weather != null) weather.SetMotion(enabled && displayedTheme == HolidayTheme.LunarNewYear && !config.ApricotNewYear);
            holidayScene.SetMotion(enabled);
            if (enabled) return;
            days.BeginAnimation(OpacityProperty, null);
            foreach (var number in time) number.BeginAnimation(OpacityProperty, null);
        }

        internal int CheckWindowLifecycle()
        {
            if (!IsVisible || !AllowsTransparency || ShowInTaskbar || !tray.Visible ||
                !tray.Text.StartsWith(ProductName, StringComparison.Ordinal) ||
                !tray.ContextMenuStrip.Items.Cast<Forms.ToolStripItem>().Any(item => item.Text == "Thoát " + ProductName)) return 21;
            if (settingsWindow != null || panel != null || music != null || !tick.IsEnabled) return 36;
            if (Math.Abs(ActualWidth - 200) > 1 || Math.Abs(ActualHeight - 250) > 1) return 22;
            config.Glass = 100;
            ApplyGlass();
            var fill = (DrawingBrush)glass.Background;
            var baseLayer = (GeometryDrawing)((DrawingGroup)fill.Drawing).Children[0];
            if (fill.Opacity != 1 || ((LinearGradientBrush)baseLayer.Brush).GradientStops.Any(stop => stop.Color.A != 255)) return 27;
            if (Title != ProductName || dateStatus.Visibility != Visibility.Visible ||
                year.FontSize != 14 || days.FontSize != 70 || dateStatus.TranslatePoint(new Point(), glass).Y + dateStatus.ActualHeight > 245) return 30;
            SetScale(.8);
            UpdateLayout();
            if (dateStatus.Visibility != Visibility.Collapsed || year.FontSize != 17 || days.FontSize != 87 ||
                clock.Visibility != Visibility.Visible || clock.TranslatePoint(new Point(), glass).Y + clock.ActualHeight > 250) return 31;
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
            if (weather.Visibility != Visibility.Collapsed || !celebrating ||
                (config.Motion && !holidayScene.IsAnimating)) return 73;
            config.ApricotNewYear = false;
            ApplyHolidayTheme(displayedTheme);
            if (weather.Visibility != Visibility.Visible) return 74;
            UpdateClock(midnight.AddSeconds(-2));
            if (time[2].Text != "02" || dayUnit.Text != "NGÀY") return 55;
            config.Automatic = true;
            config.SelectedHolidays = new[] { HolidaySetting.NewYear, HolidaySetting.HungKings };
            config.Motion = true;
            var newYear = new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.FromHours(7));
            UpdateClock(newYear.AddSeconds(-1));
            if (displayedTheme != HolidayTheme.NewYear || year.Text != "Tết Dương lịch" ||
                time[2].Text != "01" || weather.IsAnimating || weather.Visibility != Visibility.Collapsed ||
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
            if (!weather.IsAnimating || !weather.IsDescendantOf(glass)) return 23;
            config.Motion = false;
            UpdateMotion();
            if (weather.IsAnimating || !tick.IsEnabled || weather.Visibility != Visibility.Visible) return 35;
            config.Motion = true;
            UpdateMotion();
            SetScale(.6);
            var widgetPosition = new Point(Left, Top);
            OpenSettings();
            UpdateLayout();
            settingsWindow.UpdateLayout();
            if (!settingsWindow.IsVisible || settingsWindow.Title != "Cài đặt · " + ProductName ||
                !weather.IsAnimating || !tick.IsEnabled || panel != null || music != null) return 24;
            if (settingsWindow.ActualWidth < settingsWindow.MinWidth) return 38;
            if (Math.Abs(ActualWidth - 120) > 1 || Math.Abs(ActualHeight - 150) > 1) return 56;
            if (widgetPosition != new Point(Left, Top)) return 57;
            ShowOptions();
            ShowMusic();
            settingsWindow.UpdateLayout();
            if (!music.IsVisible || music.ActualWidth < 200 ||
                music.ActualHeight + music.Margin.Top > settingsWindow.ActualHeight || !weather.IsAnimating) return 34;

            OpenSettings();
            config.Horizontal = true;
            ApplyOrientation();
            settingsWindow.Close();
            UpdateLayout();
            if (settingsWindow.IsVisible || Math.Abs(ActualWidth - 216) > 1 ||
                Math.Abs(ActualHeight - 50.4) > 1 || !weather.IsAnimating) return 50;
            var horizontalScale = WidgetSize.FromDrag(.6, 144, 33.6, true);
            SetScale(horizontalScale);
            UpdateLayout();
            if (Math.Abs(ActualWidth - 360) > 1 || Math.Abs(ActualHeight - 84) > 1 || dateStatus.Visibility != Visibility.Collapsed ||
                clock.TranslatePoint(new Point(clock.ActualWidth, 0), glass).X > glass.ActualWidth ||
                days.TranslatePoint(new Point(), glass).X >= clock.TranslatePoint(new Point(), glass).X) return 51;

            OpenSettings();
            UpdateLayout();
            if (!settingsWindow.IsVisible || Math.Abs(ActualWidth - 360) > 1 ||
                Math.Abs(ActualHeight - 84) > 1 || !tick.IsEnabled || !weather.IsAnimating) return 52;
            config.Horizontal = false;
            ApplyOrientation();
            settingsWindow.Hide();
            if (!tick.IsEnabled) return 37;
            Close();
            if (IsVisible || tick.IsEnabled || weather.IsAnimating) return 25;
            ShowWidget();
            return IsVisible && tick.IsEnabled && weather.IsAnimating ? 0 : 26;
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
                app.SessionEnding += (s, e) => app.Shutdown();
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                // Keep the settings directory used by builds released as Hẹn Tết.
                var path = System.IO.Path.Combine(appData, "HenTet", "settings.json");
                var legacy = System.IO.Path.Combine(appData, "Nocturne", "settings.json");
                var widget = new Widget(new SettingsFile(path, legacy));
                StartupShortcut.ForCurrentUser().UpgradeLegacyTarget();
                var waiter = ThreadPool.RegisterWaitForSingleObject(reveal,
                    (s, timeout) => widget.Dispatcher.BeginInvoke(new Action(widget.ShowWidget)), null, Timeout.Infinite, false);
                try { return app.Run(widget); }
                finally { waiter.Unregister(null); }
            }
        }

        private static int SmokeTest()
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "NhipNgay-ui-test-" + Guid.NewGuid().ToString("N") + ".json");
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

        private static int SelfTest()
        {
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
            var expectedNext = new[]
            {
                Tuple.Create(HolidayTheme.NewYear, newYear),
                Tuple.Create(HolidayTheme.LunarNewYear, tet2027),
                Tuple.Create(HolidayTheme.HungKings, hungKings),
                Tuple.Create(HolidayTheme.Reunification, new DateTimeOffset(2027, 4, 30, 0, 0, 0, offset)),
                Tuple.Create(HolidayTheme.LabourDay, new DateTimeOffset(2027, 5, 1, 0, 0, 0, offset)),
                Tuple.Create(HolidayTheme.NationalDay, new DateTimeOffset(2027, 9, 2, 0, 0, 0, offset)),
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
            s.SelectedHolidays = HolidaySetting.All.Select(holiday => holiday.Id).ToArray();
            if (HolidayCountdown.Find(s, reference).Value.Theme != HolidayTheme.Christmas ||
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
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "NhipNgay-test-" + Guid.NewGuid().ToString("N") + ".json");
            var legacyPath = path + ".legacy";
            try
            {
                var file = new SettingsFile(path);
                s.X = -200; s.Y = 10; s.Glass = 100; s.Scale = .6; s.Motion = false; s.AlwaysOnTop = false;
                s.SelectedHolidays = new[] { HolidaySetting.LunarNewYear, "mid-autumn" };
                s.SettingsWidth = 740;
                s.SettingsHeight = 700;
                s.ApricotNewYear = true;
                if (!file.Save(s)) return 14;
                s.Year = 2030; s.Automatic = false; s.Horizontal = true;
                if (!file.Save(s)) return 15;
                var restored = file.Read();
                if (restored.Year != 2030 || restored.Automatic || restored.Motion || restored.AlwaysOnTop ||
                    restored.Glass != 100 || restored.Scale != .6 || restored.X != -200 || restored.Y != 10 || !restored.Horizontal ||
                    !restored.SelectedHolidays.SequenceEqual(s.SelectedHolidays) ||
                    restored.SettingsWidth != 740 || restored.SettingsHeight != 700 || !restored.ApricotNewYear) return 16;
                File.WriteAllText(path, "{\"Glass\":88}");
                var legacyDefaults = file.Read();
                if (legacyDefaults.Scale != 1 || legacyDefaults.Glass != 100 || legacyDefaults.Horizontal ||
                    !legacyDefaults.SelectedHolidays.SequenceEqual(new[] { HolidaySetting.LunarNewYear })) return 18;
                restored.Glass = 88;
                restored.SelectedHolidays = new string[0];
                if (!file.Save(restored) || file.Read().Glass != 88 || file.Read().SelectedHolidays.Length != 0) return 19;
                File.Move(path, legacyPath);
                var migrated = new SettingsFile(path, legacyPath).Read();
                if (migrated.Scale != restored.Scale || migrated.Year != restored.Year || migrated.Glass != 88) return 33;
                File.WriteAllText(path, "{oops!");
                return file.Read().Automatic ? 0 : 17;
            }
            finally { File.Delete(path); File.Delete(legacyPath); }
        }
    }
}
