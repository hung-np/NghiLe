using System;

namespace NghiLe
{
    public enum CountdownUnit { Days, Weeks, Months }

    internal struct CountdownQuantity
    {
        internal readonly long WholeUnits;
        internal readonly long ExtraDays;

        internal CountdownQuantity(long wholeUnits, long extraDays = 0)
        {
            WholeUnits = wholeUnits;
            ExtraDays = extraDays;
        }
    }

    internal static class CountdownUnits
    {
        private const int SecondsPerDay = 86400;

        internal static string Label(CountdownUnit unit)
        {
            switch (unit)
            {
                case CountdownUnit.Weeks: return "Theo tuần";
                case CountdownUnit.Months: return "Theo tháng";
                default: return "Theo ngày";
            }
        }

        internal static string Symbol(CountdownUnit unit)
        {
            switch (unit)
            {
                case CountdownUnit.Weeks: return "TUẦN";
                case CountdownUnit.Months: return "THÁNG";
                default: return "NGÀY";
            }
        }

        internal static CountdownQuantity Measure(CountdownUnit unit, DateTimeOffset target, long seconds)
        {
            if (seconds <= 0) return new CountdownQuantity(0);
            long days = seconds / SecondsPerDay;
            if (unit == CountdownUnit.Weeks) return new CountdownQuantity(days / 7, days % 7);
            if (unit == CountdownUnit.Days) return new CountdownQuantity(days);

            // Use the same rounded time as the displayed days, hours, minutes and seconds.
            var localNow = target.AddSeconds(-seconds);
            int months = (target.Year - localNow.Year) * 12 + target.Month - localNow.Month;
            var monthEnd = localNow.AddMonths(months);
            if (monthEnd > target) monthEnd = localNow.AddMonths(--months);
            return new CountdownQuantity(months, (target - monthEnd).Days);
        }
    }
}
