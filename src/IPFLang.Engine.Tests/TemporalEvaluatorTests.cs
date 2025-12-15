using IPFLang.Temporal;

namespace IPFLang.Engine.Tests
{
    public class TemporalEvaluatorTests
    {
        [Fact]
        public void TestEvaluateBusinessDaysBetween()
        {
            var evaluator = new TemporalEvaluator();
            var start = new DateOnly(2024, 1, 15); // Monday
            var end = new DateOnly(2024, 1, 19);   // Friday
            
            var result = evaluator.EvaluateBusinessDaysBetween(start, end);
            
            Assert.Equal(4, result);
        }

        [Fact]
        public void TestCalculateLateFeeMultiplier_OnTime()
        {
            var evaluator = new TemporalEvaluator();
            var deadline = new DateOnly(2024, 1, 31);
            var actualDate = new DateOnly(2024, 1, 30);
            
            var multiplier = evaluator.CalculateLateFeeMultiplier(deadline, actualDate);
            
            Assert.Equal(1.0m, multiplier);
        }

        [Fact]
        public void TestCalculateLateFeeMultiplier_Late()
        {
            var evaluator = new TemporalEvaluator();
            var deadline = new DateOnly(2024, 1, 31);
            var actualDate = new DateOnly(2024, 2, 10); // 10 days late
            
            var multiplier = evaluator.CalculateLateFeeMultiplier(
                deadline, 
                actualDate,
                baseMultiplier: 1.0m,
                dailyIncrease: 0.01m,
                maxMultiplier: 2.0m
            );
            
            Assert.Equal(1.10m, multiplier); // 1.0 + (10 * 0.01)
        }

        [Fact]
        public void TestCalculateLateFeeMultiplier_MaxCap()
        {
            var evaluator = new TemporalEvaluator();
            var deadline = new DateOnly(2024, 1, 31);
            var actualDate = new DateOnly(2024, 5, 31); // 121 days late
            
            var multiplier = evaluator.CalculateLateFeeMultiplier(
                deadline,
                actualDate,
                baseMultiplier: 1.0m,
                dailyIncrease: 0.01m,
                maxMultiplier: 2.0m
            );
            
            Assert.Equal(2.0m, multiplier); // Capped at max
        }

        [Fact]
        public void TestCalculateSteppedLateFee_OnTime()
        {
            var evaluator = new TemporalEvaluator();
            var deadline = new DateOnly(2024, 1, 31);
            var actualDate = new DateOnly(2024, 1, 30);
            
            var fee = evaluator.CalculateSteppedLateFee(
                deadline,
                actualDate,
                (1, 50m),
                (30, 100m),
                (60, 200m)
            );
            
            Assert.Equal(0m, fee);
        }

        [Fact]
        public void TestCalculateSteppedLateFee_FirstTier()
        {
            var evaluator = new TemporalEvaluator();
            var deadline = new DateOnly(2024, 1, 31);
            var actualDate = new DateOnly(2024, 2, 10); // 10 days late
            
            var fee = evaluator.CalculateSteppedLateFee(
                deadline,
                actualDate,
                (1, 50m),
                (30, 100m),
                (60, 200m)
            );
            
            Assert.Equal(50m, fee);
        }

        [Fact]
        public void TestCalculateSteppedLateFee_SecondTier()
        {
            var evaluator = new TemporalEvaluator();
            var deadline = new DateOnly(2024, 1, 31);
            var actualDate = new DateOnly(2024, 3, 15); // 44 days late
            
            var fee = evaluator.CalculateSteppedLateFee(
                deadline,
                actualDate,
                (1, 50m),
                (30, 100m),
                (60, 200m)
            );
            
            Assert.Equal(100m, fee);
        }

        [Fact]
        public void TestIsRenewalDue()
        {
            var filingDate = new DateOnly(2020, 1, 15);
            var checkDate = new DateOnly(2024, 2, 1);
            var evaluator = new TemporalEvaluator(checkDate);
            
            var isDue3Year = evaluator.IsRenewalDue(filingDate, checkDate, 3);
            var isDue5Year = evaluator.IsRenewalDue(filingDate, checkDate, 5);
            
            Assert.True(isDue3Year);  // 4 years >= 3
            Assert.False(isDue5Year); // 4 years < 5
        }

        [Fact]
        public void TestCalculateNextRenewalDate()
        {
            var filingDate = new DateOnly(2020, 1, 15);
            var evaluationDate = new DateOnly(2024, 6, 1);
            var evaluator = new TemporalEvaluator(evaluationDate);
            
            var nextRenewal = evaluator.CalculateNextRenewalDate(filingDate, 3);
            
            Assert.Equal(new DateOnly(2026, 1, 15), nextRenewal); // Next 3-year mark is year 6
        }

