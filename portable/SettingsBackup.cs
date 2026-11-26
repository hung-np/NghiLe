using System;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using System.Linq;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using Microsoft.Win32;

namespace NghiLe
{
    internal static class SettingsBackup
    {
        private const string Format = "vn.nghile.backup";
        private const int Version = 1;

        internal static bool Export(string path, Settings current)
        {
            var data = current.Copy();
            data.Version = SettingsFile.SettingsVersion;
            data.X = data.Y = null;
            data.SettingsWidth = SettingsWindow.DefaultWidth;
            data.SettingsHeight = SettingsWindow.DefaultHeight;
            data.DeliveredReminders = new string[0];
            data.AdditionalWidgets = data.AdditionalWidgets.Select(item =>
            {
                var copy = item.Copy();
                copy.X = copy.Y = null;
                return copy;
            }).ToArray();
            try
            {
                var json = new JavaScriptSerializer();
                return SettingsFile.WriteJson(path, json.Serialize(new { format = Format, version = Version, settings = data }));
            }
            catch (InvalidOperationException) { return false; }
        }

        internal static Settings Import(string path)
        {
            try
            {
                var json = new JavaScriptSerializer();
                string text;
                using (var stream = File.OpenRead(path))
                {
                    if (stream.Length > json.MaxJsonLength * 4L) return null;
                    using (var reader = new StreamReader(stream)) text = reader.ReadToEnd();
                }
                var document = json.DeserializeObject(text) as Dictionary<string, object>;
                object storedFormat, storedVersion, content;
                if (document == null || !document.TryGetValue("format", out storedFormat) || !Equals(storedFormat, Format) ||
                    !document.TryGetValue("version", out storedVersion) || !Equals(storedVersion, Version) ||
                    !document.TryGetValue("settings", out content)) return null;
                var data = content as Dictionary<string, object>;
                if (!HasBackupData(data)) return null;
                object widgets;
                bool hasWidgets = data.TryGetValue("AdditionalWidgets", out widgets);
                if (hasWidgets && !(widgets is IList)) return null;
                var settings = json.ConvertToType<Settings>(data);
                var personal = PersonalDate.Normalize(settings.PersonalDates);
                if (settings.PersonalDates == null || personal.Length != settings.PersonalDates.Length) return null;
                SettingsFile.Normalize(settings);
                if (!hasWidgets) settings.AdditionalWidgets = null;
                return settings;
            }
            catch (IOException) { return null; }
            catch (UnauthorizedAccessException) { return null; }
            catch (ArgumentException) { return null; }
            catch (FormatException) { return null; }
            catch (OverflowException) { return null; }
            catch (InvalidOperationException) { return null; }
        }

        private static bool HasBackupData(Dictionary<string, object> data)
        {
            if (data == null) return false;
            foreach (var key in new[] { "SelectedHolidays", "PersonalDates", "PinnedOccasionIds" })
            {
                object value;
                if (!data.TryGetValue(key, out value) || !(value is IList)) return false;
            }
            foreach (var key in new[] { "Automatic", "AlwaysOnTop", "Motion", "PauseOnBattery", "DimOnSchedule",
                "Use3D", "ApricotNewYear", "Horizontal", "PetEnabled", "PetRoams", "PetAtEnd" })
            {
                object value;
                if (!data.TryGetValue(key, out value) || !(value is bool)) return false;
            }
            foreach (var key in new[] { "Year", "Glass", "DimFromHour", "DimUntilHour", "CounterMode",
                "PetAnimal", "PetDelayMinutes", "PetSizePercent" })
            {
                object value;
                if (!data.TryGetValue(key, out value) || !(value is int)) return false;
            }
            object reminders, scale;
            return data.TryGetValue("ReminderDays", out reminders) && reminders is Dictionary<string, object> &&
                data.TryGetValue("Scale", out scale) && (scale is int || scale is double || scale is decimal);
        }

        internal static Settings ForThisDevice(Settings backup, Settings local)
        {
            var next = backup.Copy();
            next.X = local.X;
            next.Y = local.Y;
            next.SettingsWidth = local.SettingsWidth;
            next.SettingsHeight = local.SettingsHeight;
            next.DeliveredReminders = local.DeliveredReminders;
            next.AdditionalWidgets = (backup.AdditionalWidgets ?? local.AdditionalWidgets).Select(item =>
            {
                var copy = item.Copy();
                var current = local.AdditionalWidgets.FirstOrDefault(existing => existing.Id == item.Id);
                copy.X = current == null ? null : current.X;
                copy.Y = current == null ? null : current.Y;
                return copy;
            }).ToArray();
            return next;
        }
    }

