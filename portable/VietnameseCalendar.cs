using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace NghiLe
{
    internal struct VietnameseLunarDate
    {
        internal readonly int Day;
        internal readonly int Month;
        internal readonly bool Leap;

        internal VietnameseLunarDate(int day, int month, bool leap)
        {
            Day = day;
            Month = month;
            Leap = leap;
        }

        internal string ShortLabel
        {
            get { return Day + "/" + Month + (Leap ? " N" : ""); }
        }

        internal string Description
        {
            get { return "ngày " + Day + " tháng " + Month + (Leap ? " nhuận" : "") + " âm lịch"; }
        }
    }

    internal static class VietnameseCalendar
    {
        internal static readonly DateTime FirstMonth = new DateTime(2026, 1, 1);
        internal static readonly DateTime LastMonth = new DateTime(2036, 12, 1);

        private static readonly DateTime[] starts;
        private static readonly VietnameseLunarDate[] months;

        static VietnameseCalendar()
        {
            using (var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("NghiLe.LunarMonths")))
            {
                var records = reader.ReadToEnd().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Skip(1).Select(line => line.Split(',')).ToArray();
                starts = records.Select(row => DateTime.ParseExact(row[0], "yyyy-MM-dd", CultureInfo.InvariantCulture)).ToArray();
                months = records.Select(row => new VietnameseLunarDate(1, int.Parse(row[1], CultureInfo.InvariantCulture), row[2] == "1")).ToArray();
            }
        }

        internal static VietnameseLunarDate Convert(DateTime date)
        {
            if (date.Year < FirstMonth.Year || date.Year > LastMonth.Year) throw new ArgumentOutOfRangeException("date");
            int index = Array.BinarySearch(starts, date.Date);
            if (index < 0) index = ~index - 1;
            var month = months[index];
            return new VietnameseLunarDate((date.Date - starts[index]).Days + 1, month.Month, month.Leap);
        }
    }
}
