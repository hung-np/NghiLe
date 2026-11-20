using System;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using Forms = System.Windows.Forms;

namespace NhipNgay
{
    internal sealed class HolidaySetting
    {
        internal const string LunarNewYear = "lunar-new-year";
        internal const string NewYear = "new-year";
        internal const string HungKings = "hung-kings";
        internal static readonly HolidaySetting[] PublicHolidays =
        {
            new HolidaySetting(HolidayTheme.NewYear, NewYear, "Tết Dương lịch", 1, 1),
            new HolidaySetting(HolidayTheme.LunarNewYear, LunarNewYear, "Tết Nguyên đán", 1, 1, true),
            new HolidaySetting(HolidayTheme.HungKings, HungKings, "Giỗ Tổ Hùng Vương", 10, 3, true),
            new HolidaySetting(HolidayTheme.Reunification, "reunification", "Giải phóng miền Nam", 30, 4),
            new HolidaySetting(HolidayTheme.LabourDay, "labour-day", "Quốc tế Lao động", 1, 5),
            new HolidaySetting(HolidayTheme.NationalDay, "national-day", "Quốc khánh", 2, 9)
        };
        internal static readonly HolidaySetting[] Observances =
        {
            new HolidaySetting(HolidayTheme.MidAutumn, "mid-autumn", "Trung thu", 15, 8, true),
            new HolidaySetting(HolidayTheme.Christmas, "christmas", "Giáng sinh", 25, 12)
        };
        internal static readonly HolidaySetting[] All = PublicHolidays.Concat(Observances).ToArray();

        internal readonly HolidayTheme Theme;
        internal readonly string Id;
        internal readonly string Name;
        internal readonly int Day;
        internal readonly int Month;
        internal readonly bool IsLunar;

        private HolidaySetting(HolidayTheme theme, string id, string name, int day, int month, bool isLunar = false)
        {
            Theme = theme;
            Id = id;
            Name = name;
            Day = day;
            Month = month;
            IsLunar = isLunar;
        }
    }

    internal sealed class SettingsWindow : Window
    {
        internal const double DefaultWidth = 760;
        internal const double DefaultHeight = 750;
        private readonly FontFamily serif = new FontFamily("Georgia");
        private readonly ContentControl page = new ContentControl();
        private readonly Grid holidays;
        private readonly DockPanel optionsTitleBar;
        private readonly TextBlock selectedCount;
        private readonly Button done;
        private readonly Settings settings;
        private readonly Action persist;
        private readonly Brush gold;
        private readonly Brush ink;
        private readonly Brush muted;
        private readonly ToggleButton springSwitch;

        internal SettingsWindow(Window owner, Settings settings, Action saveSelection, Action scheduleSave,
            Action openOptions, Action<bool> setSpringStyle)
        {
            this.settings = settings;
            this.persist = saveSelection;
            Owner = owner;
            Title = "Cài đặt · " + Widget.ProductName;
            ShowInTaskbar = false;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.CanResize;
            UseLayoutRounding = true;
            FontFamily = new FontFamily("Segoe UI");
            Background = Paint("#1D1117");
            Resources.MergedDictionaries.Add(PanelPage.Theme.Value);
            gold = (Brush)FindResource("PanelGold");
            ink = (Brush)FindResource("PanelInk");
            muted = (Brush)FindResource("PanelMuted");
            springSwitch = new ToggleButton
            {
                Style = (Style)FindResource("SpringStyleSwitch"), HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 19, 17),
                ToolTip = "Đổi giao diện Tết giữa cành đào và cành mai"
            };
            AutomationProperties.SetName(springSwitch, "Giao diện Tết: cành mai");
            AutomationProperties.SetHelpText(springSwitch, "Bật để chọn cành mai, tắt để chọn cành đào.");
            RoutedEventHandler springChanged = (s, e) => setSpringStyle(springSwitch.IsChecked == true);
            springSwitch.Checked += springChanged;
            springSwitch.Unchecked += springChanged;
            WindowChrome.SetWindowChrome(this, new WindowChrome
            {
                CaptionHeight = 0, ResizeBorderThickness = new Thickness(6),
                GlassFrameThickness = new Thickness(0)
            });

            SizeChanged += (s, e) =>
            {
                if (!IsLoaded || !IsVisible) return;
                settings.SettingsWidth = Width;
                settings.SettingsHeight = Height;
                scheduleSave();
            };
            Closing += (s, e) => { e.Cancel = true; Hide(); };
            PreviewKeyDown += (s, e) =>
            {
                if (e.Key != Key.Escape && e.Key != Key.F2) return;
                e.Handled = true;
                Hide();
            };
            KeyboardNavigation.SetTabNavigation(this, KeyboardNavigationMode.Cycle);

