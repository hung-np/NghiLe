using System;
using System.Linq;

namespace NhipNgay
{
    internal enum HolidayTheme { LunarNewYear, NewYear, HungKings, Reunification, LabourDay, NationalDay, MidAutumn, Christmas }

    internal struct HolidayOccurrence
    {
        internal readonly HolidayTheme Theme;
        internal readonly DateTimeOffset Date;
        internal readonly DateTimeOffset LastDay;

        internal HolidayOccurrence(HolidayTheme theme, DateTimeOffset date, DateTimeOffset lastDay)
        {
            Theme = theme;
            Date = date;
            LastDay = lastDay;
        }
    }

    internal static class HolidayCountdown
    {
        internal static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);
        private static readonly Lazy<DateTimeOffset[]> HungKingsDays = new Lazy<DateTimeOffset[]>(() => Calendar.Load("NhipNgay.HungKings"));
        private static readonly Lazy<DateTimeOffset[]> MidAutumnDays = new Lazy<DateTimeOffset[]>(() => Calendar.Load("NhipNgay.MidAutumn"));

        internal static HolidayOccurrence? Find(Settings settings, DateTimeOffset now)
        {
            var local = now.ToOffset(VietnamOffset);
            return FindForDay(settings, local);
        }

        internal static HolidayOccurrence? FindForDay(Settings settings, DateTimeOffset local)
        {
            HolidayOccurrence? next = null;
            HolidayOccurrence? fixedTet = null;
            foreach (var holiday in HolidaySetting.All)
            {
                if (!settings.SelectedHolidays.Contains(holiday.Id)) continue;
                var occurrence = Upcoming(holiday, settings, local);
                if (!occurrence.HasValue) continue;
                if (occurrence.Value.LastDay.Date < local.Date)
                {
                    fixedTet = occurrence;
                    continue;
                }
                if (!next.HasValue || occurrence.Value.Date < next.Value.Date) next = occurrence;
            }
            return next ?? fixedTet;
        }

        private static HolidayOccurrence? Upcoming(HolidaySetting holiday, Settings settings, DateTimeOffset local)
        {
            if (!holiday.IsLunar)
            {
                var occurrence = Period(holiday.Theme, Midnight(local.Year, holiday.Month, holiday.Day));
                if (occurrence.LastDay.Date >= local.Date) return occurrence;
                return local.Year == 9999 ? (HolidayOccurrence?)null :
                    Period(holiday.Theme, Midnight(local.Year + 1, holiday.Month, holiday.Day));
            }

            bool fixedYear = holiday.Theme == HolidayTheme.LunarNewYear && !settings.Automatic;
            var dates = holiday.Theme == HolidayTheme.LunarNewYear ? Calendar.Holidays :
                holiday.Theme == HolidayTheme.HungKings ? HungKingsDays.Value : MidAutumnDays.Value;
            foreach (var day in dates)
            {
                var occurrence = Period(holiday.Theme, day);
                if (fixedYear ? day.Year == settings.Year : occurrence.LastDay.Date >= local.Date)
                    return occurrence;
            }
            return null;
        }

        private static HolidayOccurrence Period(HolidayTheme theme, DateTimeOffset day)
        {
            // The countdown stays on the principal date. The published 2027 schedule
            // only extends how long these occasions remain active.
            if (day.Year == 2027 && theme == HolidayTheme.LunarNewYear)
                return new HolidayOccurrence(theme, day, Midnight(2027, 2, 10));
            if (day.Year == 2027 && theme == HolidayTheme.NationalDay)
                return new HolidayOccurrence(theme, day, Midnight(2027, 9, 5));
            return new HolidayOccurrence(theme, day, day);
        }

        private static DateTimeOffset Midnight(int year, int month, int day)
        {
            return new DateTimeOffset(year, month, day, 0, 0, 0, VietnamOffset);
        }
    }
}
