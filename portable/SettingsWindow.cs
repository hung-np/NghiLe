using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using Forms = System.Windows.Forms;

namespace NghiLe
{
    internal sealed class HolidaySetting
    {
        internal const string LunarNewYear = "lunar-new-year";
        internal const string NewYear = "new-year";
        internal const string HungKings = "hung-kings";
        internal const string VietnamCultureDay = "vietnam-culture-day";
        internal const string VietnamCultureDayName = "Ngày Văn hóa Việt Nam";
        internal static readonly HolidaySetting[] PublicHolidays =
        {
            new HolidaySetting(HolidayTheme.NewYear, NewYear, "Tết Dương lịch", 1, 1),
            new HolidaySetting(HolidayTheme.LunarNewYear, LunarNewYear, "Tết Nguyên đán", 1, 1, true),
            new HolidaySetting(HolidayTheme.HungKings, HungKings, "Giỗ Tổ Hùng Vương", 10, 3, true),
            new HolidaySetting(HolidayTheme.Reunification, "reunification", "Giải phóng miền Nam", 30, 4),
            new HolidaySetting(HolidayTheme.LabourDay, "labour-day", "Quốc tế Lao động", 1, 5),
            new HolidaySetting(HolidayTheme.NationalDay, "national-day", "Quốc khánh", 2, 9),
            new HolidaySetting(HolidayTheme.VietnamCultureDay, VietnamCultureDay, VietnamCultureDayName, 24, 11, firstYear: 2026)
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
        internal readonly int FirstYear;

        private HolidaySetting(HolidayTheme theme, string id, string name, int day, int month, bool isLunar = false, int firstYear = 1)
        {
            Theme = theme;
            Id = id;
            Name = name;
            Day = day;
            Month = month;
            IsLunar = isLunar;
            FirstYear = firstYear;
        }
    }

    internal sealed class SettingsWindow : Window
    {
        internal const double DefaultWidth = 650;
        internal const double DefaultHeight = 700;
        private const double DateWidth = 50;
        private const double LunarWidth = 65;
        private const double RowPadding = 12;
        private readonly TabItem holidaysTab;
        private readonly TabItem upcomingTab;
        private UpcomingPage upcomingPage;
        private readonly TextBlock selectedCount;
        private readonly TextBlock occurrenceStatus;
        private readonly TextBlock occurrenceName;
        private readonly TextBlock occurrenceDate;
        private readonly TextBlock occurrenceYear;
        private readonly Border occurrenceStamp;
        private readonly Settings settings;
        private readonly Action persist;
        private readonly Brush gold;
        private readonly Brush ink;
        private readonly Brush muted;
        private readonly ToggleButton springSwitch;

        internal SettingsWindow(Window owner, Settings settings, Action saveSelection, Action scheduleSave,
            Func<FrameworkElement> createOptions, Func<FrameworkElement> createPet, Func<ResourceDictionary, FrameworkElement> createMusic,
            Action<bool> setSpringStyle, Func<PersonalDate[], bool> savePersonalDates,
            Func<string, int, bool> saveReminder)
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
            var theme = PanelPage.LoadTheme();
            Resources.MergedDictionaries.Add(theme);
            Background = (Brush)FindResource("PanelCanvas");
            gold = (Brush)FindResource("PanelGold");
            ink = (Brush)FindResource("PanelInk");
            muted = (Brush)FindResource("PanelMuted");
            springSwitch = new ToggleButton
            {
                Style = (Style)FindResource("SpringStyleSwitch"), HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, RowPadding + LunarWidth + DateWidth + 12, 0),
                IsChecked = settings.ApricotNewYear,
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
            KeyDown += (s, e) =>
            {
                if (e.Key != Key.Escape && e.Key != Key.F2) return;
                e.Handled = true;
                Close();
            };
            KeyboardNavigation.SetTabNavigation(this, KeyboardNavigationMode.Cycle);

            var shell = new DockPanel();
            var titleBar = CreateTitleBar();
            titleBar.Margin = new Thickness(20, 12, 14, 6);
            DockPanel.SetDock(titleBar, Dock.Top);
            shell.Children.Add(titleBar);
            Content = new Border { Child = shell, BorderBrush = (Brush)FindResource("PanelLine"),
                BorderThickness = new Thickness(1), Background = Background };

            var done = ActionButton("Xong", 100);
            done.Background = gold;
            done.Foreground = (Brush)FindResource("PanelOnGold");
            done.BorderBrush = gold;
            done.Click += (s, e) => Close();
            var footer = new DockPanel();
            DockPanel.SetDock(done, Dock.Right);
            footer.Children.Add(done);
            selectedCount = Label("", 14, muted);
            selectedCount.VerticalAlignment = VerticalAlignment.Center;
            selectedCount.FontWeight = FontWeights.SemiBold;
            footer.Children.Add(selectedCount);
            UpdateSelectedCount();

