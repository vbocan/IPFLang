using IPFLang.Temporal;

namespace IPFLang.Engine.Tests
{
    public class TemporalOperatorsTests
    {
        [Fact]
        public void TestBusinessDaysBetween_SameDay()
        {
            var date = new DateOnly(2024, 1, 15); // Monday
            var result = TemporalOperators.BusinessDaysBetween(date, date);
            
            Assert.Equal(0, result);
        }

        [Fact]
        public void TestBusinessDaysBetween_OnlyWeekdays()
        {
            var start = new DateOnly(2024, 1, 15); // Monday
            var end = new DateOnly(2024, 1, 19);   // Friday
            var result = TemporalOperators.BusinessDaysBetween(start, end);
            
            Assert.Equal(4, result); // Mon, Tue, Wed, Thu (not including end day)
        }

        [Fact]
        public void TestBusinessDaysBetween_SkipsWeekend()
        {
            var start = new DateOnly(2024, 1, 15); // Monday
            var end = new DateOnly(2024, 1, 22);   // Monday next week
            var result = TemporalOperators.BusinessDaysBetween(start, end);
            
            Assert.Equal(5, result); // Mon-Fri, skips Sat-Sun
        }

        [Fact]
        public void TestBusinessDaysBetween_Negative()
        {
            var start = new DateOnly(2024, 1, 19); // Friday
            var end = new DateOnly(2024, 1, 15);   // Monday
            var result = TemporalOperators.BusinessDaysBetween(start, end);
            
            Assert.Equal(-4, result);
        }

        [Fact]
        public void TestCalendarDaysBetween()
        {
            var start = new DateOnly(2024, 1, 1);
            var end = new DateOnly(2024, 1, 10);
            var result = TemporalOperators.CalendarDaysBetween(start, end);
            
            Assert.Equal(9, result);
        }

        [Fact]
        public void TestAddBusinessDays_PositiveSkipsWeekend()
        {
            var start = new DateOnly(2024, 1, 19); // Friday
            var result = TemporalOperators.AddBusinessDays(start, 3);
            
            Assert.Equal(new DateOnly(2024, 1, 24), result); // Wednesday (skips Sat-Sun-Mon-Tue)
        }

        [Fact]
        public void TestAddBusinessDays_Negative()
        {
            var start = new DateOnly(2024, 1, 22); // Monday
            var result = TemporalOperators.AddBusinessDays(start, -3);
            
            Assert.Equal(new DateOnly(2024, 1, 17), result); // Wednesday previous week
        }

        [Fact]
        public void TestAddBusinessDays_Zero()
        {
            var date = new DateOnly(2024, 1, 15);
            var result = TemporalOperators.AddBusinessDays(date, 0);
            
            Assert.Equal(date, result);
        }

        [Fact]
        public void TestIsWeekend()
        {
            Assert.True(TemporalOperators.IsWeekend(new DateOnly(2024, 1, 20)));  // Saturday
            Assert.True(TemporalOperators.IsWeekend(new DateOnly(2024, 1, 21)));  // Sunday
            Assert.False(TemporalOperators.IsWeekend(new DateOnly(2024, 1, 22))); // Monday
        }

        [Fact]
        public void TestIsWeekday()
        {
            Assert.True(TemporalOperators.IsWeekday(new DateOnly(2024, 1, 15)));  // Monday
            Assert.False(TemporalOperators.IsWeekday(new DateOnly(2024, 1, 20))); // Saturday
        }

        [Fact]
        public void TestNextBusinessDay()
        {
            var friday = new DateOnly(2024, 1, 19);
            var result = TemporalOperators.NextBusinessDay(friday);
            
            Assert.Equal(new DateOnly(2024, 1, 22), result); // Monday
        }

        [Fact]
        public void TestPreviousBusinessDay()
        {
            var monday = new DateOnly(2024, 1, 22);
            var result = TemporalOperators.PreviousBusinessDay(monday);
            
            Assert.Equal(new DateOnly(2024, 1, 19), result); // Friday
        }

        [Fact]
        public void TestMonthsBetween()
        {
            var start = new DateOnly(2024, 1, 15);
            var end = new DateOnly(2024, 6, 15);
            var result = TemporalOperators.MonthsBetween(start, end);
            
            Assert.Equal(5, result);
        }

