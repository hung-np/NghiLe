using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace NghiLe
{
    internal sealed class Playlist : IDisposable
    {
        private static readonly HashSet<string> Extensions =
            new HashSet<string>(new[] { ".mp3", ".wav", ".m4a" }, StringComparer.OrdinalIgnoreCase);
        private static readonly StringComparer Order = StringComparer.OrdinalIgnoreCase;
        private readonly string folder;
        private readonly HashSet<string> skipped = new HashSet<string>(Order);
        private readonly double volume;
        private MediaPlayer player;

        internal string[] Tracks { get; private set; }
        internal string Selected { get; private set; }
        internal string Current { get; private set; }
        internal string Error { get; private set; }
        internal bool Playing { get; private set; }
        internal bool Opened { get; private set; }
        internal string PlaybackDiagnostic { get; private set; }
        internal event Action Changed;

        internal Playlist(string folder, double volume = .6)
        {
            this.folder = folder;
            this.volume = volume;
            Tracks = new string[0];
        }

        internal static string[] Read(string folder)
        {
            try
            {
                return Directory.EnumerateFiles(folder)
                    .Where(path => Extensions.Contains(Path.GetExtension(path)))
                    .OrderBy(path => path, Order)
                    .ToArray();
            }
            catch (DirectoryNotFoundException) { return new string[0]; }
        }

        internal static string Following(string[] tracks, string current, HashSet<string> skipped)
        {
            return tracks.FirstOrDefault(path => Order.Compare(path, current) > 0 && !skipped.Contains(path))
                ?? tracks.FirstOrDefault(path => !skipped.Contains(path));
        }

        internal void Rescan()
        {
            try
            {
                var tracks = Read(folder);
                if (Tracks.SequenceEqual(tracks, Order)) return;
                Tracks = tracks;
                if (Selected != null && !Tracks.Contains(Selected, Order)) Selected = null;
                Notify();
            }
            catch (IOException) { Report("Không đọc được thư mục nhac."); }
            catch (UnauthorizedAccessException) { Report("Không có quyền đọc thư mục nhac."); }
        }

        internal void Select(string path)
        {
            if (!Tracks.Contains(path, Order) || Order.Equals(path, Selected ?? Current)) return;
            ClosePlayer();
            Selected = path;
            Current = null;
            Playing = false;
            Report(null);
        }

        internal void Toggle()
        {
            if (player == null)
            {
                var selected = Selected;
                Rescan();
                var path = selected ?? Tracks.FirstOrDefault();
                if (path != null && Tracks.Contains(path, Order))
                {
                    skipped.Clear();
                    Start(path);
                }
                return;
            }

            if (Playing) player.Pause(); else player.Play();
            Playing = !Playing;
            Notify();
        }

        private void Start(string path)
        {
            ClosePlayer();
            Selected = null;
            Current = path;
            Error = null;
            Playing = true;
            var next = new MediaPlayer { Volume = volume };
            player = next;
            next.MediaOpened += (s, e) =>
            {
                if (next != player) return;
                Opened = true;
                Notify();
            };
            next.MediaEnded += (s, e) =>
            {
                if (next == player) Advance(false);
            };
            next.MediaFailed += (s, e) =>
            {
                if (next != player) return;
                PlaybackDiagnostic = e.ErrorException == null ? "MediaFailed" : e.ErrorException.ToString();
                Advance(true);
            };
            try
            {
                next.Open(new Uri(path, UriKind.Absolute));
                next.Play();
                Notify();
            }
            catch (IOException error) { PlaybackDiagnostic = error.ToString(); Advance(true); }
            catch (InvalidOperationException error) { PlaybackDiagnostic = error.ToString(); Advance(true); }
            catch (NotSupportedException error) { PlaybackDiagnostic = error.ToString(); Advance(true); }
        }

        private void Advance(bool failed)
        {
            if (!Playing) return;
            var previous = Current;
            var completed = player;
            if (failed) skipped.Add(previous); else skipped.Clear();
            Rescan();
            if (!Playing || completed != player) return;
            var next = Following(Tracks, previous, skipped);
            if (next != null)
            {
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    if (Playing && completed == player) Start(next);
                }));
                return;
            }
            ClosePlayer();
            Current = null;
            Playing = false;
            Report(failed ? "Không phát được tệp nhạc." : null);
        }

        private void Report(string message)
        {
            Error = message;
            Notify();
        }

        private void Notify() { if (Changed != null) Changed(); }

        private void ClosePlayer()
        {
            var previous = player;
            player = null;
            Opened = false;
            if (previous != null) previous.Close();
        }

        public void Dispose() { ClosePlayer(); }
    }

    internal sealed class MusicView : PanelPage, IDisposable
    {
        private sealed class TrackItem
        {
            public string Path { get; private set; }
            public string Title { get; private set; }

            internal TrackItem(string path)
            {
                Path = path;
                Title = System.IO.Path.GetFileNameWithoutExtension(path);
            }
        }

        private readonly Playlist playlist;
        private readonly ListBox songs;
        private readonly Button toggle;
        private readonly TextBlock status;
        private readonly DispatcherTimer refresh;
        private string[] displayed;
        private bool rendering;

        internal MusicView(Playlist playlist) : base("Danh sách nhạc")
        {
            this.playlist = playlist;
            songs = new ListBox { FontSize = 14, Background = (Brush)FindResource("PanelField"),
                BorderBrush = (Brush)FindResource("PanelLine"), BorderThickness = new Thickness(1),
                Foreground = (Brush)FindResource("PanelInk"), Padding = new Thickness(2),
                ItemTemplate = (DataTemplate)FindResource("PanelTrackItem") };
            ScrollViewer.SetHorizontalScrollBarVisibility(songs, ScrollBarVisibility.Disabled);
            ScrollViewer.SetVerticalScrollBarVisibility(songs, ScrollBarVisibility.Auto);
            ScrollViewer.SetCanContentScroll(songs, true);
            VirtualizingPanel.SetVirtualizationMode(songs, VirtualizationMode.Recycling);
            System.Windows.Automation.AutomationProperties.SetName(songs, "Danh sách nhạc");
            songs.SelectionChanged += (s, e) =>
            {
                if (!rendering && songs.SelectedItem is TrackItem)
                    playlist.Select(((TrackItem)songs.SelectedItem).Path);
            };
            toggle = new Button { Content = "Bắt đầu phát", Style = (Style)FindResource("PanelPrimary"),
                FontSize = 14, MinHeight = 34, MinWidth = 130, HorizontalAlignment = HorizontalAlignment.Left };
            toggle.Click += (s, e) => playlist.Toggle();
            status = new TextBlock { Foreground = (Brush)FindResource("PanelMuted"), FontSize = 13,
                TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 0, 0, 5) };
            var footer = new StackPanel();
            footer.Children.Add(status);
            footer.Children.Add(toggle);
            SetContent(songs, footer);

            refresh = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            refresh.Tick += (s, e) => playlist.Rescan();
            IsVisibleChanged += (s, e) =>
            {
                if (IsVisible) { playlist.Rescan(); refresh.Start(); }
                else refresh.Stop();
            };
            playlist.Changed += Render;
            Render();
        }

        private void Render()
        {
            rendering = true;
            try
            {
                if (!ReferenceEquals(displayed, playlist.Tracks))
                {
                    displayed = playlist.Tracks;
                    songs.ItemsSource = displayed.Select(path => new TrackItem(path)).ToArray();
                }
                var highlighted = playlist.Selected ?? playlist.Current;
                var selected = Array.FindIndex(displayed, path => StringComparer.OrdinalIgnoreCase.Equals(path, highlighted));
                if (songs.SelectedIndex != selected)
                {
                    songs.SelectedIndex = selected;
                    if (selected >= 0) songs.ScrollIntoView(songs.SelectedItem);
                }
                toggle.Content = playlist.Playing ? "Tạm dừng" : playlist.Current != null ? "Tiếp tục" : "Bắt đầu phát";
                toggle.IsEnabled = displayed.Length > 0 || playlist.Current != null;
                status.Text = playlist.Error ?? (playlist.Current != null
                    ? (playlist.Playing ? "Đang phát: " : "Tạm dừng: ") + Path.GetFileNameWithoutExtension(playlist.Current)
                    : playlist.Selected != null ? "Đã chọn: " + Path.GetFileNameWithoutExtension(playlist.Selected)
                    : displayed.Length == 0 ? "Thêm nhạc vào thư mục nhac." : displayed.Length + " bài trong thư mục");
                status.ToolTip = status.Text;
            }
            finally { rendering = false; }
        }

        public void Dispose()
        {
            refresh.Stop();
            playlist.Changed -= Render;
            playlist.Dispose();
        }
    }
}