    internal sealed partial class Widget
    {
        private void AddBackupSettings(StackPanel fields)
        {
            fields.Children.Add(PanelPage.SectionHeading("Sao lưu và khôi phục", settingsWindow, 20));
            var hint = new TextBlock { Text = "Lưu ngày cá nhân, lời nhắc, dịp ghim và tùy chọn vào tệp JSON.",
                FontSize = 13, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(12, 0, 12, 8) };
            hint.SetResourceReference(TextBlock.ForegroundProperty, "PanelMuted");
            fields.Children.Add(hint);
            var actions = new WrapPanel { Margin = new Thickness(12, 0, 12, 0) };
            var create = new Button { Name = "SaveBackup", Content = "Sao lưu…", MinHeight = 32, MinWidth = 108,
                FontSize = 13, Margin = new Thickness(0, 0, 8, 5) };
            var restore = new Button { Name = "RestoreBackup", Content = "Khôi phục…", MinHeight = 32, MinWidth = 108,
                FontSize = 13, Margin = new Thickness(0, 0, 0, 5) };
            actions.Children.Add(create);
            actions.Children.Add(restore);
            fields.Children.Add(actions);
            var status = new TextBlock { Name = "BackupStatus", Visibility = Visibility.Collapsed,
                TextWrapping = TextWrapping.Wrap, FontSize = 13, Margin = new Thickness(12, 3, 12, 0) };
            AutomationProperties.SetLiveSetting(status, AutomationLiveSetting.Polite);
            fields.Children.Add(status);
            create.Click += (s, e) => ExportBackup(status);
            restore.Click += (s, e) => ImportBackup(status);
        }

        private void ExportBackup(TextBlock status)
        {
            var dialog = new SaveFileDialog
            {
                Title = "Sao lưu Nghỉ Lễ", DefaultExt = ".json", AddExtension = true,
                Filter = "Bản sao lưu Nghỉ Lễ (*.json)|*.json",
                FileName = "NghiLe-sao-luu-" + DateTime.Now.ToString("yyyy-MM-dd")
            };
            if (dialog.ShowDialog(settingsWindow) != true) return;
            if (file.IsSettingsPath(dialog.FileName))
            {
                BackupStatus(status, "Hãy chọn tệp khác tệp cấu hình đang dùng.", true);
                return;
            }
            if (!SettingsBackup.Export(dialog.FileName, config))
            {
                BackupStatus(status, "Không ghi được bản sao lưu. Hãy chọn thư mục khác.", true);
                return;
            }
            BackupStatus(status, "Đã lưu " + Path.GetFileName(dialog.FileName) + ".", false);
        }

        private void ImportBackup(TextBlock status)
        {
            var dialog = new OpenFileDialog { Title = "Chọn bản sao lưu Nghỉ Lễ", CheckFileExists = true,
                Filter = "Bản sao lưu Nghỉ Lễ (*.json)|*.json" };
            if (dialog.ShowDialog(settingsWindow) != true) return;
            var backup = SettingsBackup.Import(dialog.FileName);
            if (backup == null)
            {
                BackupStatus(status, "Không đọc được bản sao lưu Nghỉ Lễ. Hãy chọn tệp JSON đã sao lưu từ ứng dụng.", true);
                return;
            }
            string widgets = backup.AdditionalWidgets == null ? "" : ", " + backup.AdditionalWidgets.Length + " widget bổ sung";
            string description = "Bản sao lưu có " + backup.PersonalDates.Length + " ngày cá nhân, " +
                backup.ReminderDays.Count + " lời nhắc, " + backup.PinnedOccasionIds.Length + " dịp ghim" + widgets + ".\n\n" +
                "Bạn có muốn thay thế dữ liệu và tùy chọn hiện tại bằng bản sao lưu này không?";
            if (System.Windows.MessageBox.Show(settingsWindow, description, "Khôi phục Nghỉ Lễ",
                MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) return;

            if (!ApplyBackup(backup))
            {
                BackupStatus(status, "Không lưu được cấu hình đã khôi phục. Hãy thử lại.", true);
                return;
            }
            ShowRestoreResult(Path.GetFileName(dialog.FileName));
        }

        private void ShowRestoreResult(string fileName)
        {
            OpenSettings();
            var options = (TabItem)LogicalTreeHelper.FindLogicalNode(settingsWindow, "OptionsTab");
            options.IsSelected = true;
            var updated = (TextBlock)LogicalTreeHelper.FindLogicalNode(optionsPage, "BackupStatus");
            BackupStatus(updated, "Đã khôi phục từ " + fileName + ".", false);
            settingsWindow.UpdateLayout();
            optionsPage.ScrollToEnd();
        }

        private bool ApplyBackup(Settings backup)
        {
            if (resizing) EndEdgeResize();
            CollectCompanionPlacement();
            var next = SettingsBackup.ForThisDevice(backup, config);
            if (ready) { next.X = Left; next.Y = Top; }
            if (!file.Save(next)) return false;
            save.Stop();
            config = next;
            bool bringBackPet = pet != null && pet.IsRunning && config.PetEnabled;
            if (pet != null)
            {
                pet.Stop();
                pet.Close();
                pet = null;
            }
            showPetItem.Enabled = showBothItem.Enabled = config.PetEnabled;
            Topmost = config.AlwaysOnTop;
            ApplyOrientation();
            UpdateClock();
            ApplyHolidayTheme(displayedTheme);
            if (settingsWindow != null) settingsWindow.Close();
            CloseCompanions();
            RestoreCompanions();
            UpdateReminderTimer();
            if (bringBackPet) ShowPet(true);
            ResetPetDelay();
            return true;
        }

        private static void BackupStatus(TextBlock status, string message, bool failed)
        {
            status.SetResourceReference(TextBlock.ForegroundProperty, failed ? "PanelError" : "PanelGold");
            status.Text = message;
            status.Visibility = Visibility.Visible;
            var peer = UIElementAutomationPeer.FromElement(status);
            if (peer != null) peer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
        }
    }
}
