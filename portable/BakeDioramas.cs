using System;
using System.Windows;

namespace NghiLe
{
    internal static class BakeDioramas
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Widget.ConfigureRendering();
            var app = new Application();
            try { HolidayDiorama.BakeArchives(args[0]); }
            finally { app.Shutdown(); }
        }
    }
}
