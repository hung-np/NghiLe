using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Forms = System.Windows.Forms;

namespace NghiLe
{
    public sealed class WidgetProfile
    {
        public Guid Id { get; set; }
        public string OccasionId { get; set; }
        public double Scale { get; set; }
        public bool Horizontal { get; set; }
        public double? X { get; set; }
        public double? Y { get; set; }
        public bool Hidden { get; set; }

        internal WidgetProfile Copy() { return (WidgetProfile)MemberwiseClone(); }

        internal static WidgetProfile[] Normalize(WidgetProfile[] profiles)
        {
            var valid = (profiles ?? new WidgetProfile[0]).Where(item => item != null && item.Id != Guid.Empty)
                .GroupBy(item => item.Id).Select(group => group.First()).ToArray();
            foreach (var profile in valid)
            {
                profile.Scale = WidgetSize.Clamp(profile.Scale);
                if (!IsCoordinate(profile.X) || !IsCoordinate(profile.Y)) profile.X = profile.Y = null;
            }
            return valid;
        }

        private static bool IsCoordinate(double? value)
        {
            return value.HasValue && !double.IsNaN(value.Value) && !double.IsInfinity(value.Value);
        }
    }

    internal sealed partial class Widget
    {
        private readonly Widget host;
        private readonly WidgetProfile profile;
        private readonly Dictionary<Guid, Widget> companions = new Dictionary<Guid, Widget>();
        private Forms.MenuItem companionMenu;
        private Window companionSettings;
        private ComboBox companionOccasionPicker;
        private PersonalDate[] syncedPersonalDates;
        private bool refreshingCompanionPicker;
        private bool changingCompanionVisibility;

        private bool IsCompanion { get { return host != null; } }

        private IEnumerable<KeyValuePair<string, string>> WidgetOccasions()
        {
            foreach (var holiday in HolidaySetting.All)
                yield return new KeyValuePair<string, string>(holiday.Id, holiday.Name);
            foreach (var date in config.PersonalDates.Where(day => day.Enabled))
                yield return new KeyValuePair<string, string>(date.Id.ToString("N"), date.Name);
        }

        private string CompanionTitle(WidgetProfile item)
        {
            int index = Array.IndexOf(config.AdditionalWidgets, item);
            string name = WidgetOccasions().FirstOrDefault(option => option.Key == item.OccasionId).Value;
            return "Widget " + (index + 2) + " · " + (name ?? "Chưa chọn dịp");
        }

        private Settings SettingsForWidget(WidgetProfile item)
        {
            var value = config.Copy();
            value.Horizontal = item.Horizontal;
            value.Scale = item.Scale;
            value.X = item.X;
            value.Y = item.Y;
            value.Automatic = true;
            value.DisplayedOccurrenceKey = null;
            value.PinnedOccasionIds = new string[0];
            value.SelectedHolidays = HolidaySetting.All.Where(day => day.Id == item.OccasionId).Select(day => day.Id).ToArray();
            value.PersonalDates = config.PersonalDates.Where(day => day.Enabled && day.Id.ToString("N") == item.OccasionId).ToArray();
            value.ReminderDays = new Dictionary<string, int>();
            value.PetEnabled = false;
            return value;
        }

        private void AddCompanionSetting(StackPanel fields)
        {
            fields.Children.Add(PanelPage.SectionHeading("Widget bổ sung", settingsWindow, 0));
            var button = new Button { Name = "AddWidget", Content = "Thêm widget", MinHeight = 36,
                FontSize = 14, Padding = new Thickness(14, 2, 14, 2),
                HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(12, 0, 0, 0) };
            button.SetResourceReference(StyleProperty, "PanelPrimary");
            button.Click += (s, e) => AddCompanion();
            fields.Children.Add(button);
        }

