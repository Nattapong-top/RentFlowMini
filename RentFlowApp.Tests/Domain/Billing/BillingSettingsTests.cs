using RentFlowApp.Domain.Billing;

namespace RentFlowApp.Tests.Domain.Billing;

public class BillingSettingsTests
{
    [Fact]
    public void Create_WithRates_StoresAllRates()
    {
        var settings = BillingSettings.Create(30m, 4m, 100m, 300m, 500m);

        Assert.Equal(30m, settings.WaterRate.Amount);
        Assert.Equal(4m, settings.ElectricityRate.Amount);
        Assert.Equal(100m, settings.MotorcycleParkingRate.Amount);
        Assert.Equal(300m, settings.CarParkingRate.Amount);
        Assert.Equal(500m, settings.CableTvRate.Amount);
    }

    [Fact]
    public void Create_WithZeroRates_IsAllowed()
    {
        var settings = BillingSettings.Create(0m, 0m, 0m, 0m, 0m);

        Assert.Equal(0m, settings.WaterRate.Amount);
        Assert.Equal(0m, settings.CableTvRate.Amount);
    }

    [Theory]
    [InlineData(-1, 4, 100, 300, 500)]
    [InlineData(30, -1, 100, 300, 500)]
    [InlineData(30, 4, -1, 300, 500)]
    [InlineData(30, 4, 100, -1, 500)]
    [InlineData(30, 4, 100, 300, -1)]
    public void Create_WithNegativeRate_ThrowsArgumentOutOfRangeException(
        decimal waterRate,
        decimal electricityRate,
        decimal motorcycleParkingRate,
        decimal carParkingRate,
        decimal cableTvRate)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BillingSettings.Create(
            waterRate,
            electricityRate,
            motorcycleParkingRate,
            carParkingRate,
            cableTvRate));
    }
}