            var footerFrame = new Border { Child = footer, BorderBrush = (Brush)FindResource("PanelDivider"),
                BorderThickness = new Thickness(0, 1, 0, 0), Padding = new Thickness(20, 8, 20, 8) };
            DockPanel.SetDock(footerFrame, Dock.Bottom);
            shell.Children.Add(footerFrame);

            occurrenceStatus = Label("", 11, gold);
            occurrenceStatus.Name = "OccurrenceStatus";
            occurrenceStatus.FontWeight = FontWeights.SemiBold;
            occurrenceName = Label("", 27, ink, Widget.DisplayFont);
            occurrenceName.Margin = new Thickness(0, 4, 0, 0);
            occurrenceDate = Label("", 30, ink, Widget.CounterFont);
            occurrenceDate.Name = "OccurrenceDate";
            occurrenceYear = Label("", 12, gold, Widget.CounterFont);
            occurrenceStamp = CreateDateStamp();

            var body = new StackPanel();
            body.Children.Add(CreateCalendarHeader());
            AddGroup(body, "Ngày nghỉ lễ", HolidaySetting.PublicHolidays);
            AddGroup(body, "Ngày kỷ niệm", HolidaySetting.Observances);

            var tabs = new TabControl { Style = (Style)FindResource("SettingsTabs") };
            var tabStyle = (Style)FindResource("SettingsTab");
            holidaysTab = new TabItem { Header = "Ngày lễ", Style = tabStyle, Content = PanelPage.Scroll(body) };
            upcomingTab = new TabItem { Name = "UpcomingTab", Header = "Sắp tới", Style = tabStyle };
            var personalDatesTab = new TabItem { Name = "PersonalDatesTab", Header = "Thêm ngày", Style = tabStyle };
            var optionsTab = new TabItem { Name = "OptionsTab", Header = "Tùy chọn", Style = tabStyle };
            var petTab = new TabItem { Name = "PetTab", Header = "Thú cưng", Style = tabStyle };
            var musicTab = new TabItem { Name = "MusicTab", Header = "Nhạc", Style = tabStyle };
            tabs.Items.Add(holidaysTab);
            tabs.Items.Add(upcomingTab);
            tabs.Items.Add(personalDatesTab);
            tabs.Items.Add(optionsTab);
            tabs.Items.Add(petTab);
            tabs.Items.Add(musicTab);
            tabs.SelectionChanged += (s, e) =>
            {
                if (e.Source != tabs) return;
                if (upcomingTab.IsSelected)
                {
                    if (upcomingPage == null)
                    {
                        upcomingPage = new UpcomingPage(settings, theme, saveReminder, () => Open(false));
                        upcomingTab.Content = PanelPage.Scroll(upcomingPage);
                    }
                    upcomingPage.Refresh(DateTimeOffset.UtcNow.ToOffset(HolidayCountdown.VietnamOffset));
                }
                if (personalDatesTab.IsSelected && personalDatesTab.Content == null)
                    personalDatesTab.Content = PanelPage.Scroll(new PersonalDatesPage(settings, theme, savePersonalDates));
                if (optionsTab.IsSelected && optionsTab.Content == null) optionsTab.Content = createOptions();
                if (petTab.IsSelected && petTab.Content == null) petTab.Content = createPet();
                if (musicTab.IsSelected && musicTab.Content == null) musicTab.Content = createMusic(theme);
                selectedCount.Visibility = holidaysTab.IsSelected ? Visibility.Visible : Visibility.Collapsed;
            };
            tabs.SelectedItem = holidaysTab;
            shell.Children.Add(tabs);
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

        internal void Open(bool upcoming)
        {
            springSwitch.IsChecked = settings.ApricotNewYear;
            var tab = upcoming ? upcomingTab : holidaysTab;
            tab.IsSelected = true;
            if (!IsVisible)
            {
                SizeToMonitor();
                Show();
            }
            if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
            Activate();
            tab.Focus();
        }

        internal void RefreshUpcoming(DateTimeOffset local)
        {
            if (upcomingPage != null && upcomingTab.IsSelected && upcomingPage.LocalDay != local.Date)
                upcomingPage.Refresh(local);
        }

        private DockPanel CreateTitleBar()
        {
            var bar = new DockPanel();
            var close = ActionButton("×", 32);
            close.Height = 32;
            close.FontSize = 20;
            close.Background = (Brush)FindResource("PanelField");
            close.BorderBrush = (Brush)FindResource("PanelDivider");
            AutomationProperties.SetName(close, "Đóng cài đặt");
            close.Click += (s, e) => Close();
            DockPanel.SetDock(close, Dock.Right);
            bar.Children.Add(close);
            var caption = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            caption.Children.Add(new Border { Width = 3, Height = 18, Background = gold, CornerRadius = new CornerRadius(1.5),
                Margin = new Thickness(0, 0, 9, 0) });
            var product = Label(Widget.ProductName, 16, ink);
            product.FontWeight = FontWeights.SemiBold;
            caption.Children.Add(product);
            var view = Label("Cài đặt", 13, muted);
            view.Margin = new Thickness(12, 0, 0, 0);
            view.VerticalAlignment = VerticalAlignment.Center;
            caption.Children.Add(view);
            var dragArea = new Border { Child = caption, Background = Brushes.Transparent };
            dragArea.MouseLeftButtonDown += (s, e) => DragMove();
            bar.Children.Add(dragArea);
            return bar;
        }

