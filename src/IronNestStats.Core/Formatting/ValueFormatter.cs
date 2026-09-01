using System;
using System.Globalization;
using IronNestStats.Core.Stats;

namespace IronNestStats.Core.Formatting
{
    public static class ValueFormatter
    {
        public static string Format(double value, StatFormat format, bool chinese)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return "-";
            switch (format)
            {
                case StatFormat.Count:
                    return Math.Round(value).ToString("0", CultureInfo.InvariantCulture);
                case StatFormat.Decimal:
                    return value.ToString("0.00", CultureInfo.InvariantCulture);
                case StatFormat.Percent:
                    return value.ToString("0.0", CultureInfo.InvariantCulture) + "%";
                case StatFormat.Boolean:
                    return value != 0 ? (chinese ? "是" : "Yes") : (chinese ? "否" : "No");
                case StatFormat.Duration:
                    var total = Math.Max(0, (int)Math.Round(value));
                    return total >= 60
                        ? (total / 60).ToString(CultureInfo.InvariantCulture) + ":" + (total % 60).ToString("00", CultureInfo.InvariantCulture)
                        : total.ToString(CultureInfo.InvariantCulture) + (chinese ? " 秒" : " s");
                default:
                    return value.ToString(CultureInfo.InvariantCulture);
            }
        }
    }
}
