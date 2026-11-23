using System;
using System.Linq;

namespace NghiLe
{
    public sealed class PersonalDate
    {
        internal const string BirthdayName = "Sinh nhật";
        internal const string SpecialDayName = "Ngày đặc biệt";
        internal const int MaxNameLength = 80;

        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Day { get; set; }
        public int Month { get; set; }
        public bool Birthday { get; set; }
        public bool Enabled { get; set; }

        internal static bool IsTheme(HolidayTheme? theme)
        {
            return theme == HolidayTheme.Birthday || theme == HolidayTheme.PersonalDay;
        }

        internal static bool IsValidDay(int month, int day)
        {
            return month >= 1 && month <= 12 && day >= 1 && day <= DateTime.DaysInMonth(2000, month);
        }

        internal static PersonalDate[] Normalize(PersonalDate[] dates)
        {
            if (dates == null) return new PersonalDate[0];
            return dates.Where(date => date != null && date.Id != Guid.Empty &&
                    !string.IsNullOrWhiteSpace(date.Name) && date.Name.Length <= MaxNameLength &&
                    IsValidDay(date.Month, date.Day))
                .GroupBy(date => date.Id)
                .Select(group => group.First())
                .ToArray();
        }

        internal PersonalDate WithEnabled(bool enabled)
        {
            var next = (PersonalDate)MemberwiseClone();
            next.Enabled = enabled;
            return next;
        }
    }
}