            var shell = new DockPanel();
            optionsTitleBar = CreateTitleBar("Cài đặt bộ đếm");
            optionsTitleBar.Margin = new Thickness(24, 12, 16, 0);
            optionsTitleBar.Visibility = Visibility.Collapsed;
            DockPanel.SetDock(optionsTitleBar, Dock.Top);
            shell.Children.Add(optionsTitleBar);
            page.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            page.VerticalContentAlignment = VerticalAlignment.Stretch;
            shell.Children.Add(page);
            Content = new Border { Child = shell, BorderBrush = Paint("#63404A"), BorderThickness = new Thickness(1), Background = Background };

            done = ActionButton("Xong", 100);
            done.Background = gold;
            done.Foreground = (Brush)FindResource("PanelOnGold");
            done.BorderBrush = gold;
            done.Click += (s, e) => Hide();
            var options = ActionButton("Tùy chọn", 108);
            options.Click += (s, e) => openOptions();
            var footer = new DockPanel { Margin = new Thickness(0, 14, 0, 0), LastChildFill = false };
            DockPanel.SetDock(done, Dock.Right);
            footer.Children.Add(done);
            footer.Children.Add(options);

            holidays = new Grid { Margin = new Thickness(24, 20, 24, 20) };
            holidays.RowDefinitions.Add(new RowDefinition());
            holidays.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var body = new StackPanel();
            selectedCount = Label("", 12, (Brush)FindResource("PanelOnGold"));
            selectedCount.FontWeight = FontWeights.SemiBold;
            UpdateSelectedCount();
            body.Children.Add(CreateHeading());
            AddGroup(body, "Ngày nghỉ lễ", HolidaySetting.PublicHolidays);
            AddGroup(body, "Ngày kỷ niệm", HolidaySetting.Observances);
            holidays.Children.Add(new ScrollViewer { Content = body, Focusable = false,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled });
            Grid.SetRow(footer, 1);
            holidays.Children.Add(footer);
        }

        private void SizeToMonitor()
        {
            var source = PresentationSource.FromVisual(Owner);
            var screen = Forms.Screen.FromHandle(new WindowInteropHelper(Owner).Handle);
            var area = screen.WorkingArea;
            var fromDevice = source == null ? Matrix.Identity : source.CompositionTarget.TransformFromDevice;
            var start = fromDevice.Transform(new Point(area.Left, area.Top));
            var end = fromDevice.Transform(new Point(area.Right, area.Bottom));
            MinWidth = Math.Min(540, end.X - start.X);
            MinHeight = Math.Min(360, end.Y - start.Y);
            Width = ClampSize(settings.SettingsWidth, MinWidth, end.X - start.X, DefaultWidth);
            Height = ClampSize(settings.SettingsHeight, MinHeight, end.Y - start.Y, DefaultHeight);
            Left = start.X + (end.X - start.X - Width) / 2;
            Top = start.Y + (end.Y - start.Y - Height) / 2;
        }

        private static double ClampSize(double value, double min, double max, double fallback)
        {
            return Math.Max(min, Math.Min(max, double.IsNaN(value) || value <= 0 ? fallback : value));
        }

        internal void OpenHolidays()
        {
            springSwitch.IsChecked = settings.ApricotNewYear;
            optionsTitleBar.Visibility = Visibility.Collapsed;
            page.Content = holidays;
            if (!IsVisible)
            {
                SizeToMonitor();
                Show();
            }
            if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
            Activate();
            done.Focus();
        }

        internal void OpenPage(PanelPage panel)
        {
            optionsTitleBar.Visibility = Visibility.Visible;
            page.Content = panel;
            panel.BackButton.Focus();
        }

        private DockPanel CreateTitleBar(string text)
        {
            var bar = new DockPanel();
            var close = ActionButton("×", 32);
            close.Height = 32;
            close.FontSize = 20;
            close.Background = Paint("#38FFFFFF");
            close.BorderBrush = Brushes.Transparent;
            AutomationProperties.SetName(close, "Đóng cài đặt");
            close.Click += (s, e) => Hide();
            DockPanel.SetDock(close, Dock.Right);
            bar.Children.Add(close);
            var caption = Label(text, 12, gold);
            caption.VerticalAlignment = VerticalAlignment.Center;
            var dragArea = new Border { Child = caption, Background = Brushes.Transparent };
            dragArea.MouseLeftButtonDown += (s, e) => DragMove();
            bar.Children.Add(dragArea);
            return bar;
        }

