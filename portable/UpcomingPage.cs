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
        private readonly StackPanel rows = new StackPanel();
        private readonly TextBlock status;
        private readonly Button chooseHolidays;

        internal DateTime LocalDay { get; private set; }

        internal UpcomingPage(Settings settings, ResourceDictionary theme, Func<string, int, bool> saveReminder, Action openHolidays)
        {
            this.settings = settings;
            this.saveReminder = saveReminder;
            Resources.MergedDictionaries.Add(theme);
            Children.Add(PanelPage.SectionHeading("Dịp sắp tới", this, 0));
            var hint = Label("Nhắc từ 9:00 theo giờ Việt Nam khi Nghỉ Lễ đang chạy, kể cả khi widget ẩn.", 13, "PanelMuted");
            hint.Margin = new Thickness(0, 0, 0, 12);
            Children.Add(hint);
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
            ShowSaveError(false);
            rows.Children.Clear();
            var occasions = HolidayCountdown.UpcomingTracked(settings, local).ToArray();
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
                    ShowSaveError(false);
                    return;
                }
                restoring = true;
                picker.SelectedIndex = Array.IndexOf(OccasionReminders.Choices, configured);
                restoring = false;
                ShowSaveError(true);
                status.BringIntoView();
            };

            var row = new DockPanel();
            DockPanel.SetDock(picker, Dock.Right);
            row.Children.Add(picker);
            row.Children.Add(detail);
            return new Border
            {
                Child = row, Background = BrushFor("PanelField"),
                BorderBrush = BrushFor(occasion.Date.Date <= LocalDay ? "PanelGold" : "PanelDivider"),
                BorderThickness = new Thickness(3, 0, 0, 0), CornerRadius = new CornerRadius(5),
                Padding = new Thickness(11, 8, 11, 8), Margin = new Thickness(0, 0, 0, 5)
            };
        }

        private void ShowSaveError(bool visible)
        {
            status.Text = visible ? "Không lưu được tùy chọn nhắc. Hãy thử lại." : "";
            status.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            if (!visible) return;
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
