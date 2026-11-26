using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Media;

namespace NghiLe
{
    internal sealed class UpcomingPage : StackPanel
    {
        private readonly Settings settings;
        private readonly Func<string, int, bool> saveReminder;
        private readonly Func<string, bool, bool> savePin;
        private readonly StackPanel rows = new StackPanel();
        private readonly TextBlock status;
        private readonly TextBlock pinSummary;
        private readonly StackPanel unavailablePins = new StackPanel();
        private string[] upcomingIds = new string[0];
        private readonly Button chooseHolidays;
        private readonly Button switchView;
        private readonly ResourceDictionary theme;
        private MonthCalendarView calendar;
        private bool showingCalendar;

        internal DateTime LocalDay { get; private set; }

        internal UpcomingPage(Settings settings, ResourceDictionary theme, Func<string, int, bool> saveReminder,
            Func<string, bool, bool> savePin, Action openHolidays)
        {
            this.settings = settings;
            this.theme = theme;
            this.saveReminder = saveReminder;
            this.savePin = savePin;
            Resources.MergedDictionaries.Add(theme);
            var header = new DockPanel();
            switchView = new Button { Name = "SwitchUpcomingView", Content = "Xem lịch tháng", FontSize = 13, MinHeight = 30,
                VerticalAlignment = VerticalAlignment.Center };
            switchView.Click += (s, e) =>
            {
                showingCalendar = !showingCalendar;
                Refresh(new DateTimeOffset(LocalDay, HolidayCountdown.VietnamOffset));
            };
            DockPanel.SetDock(switchView, Dock.Right);
            header.Children.Add(switchView);
            header.Children.Add(PanelPage.SectionHeading("Dịp sắp tới", this, 0));
            Children.Add(header);
            var hint = Label("Nhắc từ 9:00 theo giờ Việt Nam khi Nghỉ Lễ đang chạy, kể cả khi widget ẩn.", 13, "PanelMuted");
            hint.Margin = new Thickness(0, 0, 0, 12);
            Children.Add(hint);
            pinSummary = Label("", 13, "PanelGold");
            pinSummary.Margin = new Thickness(0, 0, 0, 8);
            AutomationProperties.SetLiveSetting(pinSummary, AutomationLiveSetting.Polite);
            Children.Add(pinSummary);
            Children.Add(unavailablePins);
            status = Label("", 13, "PanelError");
            status.Margin = new Thickness(0, 0, 0, 8);
            status.Visibility = Visibility.Collapsed;
            AutomationProperties.SetLiveSetting(status, AutomationLiveSetting.Polite);
            Children.Add(status);
            Children.Add(rows);

            chooseHolidays = new Button
            {
                Content = "Chọn ngày lễ theo dõi", Style = (Style)FindResource(typeof(Button)),
                HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 10, 0, 0),
                MinHeight = 32, Visibility = Visibility.Collapsed
            };
            chooseHolidays.Click += (s, e) => openHolidays();
            Children.Add(chooseHolidays);
        }

        internal void Refresh(DateTimeOffset local)
        {
            LocalDay = local.Date;
            ShowStatus("");
            var occasions = HolidayCountdown.UpcomingTracked(settings, local).ToArray();
            upcomingIds = occasions.Select(item => item.Id).ToArray();
            UpdatePinSummary();
            rows.Children.Clear();
            switchView.Content = showingCalendar ? "Xem danh sách" : "Xem lịch tháng";
            if (showingCalendar)
            {
                if (calendar == null) calendar = new MonthCalendarView(settings, theme, CreateRow);
                rows.Children.Add(calendar);
                calendar.Refresh(LocalDay);
                chooseHolidays.Visibility = settings.SelectedHolidays.Length == 0 && !settings.PersonalDates.Any(date => date.Enabled)
                    ? Visibility.Visible : Visibility.Collapsed;
                return;
            }
            chooseHolidays.Visibility = occasions.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
            if (occasions.Length == 0)
                rows.Children.Add(Label("Chưa có dịp sắp tới. Bạn cũng có thể theo dõi ngày cá nhân trong tab Thêm ngày.", 14, "PanelMuted"));
            foreach (var occasion in occasions) rows.Children.Add(CreateRow(occasion));
        }