        private Border CreateCalendarHeader()
        {
            var heading = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            heading.Children.Add(occurrenceStatus);
            heading.Children.Add(occurrenceName);
            var body = new DockPanel();
            DockPanel.SetDock(occurrenceStamp, Dock.Right);
            body.Children.Add(occurrenceStamp);
            body.Children.Add(heading);
            return new Border { Child = body, MinHeight = 96, Padding = new Thickness(16, 9, 10, 9),
                Background = (Brush)FindResource("PanelField"), BorderBrush = gold,
                BorderThickness = new Thickness(3, 0, 0, 0), CornerRadius = new CornerRadius(10) };
        }

        private Border CreateDateStamp()
        {
            var date = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            occurrenceDate.TextAlignment = TextAlignment.Center;
            occurrenceYear.TextAlignment = TextAlignment.Center;
            occurrenceDate.FontWeight = FontWeights.SemiBold;
            date.Children.Add(occurrenceDate);
            date.Children.Add(new System.Windows.Shapes.Rectangle { Width = 59, Height = 1,
                Stroke = gold, StrokeDashArray = new DoubleCollection { 1, 3 }, Margin = new Thickness(0, 2, 0, 3) });
            date.Children.Add(occurrenceYear);
            return new Border { Child = date, Width = 106, MinHeight = 74, Margin = new Thickness(14, 0, 0, 0),
                BorderBrush = (Brush)FindResource("PanelLine"), BorderThickness = new Thickness(1, 0, 0, 0) };
        }

        internal void ShowOccurrence(string name, DateTimeOffset? date, bool active, string unavailable)
        {
            occurrenceStamp.Visibility = date.HasValue ? Visibility.Visible : Visibility.Collapsed;
            if (!date.HasValue)
            {
                bool missingDates = unavailable == "Hết dữ liệu lịch";
                occurrenceStatus.Text = missingDates ? "HẾT DỮ LIỆU LỊCH" : "CHƯA CHỌN DỊP";
                occurrenceName.Text = missingDates ? "Chọn một dịp khác để tiếp tục" : "Chọn một dịp để bắt đầu đếm ngược";
                return;
            }

            occurrenceStatus.Text = active ? "ĐANG DIỄN RA" : "ĐANG ĐẾM NGƯỢC";
            occurrenceName.Text = name;
            occurrenceDate.Text = date.Value.ToString("dd/MM", CultureInfo.InvariantCulture);
            occurrenceYear.Text = date.Value.ToString("yyyy", CultureInfo.InvariantCulture);
        }

        private void UpdateSelectedCount()
        {
            int total = HolidaySetting.All.Length;
            int count = HolidaySetting.All.Count(item => settings.SelectedHolidays.Contains(item.Id));
            selectedCount.Text = string.Format("Đã chọn {0}/{1} dịp", count, total);
        }

        private void AddGroup(StackPanel body, string title, HolidaySetting[] items)
        {
            body.Children.Add(PanelPage.SectionHeading(title, this, 10));
            var style = (Style)FindResource("HolidayRow");
            foreach (var item in items)
            {
                var input = new CheckBox { Style = style, Content = CreateHolidayContent(item),
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
                    var row = new Grid();
                    row.Children.Add(input);
                    row.Children.Add(springSwitch);
                    body.Children.Add(row);
                }
                else body.Children.Add(input);
            }
        }

        private FrameworkElement CreateHolidayContent(HolidaySetting item)
        {
            var content = new Grid();
            content.ColumnDefinitions.Add(new ColumnDefinition());
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(DateWidth) });
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(LunarWidth) });
            var name = Label(item.Name, 15);
            name.FontWeight = FontWeights.SemiBold;
            name.VerticalAlignment = VerticalAlignment.Center;
            name.Margin = new Thickness(0, 0, item.Theme == HolidayTheme.LunarNewYear ? springSwitch.Width + 24 : 10, 0);
            content.Children.Add(name);
            var date = Label(string.Format("{0:00}/{1:00}", item.Day, item.Month), 14, muted);
            Typography.SetNumeralAlignment(date, FontNumeralAlignment.Tabular);
            date.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(date, 1);
            content.Children.Add(date);
            if (item.IsLunar)
            {
                var badge = new Border { Child = Label("Âm lịch", 11, muted), Background = (Brush)FindResource("PanelField"),
                    BorderBrush = (Brush)FindResource("PanelLine"), BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(10), HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center, Padding = new Thickness(7, 2, 7, 2) };
                Grid.SetColumn(badge, 2);
                content.Children.Add(badge);
            }
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

    }
}
