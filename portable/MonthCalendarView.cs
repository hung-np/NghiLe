using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace NghiLe
{
    internal sealed class MonthCalendarView : StackPanel
    {
        private readonly Settings settings;
        private readonly Func<HolidayOccurrence, Border> reminderRow;
        private readonly TextBlock monthTitle;
        private readonly Button previous;
        private readonly Button next;
        private readonly Button todayButton;
        private readonly UniformGrid days = new UniformGrid { Columns = 7 };
        private readonly StackPanel details = new StackPanel();
        private DateTime today;
        private DateTime month;
        private DateTime selected;
        private HolidayOccurrence[] occasions;

        internal MonthCalendarView(Settings settings, ResourceDictionary theme, Func<HolidayOccurrence, Border> reminderRow)
        {
            this.settings = settings;
            this.reminderRow = reminderRow;
            Resources.MergedDictionaries.Add(theme);

            var navigation = new DockPanel { LastChildFill = true };
            previous = NavigationButton("‹", "Tháng trước", () => ShowMonth(month.AddMonths(-1), null));
            next = NavigationButton("›", "Tháng sau", () => ShowMonth(month.AddMonths(1), null));
            todayButton = NavigationButton("Hôm nay", "Về ngày hôm nay theo giờ Việt Nam", () => ShowMonth(today, today));
            DockPanel.SetDock(previous, Dock.Left);
            DockPanel.SetDock(todayButton, Dock.Right);
            DockPanel.SetDock(next, Dock.Right);
            navigation.Children.Add(previous);
            navigation.Children.Add(todayButton);
            navigation.Children.Add(next);
            monthTitle = Text("", 18, "PanelInk");
            monthTitle.FontFamily = Widget.DisplayFont;
            monthTitle.VerticalAlignment = VerticalAlignment.Center;
            monthTitle.Margin = new Thickness(8, 0, 4, 0);
            navigation.Children.Add(monthTitle);
            Children.Add(navigation);

            var weekdays = new UniformGrid { Columns = 7, Margin = new Thickness(0, 8, 0, 3) };
            foreach (string weekday in new[] { "T2", "T3", "T4", "T5", "T6", "T7", "CN" })
            {
                var label = Text(weekday, 12, "PanelMuted");
                label.TextAlignment = TextAlignment.Center;
                weekdays.Children.Add(label);
            }
            Children.Add(weekdays);
            KeyboardNavigation.SetTabNavigation(days, KeyboardNavigationMode.Once);
            KeyboardNavigation.SetDirectionalNavigation(days, KeyboardNavigationMode.Contained);
            Children.Add(days);
            var legend = Text("Số nhỏ là ngày/tháng âm lịch · N là tháng nhuận · ● là dịp đang theo dõi", 12, "PanelMuted");
            legend.TextWrapping = TextWrapping.Wrap;
            legend.Margin = new Thickness(1, 6, 0, 10);
            Children.Add(legend);
            Children.Add(details);
        }

        internal void Refresh(DateTime localDay)
        {
            bool followToday = month == default(DateTime) || selected == today;
            today = localDay.Date;
            todayButton.IsEnabled = today.Year >= VietnameseCalendar.FirstMonth.Year && today.Year <= VietnameseCalendar.LastMonth.Year;
            ShowMonth(followToday ? today : month, followToday ? today : selected);
        }

        private void ShowMonth(DateTime target, DateTime? preferredDay)
        {
            month = new DateTime(target.Year, target.Month, 1);
            if (month < VietnameseCalendar.FirstMonth) month = VietnameseCalendar.FirstMonth;
            if (month > VietnameseCalendar.LastMonth) month = VietnameseCalendar.LastMonth;
            previous.IsEnabled = month > VietnameseCalendar.FirstMonth;
            next.IsEnabled = month < VietnameseCalendar.LastMonth;
            monthTitle.Text = "Tháng " + month.Month + " năm " + month.Year;
            var start = new DateTimeOffset(month, HolidayCountdown.VietnamOffset);
            occasions = HolidayCountdown.UpcomingTracked(settings, start)
                .Where(item => item.Date < start.AddMonths(1)).ToArray();

            days.Children.Clear();
            int leading = ((int)month.DayOfWeek + 6) % 7;
            for (int i = 0; i < leading; i++) days.Children.Add(new Border());
            int length = DateTime.DaysInMonth(month.Year, month.Month);
            var choice = month;
            if (preferredDay.HasValue && preferredDay.Value.Year == month.Year && preferredDay.Value.Month == month.Month)
                choice = preferredDay.Value;
            else if (occasions.Length > 0 && occasions[0].Date.Date > month)
                choice = occasions[0].Date.Date;
            for (int i = 0; i < length; i++) days.Children.Add(DayButton(month.AddDays(i)));
            SelectDay(choice, false);
        }

        private HolidayOccurrence[] OnDate(DateTime day)
        {
            return occasions.Where(item => item.Date.Date <= day && day <= item.LastDay.Date).ToArray();
        }

        private Button DayButton(DateTime day)
        {
            var lunar = VietnameseCalendar.Convert(day);
            var events = OnDate(day);
            var panel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var solar = Text(day.Day.ToString(CultureInfo.InvariantCulture), 17, "PanelInk");
            solar.FontFamily = Widget.CounterFont;
            solar.TextAlignment = TextAlignment.Center;
            if (day == today) solar.TextDecorations = TextDecorations.Underline;
            var small = Text(lunar.ShortLabel, 11, "PanelMuted");
            small.TextAlignment = TextAlignment.Center;
            panel.Children.Add(solar);
            panel.Children.Add(small);
            var cell = new Grid();
            cell.Children.Add(panel);
            if (events.Length > 0)
            {
                var marker = Text("●", 9, "PanelGold");
                marker.HorizontalAlignment = HorizontalAlignment.Right;
                marker.VerticalAlignment = VerticalAlignment.Top;
                cell.Children.Add(marker);
            }
            string description = (day == today ? "Hôm nay, " : "") + day.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
                + ", " + lunar.Description;
            if (events.Length > 0) description += " · " + string.Join(", ", events.Select(item => item.Name));
            var button = new Button { Content = cell, Name = "CalendarDay" + day.Day, Tag = day, ToolTip = description,
                MinHeight = 43, Margin = new Thickness(1), Padding = new Thickness(3, 1, 3, 1),
                HorizontalContentAlignment = HorizontalAlignment.Stretch };
            AutomationProperties.SetName(button, description);
            button.Click += (s, e) => SelectDay(day, true);
            return button;
        }

        private void SelectDay(DateTime day, bool bringRemindersIntoView)
        {
            selected = day;
            foreach (var button in days.Children.OfType<Button>())
            {
                bool active = (DateTime)button.Tag == day;
                button.Background = BrushFor(active ? "PanelSelected" : "PanelField");
                button.BorderBrush = BrushFor(active ? "PanelGold" : "PanelDivider");
                if (active) KeyboardNavigation.SetTabIndex(button, 0);
                else KeyboardNavigation.SetTabIndex(button, 1);
            }
            details.Children.Clear();
            var heading = Text(day.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + " · " +
                VietnameseCalendar.Convert(day).Description, 14, "PanelInk");
            heading.FontWeight = FontWeights.SemiBold;
            heading.Margin = new Thickness(1, 0, 0, 5);
            details.Children.Add(heading);
            var events = OnDate(day);
            foreach (var item in events) details.Children.Add(reminderRow(item));
            if (events.Length == 0) details.Children.Add(Text("Chưa có dịp theo dõi vào ngày này.", 13, "PanelMuted"));
            else if (bringRemindersIntoView) ((FrameworkElement)details.Children[1]).BringIntoView();
        }

        private Button NavigationButton(string caption, string description, Action action)
        {
            var button = new Button { Content = caption, ToolTip = description, MinWidth = 32, MinHeight = 32,
                FontSize = caption.Length == 1 ? 18 : 13, Margin = new Thickness(1, 0, 1, 0) };
            AutomationProperties.SetName(button, description);
            button.Click += (s, e) => action();
            return button;
        }

        private Brush BrushFor(string key) { return (Brush)FindResource(key); }

        private TextBlock Text(string value, double size, string brush)
        {
            return new TextBlock { Text = value, FontSize = size, Foreground = BrushFor(brush) };
        }
    }
}