        [Fact]
        public void TestIsWithinGracePeriod()
        {
            var evaluator = new TemporalEvaluator();
            var deadline = new DateOnly(2024, 1, 31);
            
            var withinGrace = new DateOnly(2024, 3, 15); // Within 6 months
            var outsideGrace = new DateOnly(2024, 9, 1);  // Outside 6 months
            
            Assert.True(evaluator.IsWithinGracePeriod(deadline, withinGrace, 6));
            Assert.False(evaluator.IsWithinGracePeriod(deadline, outsideGrace, 6));
        }

        [Fact]
        public void TestCalculatePriorityPeriodEnd()
        {
            var priorityDate = new DateOnly(2024, 1, 15);
            var evaluator = new TemporalEvaluator();
            
            var periodEnd = evaluator.CalculatePriorityPeriodEnd(priorityDate, 12);
            
            Assert.Equal(new DateOnly(2025, 1, 15), periodEnd);
        }

        [Fact]
        public void TestIsWithinPriorityPeriod()
        {
            var priorityDate = new DateOnly(2024, 1, 15);
            var evaluator = new TemporalEvaluator();
            
            var withinPeriod = new DateOnly(2024, 10, 1);
            var outsidePeriod = new DateOnly(2025, 3, 1);
            
            Assert.True(evaluator.IsWithinPriorityPeriod(priorityDate, withinPeriod, 12));
            Assert.False(evaluator.IsWithinPriorityPeriod(priorityDate, outsidePeriod, 12));
        }

        [Fact]
        public void TestEvaluateExpression_TodayPlus6Months()
        {
            var today = new DateOnly(2024, 1, 15);
            var evaluator = new TemporalEvaluator(today);
            
            var expr = new AddDuration(new TodayExpression(), new MonthsDuration(6));
            var result = evaluator.EvaluateExpression(expr);
            
            Assert.Equal(new DateOnly(2024, 7, 15), result);
        }

        [Fact]
        public void TestIsBeforeDeadline()
        {
            var evaluator = new TemporalEvaluator();
            var deadline = new DateOnly(2024, 12, 31);
            
            Assert.True(evaluator.IsBeforeDeadline(new DateOnly(2024, 6, 15), deadline));
            Assert.False(evaluator.IsBeforeDeadline(new DateOnly(2025, 1, 1), deadline));
        }

        [Fact]
        public void TestIsAfterDeadline()
        {
            var evaluator = new TemporalEvaluator();
            var deadline = new DateOnly(2024, 12, 31);
            
            Assert.False(evaluator.IsAfterDeadline(new DateOnly(2024, 6, 15), deadline));
            Assert.True(evaluator.IsAfterDeadline(new DateOnly(2025, 1, 1), deadline));
        }

        [Fact]
        public void TestRealWorldScenario_PatentRenewal()
        {
            // Patent filed on 2020-01-15
            // Check renewals in 2024
            var filingDate = new DateOnly(2020, 1, 15);
            var checkDate = new DateOnly(2024, 6, 1);
            var evaluator = new TemporalEvaluator(checkDate);
            
            // 3-year renewal
            var isDue3 = evaluator.IsRenewalDue(filingDate, checkDate, 3);
            var next3 = evaluator.CalculateNextRenewalDate(filingDate, 3);
            
            Assert.True(isDue3); // 4+ years have passed
            Assert.Equal(new DateOnly(2026, 1, 15), next3); // Next is year 6
        }

        [Fact]
        public void TestRealWorldScenario_PriorityRight()
        {
            // Priority filing on 2024-01-15
            // Later filing on 2024-10-15
            var priorityDate = new DateOnly(2024, 1, 15);
            var laterFiling = new DateOnly(2024, 10, 15);
            var evaluator = new TemporalEvaluator();
            
            var hasPriority = evaluator.IsWithinPriorityPeriod(priorityDate, laterFiling, 12);
            
            Assert.True(hasPriority); // Within 12 months
        }

        [Fact]
        public void TestRealWorldScenario_LateFiling()
        {
            // Deadline: 2024-01-31
            // Filed: 2024-02-15 (15 days late)
            var deadline = new DateOnly(2024, 1, 31);
            var actualFiling = new DateOnly(2024, 2, 15);
            var evaluator = new TemporalEvaluator();
            
            var multiplier = evaluator.CalculateLateFeeMultiplier(deadline, actualFiling, 1.0m, 0.02m, 2.0m);
            
            Assert.Equal(1.30m, multiplier); // 1.0 + (15 * 0.02)
        }
    }
}
