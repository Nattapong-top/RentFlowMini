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
    public void ToBuddhistValue_ReturnsBuddhistEraFormat()
    {
        var period = BillingPeriod.Create("2026-10");

        Assert.Equal("2569-10", period.ToBuddhistValue());
    }

    [Fact]
    public void Create_WhenValueIsNotYearMonth_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            BillingPeriod.Create("2026-9"));
    }
}