        private Border CreateHeading()
        {
            var intro = new StackPanel();
            intro.Children.Add(CreateTitleBar("CÀI ĐẶT BỘ ĐẾM"));
            var title = Label("Ngày lễ và sự kiện", 33, ink, serif);
            title.FontWeight = FontWeights.Bold;
            intro.Children.Add(title);
            var description = Label("Chọn những dịp bạn muốn theo dõi.", 14, ink);
            description.Margin = new Thickness(0, 4, 0, 12);
            intro.Children.Add(description);
            intro.Children.Add(new Border { Child = selectedCount, Background = gold, CornerRadius = new CornerRadius(12),
                HorizontalAlignment = HorizontalAlignment.Left, Padding = new Thickness(12, 4, 12, 4) });
            return new Border { Child = intro, Background = Paint("#781F30"), CornerRadius = new CornerRadius(22),
                Padding = new Thickness(22, 13, 16, 17) };
        }

        private void UpdateSelectedCount()
        {
            int total = HolidaySetting.All.Length;
            int count = HolidaySetting.All.Count(item => settings.SelectedHolidays.Contains(item.Id));
            selectedCount.Text = string.Format("Đã chọn {0}/{1} dịp", count, total);
        }

        private void AddGroup(StackPanel body, string title, HolidaySetting[] items)
        {
            var heading = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 14, 0, 7) };
            heading.Children.Add(Label(title, 21, gold, serif));
            var count = Label(items.Length + " dịp", 12, muted);
            count.Margin = new Thickness(8, 0, 0, 2);
            count.VerticalAlignment = VerticalAlignment.Bottom;
            heading.Children.Add(count);
            body.Children.Add(heading);

            var cards = new UniformGrid { Columns = 3, Margin = new Thickness(-5, 0, -5, 0) };
            var style = (Style)FindResource("HolidayCard");
            foreach (var item in items)
            {
                var input = new CheckBox { Style = style, Content = CreateCardContent(item),
                    IsChecked = settings.SelectedHolidays.Contains(item.Id) };
                AutomationProperties.SetName(input, string.Format("{0}, {1}/{2}{3}",
                    item.Name, item.Day, item.Month, item.IsLunar ? " âm lịch" : ""));
                RoutedEventHandler update = (s, e) =>
                {
                    settings.SelectedHolidays = input.IsChecked == true
                        ? settings.SelectedHolidays.Union(new[] { item.Id }).ToArray()
                        : settings.SelectedHolidays.Where(id => id != item.Id).ToArray();
                    UpdateSelectedCount();
                    persist();
                };
                input.Checked += update;
                input.Unchecked += update;
                if (item.Theme == HolidayTheme.LunarNewYear)
                {
                    var card = new Grid();
                    card.Children.Add(input);
                    card.Children.Add(springSwitch);
                    cards.Children.Add(card);
                }
                else cards.Children.Add(input);
            }
            body.Children.Add(cards);
        }

        private FrameworkElement CreateCardContent(HolidaySetting item)
        {
            var content = new DockPanel { LastChildFill = false };
            var month = Label("THÁNG " + item.Month, 11);
            month.Margin = new Thickness(0, 2, 26, 0);
            DockPanel.SetDock(month, Dock.Top);
            content.Children.Add(month);
            var day = Label(item.Day.ToString(), 34, null, serif);
            day.FontWeight = FontWeights.Bold;
            day.Margin = new Thickness(0, 5, 0, 2);
            DockPanel.SetDock(day, Dock.Top);
            content.Children.Add(day);
            var detail = new StackPanel();
            var name = Label(item.Name, 14);
            name.FontWeight = FontWeights.SemiBold;
            detail.Children.Add(name);
            if (item.IsLunar)
                detail.Children.Add(new Border { Child = Label("Âm lịch", 11), Background = Paint("#337C2337"),
                    CornerRadius = new CornerRadius(9), HorizontalAlignment = HorizontalAlignment.Left,
                    Padding = new Thickness(8, 1, 8, 2), Margin = new Thickness(0, 4, 0, 0) });
            DockPanel.SetDock(detail, Dock.Bottom);
            content.Children.Add(detail);
            return content;
        }

        private static TextBlock Label(string text, double fontSize, Brush foreground = null, FontFamily font = null)
        {
            var label = new TextBlock { Text = text, FontSize = fontSize, TextWrapping = TextWrapping.Wrap };
            if (foreground != null) label.Foreground = foreground;
            if (font != null) label.FontFamily = font;
            return label;
        }

        private Button ActionButton(string text, double width)
        {
            return new Button { Content = text, Width = width, Height = 40, FontSize = 14,
                Style = (Style)FindResource("SettingsAction") };
        }

        private static Brush Paint(string value) { return (Brush)new BrushConverter().ConvertFromString(value); }
    }
}
