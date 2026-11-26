using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace NghiLe
{
    internal static class OccasionReminders
    {
        internal const int Off = -1;
        internal static readonly int[] Choices = { Off, 7, 1, 0 };
        internal static readonly TimeSpan ReminderTime = TimeSpan.FromHours(9);

        internal static string Label(int days)
        {
            if (days == Off) return "Không nhắc";
            if (days == 0) return "Đúng ngày";
            return "Trước " + days.ToString(CultureInfo.InvariantCulture) + " ngày";
        }

        internal static string CountdownLabel(HolidayOccurrence occasion, DateTime today)
        {
            int days = (occasion.Date.Date - today).Days;
            if (days < 0) return occasion.LastDay.Date < today ? "Đã qua" : "Đang diễn ra";
            if (days == 0) return "Hôm nay";
            return "Còn " + days.ToString(CultureInfo.InvariantCulture) + " ngày";
        }

        internal static Dictionary<string, HolidayOccurrence> Due(Settings settings, DateTimeOffset now)
        {
            var due = new Dictionary<string, HolidayOccurrence>(StringComparer.Ordinal);
            var local = now.ToOffset(HolidayCountdown.VietnamOffset);
            if (local.TimeOfDay < ReminderTime || settings.ReminderDays.Count == 0) return due;

            var delivered = new HashSet<string>(settings.DeliveredReminders, StringComparer.Ordinal);
            string day = DateKey(local.Date);
            foreach (var occasion in HolidayCountdown.UpcomingTracked(settings, local))
            {
                int days;
                if (!settings.ReminderDays.TryGetValue(occasion.Id, out days) ||
                    days != (occasion.Date.Date - local.Date).Days) continue;
                string key = day + "|" + occasion.Key;
                if (!delivered.Contains(key)) due.Add(key, occasion);
            }
            return due;
        }

        internal static string[] Record(string[] delivered, IEnumerable<string> due, DateTime today)
        {
            string earliest = DateKey(today.AddDays(-14));
            return delivered.Where(key => string.CompareOrdinal(key, earliest) >= 0).Concat(due).ToArray();
        }

        private static string DateKey(DateTime date)
        {
            return date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        }
    }
}
