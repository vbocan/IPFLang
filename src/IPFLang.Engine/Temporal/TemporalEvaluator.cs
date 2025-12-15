namespace IPFLang.Temporal
{
    /// <summary>
    /// Evaluates temporal expressions within DSL context
    /// </summary>
    public class TemporalEvaluator
    {
        private readonly DateOnly _evaluationDate;

        public TemporalEvaluator(DateOnly? evaluationDate = null)
        {
            _evaluationDate = evaluationDate ?? DateOnly.FromDateTime(DateTime.Today);
        }

        /// <summary>
        /// Calculate business days between two dates
        /// </summary>
        public int EvaluateBusinessDaysBetween(DateOnly start, DateOnly end)
        {
            return TemporalOperators.BusinessDaysBetween(start, end);
        }

        /// <summary>
        /// Calculate calendar days between two dates
        /// </summary>
        public int EvaluateCalendarDaysBetween(DateOnly start, DateOnly end)
        {
            return TemporalOperators.CalendarDaysBetween(start, end);
        }

        /// <summary>
        /// Check if date is before deadline
        /// </summary>
        public bool IsBeforeDeadline(DateOnly date, DateOnly deadline)
        {
            return TemporalOperators.IsBefore(date, deadline);
        }

        /// <summary>
        /// Check if date is after deadline
        /// </summary>
        public bool IsAfterDeadline(DateOnly date, DateOnly deadline)
        {
            return TemporalOperators.IsAfter(date, deadline);
        }

        /// <summary>
        /// Calculate late fee multiplier based on days past deadline
        /// </summary>
        public decimal CalculateLateFeeMultiplier(DateOnly deadline, DateOnly actualDate, 
            decimal baseMultiplier = 1.0m, decimal dailyIncrease = 0.01m, decimal maxMultiplier = 2.0m)
        {
            if (actualDate <= deadline)
                return 1.0m;

            var daysLate = TemporalOperators.CalendarDaysBetween(deadline, actualDate);
            var multiplier = baseMultiplier + (daysLate * dailyIncrease);

            return Math.Min(multiplier, maxMultiplier);
        }

        /// <summary>
        /// Calculate stepped late fee based on time periods
        /// </summary>
        public decimal CalculateSteppedLateFee(DateOnly deadline, DateOnly actualDate,
            params (int days, decimal fee)[] steps)
        {
            if (actualDate <= deadline)
                return 0m;

            var daysLate = TemporalOperators.CalendarDaysBetween(deadline, actualDate);

            // Find the applicable step
            decimal applicableFee = 0m;
            foreach (var (days, fee) in steps.OrderBy(s => s.days))
            {
                if (daysLate >= days)
                    applicableFee = fee;
                else
                    break;
            }

            return applicableFee;
        }

        /// <summary>
        /// Evaluate a temporal expression
        /// </summary>
        public DateOnly EvaluateExpression(TemporalExpression expression)
        {
            return expression.Evaluate(_evaluationDate);
        }

        /// <summary>
        /// Check if a renewal is due based on anniversary date
        /// </summary>
        public bool IsRenewalDue(DateOnly filingDate, DateOnly checkDate, int yearInterval)
        {
            var yearsSinceFiling = TemporalOperators.YearsBetween(filingDate, checkDate);
            return yearsSinceFiling >= yearInterval;
        }

        /// <summary>
        /// Calculate next renewal date
        /// </summary>
        public DateOnly CalculateNextRenewalDate(DateOnly filingDate, int yearInterval)
        {
            var yearsSinceFiling = TemporalOperators.YearsBetween(filingDate, _evaluationDate);
            var nextRenewalYear = ((yearsSinceFiling / yearInterval) + 1) * yearInterval;
            return TemporalOperators.AddYears(filingDate, nextRenewalYear);
        }

        /// <summary>
        /// Check if date falls within grace period
        /// </summary>
        public bool IsWithinGracePeriod(DateOnly deadline, DateOnly actualDate, int gracePeriodMonths)
        {
            var gracePeriodEnd = TemporalOperators.AddMonths(deadline, gracePeriodMonths);
            return actualDate >= deadline && actualDate <= gracePeriodEnd;
        }

        /// <summary>
        /// Calculate priority period end date
        /// </summary>
        public DateOnly CalculatePriorityPeriodEnd(DateOnly priorityDate, int months = 12)
        {
            return TemporalOperators.AddMonths(priorityDate, months);
        }

        /// <summary>
        /// Check if filing is within priority period
        /// </summary>
        public bool IsWithinPriorityPeriod(DateOnly priorityDate, DateOnly filingDate, int months = 12)
        {
            var periodEnd = CalculatePriorityPeriodEnd(priorityDate, months);
            return TemporalOperators.IsWithinRange(filingDate, priorityDate, periodEnd);
        }
    }

    /// <summary>
    /// Results from temporal analysis
    /// </summary>
    public record TemporalAnalysisResult(
        DateOnly EvaluationDate,
        Dictionary<string, DateOnly> Deadlines,
        Dictionary<string, int> DaysRemaining,
        List<DeadlineWarning> Warnings
    );

    /// <summary>
    /// Warning about approaching or missed deadlines
    /// </summary>
    public record DeadlineWarning(
        string DeadlineName,
        DateOnly Deadline,
        DateOnly CheckDate,
        int DaysLate,
        DeadlineStatus Status
    );

    public enum DeadlineStatus
    {
        Future,
        Approaching,
        DueToday,
        Overdue
    }
}
