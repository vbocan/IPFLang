namespace IPFLang.Temporal
{
    /// <summary>
    /// Temporal operators for date and deadline calculations
    /// </summary>
    public static class TemporalOperators
    {
        /// <summary>
        /// Calculate business days between two dates (excluding weekends)
        /// </summary>
        public static int BusinessDaysBetween(DateOnly start, DateOnly end)
        {
            if (start > end)
                return -BusinessDaysBetween(end, start);

            int businessDays = 0;
            var current = start;

            while (current < end)
            {
                if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
                {
                    businessDays++;
                }
                current = current.AddDays(1);
            }

            return businessDays;
        }

        /// <summary>
        /// Calculate calendar days between two dates
        /// </summary>
        public static int CalendarDaysBetween(DateOnly start, DateOnly end)
        {
            return (end.ToDateTime(TimeOnly.MinValue) - start.ToDateTime(TimeOnly.MinValue)).Days;
        }

        /// <summary>
        /// Add business days to a date (excluding weekends)
        /// </summary>
        public static DateOnly AddBusinessDays(DateOnly date, int days)
        {
            if (days == 0) return date;

            var direction = days > 0 ? 1 : -1;
            var remainingDays = Math.Abs(days);
            var current = date;

            while (remainingDays > 0)
            {
                current = current.AddDays(direction);
                
                if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
                {
                    remainingDays--;
                }
            }

            return current;
        }

        /// <summary>
        /// Check if a date is a weekend
        /// </summary>
        public static bool IsWeekend(DateOnly date)
        {
            return date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
        }

        /// <summary>
        /// Check if a date is a weekday
        /// </summary>
        public static bool IsWeekday(DateOnly date)
        {
            return !IsWeekend(date);
        }

        /// <summary>
        /// Get the next business day from a given date
        /// </summary>
        public static DateOnly NextBusinessDay(DateOnly date)
        {
            var next = date.AddDays(1);
            while (IsWeekend(next))
            {
                next = next.AddDays(1);
            }
            return next;
        }

        /// <summary>
        /// Get the previous business day from a given date
        /// </summary>
        public static DateOnly PreviousBusinessDay(DateOnly date)
        {
            var previous = date.AddDays(-1);
            while (IsWeekend(previous))
            {
                previous = previous.AddDays(-1);
            }
            return previous;
        }

        /// <summary>
        /// Calculate months between two dates
        /// </summary>
        public static int MonthsBetween(DateOnly start, DateOnly end)
        {
            int months = ((end.Year - start.Year) * 12) + end.Month - start.Month;
            
            // Adjust if end day is before start day
            if (end.Day < start.Day)
            {
                months--;
            }

            return months;
        }

        /// <summary>
        /// Calculate years between two dates
        /// </summary>
        public static int YearsBetween(DateOnly start, DateOnly end)
        {
            int years = end.Year - start.Year;
            
            // Adjust if end month/day is before start month/day
            if (end.Month < start.Month || (end.Month == start.Month && end.Day < start.Day))
            {
                years--;
            }

            return years;
        }

        /// <summary>
        /// Add months to a date
        /// </summary>
        public static DateOnly AddMonths(DateOnly date, int months)
        {
            return date.AddMonths(months);
        }

        /// <summary>
        /// Add years to a date
        /// </summary>
        public static DateOnly AddYears(DateOnly date, int years)
        {
            return date.AddYears(years);
        }

        /// <summary>
        /// Check if a date falls within a range
        /// </summary>
        public static bool IsWithinRange(DateOnly date, DateOnly start, DateOnly end)
        {
            return date >= start && date <= end;
        }

        /// <summary>
        /// Check if a date is before another date
        /// </summary>
        public static bool IsBefore(DateOnly date1, DateOnly date2)
        {
            return date1 < date2;
        }

        /// <summary>
        /// Check if a date is after another date
        /// </summary>
        public static bool IsAfter(DateOnly date1, DateOnly date2)
        {
            return date1 > date2;
        }

        /// <summary>
        /// Get the earlier of two dates
        /// </summary>
        public static DateOnly Min(DateOnly date1, DateOnly date2)
        {
            return date1 < date2 ? date1 : date2;
        }

        /// <summary>
        /// Get the later of two dates
        /// </summary>
        public static DateOnly Max(DateOnly date1, DateOnly date2)
        {
            return date1 > date2 ? date1 : date2;
        }
    }

    /// <summary>
    /// Temporal expressions for deadline calculations
    /// </summary>
    public abstract record TemporalExpression
    {
        public abstract DateOnly Evaluate(DateOnly referenceDate);
    }

    /// <summary>
    /// Literal date expression
    /// </summary>
    public record LiteralDate(DateOnly Date) : TemporalExpression
    {
        public override DateOnly Evaluate(DateOnly referenceDate) => Date;
    }

    /// <summary>
    /// Today reference
    /// </summary>
    public record TodayExpression() : TemporalExpression
    {
        public override DateOnly Evaluate(DateOnly referenceDate) => referenceDate;
    }

    /// <summary>
    /// Add duration to date
    /// </summary>
    public record AddDuration(TemporalExpression Base, TemporalDuration Duration) : TemporalExpression
    {
        public override DateOnly Evaluate(DateOnly referenceDate)
        {
            var baseDate = Base.Evaluate(referenceDate);
            return Duration.AddTo(baseDate);
        }
    }

    /// <summary>
    /// Temporal duration (days, months, years)
    /// </summary>
    public abstract record TemporalDuration
    {
        public abstract DateOnly AddTo(DateOnly date);
    }

    /// <summary>
    /// Duration in days
    /// </summary>
    public record DaysDuration(int Days, bool BusinessDaysOnly = false) : TemporalDuration
    {
        public override DateOnly AddTo(DateOnly date)
        {
            return BusinessDaysOnly 
                ? TemporalOperators.AddBusinessDays(date, Days)
                : date.AddDays(Days);
        }
    }

    /// <summary>
    /// Duration in months
    /// </summary>
    public record MonthsDuration(int Months) : TemporalDuration
    {
        public override DateOnly AddTo(DateOnly date) => TemporalOperators.AddMonths(date, Months);
    }

    /// <summary>
    /// Duration in years
    /// </summary>
    public record YearsDuration(int Years) : TemporalDuration
    {
        public override DateOnly AddTo(DateOnly date) => TemporalOperators.AddYears(date, Years);
    }
}