        [Fact]
        public void TestMonthsBetween_DayAdjustment()
        {
            var start = new DateOnly(2024, 1, 31);
            var end = new DateOnly(2024, 2, 28);
            var result = TemporalOperators.MonthsBetween(start, end);
            
            Assert.Equal(0, result); // Not a full month because day is earlier
        }

        [Fact]
        public void TestYearsBetween()
        {
            var start = new DateOnly(2020, 1, 15);
            var end = new DateOnly(2024, 1, 15);
            var result = TemporalOperators.YearsBetween(start, end);
            
            Assert.Equal(4, result);
        }

        [Fact]
        public void TestYearsBetween_DayAdjustment()
        {
            var start = new DateOnly(2020, 1, 31);
            var end = new DateOnly(2024, 1, 30);
            var result = TemporalOperators.YearsBetween(start, end);
            
            Assert.Equal(3, result); // Not a full 4 years because day is earlier
        }

        [Fact]
        public void TestIsWithinRange()
        {
            var start = new DateOnly(2024, 1, 1);
            var end = new DateOnly(2024, 12, 31);
            
            Assert.True(TemporalOperators.IsWithinRange(new DateOnly(2024, 6, 15), start, end));
            Assert.True(TemporalOperators.IsWithinRange(start, start, end));
            Assert.True(TemporalOperators.IsWithinRange(end, start, end));
            Assert.False(TemporalOperators.IsWithinRange(new DateOnly(2023, 12, 31), start, end));
        }

        [Fact]
        public void TestIsBefore()
        {
            Assert.True(TemporalOperators.IsBefore(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31)));
            Assert.False(TemporalOperators.IsBefore(new DateOnly(2024, 12, 31), new DateOnly(2024, 1, 1)));
        }

        [Fact]
        public void TestIsAfter()
        {
            Assert.True(TemporalOperators.IsAfter(new DateOnly(2024, 12, 31), new DateOnly(2024, 1, 1)));
            Assert.False(TemporalOperators.IsAfter(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31)));
        }

        [Fact]
        public void TestMinMax()
        {
            var date1 = new DateOnly(2024, 1, 1);
            var date2 = new DateOnly(2024, 12, 31);
            
            Assert.Equal(date1, TemporalOperators.Min(date1, date2));
            Assert.Equal(date2, TemporalOperators.Max(date1, date2));
        }

        [Fact]
        public void TestTemporalExpression_LiteralDate()
        {
            var date = new DateOnly(2024, 6, 15);
            var expr = new LiteralDate(date);
            var result = expr.Evaluate(DateOnly.FromDateTime(DateTime.Today));
            
            Assert.Equal(date, result);
        }

        [Fact]
        public void TestTemporalExpression_Today()
        {
            var today = new DateOnly(2024, 1, 15);
            var expr = new TodayExpression();
            var result = expr.Evaluate(today);
            
            Assert.Equal(today, result);
        }

        [Fact]
        public void TestTemporalExpression_AddDays()
        {
            var today = new DateOnly(2024, 1, 15);
            var expr = new AddDuration(new TodayExpression(), new DaysDuration(30));
            var result = expr.Evaluate(today);
            
            Assert.Equal(new DateOnly(2024, 2, 14), result);
        }

        [Fact]
        public void TestTemporalExpression_AddBusinessDays()
        {
            var friday = new DateOnly(2024, 1, 19);
            var expr = new AddDuration(new LiteralDate(friday), new DaysDuration(5, BusinessDaysOnly: true));
            var result = expr.Evaluate(friday);
            
            Assert.Equal(new DateOnly(2024, 1, 26), result); // Friday next week
        }

        [Fact]
        public void TestTemporalExpression_AddMonths()
        {
            var date = new DateOnly(2024, 1, 15);
            var expr = new AddDuration(new LiteralDate(date), new MonthsDuration(6));
            var result = expr.Evaluate(date);
            
            Assert.Equal(new DateOnly(2024, 7, 15), result);
        }

        [Fact]
        public void TestTemporalExpression_AddYears()
        {
            var date = new DateOnly(2024, 1, 15);
            var expr = new AddDuration(new LiteralDate(date), new YearsDuration(2));
            var result = expr.Evaluate(date);
            
            Assert.Equal(new DateOnly(2026, 1, 15), result);
        }
    }
}
