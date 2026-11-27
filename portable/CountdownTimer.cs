using System;
using System.Globalization;
using System.Media;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace NghiLe
{
    public enum TimerState { Ready, Running, Paused, Finished }

    public sealed class TimerSession
    {
        internal const int MaximumSeconds = 999 * 3600 + 59 * 60 + 59;
        internal const int DefaultSeconds = 30 * 60;
        public int DurationSeconds { get; set; }
        public TimerState State { get; set; }
        public long DeadlineUtcTicks { get; set; }
        public long PausedTicks { get; set; }
        public double? X { get; set; }
        public double? Y { get; set; }
        public double Scale { get; set; }
        public bool Compact { get; set; }
        public bool Visible { get; set; }

        public TimerSession() { DurationSeconds = DefaultSeconds; Scale = 1; }

        internal TimerSession Copy() { return (TimerSession)MemberwiseClone(); }

        internal static void Normalize(TimerSession session)
        {
            if (session == null) return;
            if (session.DurationSeconds < 1 || session.DurationSeconds > MaximumSeconds)
                session.DurationSeconds = DefaultSeconds;
            if (!ValidCoordinate(session.X) || !ValidCoordinate(session.Y)) session.X = session.Y = null;
            session.Scale = WidgetSize.Clamp(session.Scale);
            switch (session.State)
            {
                case TimerState.Running:
                case TimerState.Finished:
                    if (session.DeadlineUtcTicks > 0 && session.DeadlineUtcTicks <= DateTime.MaxValue.Ticks) return;
                    break;
                case TimerState.Paused:
                    if (session.PausedTicks > 0 && session.PausedTicks <= TimeSpan.FromSeconds(session.DurationSeconds).Ticks) return;
                    break;
            }
            session.State = TimerState.Ready;
        }

        private static bool ValidCoordinate(double? value)
        {
            return value.HasValue && Math.Abs(value.Value) < int.MaxValue;
        }
    }

    internal sealed class CountdownTimer : IDisposable
    {
        private readonly Func<DateTime> utcNow;
        private readonly DispatcherTimer tick = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        private DateTime deadline;
        private TimeSpan remaining;

        internal TimeSpan Duration { get; private set; }
        internal TimerState State { get; private set; }
        internal int Seconds { get; private set; }
        internal long OvertimeSeconds { get; private set; }
        internal string DisplayTime
        {
            get
            {
                long value = State == TimerState.Finished ? OvertimeSeconds : Seconds;
                return string.Format(CultureInfo.InvariantCulture, "{0}{1:00}:{2:00}:{3:00}",
                    State == TimerState.Finished ? "+" : "", value / 3600, value / 60 % 60, value % 60);
            }
        }
        internal double Progress { get { return Math.Max(0, Math.Min(1, 1 - Seconds / Duration.TotalSeconds)); } }
        internal bool CanStartNew { get { return State == TimerState.Ready || State == TimerState.Finished; } }
        internal event Action Changed;
        internal event Action SessionChanged;
        internal event Action Finished;

        internal CountdownTimer(Func<DateTime> clock = null, TimerSession session = null)
        {
            utcNow = clock ?? (() => DateTime.UtcNow);
            TimerSession.Normalize(session);
            Duration = TimeSpan.FromSeconds(session == null ? TimerSession.DefaultSeconds : session.DurationSeconds);
            remaining = Duration;
            Seconds = (int)Duration.TotalSeconds;
            tick.Tick += (s, e) => Refresh();
            if (session == null) return;

            State = session.State;
            if (State == TimerState.Paused)
            {
                remaining = TimeSpan.FromTicks(session.PausedTicks);
                Seconds = (int)Math.Ceiling(remaining.TotalSeconds);
            }
            if (State == TimerState.Running || State == TimerState.Finished)
            {
                deadline = new DateTime(session.DeadlineUtcTicks, DateTimeKind.Utc);
                if (State == TimerState.Finished) tick.Interval = TimeSpan.FromMilliseconds(500);
                Refresh();
                tick.Start();
            }
        }

        internal void SaveTo(TimerSession session)
        {
            session.DurationSeconds = (int)Duration.TotalSeconds;
            session.State = State;
            session.DeadlineUtcTicks = State == TimerState.Running || State == TimerState.Finished ? deadline.Ticks : 0;
            session.PausedTicks = State == TimerState.Paused ? remaining.Ticks : 0;
        }

        internal void SetDuration(TimeSpan duration)
        {
            if (duration.TotalSeconds < 1 || duration.TotalSeconds > TimerSession.MaximumSeconds) return;
            Duration = remaining = duration;
            if (State == TimerState.Running) deadline = utcNow().Add(duration);
            if (State == TimerState.Finished)
            {
                State = TimerState.Ready;
                tick.Stop();
            }
            PublishSession();
        }

        internal void Toggle()
        {
            if (State == TimerState.Running)
            {
                Refresh();
                if (State != TimerState.Running) return;
                State = TimerState.Paused;
                tick.Stop();
                PublishSession();
                return;
            }

            if (State != TimerState.Paused) remaining = Duration;
            deadline = utcNow().Add(remaining);
            State = TimerState.Running;
            tick.Start();
            PublishSession();
        }

        internal void Reset()
        {
            tick.Stop();
            State = TimerState.Ready;
            remaining = Duration;
            PublishSession();
        }

        internal void Refresh()
        {
            if (State != TimerState.Running && State != TimerState.Finished) return;
            var delta = deadline - utcNow();
            if (State == TimerState.Finished)
            {
                long overtime = Math.Max(0, (long)Math.Floor(-delta.TotalSeconds));
                if (Seconds == 0 && overtime == OvertimeSeconds) return;
                remaining = TimeSpan.Zero;
                OvertimeSeconds = overtime;
                Publish();
                return;
            }
            if (delta <= TimeSpan.Zero)
            {
                remaining = TimeSpan.Zero;
                State = TimerState.Finished;
                OvertimeSeconds = (long)Math.Floor(-delta.TotalSeconds);
                tick.Interval = TimeSpan.FromMilliseconds(500);
                PublishSession();
                if (Finished != null) Finished();
                return;
            }

            remaining = delta < Duration ? delta : Duration;
            if (Seconds != (int)Math.Ceiling(remaining.TotalSeconds)) Publish();
        }

        private void PublishSession()
        {
            if (State != TimerState.Finished)
            {
                OvertimeSeconds = 0;
                tick.Interval = TimeSpan.FromMilliseconds(150);
            }
            Publish();
            if (SessionChanged != null) SessionChanged();
        }

        private void Publish()
        {
            Seconds = (int)Math.Ceiling(remaining.TotalSeconds);
            if (Changed != null) Changed();
        }

        internal static bool Check()
        {
            var now = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            using (var timer = new CountdownTimer(() => now))
            {
                int completions = 0;
                timer.Finished += () => completions++;
                timer.SetDuration(TimeSpan.FromHours(7));
                if (timer.Seconds != 25200 || timer.State != TimerState.Ready) return false;
                timer.Toggle();
                now = now.AddMinutes(10);
                timer.SetDuration(TimeSpan.FromMinutes(30));
                if (timer.Seconds != 1800 || timer.State != TimerState.Running) return false;
                now = now.AddSeconds(60.5);
                timer.Toggle();
                if (timer.Seconds != 1740 || timer.State != TimerState.Paused) return false;
                now = now.AddHours(1);
                timer.Refresh();
                if (timer.Seconds != 1740) return false;
                timer.SetDuration(TimeSpan.FromMinutes(3));
                if (timer.Seconds != 180 || timer.State != TimerState.Paused) return false;
                timer.Toggle();
                now = now.AddSeconds(180);
                timer.Refresh();
                timer.Refresh();
                if (timer.Seconds != 0 || timer.State != TimerState.Finished || completions != 1) return false;
                timer.Reset();
                return timer.State == TimerState.Ready && timer.Seconds == 180 && CheckRestore();
            }
        }

        private static bool CheckRestore()
        {
            var now = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var session = new TimerSession();
            using (var timer = new CountdownTimer(() => now))
            {
                timer.SetDuration(TimeSpan.FromMinutes(1));
                timer.Toggle();
                timer.SaveTo(session);
            }
            now = now.AddSeconds(25);
            using (var resumed = new CountdownTimer(() => now, session))
            {
                if (resumed.State != TimerState.Running || resumed.DisplayTime != "00:00:35") return false;
                now = now.AddMilliseconds(250);
                resumed.Toggle();
                resumed.SaveTo(session);
            }
            now = now.AddMinutes(10);
            using (var resumed = new CountdownTimer(() => now, session))
            {
                if (resumed.State != TimerState.Paused || resumed.DisplayTime != "00:00:35") return false;
                resumed.Toggle();
                now = now.AddMilliseconds(34750);
                resumed.Refresh();
                if (resumed.DisplayTime != "+00:00:00") return false;
                now = now.AddSeconds(138);
                resumed.Refresh();
                if (resumed.DisplayTime != "+00:02:18" || resumed.Progress != 1) return false;
                resumed.SaveTo(session);
            }
            now = now.AddHours(1);
            using (var resumed = new CountdownTimer(() => now, session))
            {
                if (resumed.State != TimerState.Finished || resumed.DisplayTime != "+01:02:18") return false;
            }

            session.State = TimerState.Running;
            using (var resumed = new CountdownTimer(() => now, session))
            {
                if (resumed.State != TimerState.Finished || resumed.DisplayTime != "+01:02:18") return false;
                resumed.Toggle();
                if (resumed.State != TimerState.Running || resumed.DisplayTime != "00:01:00") return false;
            }

            session.DeadlineUtcTicks = -1;
            using (var resumed = new CountdownTimer(() => now, session))
            {
                return resumed.State == TimerState.Ready && resumed.DisplayTime == "00:01:00";
            }
        }

        public void Dispose() { tick.Stop(); }
    }

    internal sealed class TimerReadout : StackPanel
    {
        private readonly CountdownTimer timer;
        private readonly TextBlock digits;
        private readonly TextBlock status;
        private readonly Button primary;
        private readonly Button reset;
        private readonly bool compact;
        private readonly TextBlock primaryLabel;
        private readonly Path arc;
        private double paintedProgress = -1;

        internal TimerReadout(CountdownTimer timer, FrameworkElement theme, bool compact, Action toggle)
        {
            this.timer = timer;
            this.compact = compact;
            digits = new TextBlock
            {
                FontFamily = Widget.CounterFont, FontSize = 43,
                TextAlignment = TextAlignment.Center
            };
            digits.SetResourceReference(TextBlock.ForegroundProperty, "PanelInk");
            Typography.SetNumeralAlignment(digits, FontNumeralAlignment.Tabular);
            status = new TextBlock
            {
                Height = 22, FontSize = 13, TextAlignment = TextAlignment.Center
            };
            status.SetResourceReference(TextBlock.ForegroundProperty, "PanelGold");
            AutomationProperties.SetLiveSetting(status, AutomationLiveSetting.Polite);
            var numbers = new Viewbox { Child = digits, StretchDirection = StretchDirection.DownOnly,
                HorizontalAlignment = HorizontalAlignment.Center };
            if (compact)
            {
                Children.Add(numbers);
                Children.Add(status);
            }
            else
            {
                numbers.MaxWidth = 172;
                var dial = new Grid { Width = 216, Height = 216, HorizontalAlignment = HorizontalAlignment.Center };
                dial.Children.Add(DialOutline(205, 6, .19));
                dial.Children.Add(DialOutline(183, 1, .24));
                arc = new Path
                {
                    StrokeThickness = 6, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round,
                    IsHitTestVisible = false
                };
                arc.SetResourceReference(Shape.StrokeProperty, "PanelGold");
                dial.Children.Add(arc);
                var reading = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 8, 0, 0) };
                reading.Children.Add(numbers);
                reading.Children.Add(status);
                dial.Children.Add(reading);
                Children.Add(dial);
            }

            var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, compact ? 6 : 10, 0, 0) };
            primary = ActionButton(theme, "", 44, true);
            primary.FontSize = 19;
            primary.Click += (s, e) => toggle();
            reset = ActionButton(theme, "↺", 44);
            reset.FontSize = 21;
            reset.Click += (s, e) => timer.Reset();
            AutomationProperties.SetName(reset, "Đặt lại");
            reset.ToolTip = "Đặt lại về thời lượng đã chọn";
            if (compact)
            {
                reset.Margin = new Thickness(8, 0, 0, 0);
                actions.Children.Add(primary);
                actions.Children.Add(reset);
            }
            else
            {
                var round = (Style)theme.FindResource("TimerRoundAction");
                primary.Style = reset.Style = round;
                primary.Width = reset.Width = primary.Height = reset.Height = 44;
                primaryLabel = ActionLabel("");
                actions.Children.Add(LabeledAction(primary, primaryLabel));
                actions.Children.Add(LabeledAction(reset, ActionLabel("Đặt lại")));
            }
            Children.Add(actions);
            Refresh();
        }

        private static Ellipse DialOutline(double size, double thickness, double opacity)
        {
            var outline = new Ellipse { Width = size, Height = size, StrokeThickness = thickness, Opacity = opacity,
                IsHitTestVisible = false };
            outline.SetResourceReference(Shape.StrokeProperty, "PanelGold");
            return outline;
        }

        private static TextBlock ActionLabel(string text)
        {
            var label = new TextBlock { Text = text, FontSize = 12, TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 6, 0, 0) };
            label.SetResourceReference(TextBlock.ForegroundProperty, "PanelInk");
            return label;
        }

        private static StackPanel LabeledAction(Button button, TextBlock label)
        {
            var stack = new StackPanel { Width = 82 };
            button.HorizontalAlignment = HorizontalAlignment.Center;
            stack.Children.Add(button);
            stack.Children.Add(label);
            return stack;
        }

        private void PaintProgress(double progress)
        {
            if (arc == null || progress == paintedProgress) return;
            paintedProgress = progress;
            const double center = 108;
            const double radius = 99.5;
            if (progress == 0) { arc.Data = Geometry.Empty; return; }
            if (progress == 1) { arc.Data = new EllipseGeometry(new Point(center, center), radius, radius); return; }
            double angle = progress * 2 * Math.PI;
            var path = new StreamGeometry();
            using (var context = path.Open())
            {
                context.BeginFigure(new Point(center, center - radius), false, false);
                context.ArcTo(new Point(center + Math.Sin(angle) * radius, center - Math.Cos(angle) * radius),
                    new Size(radius, radius), 0, progress > .5, SweepDirection.Clockwise, true, false);
            }
            path.Freeze();
            arc.Data = path;
        }

        internal bool CanStart { set { primary.IsEnabled = value; } }

        internal void Refresh()
        {
            digits.Text = timer.DisplayTime;

            bool running = timer.State == TimerState.Running;
            bool paused = timer.State == TimerState.Paused;
            bool finished = timer.State == TimerState.Finished;
            string action = running ? "Tạm dừng" : paused ? "Tiếp tục" : "Bắt đầu";
            string message = finished ? "Hết giờ" : paused ? "Đã tạm dừng" : compact ? "" : running ? "Đang đếm" : "Sẵn sàng";
            PaintProgress(timer.Progress);
            if (status.Text != message)
            {
                status.Text = message;
                if (message.Length > 0)
                {
                    var peer = UIElementAutomationPeer.FromElement(status);
                    if (peer != null) peer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
                }
            }
            primary.Content = running ? "Ⅱ" : "▶";
            if (primaryLabel != null) primaryLabel.Text = action;
            AutomationProperties.SetName(primary, action);
            primary.ToolTip = action;
            reset.IsEnabled = timer.State != TimerState.Ready;
        }

        internal static Button ActionButton(FrameworkElement theme, string text, double width, bool primary = false)
        {
            var button = new Button
            {
                Style = (Style)theme.FindResource(primary ? (object)"PanelPrimary" : typeof(Button)),
                Content = text, MinWidth = width, MinHeight = 38, FontSize = 14
            };
            button.SetResourceReference(Control.BackgroundProperty, primary ? "PanelGold" : "PanelField");
            button.SetResourceReference(Control.ForegroundProperty, primary ? "PanelOnGold" : "PanelInk");
            button.SetResourceReference(Control.BorderBrushProperty, primary ? "PanelGold" : "PanelLine");
            return button;
        }
    }

    internal sealed class TimerSettingsPage : StackPanel
    {
        private readonly CountdownTimer timer;
        private readonly TextBox[] fields = new TextBox[3];
        private readonly TextBlock error;
        private readonly TimerReadout readout;
        private readonly Button apply;
        private TimeSpan? draft;
        private bool hasChanges;
        private bool fillingFields;

        internal TimerSettingsPage(CountdownTimer timer, FrameworkElement theme, ResourceDictionary palette, Action<bool> showWidget)
        {
            this.timer = timer;
            readout = new TimerReadout(timer, theme, false, Toggle);
            readout.Margin = new Thickness(0, 4, 0, 4);
            var panel = new Grid();
            panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            panel.Children.Add(readout);
            var face = new Border
            {
                Child = panel, MaxWidth = 680, Padding = new Thickness(16),
                CornerRadius = new CornerRadius(16), BorderThickness = new Thickness(1)
            };
            face.Resources.MergedDictionaries.Add(palette);
            face.SetResourceReference(Border.BackgroundProperty, "TimerFaceBackground");
            face.SetResourceReference(Border.BorderBrushProperty, "PanelLine");
            Children.Add(face);

            var controls = new StackPanel { MaxWidth = 274 };
            var durationPanel = new Border
            {
                Child = controls, CornerRadius = new CornerRadius(12), Padding = new Thickness(12),
                BorderThickness = new Thickness(1), VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(6, 0, 0, 0)
            };
            durationPanel.SetResourceReference(Border.BackgroundProperty, "PanelField");
            durationPanel.SetResourceReference(Border.BorderBrushProperty, "PanelLine");
            Grid.SetColumn(durationPanel, 1);
            panel.Children.Add(durationPanel);
            panel.SizeChanged += (s, e) =>
            {
                bool stacked = e.NewSize.Width < 438;
                Grid.SetRow(durationPanel, stacked ? 1 : 0);
                Grid.SetColumn(durationPanel, stacked ? 0 : 1);
                Grid.SetColumnSpan(durationPanel, stacked ? 2 : 1);
                Grid.SetColumnSpan(readout, stacked ? 2 : 1);
                durationPanel.Margin = stacked ? new Thickness(0, 16, 0, 0) : new Thickness(6, 0, 0, 0);
            };

            var heading = new TextBlock { Text = "Thời lượng", FontFamily = Widget.DisplayFont, FontSize = 24 };
            heading.SetResourceReference(TextBlock.ForegroundProperty, "PanelInk");
            controls.Children.Add(heading);
            var units = new Grid { Margin = new Thickness(0, 7, 0, 0) };
            var labels = new[] { "Giờ", "Phút", "Giây" };
            var names = new[] { "TimerHours", "TimerMinutes", "TimerSeconds" };
            for (int i = 0; i < fields.Length; i++)
            {
                units.ColumnDefinitions.Add(new ColumnDefinition());
                var column = new StackPanel { Margin = new Thickness(i == 0 ? 0 : 5, 0, i == 2 ? 0 : 5, 0) };
                var input = new TextBox
                {
                    Name = names[i], Height = 36,
                    MaxLength = i == 0 ? 3 : 2, TextAlignment = TextAlignment.Center,
                    FontFamily = Widget.CounterFont, FontSize = 18, Style = (Style)theme.FindResource("SettingsTextInput")
                };
                input.SetResourceReference(Control.BorderBrushProperty, "PanelLine");
                input.SetResourceReference(Control.BackgroundProperty, "PanelSelected");
                input.SetResourceReference(Control.ForegroundProperty, "PanelInk");
                input.SetResourceReference(TextBox.CaretBrushProperty, "PanelInk");
                fields[i] = input;
                AutomationProperties.SetName(input, labels[i]);
                input.GotKeyboardFocus += (s, e) => input.SelectAll();
                input.PreviewMouseLeftButtonDown += (s, e) =>
                {
                    if (input.IsKeyboardFocusWithin || !input.Focus()) return;
                    e.Handled = true;
                };
                input.TextChanged += (s, e) =>
                {
                    if (fillingFields) return;
                    hasChanges = true;
                    ValidateDraft();
                };
                input.KeyDown += (s, e) =>
                {
                    if (e.Key != Key.Enter) return;
                    e.Handled = true;
                    if (apply.IsEnabled) Commit(draft.Value);
                };
                column.Children.Add(new Label
                {
                    Content = labels[i], Target = input, HorizontalAlignment = HorizontalAlignment.Center,
                    Padding = new Thickness(2, 0, 2, 5), FontSize = 12,
                    Foreground = (Brush)theme.FindResource("PanelMuted")
                });
                column.Children.Add(input);
                Grid.SetColumn(column, i);
                units.Children.Add(column);
            }
            controls.Children.Add(units);
            apply = TimerReadout.ActionButton(theme, "Áp dụng", 88, true);
            apply.MinHeight = 32;
            apply.HorizontalAlignment = HorizontalAlignment.Right;
            apply.Margin = new Thickness(0, 8, 0, 0);
            apply.Click += (s, e) => Commit(draft.Value);
            controls.Children.Add(apply);

            var presets = new Grid { Margin = new Thickness(0, 11, 0, 0) };
            foreach (int minutes in new[] { 5, 30, 60 })
            {
                int index = presets.Children.Count;
                presets.ColumnDefinitions.Add(new ColumnDefinition());
                var preset = TimerReadout.ActionButton(theme, minutes == 60 ? "1 giờ" : minutes + " phút", 0);
                preset.MinHeight = 30;
                preset.FontSize = 12;
                preset.Margin = new Thickness(index == 0 ? 0 : 4, 0, index == 2 ? 0 : 4, 0);
                preset.Click += (s, e) => Commit(TimeSpan.FromMinutes(minutes));
                Grid.SetColumn(preset, index);
                presets.Children.Add(preset);
            }
            controls.Children.Add(presets);
            error = new TextBlock
            {
                FontSize = 12, TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 0), Foreground = (Brush)theme.FindResource("PanelError"),
                Visibility = Visibility.Collapsed
            };
            AutomationProperties.SetLiveSetting(error, AutomationLiveSetting.Polite);
            controls.Children.Add(error);
            var divider = new Border { Height = 1, Opacity = .54, Margin = new Thickness(0, 13, 0, 11) };
            divider.SetResourceReference(Border.BackgroundProperty, "PanelLine");
            controls.Children.Add(divider);
            var layouts = new Grid();
            layouts.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) });
            layouts.ColumnDefinitions.Add(new ColumnDefinition());
            var openWidget = TimerReadout.ActionButton(theme, "Mở widget nổi", 0);
            openWidget.MinHeight = 34;
            openWidget.FontSize = 12;
            openWidget.Margin = new Thickness(0, 0, 4, 0);
            openWidget.Click += (s, e) => showWidget(false);
            layouts.Children.Add(openWidget);
            var openCompact = TimerReadout.ActionButton(theme, "Thu gọn", 0);
            openCompact.MinHeight = 34;
            openCompact.FontSize = 12;
            openCompact.Margin = new Thickness(4, 0, 0, 0);
            Grid.SetColumn(openCompact, 1);
            openCompact.ToolTip = "Mở dải hẹn giờ thu gọn";
            openCompact.Click += (s, e) => showWidget(true);
            layouts.Children.Add(openCompact);
            controls.Children.Add(layouts);

            FillFields();
            Refresh();
            Loaded += (s, e) => { timer.Changed += Refresh; Refresh(); };
            Unloaded += (s, e) => timer.Changed -= Refresh;
        }

        private void FillFields()
        {
            fillingFields = true;
            var duration = timer.Duration;
            fields[0].Text = ((int)duration.TotalHours).ToString("00", CultureInfo.InvariantCulture);
            fields[1].Text = duration.Minutes.ToString("00", CultureInfo.InvariantCulture);
            fields[2].Text = duration.Seconds.ToString("00", CultureInfo.InvariantCulture);
            fillingFields = false;
            hasChanges = false;
            ValidateDraft();
        }

        private static bool TryRead(TextBox input, int max, out int value)
        {
            value = 0;
            return input.Text.Length == 0 ||
                (int.TryParse(input.Text, NumberStyles.None, CultureInfo.InvariantCulture, out value) && value <= max);
        }

        private TimeSpan? ReadDraft()
        {
            int hours, minutes, seconds;
            if (!TryRead(fields[0], 999, out hours) || !TryRead(fields[1], 59, out minutes) ||
                !TryRead(fields[2], 59, out seconds)) return null;
            var duration = new TimeSpan(hours, minutes, seconds);
            return duration == TimeSpan.Zero ? (TimeSpan?)null : duration;
        }

        private void ValidateDraft()
        {
            draft = ReadDraft();
            apply.IsEnabled = hasChanges && draft.HasValue;
            readout.CanStart = !timer.CanStartNew || draft.HasValue;
            string message = draft.HasValue ? "" : "Nhập thời lượng lớn hơn 0. Phút và giây từ 0 đến 59.";
            if (error.Text == message) return;
            error.Text = message;
            error.Visibility = draft.HasValue ? Visibility.Collapsed : Visibility.Visible;
            var peer = UIElementAutomationPeer.FromElement(error);
            if (!draft.HasValue && peer != null) peer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
        }

        private void Commit(TimeSpan duration)
        {
            timer.SetDuration(duration);
            FillFields();
        }

        private void Toggle()
        {
            if (timer.CanStartNew && hasChanges)
            {
                if (!draft.HasValue) return;
                Commit(draft.Value);
            }
            timer.Toggle();
        }

        private void Refresh()
        {
            readout.Refresh();
            readout.CanStart = !timer.CanStartNew || draft.HasValue;
        }
    }

    internal sealed class TimerCompactStrip : Border
    {
        internal static readonly Size DesignSize = new Size(246, 42);
        internal const string Instructions = "Kéo trên dải để di chuyển, hoặc dùng Ctrl + phím mũi tên.";
        private readonly CountdownTimer timer;
        private readonly TextBlock label;
        private readonly TextBlock digits;
        private readonly ProgressBar progress;
        private readonly Button expand;

        internal TimerCompactStrip(CountdownTimer timer, FrameworkElement theme, Action restore)
        {
            this.timer = timer;
            CornerRadius = new CornerRadius(8);
            BorderThickness = new Thickness(1);
            SetResourceReference(BackgroundProperty, "TimerFaceBackground");
            SetResourceReference(BorderBrushProperty, "PanelLine");

            var layout = new DockPanel { Margin = new Thickness(8, 3, 8, 4) };
            progress = new ProgressBar
            {
                Name = "TimerCompactProgress", Maximum = 1, Height = 3,
                Style = (Style)theme.FindResource("TimerCompactProgressBar"),
                Focusable = false, IsHitTestVisible = false
            };
            AutomationProperties.SetName(progress, "Tiến độ hẹn giờ");
            progress.SetResourceReference(Control.BackgroundProperty, "PanelField");
            progress.SetResourceReference(Control.ForegroundProperty, "PanelGold");
            DockPanel.SetDock(progress, Dock.Bottom);
            layout.Children.Add(progress);

            var row = new DockPanel { Margin = new Thickness(2, 0, 0, 2) };
            label = new TextBlock { FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
            label.SetResourceReference(TextBlock.ForegroundProperty, "PanelGold");
            DockPanel.SetDock(label, Dock.Left);
            row.Children.Add(label);
            expand = TimerReadout.ActionButton(theme, "⤢", 28);
            expand.MinHeight = 24;
            expand.Margin = new Thickness(8, 0, 0, 0);
            expand.VerticalAlignment = VerticalAlignment.Center;
            expand.ToolTip = "Mở rộng widget (Ctrl + M)";
            AutomationProperties.SetName(expand, "Mở rộng widget");
            expand.Click += (s, e) => restore();
            DockPanel.SetDock(expand, Dock.Right);
            row.Children.Add(expand);

            digits = new TextBlock
            {
                FontFamily = Widget.CounterFont, FontSize = 19, TextAlignment = TextAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            digits.SetResourceReference(TextBlock.ForegroundProperty, "PanelInk");
            Typography.SetNumeralAlignment(digits, FontNumeralAlignment.Tabular);
            row.Children.Add(new Viewbox { Child = digits, StretchDirection = StretchDirection.DownOnly,
                HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center });
            layout.Children.Add(row);
            Child = layout;
            ToolTip = Instructions;
            expand.IsKeyboardFocusedChanged += (s, e) => UpdateControls(IsMouseOver);
            UpdateControls(false);
        }

        internal void UpdateControls(bool mouseOver)
        {
            expand.Opacity = mouseOver || expand.IsKeyboardFocused ? 1 : 0;
        }

        internal void Refresh()
        {
            label.Text = timer.State == TimerState.Finished ? "Hết giờ" : "Hẹn giờ";
            digits.Text = timer.DisplayTime;
            string status = timer.State == TimerState.Paused ? "Đã tạm dừng" : null;
            if (!Equals(digits.ToolTip, status)) digits.ToolTip = status;
            progress.Value = timer.Progress;
        }
    }

    internal sealed class TimerWidgetWindow : Window
    {
        private static readonly Size designSize = new Size(286, 184);
        private readonly Button editButton;
        private readonly Button closeButton;
        private readonly Button compactButton;
        private readonly TimerReadout readout;
        private readonly Viewbox expandedFace;
        private readonly TimerCompactStrip compactStrip;
        private readonly string expandedHelp;
        private bool compactMode;
        private Size expandedSize;
        private Rect resizeBounds;
        private WidgetEdge resizeEdge;
        private Point resizeStart;
        private Matrix resizeFromDevice;

        internal TimerWidgetWindow(CountdownTimer timer, ResourceDictionary theme, ResourceDictionary palette, Action edit)
        {
            Title = "Hẹn giờ · " + Widget.ProductName;
            Width = designSize.Width;
            Height = designSize.Height;
            ShowInTaskbar = false;
            Topmost = true;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            FontFamily = new FontFamily("Segoe UI");
            UseLayoutRounding = true;
            Resources.MergedDictionaries.Add(theme);
            Resources.MergedDictionaries.Add(palette);

            var content = new StackPanel();
            var header = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
            editButton = TimerReadout.ActionButton(this, "↗", 34);
            editButton.MinHeight = 30;
            editButton.ToolTip = "Mở tab Hẹn giờ";
            AutomationProperties.SetName(editButton, "Mở tab Hẹn giờ");
            editButton.Click += (s, e) => edit();

            closeButton = TimerReadout.ActionButton(this, "×", 34);
            closeButton.MinHeight = 30;
            closeButton.ToolTip = "Ẩn widget";
            AutomationProperties.SetName(closeButton, "Ẩn widget");
            closeButton.Click += (s, e) => Hide();

            compactButton = TimerReadout.ActionButton(this, "▁", 34);
            compactButton.MinHeight = 30;
            compactButton.Margin = new Thickness(0, 0, 4, 0);
            compactButton.ToolTip = "Thu gọn widget (Ctrl + M)";
            AutomationProperties.SetName(compactButton, "Thu gọn");
            compactButton.Click += (s, e) => SetCompactMode(true);
            DockPanel.SetDock(compactButton, Dock.Right);
            DockPanel.SetDock(editButton, Dock.Left);
            DockPanel.SetDock(closeButton, Dock.Right);
            header.Children.Add(editButton);
            header.Children.Add(closeButton);
            header.Children.Add(compactButton);
            var title = new TextBlock
            {
                Text = "Hẹn giờ", FontSize = 14, TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = "Kéo mặt đồng hồ để di chuyển. Kéo cạnh hoặc góc để đổi kích thước. Dùng Ctrl + phím mũi tên để di chuyển bằng bàn phím."
            };
            title.SetResourceReference(TextBlock.ForegroundProperty, "PanelGold");
            expandedHelp = (string)title.ToolTip;
            AutomationProperties.SetHelpText(this, expandedHelp);
            header.Children.Add(title);
            content.Children.Add(header);
            readout = new TimerReadout(timer, this, true, timer.Toggle);
            content.Children.Add(readout);
            var frame = new Border
            {
                Width = designSize.Width, Height = designSize.Height, Child = content,
                CornerRadius = new CornerRadius(12), BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 10, 12, 12)
            };
            frame.SetResourceReference(Border.BackgroundProperty, "TimerFaceBackground");
            frame.SetResourceReference(Border.BorderBrushProperty, "PanelLine");
            expandedFace = new Viewbox { Child = frame, Stretch = Stretch.Fill };
            compactStrip = new TimerCompactStrip(timer, this, () => SetCompactMode(false));
            Content = expandedFace;
            MouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                DragMove();
            };

            var area = SystemParameters.WorkArea;
            Left = Math.Max(area.Left, area.Right - Width - 24);
            Top = Math.Max(area.Top, area.Bottom - Height - 24);
            MouseEnter += (s, e) => UpdateHeaderControls();
            MouseLeave += (s, e) =>
            {
                if (resizeEdge == WidgetEdge.None) Cursor = null;
                UpdateHeaderControls();
            };
            editButton.IsKeyboardFocusedChanged += (s, e) => UpdateHeaderControls();
            closeButton.IsKeyboardFocusedChanged += (s, e) => UpdateHeaderControls();
            compactButton.IsKeyboardFocusedChanged += (s, e) => UpdateHeaderControls();
            UpdateHeaderControls();

            PreviewMouseLeftButtonDown += (s, e) =>
            {
                var point = e.GetPosition(this);
                var edge = ResizeEdgeAt(point);
                var source = PresentationSource.FromVisual(this);
                if (edge == WidgetEdge.None || source == null || !CaptureMouse()) return;
                resizeEdge = edge;
                resizeStart = PointToScreen(point);
                resizeBounds = Bounds;
                resizeFromDevice = source.CompositionTarget.TransformFromDevice;
                Cursor = WidgetSize.ResizeCursor(edge);
                e.Handled = true;
            };
            PreviewMouseMove += (s, e) =>
            {
                var point = e.GetPosition(this);
                if (resizeEdge == WidgetEdge.None)
                {
                    Cursor = WidgetSize.ResizeCursor(ResizeEdgeAt(point)) ?? Cursors.SizeAll;
                    return;
                }
                ResizeAt(PointToScreen(point));
                e.Handled = true;
            };
            PreviewMouseLeftButtonUp += (s, e) =>
            {
                if (resizeEdge == WidgetEdge.None) return;
                ResizeAt(PointToScreen(e.GetPosition(this)));
                EndResize();
                e.Handled = true;
            };
            LostMouseCapture += (s, e) => { if (e.OriginalSource == this) EndResize(); };
            PreviewKeyDown += HandleKey;
            timer.Changed += Refresh;
            Closed += (s, e) => timer.Changed -= Refresh;
        }

        private Rect Bounds { get { return new Rect(Left, Top, Width, Height); } }

        internal void SetCompactMode(bool enabled)
        {
            if (compactMode == enabled) return;
            EndResize();
            double right = Left + Width;
            if (enabled) expandedSize = new Size(Width, Height);
            var size = enabled ? TimerCompactStrip.DesignSize : expandedSize;
            compactMode = enabled;
            Width = size.Width;
            Height = size.Height;
            Left = right - Width;
            Content = enabled ? (UIElement)compactStrip : expandedFace;
            KeepInWorkArea();

            AutomationProperties.SetHelpText(this, enabled ? TimerCompactStrip.Instructions : expandedHelp);
            Refresh();
            UpdateHeaderControls();
        }

        internal void RestorePlacement(TimerSession session)
        {
            if (session == null) return;
            Width = designSize.Width * session.Scale;
            Height = designSize.Height * session.Scale;
            SetCompactMode(session.Compact);
            if (session.X.HasValue && session.Y.HasValue)
            {
                Left = session.X.Value;
                Top = session.Y.Value;
            }
        }

        internal void SavePlacement(TimerSession session)
        {
            session.X = Left;
            session.Y = Top;
            session.Scale = WidgetSize.Clamp((compactMode ? expandedSize.Width : Width) / designSize.Width);
            session.Compact = compactMode;
            session.Visible = IsVisible;
        }

        internal void KeepInWorkArea()
        {
            var area = GetWorkArea();
            Left = Math.Max(area.Left, Math.Min(Left, area.Right - Width));
            Top = Math.Max(area.Top, Math.Min(Top, area.Bottom - Height));
        }

        private Rect GetWorkArea()
        {
            var source = PresentationSource.FromVisual(this);
            if (source == null) return SystemParameters.WorkArea;
            var work = Forms.Screen.FromHandle(new WindowInteropHelper(this).Handle).WorkingArea;
            var fromDevice = source.CompositionTarget.TransformFromDevice;
            return new Rect(fromDevice.Transform(new Point(work.Left, work.Top)),
                fromDevice.Transform(new Point(work.Right, work.Bottom)));
        }

        private bool MoveByKey(Key key)
        {
            const int step = 12;
            switch (key)
            {
                case Key.Left: Left -= step; break;
                case Key.Right: Left += step; break;
                case Key.Up: Top -= step; break;
                case Key.Down: Top += step; break;
                default: return false;
            }
            KeepInWorkArea();
            return true;
        }

        private void Refresh()
        {
            if (compactMode) compactStrip.Refresh();
            else readout.Refresh();
        }

        private WidgetEdge ResizeEdgeAt(Point point)
        {
            return compactMode || editButton.IsMouseOver || closeButton.IsMouseOver || compactButton.IsMouseOver
                ? WidgetEdge.None : WidgetSize.EdgeAt(point, RenderSize);
        }

        private void ResizeAt(Point screenPoint)
        {
            var delta = resizeFromDevice.Transform(screenPoint - resizeStart);
            double scale = WidgetSize.ResizeScale(resizeBounds.Width / designSize.Width, delta, resizeEdge, designSize);
            ResizeTo(scale, resizeBounds, resizeEdge);
        }

        private void ResizeTo(double scale, Rect start, WidgetEdge edge)
        {
            var target = new Size(designSize.Width * scale, designSize.Height * scale);
            if (Width == target.Width && Height == target.Height) return;
            var origin = WidgetSize.ResizeOrigin(start, target, edge);
            Width = target.Width;
            Height = target.Height;
            Left = origin.X;
            Top = origin.Y;
        }

        private void EndResize()
        {
            if (resizeEdge == WidgetEdge.None) return;
            resizeEdge = WidgetEdge.None;
            if (IsMouseCaptured) ReleaseMouseCapture();
            Cursor = IsMouseOver ? WidgetSize.ResizeCursor(ResizeEdgeAt(Mouse.GetPosition(this))) ?? Cursors.SizeAll : null;
        }

        private void UpdateHeaderControls()
        {
            double opacity = IsMouseOver || editButton.IsKeyboardFocused || closeButton.IsKeyboardFocused ||
                compactButton.IsKeyboardFocused ? 1 : 0;
            editButton.Opacity = closeButton.Opacity = compactButton.Opacity = opacity;
            compactStrip.UpdateControls(IsMouseOver);
        }

        private void HandleKey(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                EndResize();
                Hide();
                e.Handled = true;
                return;
            }

            if ((Keyboard.Modifiers & ModifierKeys.Control) == 0 || resizeEdge != WidgetEdge.None) return;
            if (e.Key == Key.M)
            {
                SetCompactMode(!compactMode);
                e.Handled = true;
                return;
            }
            if (MoveByKey(e.Key))
            {
                e.Handled = true;
                return;
            }

            if (compactMode) return;
            double adjustment;
            switch (e.Key)
            {
                case Key.OemPlus:
                case Key.Add: adjustment = .1; break;
                case Key.OemMinus:
                case Key.Subtract: adjustment = -.1; break;
                default: return;
            }
            double scale = WidgetSize.Clamp(Width / designSize.Width + adjustment);
            ResizeTo(scale, Bounds, WidgetEdge.Left | WidgetEdge.Top);
            e.Handled = true;
        }
    }

    internal sealed partial class Widget
    {
        private CountdownTimer countdownTimer;
        private TimerWidgetWindow timerWindow;
        private ResourceDictionary timerPalette;

        private void ApplyTimerPalette()
        {
            if (timerPalette == null) return;
            var palette = currentPalette;
            timerPalette["TimerFaceBackground"] = HolidayGlassPaint(palette, true, 1);
            timerPalette["PanelField"] = ColorBrush(palette.To);
            timerPalette["PanelSelected"] = ColorBrush(palette.Middle);
            timerPalette["PanelInk"] = ColorBrush(palette.Ink);
            timerPalette["PanelGold"] = ColorBrush(palette.Accent);
            timerPalette["PanelOnGold"] = timerPalette["PanelField"];
            timerPalette["PanelLine"] = new SolidColorBrush(Tint(palette.Accent, .7));
        }

        private void EnsureCountdownTimer()
        {
            if (countdownTimer != null) return;
            countdownTimer = new CountdownTimer(session: config.TimerSession);
            countdownTimer.SessionChanged += ScheduleSave;
            countdownTimer.Finished += () => SystemSounds.Exclamation.Play();
            timerPalette = new ResourceDictionary();
            ApplyTimerPalette();
        }

        private FrameworkElement BuildTimerPage()
        {
            EnsureCountdownTimer();
            return PanelPage.Scroll(new TimerSettingsPage(countdownTimer, settingsWindow, timerPalette, ShowTimerWidget));
        }

        private void OpenTimerSettings()
        {
            if (!IsVisible) ShowWidget();
            OpenSettings(false, true);
        }

        private TimerWidgetWindow EnsureTimerWindow()
        {
            if (timerWindow != null) return timerWindow;
            if (settingsTheme == null) settingsTheme = PanelPage.LoadTheme();
            var window = new TimerWidgetWindow(countdownTimer, settingsTheme, timerPalette, OpenTimerSettings);
            window.RestorePlacement(config.TimerSession);
            window.LocationChanged += (s, e) => ScheduleTimerPlacementSave();
            window.SizeChanged += (s, e) => ScheduleTimerPlacementSave();
            window.IsVisibleChanged += (s, e) => ScheduleTimerPlacementSave();
            window.Closed += (s, e) =>
            {
                if (!quitting)
                {
                    CollectTimerSession();
                    config.TimerSession.Visible = false;
                    ScheduleSave();
                }
                timerWindow = null;
            };
            timerWindow = window;
            return window;
        }

        private void ScheduleTimerPlacementSave()
        {
            if (!quitting) ScheduleSave();
        }

        private void ShowTimerWidget(bool compactMode)
        {
            var window = EnsureTimerWindow();
            window.ShowActivated = true;
            if (!window.IsVisible) window.Show();
            window.SetCompactMode(compactMode);
            window.KeepInWorkArea();
            window.Activate();
        }

        private void RestoreTimerSession()
        {
            if (quitting || config.TimerSession == null) return;
            EnsureCountdownTimer();
            if (!config.TimerSession.Visible) return;
            var window = EnsureTimerWindow();
            window.ShowActivated = false;
            if (!window.IsVisible) window.Show();
            window.KeepInWorkArea();
        }

        private void CollectTimerSession()
        {
            if (countdownTimer == null) return;
            var session = config.TimerSession == null ? new TimerSession() : config.TimerSession.Copy();
            countdownTimer.SaveTo(session);
            if (timerWindow != null) timerWindow.SavePlacement(session);
            config.TimerSession = session;
        }
    }
}
