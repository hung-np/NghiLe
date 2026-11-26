using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;

namespace NghiLe
{
    internal static class PinnedOccasions
    {
        internal const int Limit = 3;

        internal static bool IsTracked(Settings settings, string id)
        {
            return HolidaySetting.All.Any(day => day.Id == id && settings.SelectedHolidays.Contains(id)) ||
                settings.PersonalDates.Any(day => day.Enabled && day.Id.ToString("N") == id);
        }

        internal static string[] Normalize(Settings settings)
        {
            return (settings.PinnedOccasionIds ?? new string[0])
                .Where(id => IsTracked(settings, id))
                .Distinct(StringComparer.Ordinal)
                .Take(Limit)
                .ToArray();
        }

        internal static string Name(Settings settings, string id)
        {
            var holiday = HolidaySetting.All.FirstOrDefault(day => day.Id == id);
            return holiday != null ? holiday.Name : settings.PersonalDates.First(day => day.Id.ToString("N") == id).Name;
        }
    }

    internal sealed partial class Widget
    {
        private const double PinBarHeight = 26;
        private const double PinBarBottom = 8;
        private readonly Grid pinnedBar = new Grid { Name = "PinnedCountdowns", Height = PinBarHeight,
            VerticalAlignment = VerticalAlignment.Bottom, Visibility = Visibility.Collapsed };
        private string[] holidayLookupPinIds;
        private HolidayOccurrence[] secondaryPins = new HolidayOccurrence[0];
        private HolidayOccurrence[] shownPins;

        private bool PinsVisible { get { return pinnedBar.Visibility == Visibility.Visible; } }

        private void PrunePins()
        {
            var normalized = PinnedOccasions.Normalize(config);
            if (!normalized.SequenceEqual(config.PinnedOccasionIds)) config.PinnedOccasionIds = normalized;
        }

        private bool SavePin(string id, bool pin)
        {
            var before = config.PinnedOccasionIds;
            if (pin && (before.Length >= PinnedOccasions.Limit || !PinnedOccasions.IsTracked(config, id))) return false;
            config.PinnedOccasionIds = pin
                ? before.Union(new[] { id }, StringComparer.Ordinal).ToArray()
                : before.Where(item => item != id).ToArray();
            if (!Persist())
            {
                config.PinnedOccasionIds = before;
                return false;
            }
            UpdateClock();
            return true;
        }

        private void ReadSecondaryPins(DateTimeOffset local, HolidayOccurrence? main)
        {
            secondaryPins = config.PinnedOccasionIds.Length == 0
                ? new HolidayOccurrence[0]
                : config.PinnedOccasionIds.Join(HolidayCountdown.UpcomingTracked(config, local),
                    id => id, item => item.Id, (id, item) => item)
                    .Where(item => !main.HasValue || item.Id != main.Value.Id)
                    .ToArray();
        }

        private bool RefreshPinnedBar(DateTime today)
        {
            if (shownPins == secondaryPins) return false;
            shownPins = secondaryPins;
            bool wasVisible = PinsVisible;
            var active = System.Windows.Input.Keyboard.FocusedElement as FrameworkElement;
            var focusId = active != null && active.Parent == pinnedBar ? active.Tag as string : null;
            pinnedBar.Children.Clear();
            pinnedBar.ColumnDefinitions.Clear();
            pinnedBar.Visibility = shownPins.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            for (int index = 0; index < shownPins.Length; index++)
            {
                var item = shownPins[index];
                int remaining = (item.Date.Date - today).Days;
                var name = new TextBlock { Text = item.Name, TextTrimming = TextTrimming.CharacterEllipsis,
                    TextWrapping = TextWrapping.NoWrap, FontSize = 9.5, LineHeight = 11,
                    LineStackingStrategy = LineStackingStrategy.BlockLineHeight, TextAlignment = TextAlignment.Center };
                var count = new TextBlock { Text = remaining <= 0 ? (remaining == 0 ? "Hôm nay" : "Đang diễn ra") :
                    remaining.ToString(CultureInfo.InvariantCulture) + " ngày", FontSize = 10,
                    FontWeight = FontWeights.SemiBold, LineHeight = 11, LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
                    TextAlignment = TextAlignment.Center };
                Typography.SetNumeralAlignment(count, System.Windows.FontNumeralAlignment.Tabular);
                var lines = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                lines.Children.Add(name);
                lines.Children.Add(count);
                var button = MakeButton("", item.Name + "\n" + OccasionReminders.CountdownLabel(item, today) +
                    " · " + item.Date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture), () =>
                {
                    config.DisplayedOccurrenceKey = item.Key;
                    UpdateClock();
                    ScheduleSave();
                });
                button.Tag = item.Id;
                button.Content = lines;
                button.Padding = new Thickness(3, 0, 3, 0);
                button.BorderThickness = new Thickness(.75);
                button.Margin = new Thickness(index == 0 ? 0 : 3, 0, 0, 0);
                button.HorizontalContentAlignment = HorizontalAlignment.Stretch;
                AutomationProperties.SetName(button, "Xem " + item.Name + ", " + OccasionReminders.CountdownLabel(item, today));
                Grid.SetColumn(button, index);
                pinnedBar.ColumnDefinitions.Add(new ColumnDefinition());
                pinnedBar.Children.Add(button);
            }
            ApplyPinPalette();
            if (focusId != null)
            {
                var next = pinnedBar.Children.OfType<Button>().FirstOrDefault(button => Equals(button.Tag, focusId)) ??
                    pinnedBar.Children.OfType<Button>().FirstOrDefault() ?? controls.Children.OfType<Button>().FirstOrDefault();
                if (next != null) next.Focus();
            }
            return wasVisible != PinsVisible;
        }

        private void ApplyPinPalette()
        {
            var caption = ColorBrush(currentPalette.Ink);
            var fill = ColorBrush(currentPalette.To);
            var border = new System.Windows.Media.SolidColorBrush(Tint(currentPalette.Accent, .51));
            border.Freeze();
            foreach (var button in pinnedBar.Children.OfType<Button>())
            {
                button.Foreground = caption;
                button.Background = fill;
                button.BorderBrush = border;
            }
        }
    }
}
