using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security;
using System.Text;

namespace NhipNgay
{
    internal sealed class StartupShortcut
    {
        [ComImport, Guid("00021401-0000-0000-C000-000000000046")]
        private class ShellLink { }

        [ComImport, Guid("000214F9-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellLink
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder path, int length, IntPtr findData, uint flags);
            void GetIDList(out IntPtr pidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder text, int length);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string text);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder directory, int length);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string directory);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder arguments, int length);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string arguments);
            void GetHotkey(out short key);
            void SetHotkey(short key);
            void GetShowCmd(out int command);
            void SetShowCmd(int command);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder path, int length, out int index);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string path, int index);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string path, uint reserved);
            void Resolve(IntPtr hwnd, uint flags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string path);
        }

        // Keep the original filename so the existing Startup entry stays in place.
        private const string ShortcutName = "Hẹn Tết.lnk";
        private const string LegacyDescription = "Hẹn Tết · Đếm ngược Tết";
        private const string LegacyExecutableName = "HenTet.exe";
        private const string ExecutableName = "NhipNgay.exe";
        private const string Description = Widget.ProductName + " · Đếm ngược ngày lễ";
        private readonly string path;
        private readonly string executable;

        internal StartupShortcut(string folder, string executable)
        {
            path = string.IsNullOrEmpty(folder) ? null : Path.Combine(folder, ShortcutName);
            this.executable = executable;
        }

        internal static StartupShortcut ForCurrentUser()
        {
            return new StartupShortcut(Environment.GetFolderPath(Environment.SpecialFolder.Startup, Environment.SpecialFolderOption.DoNotVerify),
                Assembly.GetExecutingAssembly().Location);
        }

        internal bool? ReadEnabled()
        {
            if (path == null) return null;
            if (!File.Exists(path)) return false;
            return UseShortcut<bool?>((link, exists) => exists && TargetsExecutable(link, executable), null);
        }

        internal void UpgradeLegacyTarget()
        {
            if (path == null || !File.Exists(path) ||
                !string.Equals(Path.GetFileName(executable), ExecutableName, StringComparison.OrdinalIgnoreCase)) return;

            var oldExecutable = Path.Combine(Path.GetDirectoryName(executable), LegacyExecutableName);
            UseShortcut<bool>((link, exists) =>
            {
                if (!exists || ReadDescription(link) != LegacyDescription ||
                    !TargetsExecutable(link, oldExecutable)) return false;

                SaveShortcut(link);
                return true;
            }, false);
        }

        internal bool SetEnabled(bool enabled)
        {
            if (path == null) return false;
            if (!enabled && !File.Exists(path)) return true;
            return UseShortcut<bool>((link, exists) =>
            {
                bool matches = exists && TargetsExecutable(link, executable);
                if (!enabled)
                {
                    if (exists && !matches) return false;
                    File.Delete(path);
                    return true;
                }

                // Adopt our shortcut after the app moves, or a manual shortcut to this executable.
                if (exists && !matches)
                {
                    var description = ReadDescription(link);
                    if (description != Description && description != LegacyDescription) return false;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                SaveShortcut(link);
                return true;
            }, false);
        }

        private void SaveShortcut(IShellLink link)
        {
            link.SetPath(executable);
            link.SetWorkingDirectory(Path.GetDirectoryName(executable));
            link.SetArguments("");
            link.SetDescription(Description);
            ((IPersistFile)link).Save(path, true);
        }

        private static string ReadDescription(IShellLink link)
        {
            var description = new StringBuilder(1024);
            link.GetDescription(description, description.Capacity);
            return description.ToString();
        }

        private static bool TargetsExecutable(IShellLink link, string executable)
        {
            var target = new StringBuilder(32768);
            link.GetPath(target, target.Capacity, IntPtr.Zero, 0);
            return string.Equals(target.ToString(), executable, StringComparison.OrdinalIgnoreCase);
        }

        private T UseShortcut<T>(Func<IShellLink, bool, T> action, T failure)
        {
            IShellLink shortcut = null;
            try
            {
                shortcut = (IShellLink)new ShellLink();
                bool exists = File.Exists(path);
                if (exists) ((IPersistFile)shortcut).Load(path, 0);
                return action(shortcut, exists);
            }
            catch (Exception error)
            {
                if (error is COMException || error is IOException || error is UnauthorizedAccessException || error is SecurityException)
                    return failure;
                throw;
            }
            finally
            {
                if (shortcut != null) Marshal.FinalReleaseComObject(shortcut);
            }
        }

        internal static int Check()
        {
            var folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NhipNgay-startup-test-" + Guid.NewGuid().ToString("N"));
            var shortcut = new StartupShortcut(folder, Assembly.GetExecutingAssembly().Location);
            var movedFolder = Path.Combine(folder, "Moved App");
            var movedExecutable = Path.Combine(movedFolder, ExecutableName);
            var moved = new StartupShortcut(folder, movedExecutable);
            var legacyExecutable = Path.Combine(movedFolder, LegacyExecutableName);
            var legacy = new StartupShortcut(folder, legacyExecutable);
            try
            {
                if (shortcut.ReadEnabled() != false || Directory.Exists(folder)) return 60;
                if (!shortcut.SetEnabled(true) || shortcut.ReadEnabled() != true) return 61;
                if (!shortcut.SetEnabled(true) || moved.ReadEnabled() != false) return 62;
                Directory.CreateDirectory(movedFolder);
                File.Copy(Assembly.GetExecutingAssembly().Location, movedExecutable);
                if (!moved.SetEnabled(true) || moved.ReadEnabled() != true || shortcut.ReadEnabled() != false) return 63;
                if (!moved.SetEnabled(false) || File.Exists(Path.Combine(folder, ShortcutName)) || moved.ReadEnabled() != false) return 64;
                if (!moved.SetEnabled(true) || !moved.UseShortcut<bool>((link, exists) =>
                {
                    if (!exists) return false;
                    link.SetDescription("Tệp khác");
                    ((IPersistFile)link).Save(moved.path, true);
                    return true;
                }, false)) return 65;
                if (shortcut.SetEnabled(true) || shortcut.SetEnabled(false) || moved.ReadEnabled() != true) return 66;

                File.Copy(movedExecutable, legacyExecutable);
                if (!legacy.UseShortcut<bool>((link, exists) =>
                {
                    if (!exists) return false;
                    link.SetPath(legacyExecutable);
                    ((IPersistFile)link).Save(legacy.path, true);
                    return true;
                }, false)) return 67;
                moved.UpgradeLegacyTarget();
                if (moved.ReadEnabled() != false || legacy.ReadEnabled() != true) return 68;

                if (!legacy.UseShortcut<bool>((link, exists) =>
                {
                    if (!exists) return false;
                    link.SetDescription(LegacyDescription);
                    ((IPersistFile)link).Save(legacy.path, true);
                    return true;
                }, false)) return 69;
                File.Delete(legacyExecutable);
                moved.UpgradeLegacyTarget();
                if (moved.ReadEnabled() != true || legacy.ReadEnabled() != false ||
                    !moved.UseShortcut<bool>((link, exists) => exists && ReadDescription(link) == Description, false)) return 70;
                if (!moved.SetEnabled(false) || moved.ReadEnabled() != false) return 71;
                return 0;
            }
            finally
            {
                if (Directory.Exists(folder))
                {
                    File.Delete(Path.Combine(folder, ShortcutName));
                    if (Directory.Exists(movedFolder))
                    {
                        File.Delete(movedExecutable);
                        File.Delete(legacyExecutable);
                        Directory.Delete(movedFolder);
                    }
                    Directory.Delete(folder);
                }
            }
        }
    }
}