        private Border CreateRow(HolidayOccurrence occasion)
        {
            string date = occasion.Date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            string countdown = OccasionReminders.CountdownLabel(occasion, LocalDay);
            var title = Label(occasion.Name, 15, "PanelInk");
            title.FontWeight = FontWeights.SemiBold;
            var when = Label(date + "  ·  " + countdown, 13, "PanelMuted");
            when.Margin = new Thickness(0, 3, 0, 0);
            var detail = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            detail.Children.Add(title);
            detail.Children.Add(when);

            var picker = new ComboBox { Name = "OccasionReminder", Width = 138, MinHeight = 32, FontSize = 13,
                MaxDropDownHeight = 180, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 0, 0) };
            AutomationProperties.SetName(picker, "Nhắc " + occasion.Name + ", " + date);
            foreach (int days in OccasionReminders.Choices)
                picker.Items.Add(new ComboBoxItem { Content = OccasionReminders.Label(days), Tag = days });
            int configured;
            if (!settings.ReminderDays.TryGetValue(occasion.Id, out configured)) configured = OccasionReminders.Off;
            picker.SelectedIndex = Array.IndexOf(OccasionReminders.Choices, configured);
            bool restoring = false;
            picker.SelectionChanged += (s, e) =>
            {
                if (restoring || picker.SelectedIndex < 0) return;
                int days = (int)((ComboBoxItem)picker.SelectedItem).Tag;
                if (saveReminder(occasion.Id, days))
                {
                    configured = days;
                    ShowStatus("");
                    return;
                }
                restoring = true;
                picker.SelectedIndex = Array.IndexOf(OccasionReminders.Choices, configured);
                restoring = false;
                ShowStatus("Không lưu được tùy chọn nhắc. Hãy thử lại.");
                status.BringIntoView();
            };

            var pin = CreatePinButton(occasion.Id, occasion.Name);
            var row = new DockPanel();
            DockPanel.SetDock(pin, Dock.Right);
            row.Children.Add(pin);
            DockPanel.SetDock(picker, Dock.Right);
            row.Children.Add(picker);
            row.Children.Add(detail);
            return new Border
            {
                Child = row, Background = BrushFor("PanelField"),
                BorderBrush = BrushFor(occasion.Date.Date <= LocalDay && LocalDay <= occasion.LastDay.Date ? "PanelGold" : "PanelDivider"),
                BorderThickness = new Thickness(3, 0, 0, 0), CornerRadius = new CornerRadius(5),
                Padding = new Thickness(11, 8, 11, 8), Margin = new Thickness(0, 0, 0, 5)
            };
        }

        private Button CreatePinButton(string id, string name)
        {
            var button = new Button { Name = "OccasionPin", MinWidth = 75, MinHeight = 32, FontSize = 13,
                Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            Action update = () =>
            {
                string action = settings.PinnedOccasionIds.Contains(id) ? "Bỏ ghim" : "Ghim";
                button.Content = action;
                AutomationProperties.SetName(button, action + " " + name);
            };
            update();
            button.Click += (s, e) =>
            {
                bool adding = !settings.PinnedOccasionIds.Contains(id);
                if (adding && settings.PinnedOccasionIds.Length >= PinnedOccasions.Limit)
                    ShowStatus("Đã ghim 3 dịp. Hãy bỏ ghim một dịp để thêm dịp mới.");
                else if (!savePin(id, adding))
                    ShowStatus("Không lưu được dịp ghim. Hãy thử lại.");
                else
                {
                    update();
                    UpdatePinSummary();
                    ShowStatus("");
                    return;
                }
                status.BringIntoView();
            };
            return button;
        }

        private void UpdatePinSummary()
        {
            pinSummary.Text = settings.PinnedOccasionIds.Length + "/" + PinnedOccasions.Limit + " dịp đã ghim trên widget";
            var peer = UIElementAutomationPeer.FromElement(pinSummary);
            if (peer != null) peer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
            unavailablePins.Children.Clear();
            foreach (var id in settings.PinnedOccasionIds.Where(id => !upcomingIds.Contains(id)))
            {
                var name = PinnedOccasions.Name(settings, id);
                var row = new DockPanel { Margin = new Thickness(0, 0, 0, 5) };
                var remove = CreatePinButton(id, name);
                DockPanel.SetDock(remove, Dock.Right);
                row.Children.Add(remove);
                row.Children.Add(Label(name + " · Chưa có mốc sắp tới", 13, "PanelMuted"));
                unavailablePins.Children.Add(row);
            }
        }

        private void ShowStatus(string message)
        {
            status.Text = message;
            status.Visibility = message.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
            if (message.Length == 0) return;
            var peer = UIElementAutomationPeer.FromElement(status);
            if (peer != null) peer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
        }

        private Brush BrushFor(string key) { return (Brush)FindResource(key); }

        private TextBlock Label(string text, double size, string brush)
        {
            return new TextBlock { Text = text, FontSize = size, Foreground = BrushFor(brush), TextWrapping = TextWrapping.Wrap };
        }
    }
}