        private void AddCompanion()
        {
            var item = new WidgetProfile { Id = Guid.NewGuid(), OccasionId = NextCompanionOccasion(),
                Horizontal = config.Horizontal, Scale = config.Scale };
            var previous = config.AdditionalWidgets;
            config.AdditionalWidgets = previous.Concat(new[] { item }).ToArray();
            if (!Persist())
            {
                config.AdditionalWidgets = previous;
                MessageBox.Show((Window)settingsWindow ?? this, "Không lưu được widget mới. Hãy thử lại.", ProductName,
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            OpenCompanion(item).OpenCompanionSettings();
        }

        private string NextCompanionOccasion()
        {
            var local = DateTimeOffset.UtcNow.ToOffset(HolidayCountdown.VietnamOffset);
            var current = GetHoliday(local);
            var taken = new HashSet<string>(config.AdditionalWidgets.Select(item => item.OccasionId), StringComparer.Ordinal);
            if (current.HasValue) taken.Add(current.Value.Id);
            var all = config.Copy();
            all.Automatic = true;
            all.SelectedHolidays = HolidaySetting.All.Select(day => day.Id).ToArray();
            var occasions = HolidayCountdown.UpcomingTracked(all, local).ToArray();
            return occasions.Where(day => !taken.Contains(day.Id)).Select(day => day.Id).FirstOrDefault() ??
                occasions.Select(day => day.Id).FirstOrDefault();
        }

        private void SetCompanionPlacement(WidgetProfile item)
        {
            var source = PresentationSource.FromVisual(this);
            if (source == null) return;
            var reference = companions.Values.LastOrDefault(window => window.IsVisible) ?? this;
            var work = Forms.Screen.FromHandle(new WindowInteropHelper(reference).Handle).WorkingArea;
            var fromDevice = source.CompositionTarget.TransformFromDevice;
            var start = fromDevice.Transform(new Point(work.Left, work.Top));
            var end = fromDevice.Transform(new Point(work.Right, work.Bottom));
            var size = WidgetSize.Clock(item.Horizontal);
            double width = size.Width * item.Scale, height = size.Height * item.Scale;
            double x = reference.Left - width - 14;
            double y = reference.Top;
            if (x < start.X)
            {
                double above = reference.Top - height - 14;
                if (above >= start.Y) { x = reference.Left; y = above; }
                else { x = reference.Left + 22; y = reference.Top + 22; }
            }
            item.X = Math.Max(start.X, Math.Min(end.X - width, x));
            item.Y = Math.Max(start.Y, Math.Min(end.Y - height, y));
        }

        private Widget OpenCompanion(WidgetProfile item, bool activate = true)
        {
            Widget window;
            if (!companions.TryGetValue(item.Id, out window))
            {
                if (!item.X.HasValue || !item.Y.HasValue) SetCompanionPlacement(item);
                window = new Widget(file, this, item);
                companions.Add(item.Id, window);
            }
            window.ShowWidget(activate);
            return window;
        }

        private void RestoreCompanions()
        {
            if (quitting || !config.AdditionalWidgets.Any(item => !item.Hidden)) return;
            ChangeCompanionVisibility(() =>
            {
                foreach (var item in config.AdditionalWidgets)
                    if (!item.Hidden) OpenCompanion(item, false);
            });
        }

        private void CloseCompanions()
        {
            var windows = companions.Values.ToArray();
            companions.Clear();
            foreach (var window in windows) window.Quit();
        }

        private void ShowAllWidgets()
        {
            ChangeCompanionVisibility(() =>
            {
                foreach (var item in config.AdditionalWidgets) OpenCompanion(item, false);
                ShowWidget();
            });
        }

        private void HideAllWidgets()
        {
            ChangeCompanionVisibility(() =>
            {
                foreach (var window in companions.Values.ToArray())
                    if (window.IsVisible) window.HideWidget();
                HideWidget();
            });
        }

        private void ChangeCompanionVisibility(Action change)
        {
            bool alreadyChanging = changingCompanionVisibility;
            changingCompanionVisibility = true;
            try { change(); }
            finally { changingCompanionVisibility = alreadyChanging; }
            Persist();
        }

        private void RefreshCompanionMenu()
        {
            companionMenu.MenuItems.Clear();
            var all = companionMenu.MenuItems.Add("Hiện tất cả widget", (s, e) => ShowAllWidgets());
            all.Enabled = config.AdditionalWidgets.Length > 0;
            if (all.Enabled) companionMenu.MenuItems.Add("-");
            foreach (var item in config.AdditionalWidgets)
            {
                var entry = companionMenu.MenuItems.Add(CompanionTitle(item).Replace("&", "&&"), (s, e) =>
                {
                    Widget window;
                    if (companions.TryGetValue(item.Id, out window) && window.IsVisible) window.HideWidget();
                    else OpenCompanion(item);
                });
                entry.Checked = !item.Hidden;
            }
        }

        private void ReadCompanionPlacement()
        {
            if (!ready) return;
            profile.Horizontal = config.Horizontal;
            profile.Scale = config.Scale;
            profile.X = Left;
            profile.Y = Top;
        }

        private void CollectCompanionPlacement()
        {
            foreach (var window in companions.Values) window.ReadCompanionPlacement();
        }

        private bool PersistCompanion()
        {
            if (host.quitting || !host.config.AdditionalWidgets.Contains(profile)) return true;
            ReadCompanionPlacement();
            return host.changingCompanionVisibility || file.Save(host.config);
        }

        private void SyncFromHost()
        {
            if (quitting) return;
            ReadCompanionPlacement();
            var previous = config;
            config = host.SettingsForWidget(profile);
            Topmost = config.AlwaysOnTop;
            Title = host.CompanionTitle(profile);
            if (companionSettings != null) companionSettings.Title = "Cài đặt · " + Title;
            UpdateClock();
            if (previous.Glass != config.Glass || previous.Use3D != config.Use3D || previous.ApricotNewYear != config.ApricotNewYear)
            {
                ApplyHolidayTheme(displayedTheme);
                ApplyFaceTypography(compact);
            }
            UpdateMotion();
            if (companionOccasionPicker != null && syncedPersonalDates != host.config.PersonalDates)
                FillOccasionPicker();
        }

        private void FillOccasionPicker()
        {
            refreshingCompanionPicker = true;
            syncedPersonalDates = host.config.PersonalDates;
            companionOccasionPicker.Items.Clear();
            companionOccasionPicker.Items.Add(new ComboBoxItem { Content = "Chọn dịp" });
            int selection = 0;
            foreach (var option in host.WidgetOccasions())
            {
                var item = new ComboBoxItem { Content = option.Value, Tag = option.Key };
                int index = companionOccasionPicker.Items.Add(item);
                if (option.Key == profile.OccasionId) selection = index;
            }
            companionOccasionPicker.SelectedIndex = selection;
            refreshingCompanionPicker = false;
        }

        private void OpenCompanionSettings()
        {
            if (companionSettings != null) { companionSettings.Activate(); return; }
            if (host.settingsTheme == null) host.settingsTheme = PanelPage.LoadTheme();
            var window = new Window
            {
                Owner = this, Title = "Cài đặt · " + Title, Width = 382, SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false, UseLayoutRounding = true, FontFamily = new FontFamily("Segoe UI")
            };
            companionSettings = window;
            window.Resources.MergedDictionaries.Add(host.settingsTheme);
            window.Background = (Brush)window.FindResource("PanelCanvas");
            var fields = new StackPanel { Margin = new Thickness(16, 12, 16, 14) };
            window.Content = fields;

            companionOccasionPicker = new ComboBox { Name = "WidgetOccasion", MinHeight = 32, FontSize = 13 };
            AutomationProperties.SetName(companionOccasionPicker, "Dịp đếm ngược");
            FillOccasionPicker();
            companionOccasionPicker.SelectionChanged += (s, e) =>
            {
                if (refreshingCompanionPicker || companionOccasionPicker.SelectedItem == null) return;
                var previous = profile.OccasionId;
                profile.OccasionId = ((ComboBoxItem)companionOccasionPicker.SelectedItem).Tag as string;
                if (!Persist())
                {
                    profile.OccasionId = previous;
                    FillOccasionPicker();
                    return;
                }
                SyncFromHost();
            };
            AddOptionRow(fields, companionOccasionPicker, "Dịp đếm ngược");

            var layout = new ComboBox { Name = "WidgetLayout", Width = 96, MinHeight = 30, FontSize = 13,
                HorizontalAlignment = HorizontalAlignment.Right };
            layout.Items.Add(new ComboBoxItem { Content = "Dọc" });
            layout.Items.Add(new ComboBoxItem { Content = "Ngang" });
            layout.SelectedIndex = config.Horizontal ? 1 : 0;
            layout.SelectionChanged += (s, e) =>
            {
                config.Horizontal = layout.SelectedIndex == 1;
                ApplyOrientation();
                Persist();
            };
            AutomationProperties.SetName(layout, "Bố cục widget");
            AddOptionRow(fields, layout, "Bố cục");
            AddSizeOption(fields);

            var actions = new DockPanel { LastChildFill = false, Margin = new Thickness(12, 16, 12, 0) };
            var done = new Button { Content = "Xong", MinHeight = 32, MinWidth = 85, FontSize = 13,
                Style = (Style)window.FindResource("PanelPrimary") };
            done.Click += (s, e) => window.Close();
            DockPanel.SetDock(done, Dock.Right);
            actions.Children.Add(done);
            var remove = new Button { Name = "RemoveWidget", Content = "Gỡ widget này", MinHeight = 32, FontSize = 13 };
            remove.Click += (s, e) =>
            {
                if (MessageBox.Show(window, "Bạn muốn gỡ widget này khỏi màn hình?", ProductName,
                    MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
                    host.RemoveCompanion(this);
            };
            actions.Children.Add(remove);
            fields.Children.Add(actions);
            window.KeyDown += (s, e) => { if (e.Key == Key.Escape) window.Close(); };
            window.Closed += (s, e) =>
            {
                sizeOption = null;
                companionOccasionPicker = null;
                companionSettings = null;
                if (!quitting) Persist();
            };
            window.Show();
        }

        private bool RemoveCompanion(Widget window)
        {
            var previous = config.AdditionalWidgets;
            config.AdditionalWidgets = previous.Where(item => item.Id != window.profile.Id).ToArray();
            if (!Persist())
            {
                config.AdditionalWidgets = previous;
                MessageBox.Show(window.companionSettings ?? window, "Không lưu được thay đổi. Hãy thử lại.", ProductName,
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            companions.Remove(window.profile.Id);
            window.Quit();
            return true;
        }
    }
}
