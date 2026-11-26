using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace NghiLe
{
    internal enum HolidayTheme
    {
        LunarNewYear, NewYear, HungKings, Reunification, LabourDay, NationalDay,
        MidAutumn, Christmas, VietnamCultureDay, Birthday, PersonalDay
    }

    internal struct HolidayOccurrence
    {
        internal readonly HolidayTheme Theme;
        internal readonly string Id;
        internal readonly string Name;
        internal readonly DateTimeOffset Date;
        internal readonly DateTimeOffset LastDay;

        internal string Key
        {
            get { return Id + "|" + Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture); }
        }

        internal HolidayOccurrence(HolidayTheme theme, string id, string name, DateTimeOffset date, DateTimeOffset lastDay)
        {
            Theme = theme;
            Id = id;
            Name = name;
            Date = date;
            LastDay = lastDay;
        }
    }

    internal static class HolidayCountdown
    {
        internal static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);
        private static readonly Lazy<DateTimeOffset[]> HungKingsDays = new Lazy<DateTimeOffset[]>(() => Calendar.Load("NghiLe.HungKings"));
        private static readonly Lazy<DateTimeOffset[]> MidAutumnDays = new Lazy<DateTimeOffset[]>(() => Calendar.Load("NghiLe.MidAutumn"));

        internal static HolidayOccurrence? Find(Settings settings, DateTimeOffset now)
        {
            var local = now.ToOffset(VietnamOffset);
            return FindForDay(settings, local);
        }

        internal static HolidayOccurrence? FindForDay(Settings settings, DateTimeOffset local)
        {
            HolidayOccurrence? next = null;
            HolidayOccurrence? fixedTet = null;
            HolidayOccurrence? personalToday = null;
            HolidayOccurrence? pinned = null;
            int firstPin = settings.PinnedOccasionIds.Length;
            foreach (var occurrence in Tracked(settings, local))
            {
                if (occurrence.LastDay.Date < local.Date)
                {
                    fixedTet = occurrence;
                    continue;
                }
                if (settings.DisplayedOccurrenceKey != null && occurrence.Key == settings.DisplayedOccurrenceKey)
                    return occurrence;
                // In automatic mode give the first personal date today its own greeting.
                if (!personalToday.HasValue && occurrence.Date.Date == local.Date && PersonalDate.IsTheme(occurrence.Theme))
                    personalToday = occurrence;
                int pin = Array.IndexOf(settings.PinnedOccasionIds, occurrence.Id);
                if (pin >= 0 && pin < firstPin)
                {
                    pinned = occurrence;
                    firstPin = pin;
                }
                if (!next.HasValue || occurrence.Date < next.Value.Date) next = occurrence;
            }
            return personalToday ?? pinned ?? next ?? fixedTet;
        }

        internal static IEnumerable<HolidayOccurrence> UpcomingTracked(Settings settings, DateTimeOffset local)
        {
            return Tracked(settings, local)
                .Where(occurrence => occurrence.LastDay.Date >= local.Date)
                .OrderBy(occurrence => occurrence.Date);
        }

        private static IEnumerable<HolidayOccurrence> Tracked(Settings settings, DateTimeOffset local)
        {
            foreach (var holiday in HolidaySetting.All)
            {
                if (!settings.SelectedHolidays.Contains(holiday.Id)) continue;
                var occurrence = Upcoming(holiday, settings, local);
                if (occurrence.HasValue) yield return occurrence.Value;
            }
            foreach (var reminder in settings.PersonalDates)
            {
                if (!reminder.Enabled) continue;
                var date = NextPersonalDate(reminder, local);
                if (!date.HasValue) continue;
                var theme = reminder.Birthday ? HolidayTheme.Birthday : HolidayTheme.PersonalDay;
                yield return new HolidayOccurrence(theme, reminder.Id.ToString("N"), reminder.Name, date.Value, date.Value);
            }
        }

        private static DateTimeOffset? NextPersonalDate(PersonalDate reminder, DateTimeOffset local)
        {
            for (int year = local.Year; year <= 9999; year++)
            {
                if (reminder.Day > DateTime.DaysInMonth(year, reminder.Month)) continue;
                var date = Midnight(year, reminder.Month, reminder.Day);
                if (date.Date >= local.Date) return date;
            }
            return null;
        }

        private static HolidayOccurrence? Upcoming(HolidaySetting holiday, Settings settings, DateTimeOffset local)
        {
            if (!holiday.IsLunar)
            {
                int year = Math.Max(local.Year, holiday.FirstYear);
                var occurrence = Period(holiday, Midnight(year, holiday.Month, holiday.Day));
                if (occurrence.LastDay.Date >= local.Date) return occurrence;
                return year == 9999 ? (HolidayOccurrence?)null :
                    Period(holiday, Midnight(year + 1, holiday.Month, holiday.Day));
            }

            bool fixedYear = holiday.Theme == HolidayTheme.LunarNewYear && !settings.Automatic;
            var dates = holiday.Theme == HolidayTheme.LunarNewYear ? Calendar.Holidays :
                holiday.Theme == HolidayTheme.HungKings ? HungKingsDays.Value : MidAutumnDays.Value;
            foreach (var day in dates)
            {
                var occurrence = Period(holiday, day);
                if (fixedYear ? day.Year == settings.Year : occurrence.LastDay.Date >= local.Date)
                    return occurrence;
            }
            return null;
        }

        private static HolidayOccurrence Period(HolidaySetting holiday, DateTimeOffset day)
        {
            // The countdown stays on the principal date. The published 2027 schedule
            // only extends how long these occasions remain active.
            var lastDay = day;
            if (day.Year == 2027 && holiday.Theme == HolidayTheme.LunarNewYear)
                lastDay = Midnight(2027, 2, 10);
            if (day.Year == 2027 && holiday.Theme == HolidayTheme.NationalDay)
                lastDay = Midnight(2027, 9, 5);
            return new HolidayOccurrence(holiday.Theme, holiday.Id, holiday.Name, day, lastDay);
        }

        private static DateTimeOffset Midnight(int year, int month, int day)
        {
            return new DateTimeOffset(year, month, day, 0, 0, 0, VietnamOffset);
        }
    }
}
