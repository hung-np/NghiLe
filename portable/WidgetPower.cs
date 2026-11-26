using System;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace NghiLe
{
    internal static class WidgetPower
    {
        internal const int DefaultDimFrom = 22;
        internal const int DefaultDimUntil = 6;

        internal static bool CanAnimate(Settings settings, bool onBattery, bool visible)
        {
            return visible && settings.Motion && !(settings.PauseOnBattery && onBattery);
        }

        internal static bool DimAt(Settings settings, int hour)
        {
            int from = settings.DimFromHour, until = settings.DimUntilHour;
            if (!settings.DimOnSchedule || from == until) return false;
            return from < until ? hour >= from && hour < until : hour >= from || hour < until;
        }

        internal static int ValidHour(int hour, int fallback) { return hour >= 0 && hour < 24 ? hour : fallback; }

        internal static bool RunsOnBattery()
        {
            return Forms.SystemInformation.PowerStatus.PowerLineStatus == Forms.PowerLineStatus.Offline;
        }
    }

    internal sealed partial class Widget
    {
        private bool onBattery;
        private readonly Border dimLayer = new Border
        {
            Background = Brushes.Black, Opacity = .24, IsHitTestVisible = false, Visibility = Visibility.Collapsed
        };

        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs args)
        {
            if (args.Mode == PowerModes.Suspend) return;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (quitting) return;
                onBattery = WidgetPower.RunsOnBattery();
                UpdateMotion();
                UpdateDim(DateTime.Now);
            }));
        }

        private void UpdateDim(DateTime local)
        {
            dimLayer.Visibility = WidgetPower.DimAt(config, local.Hour) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void AddPowerSettings(StackPanel fields)
        {
            fields.Children.Add(PanelPage.SectionHeading("Tiết kiệm pin", settingsWindow, 20));
            var battery = MakeSwitch("Dừng chuyển động khi dùng pin", "Dừng chuyển động của widget khi máy dùng pin",
                config.PauseOnBattery, value => { config.PauseOnBattery = value; UpdateMotion(); });
            battery.Name = "PauseOnBattery";
            AddOptionRow(fields, battery);

            ComboBox begin = HourPicker("DimFromHour", "Bắt đầu giảm sáng", config.DimFromHour);
            ComboBox end = HourPicker("DimUntilHour", "Kết thúc giảm sáng", config.DimUntilHour);
            var status = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 13, Margin = new Thickness(12, 4, 12, 8) };
            status.SetResourceReference(TextBlock.ForegroundProperty, "PanelMuted");
            AutomationProperties.SetLiveSetting(status, AutomationLiveSetting.Polite);
            Action refresh = () =>
            {
                begin.IsEnabled = end.IsEnabled = config.DimOnSchedule;
                status.Text = config.DimOnSchedule && config.DimFromHour == config.DimUntilHour
                    ? "Chọn hai giờ khác nhau để giảm sáng." : "Giờ giảm sáng theo đồng hồ Windows.";
                UpdateDim(DateTime.Now);
            };
            var dim = MakeSwitch("Giảm sáng theo giờ", "Giảm sáng widget theo lịch", config.DimOnSchedule,
                value => { config.DimOnSchedule = value; refresh(); });
            dim.Name = "DimOnSchedule";
            AddOptionRow(fields, dim);
            begin.SelectionChanged += (s, e) =>
            {
                if (begin.SelectedIndex < 0) return;
                config.DimFromHour = begin.SelectedIndex;
                refresh();
                ScheduleSave();
            };
            end.SelectionChanged += (s, e) =>
            {
                if (end.SelectedIndex < 0) return;
                config.DimUntilHour = end.SelectedIndex;
                refresh();
                ScheduleSave();
            };
            AddOptionRow(fields, begin, "Bắt đầu giảm sáng");
            AddOptionRow(fields, end, "Kết thúc giảm sáng");
            fields.Children.Add(status);
            refresh();
        }

        private static ComboBox HourPicker(string name, string label, int hour)
        {
            var picker = new ComboBox
            {
                Name = name, Width = 96, MinHeight = 30, FontSize = 14, HorizontalAlignment = HorizontalAlignment.Right
            };
            AutomationProperties.SetName(picker, label);
            for (int value = 0; value < 24; value++)
                picker.Items.Add(new ComboBoxItem { Content = value.ToString("00", CultureInfo.InvariantCulture) + ":00" });
            picker.SelectedIndex = hour;
            return picker;
        }
    }
}
