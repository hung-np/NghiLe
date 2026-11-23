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
    internal sealed class PersonalDatesPage : StackPanel
    {
        private readonly Settings settings;
        private readonly Func<PersonalDate[], bool> save;
        private readonly ComboBox type;
        private readonly ComboBox day;
        private readonly ComboBox month;
        private readonly TextBox name;
        private readonly HolidayScene preview;
        private readonly Border previewFrame;
        private readonly TextBlock heading;
        private readonly TextBlock status;
        private readonly StackPanel list = new StackPanel();
        private readonly StackPanel actions;
        private readonly StackPanel confirmation;
        private readonly Button submit;
        private readonly Button cancelEdit;
        private readonly Button delete;
        private readonly Button confirmDelete;
        private Guid? editingId;

        internal PersonalDatesPage(Settings settings, ResourceDictionary theme, Func<PersonalDate[], bool> save)
        {
            this.settings = settings;
            this.save = save;
            Resources.MergedDictionaries.Add(theme);

            var title = PanelPage.SectionHeading("Thêm ngày cá nhân", this, 0);
            heading = (TextBlock)title.Children[0];
            Children.Add(title);

            type = Picker("PersonalDateType", PersonalDate.BirthdayName, PersonalDate.SpecialDayName);
            name = new TextBox
            {
                Name = "PersonalDateName", MaxLength = PersonalDate.MaxNameLength,
                Style = (Style)FindResource("SettingsTextInput")
            };
            day = Picker("PersonalDateDay", DateChoices("Chọn ngày", 31));
            month = Picker("PersonalDateMonth", DateChoices("Chọn tháng", 12));

            var fields = new StackPanel();
            fields.Children.Add(Field("Loại ngày", type));
            fields.Children.Add(Field("Tên hiển thị (không bắt buộc)", name));
            var dateFields = new Grid();
            dateFields.ColumnDefinitions.Add(new ColumnDefinition());
            dateFields.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            dateFields.ColumnDefinitions.Add(new ColumnDefinition());
            dateFields.Children.Add(Field("Ngày", day));
            var monthField = Field("Tháng", month);
            Grid.SetColumn(monthField, 2);
            dateFields.Children.Add(monthField);
            fields.Children.Add(dateFields);

            var dateHint = Copy("Dương lịch · Lặp lại hằng năm", 12);
            dateHint.Margin = new Thickness(0, 7, 0, 0);
            fields.Children.Add(dateHint);
            SelectionChangedEventHandler updateDateHint = (s, e) =>
            {
                dateHint.Text = day.SelectedIndex == 29 && month.SelectedIndex == 2
                    ? "Dương lịch · Chỉ có trong năm nhuận"
                    : "Dương lịch · Lặp lại hằng năm";
            };
            day.SelectionChanged += updateDateHint;
            month.SelectionChanged += updateDateHint;

            preview = new HolidayScene(true) { Name = "PersonalDatePreview", Height = 91, ClipToBounds = true };
            previewFrame = new Border
            {
                Child = preview, CornerRadius = new CornerRadius(10), BorderThickness = new Thickness(1),
                BorderBrush = BrushFor("PanelLine")
            };
            var previewPanel = new StackPanel { Width = 156, Margin = new Thickness(18, 0, 0, 0) };
            var previewLabel = Copy("Xem trước", 12);
            previewLabel.Margin = new Thickness(0, 0, 0, 5);
            previewPanel.Children.Add(previewLabel);
            previewPanel.Children.Add(previewFrame);
            var fieldsAndPreview = new DockPanel { Margin = new Thickness(0, 8, 0, 0) };
            DockPanel.SetDock(previewPanel, Dock.Right);
            fieldsAndPreview.Children.Add(previewPanel);
            fieldsAndPreview.Children.Add(fields);
            Children.Add(fieldsAndPreview);

            submit = Action("Thêm ngày", "SavePersonalDate", true);
            submit.Click += (s, e) => SaveEntry();
            cancelEdit = Action("Hủy sửa", "CancelPersonalDate");
            cancelEdit.Click += (s, e) => ResetEditor(true);
            delete = Action("Xóa ngày", "DeletePersonalDate");
            delete.Click += (s, e) =>
            {
                SetConfirmation(true);
                Announce("Xóa “" + CurrentEntry().Name + "” khỏi danh sách?");
                confirmDelete.Focus();
            };
            actions = ActionRow(submit, cancelEdit, delete);
            Children.Add(actions);

            confirmDelete = Action("Xác nhận xóa", "ConfirmDeletePersonalDate", true);
            confirmDelete.Click += (s, e) => DeleteEntry();
            var keep = Action("Giữ lại", "KeepPersonalDate");
            keep.Click += (s, e) =>
            {
                SetConfirmation(false);
                Announce("");
                delete.Focus();
            };
            confirmation = ActionRow(keep, confirmDelete);
            Children.Add(confirmation);

            status = Copy("", 13);
            status.Name = "PersonalDateStatus";
            status.MinHeight = 22;
            status.Margin = new Thickness(0, 4, 0, 0);
            AutomationProperties.SetLiveSetting(status, AutomationLiveSetting.Polite);
            Children.Add(status);

            Children.Add(PanelPage.SectionHeading("Ngày của bạn", this, 12));
            var listHint = Copy("Widget đếm đến dịp được theo dõi gần nhất.", 12);
            listHint.Margin = new Thickness(0, 2, 0, 8);
            Children.Add(listHint);
            Children.Add(list);

            type.SelectionChanged += (s, e) => UpdatePreview();
            preview.IsVisibleChanged += (s, e) => preview.SetMotion(preview.IsVisible && settings.Motion);
            ResetEditor(false);
            RenderList();
        }

        private static string[] DateChoices(string label, int count)
        {
            return new[] { label }.Concat(Enumerable.Range(1, count)
                .Select(value => value.ToString("00", CultureInfo.InvariantCulture))).ToArray();
        }

        private static ComboBox Picker(string controlName, params string[] items)
        {
            return new ComboBox { Name = controlName, ItemsSource = items, FontSize = 14, MinHeight = 32, MaxDropDownHeight = 220 };
        }

        private Brush BrushFor(string key) { return (Brush)FindResource(key); }

        private TextBlock Copy(string text, double size)
        {
            return new TextBlock { Text = text, FontSize = size, Foreground = BrushFor("PanelMuted"), TextWrapping = TextWrapping.Wrap };
        }

        private StackPanel Field(string caption, Control input)
        {
            var label = new Label
            {
                Content = caption, Target = input, Foreground = BrushFor("PanelInk"), FontSize = 13,
                Padding = new Thickness(0, 0, 0, 4)
            };
            AutomationProperties.SetLabeledBy(input, label);
            var field = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
            field.Children.Add(label);
            field.Children.Add(input);
            return field;
        }

        private Button Action(string title, string controlName, bool primary = false)
        {
            return new Button
            {
                Content = title, Name = controlName, FontSize = 13, MinHeight = 32,
                Style = primary ? (Style)FindResource("PanelPrimary") : (Style)FindResource(typeof(Button))
            };
        }

        private static StackPanel ActionRow(params Button[] buttons)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 13, 0, 0) };
            foreach (var button in buttons)
            {
                button.Margin = new Thickness(0, 0, 8, 0);
                row.Children.Add(button);
            }
            return row;
        }

        private void UpdatePreview()
        {
            var theme = type.SelectedIndex == 0 ? HolidayTheme.Birthday : HolidayTheme.PersonalDay;
            previewFrame.Background = Widget.HolidayGlassPaint(theme);
            preview.ShowTheme(theme, false);
        }

        private void SetConfirmation(bool visible)
        {
            confirmation.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            actions.Visibility = visible ? Visibility.Collapsed : Visibility.Visible;
            type.IsEnabled = name.IsEnabled = day.IsEnabled = month.IsEnabled = !visible;
            list.IsEnabled = !visible;
            list.Opacity = visible ? .55 : 1;
        }

        private void Announce(string message, bool error = false)
        {
            status.Text = message;
            status.Foreground = BrushFor(error ? "PanelError" : "PanelMuted");
            var peer = UIElementAutomationPeer.FromElement(status);
            if (peer != null) peer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
        }

        private PersonalDate CurrentEntry()
        {
            return settings.PersonalDates.FirstOrDefault(date => date.Id == editingId);
        }

        private void ResetEditor(bool focus)
        {
            editingId = null;
            heading.Text = "Thêm ngày cá nhân";
            type.SelectedIndex = 0;
            name.Clear();
            day.SelectedIndex = month.SelectedIndex = 0;
            submit.Content = "Thêm ngày";
            cancelEdit.Visibility = delete.Visibility = Visibility.Collapsed;
            SetConfirmation(false);
            Announce("");
            if (focus) FocusName();
        }

        private void EditEntry(PersonalDate entry)
        {
            editingId = entry.Id;
            heading.Text = "Sửa ngày cá nhân";
            type.SelectedIndex = entry.Birthday ? 0 : 1;
            name.Text = entry.Name;
            day.SelectedIndex = entry.Day;
            month.SelectedIndex = entry.Month;
            submit.Content = "Lưu thay đổi";
            cancelEdit.Visibility = delete.Visibility = Visibility.Visible;
            SetConfirmation(false);
            Announce("");
            FocusName();
        }

        private void FocusName()
        {
            heading.BringIntoView();
            name.Focus();
        }

        private bool TrySave(PersonalDate[] dates)
        {
            if (save(dates)) return true;
            Announce("Không lưu được ngày cá nhân. Hãy thử lại.", true);
            return false;
        }

        private void SaveEntry()
        {
            if (month.SelectedIndex <= 0 || !PersonalDate.IsValidDay(month.SelectedIndex, day.SelectedIndex))
            {
                bool missing = day.SelectedIndex <= 0 || month.SelectedIndex <= 0;
                Announce(missing ? "Chọn ngày và tháng." : "Ngày này không có trong tháng đã chọn. Hãy chọn lại ngày.", true);
                (day.SelectedIndex > 0 && month.SelectedIndex <= 0 ? month : day).Focus();
                return;
            }

            var original = CurrentEntry();
            bool birthday = type.SelectedIndex == 0;
            string title = name.Text.Trim();
            var entry = new PersonalDate
            {
                Id = original == null ? Guid.NewGuid() : original.Id,
                Name = title.Length > 0 ? title : birthday ? PersonalDate.BirthdayName : PersonalDate.SpecialDayName,
                Day = day.SelectedIndex,
                Month = month.SelectedIndex,
                Birthday = birthday,
                Enabled = original == null || original.Enabled
            };
            var dates = original == null ? settings.PersonalDates.Concat(new[] { entry }).ToArray() :
                settings.PersonalDates.Select(date => date.Id == entry.Id ? entry : date).ToArray();
            if (!TrySave(dates)) return;
            Finish(original == null ? "Đã thêm “" + entry.Name + "”." : "Đã lưu thay đổi.");
        }

        private void DeleteEntry()
        {
            var dates = settings.PersonalDates.Where(date => date.Id != editingId).ToArray();
            if (TrySave(dates)) Finish("Đã xóa ngày.");
        }

        private void Finish(string message)
        {
            ResetEditor(true);
            RenderList();
            Announce(message);
        }

        private void RenderList()
        {
            list.Children.Clear();
            if (settings.PersonalDates.Length == 0)
                list.Children.Add(Copy("Bạn chưa thêm ngày nào.", 14));
            foreach (var entry in settings.PersonalDates)
            {
                string stamp = string.Format(CultureInfo.InvariantCulture, "{0:00}/{1:00}", entry.Day, entry.Month);
                var edit = Action("Sửa", "EditPersonalDate");
                edit.Margin = new Thickness(8, 0, 0, 0);
                edit.VerticalAlignment = VerticalAlignment.Center;
                AutomationProperties.SetName(edit, "Sửa " + entry.Name + ", " + stamp);
                edit.Click += (s, e) => EditEntry(settings.PersonalDates.First(date => date.Id == entry.Id));
                var rowContent = new DockPanel();
                var dateLabel = Copy(stamp, 14);
                dateLabel.Width = 47;
                DockPanel.SetDock(dateLabel, Dock.Right);
                rowContent.Children.Add(dateLabel);
                var title = Copy(entry.Name, 15);
                title.Foreground = BrushFor("PanelInk");
                title.FontWeight = FontWeights.SemiBold;
                title.TextWrapping = TextWrapping.NoWrap;
                title.TextTrimming = TextTrimming.CharacterEllipsis;
                title.ToolTip = entry.Name;
                rowContent.Children.Add(title);
                var toggle = new CheckBox
                {
                    Name = "TrackPersonalDate", Content = rowContent, IsChecked = entry.Enabled,
                    Style = (Style)FindResource("HolidayRow")
                };
                AutomationProperties.SetName(toggle, "Theo dõi " + entry.Name + ", " + stamp);
                toggle.Click += (s, e) =>
                {
                    bool enabled = toggle.IsChecked == true;
                    var dates = settings.PersonalDates.Select(date => date.Id == entry.Id ? date.WithEnabled(enabled) : date).ToArray();
                    if (!TrySave(dates)) toggle.IsChecked = !enabled;
                    else Announce(enabled ? "Đang theo dõi “" + entry.Name + "”." : "Đã ngừng theo dõi “" + entry.Name + "”.");
                };
                var row = new DockPanel { Margin = new Thickness(0, 0, 0, 3) };
                DockPanel.SetDock(edit, Dock.Right);
                row.Children.Add(edit);
                row.Children.Add(toggle);
                list.Children.Add(row);
            }
        }
    }
}
