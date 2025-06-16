using System;
using System.Collections.Generic;
using System.Linq;

namespace MorWalPiz.VideoImporter.Models
{
    public static class WeekdayHelper
    {
        /// <summary>
        /// Weekday flags for bitmask operations
        /// </summary>
        [Flags]
        public enum WeekdayFlags
        {
            None = 0,
            Monday = 1,
            Tuesday = 2,
            Wednesday = 4,
            Thursday = 8,
            Friday = 16,
            Saturday = 32,
            Sunday = 64
        }

        private static readonly Dictionary<DayOfWeek, WeekdayFlags> DayOfWeekToFlag = new()
        {
            { DayOfWeek.Monday, WeekdayFlags.Monday },
            { DayOfWeek.Tuesday, WeekdayFlags.Tuesday },
            { DayOfWeek.Wednesday, WeekdayFlags.Wednesday },
            { DayOfWeek.Thursday, WeekdayFlags.Thursday },
            { DayOfWeek.Friday, WeekdayFlags.Friday },
            { DayOfWeek.Saturday, WeekdayFlags.Saturday },
            { DayOfWeek.Sunday, WeekdayFlags.Sunday }
        };

        private static readonly Dictionary<WeekdayFlags, string> FlagToDisplayName = new()
        {
            { WeekdayFlags.Monday, "Lunedì" },
            { WeekdayFlags.Tuesday, "Martedì" },
            { WeekdayFlags.Wednesday, "Mercoledì" },
            { WeekdayFlags.Thursday, "Giovedì" },
            { WeekdayFlags.Friday, "Venerdì" },
            { WeekdayFlags.Saturday, "Sabato" },
            { WeekdayFlags.Sunday, "Domenica" }
        };

        /// <summary>
        /// Converts a DayOfWeek to the corresponding WeekdayFlags
        /// </summary>
        public static WeekdayFlags GetWeekdayFlag(DayOfWeek dayOfWeek)
        {
            return DayOfWeekToFlag[dayOfWeek];
        }

        /// <summary>
        /// Checks if a specific day is included in the bitmask
        /// </summary>
        public static bool HasDay(int bitmask, DayOfWeek dayOfWeek)
        {
            var flag = GetWeekdayFlag(dayOfWeek);
            return ((WeekdayFlags)bitmask & flag) == flag;
        }

        /// <summary>
        /// Gets all days of the week that are set in the bitmask
        /// </summary>
        public static List<DayOfWeek> GetDaysFromBitmask(int bitmask)
        {
            var days = new List<DayOfWeek>();
            var flags = (WeekdayFlags)bitmask;

            foreach (var kvp in DayOfWeekToFlag)
            {
                if ((flags & kvp.Value) == kvp.Value)
                {
                    days.Add(kvp.Key);
                }
            }

            return days.OrderBy(d => (int)d == 0 ? 7 : (int)d).ToList(); // Sort Monday-Sunday
        }

        /// <summary>
        /// Creates a display string for the days (e.g., "Lun-Ven", "Sab-Dom")
        /// </summary>
        public static string GetDisplayString(int bitmask)
        {
            var days = GetDaysFromBitmask(bitmask);
            if (!days.Any()) return "Nessun giorno";

            if (days.Count == 7) return "Tutti i giorni";

            // Check for common patterns
            var weekdays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
            var weekend = new[] { DayOfWeek.Saturday, DayOfWeek.Sunday };

            if (days.SequenceEqual(weekdays))
                return "Lun-Ven";

            if (days.SequenceEqual(weekend))
                return "Sab-Dom";

            // Check for consecutive days
            var consecutiveRanges = GetConsecutiveRanges(days);
            if (consecutiveRanges.Count == 1 && consecutiveRanges[0].Count > 2)
            {
                var first = consecutiveRanges[0].First();
                var last = consecutiveRanges[0].Last();
                return $"{GetShortDisplayName(first)}-{GetShortDisplayName(last)}";
            }

            // For non-consecutive or multiple ranges, list individual days
            return string.Join(", ", days.Select(GetShortDisplayName));
        }

        private static string GetShortDisplayName(DayOfWeek day)
        {
            return day switch
            {
                DayOfWeek.Monday => "Lun",
                DayOfWeek.Tuesday => "Mar",
                DayOfWeek.Wednesday => "Mer",
                DayOfWeek.Thursday => "Gio",
                DayOfWeek.Friday => "Ven",
                DayOfWeek.Saturday => "Sab",
                DayOfWeek.Sunday => "Dom",
                _ => day.ToString()
            };
        }

        private static List<List<DayOfWeek>> GetConsecutiveRanges(List<DayOfWeek> days)
        {
            var ranges = new List<List<DayOfWeek>>();
            if (!days.Any()) return ranges;

            var sortedDays = days.OrderBy(d => (int)d == 0 ? 7 : (int)d).ToList();
            var currentRange = new List<DayOfWeek> { sortedDays[0] };

            for (int i = 1; i < sortedDays.Count; i++)
            {
                var prevDay = (int)sortedDays[i - 1] == 0 ? 7 : (int)sortedDays[i - 1];
                var currentDay = (int)sortedDays[i] == 0 ? 7 : (int)sortedDays[i];

                if (currentDay == prevDay + 1)
                {
                    currentRange.Add(sortedDays[i]);
                }
                else
                {
                    ranges.Add(currentRange);
                    currentRange = new List<DayOfWeek> { sortedDays[i] };
                }
            }

            ranges.Add(currentRange);
            return ranges;
        }

        /// <summary>
        /// Gets the full display name for a weekday flag
        /// </summary>
        public static string GetFullDisplayName(WeekdayFlags flag)
        {
            return FlagToDisplayName.TryGetValue(flag, out var name) ? name : flag.ToString();
        }

        /// <summary>
        /// Creates a bitmask from a list of days
        /// </summary>
        public static int CreateBitmask(IEnumerable<DayOfWeek> days)
        {
            var bitmask = WeekdayFlags.None;
            foreach (var day in days)
            {
                bitmask |= GetWeekdayFlag(day);
            }
            return (int)bitmask;
        }
    }
}
