using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Tests.Domain.Billing;

public class BillingPeriodTests
{
    [Fact]
    public void Create_WithYearAndMonth_ReturnsBillingPeriod()
    {
        var period = BillingPeriod.Create("2026-09");

        Assert.Equal(2026, period.Year);
        Assert.Equal(9, period.Month);
        Assert.Equal("2026-09", period.Value);
    }

    [Fact]
    public void Create_WhenValueIsNotYearMonth_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            BillingPeriod.Create("2026-9"));
    }
}
