using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Threading;

namespace NhipNgay
{
    internal static class MusicChecks
    {
        internal static int Run()
        {
            var folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NhipNgay-music-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            var first = Path.Combine(folder, "01.wav");
            var second = Path.Combine(folder, "02.WAV");
            var added = Path.Combine(folder, "03.wav");
            var ignored = Path.Combine(folder, "not-a-track.txt");
            try
            {
                WriteWave(first);
                WriteWave(second);
                File.WriteAllText(ignored, "ignore");
                using (var playlist = new Playlist(folder, 0))
                {
                    playlist.Rescan();
                    if (!playlist.Tracks.SequenceEqual(new[] { first, second })) return 40;
                    var skipped = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (Playlist.Following(playlist.Tracks, second, skipped) != first) return 41;
                    skipped.Add(first);
                    if (Playlist.Following(playlist.Tracks, second, skipped) != second) return 42;

                    var played = new List<string>();
                    var frame = new DispatcherFrame();
                    playlist.Changed += () =>
                    {
                        if (playlist.Error != null) { Console.Error.WriteLine(playlist.PlaybackDiagnostic ?? playlist.Error); frame.Continue = false; return; }
                        if (!playlist.Opened) return;
                        var path = playlist.Current;
                        if (played.Count > 0 && played[played.Count - 1] == path) return;
                        played.Add(path);
                        if (played.Count == 1) WriteWave(added);
                        if (played.Count == 4) frame.Continue = false;
                    };
                    if (!SelectAndPlay(playlist, second)) return 46;
                    WaitFor(frame);
                    Console.Error.WriteLine("Opened: " + string.Join(", ", played.Select(Path.GetFileName)));
                    return played.SequenceEqual(new[] { second, added, first, second }) ? 0 : 43;
                }
            }
            finally
            {
                foreach (var path in new[] { first, second, added, ignored }) File.Delete(path);
                Directory.Delete(folder);
            }
        }

        internal static int VerifyFolder(string folder)
        {
            using (var playlist = new Playlist(folder, 0))
            {
                playlist.Rescan();
                var tracks = playlist.Tracks;
                if (tracks.Length == 0) return 44;
                var frame = new DispatcherFrame();
                var opened = 0;
                playlist.Changed += () =>
                {
                    if (playlist.Error != null) { frame.Continue = false; return; }
                    if (!playlist.Opened || opened == tracks.Length) return;
                    if (playlist.Current != tracks[opened]) { frame.Continue = false; return; }
                    opened++;
                    if (opened == tracks.Length) frame.Continue = false;
                    else Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Normal,
                        new Action(() => { if (!SelectAndPlay(playlist, tracks[opened])) frame.Continue = false; }));
                };
                if (!SelectAndPlay(playlist, tracks[0])) return 46;
                WaitFor(frame);
                Console.Error.WriteLine("Verified audio files: " + opened + "/" + tracks.Length);
                return opened == tracks.Length ? 0 : 45;
            }
        }

        private static bool SelectAndPlay(Playlist playlist, string path)
        {
            playlist.Select(path);
            playlist.Rescan();
            if (playlist.Selected != path || playlist.Current != null || playlist.Playing || playlist.Opened) return false;
            playlist.Toggle();
            return playlist.Current == path && playlist.Playing;
        }

        private static void WaitFor(DispatcherFrame frame)
        {
            var timeout = new DispatcherTimer { Interval = TimeSpan.FromSeconds(12) };
            timeout.Tick += (s, e) => frame.Continue = false;
            timeout.Start();
            Dispatcher.PushFrame(frame);
            timeout.Stop();
        }

        private static void WriteWave(string path)
        {
            const int rate = 16000;
            const int bytes = rate; // 500 ms, 16-bit mono.
            using (var file = new BinaryWriter(File.Create(path)))
            {
                file.Write(Encoding.ASCII.GetBytes("RIFF"));
                file.Write(36 + bytes);
                file.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
                file.Write(16);
                file.Write((short)1);
                file.Write((short)1);
                file.Write(rate);
                file.Write(rate * 2);
                file.Write((short)2);
                file.Write((short)16);
                file.Write(Encoding.ASCII.GetBytes("data"));
                file.Write(bytes);
                file.Write(new byte[bytes]);
            }
        }
    }
}
