using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Tests.Domain.Billing;

public class BillingCalculatorTests
{
    [Fact]
    public void Calculate_WithCompleteInput_ReturnsBillAmountsAndTotal()
    {
        var result = BillingCalculator.Calculate(
            monthlyRent: 2800m,
            previousWaterMeter: 120m,
            currentWaterMeter: 125m,
            previousElectricityMeter: 1000m,
            currentElectricityMeter: 1010m,
            waterRate: 30m,
            electricityRate: 4m);

        Assert.Equal(2800m, result.Rent.Amount);
        Assert.Equal(150m, result.Water.Amount);
        Assert.Equal(40m, result.Electricity.Amount);
        Assert.Equal(2990m, result.Total.Amount);
    }

    [Fact]
    public void Calculate_WhenCurrentWaterMeterIsLower_ThrowsInvalidMeterReadingException()
    {
        Assert.Throws<InvalidMeterReadingException>(() =>
            BillingCalculator.Calculate(
                monthlyRent: 2800m,
                previousWaterMeter: 120m,
                currentWaterMeter: 119m,
                previousElectricityMeter: 1000m,
                currentElectricityMeter: 1010m,
                waterRate: 30m,
                electricityRate: 4m));
    }

    [Fact]
    public void Calculate_WhenCurrentElectricityMeterIsLower_ThrowsInvalidMeterReadingException()
    {
        Assert.Throws<InvalidMeterReadingException>(() =>
            BillingCalculator.Calculate(
                monthlyRent: 2800m,
                previousWaterMeter: 120m,
                currentWaterMeter: 125m,
                previousElectricityMeter: 1000m,
                currentElectricityMeter: 999m,
                waterRate: 30m,
                electricityRate: 4m));
    }

    [Fact]
    public void Calculate_WhenWaterMeterIsMissing_ThrowsInvalidMeterReadingExceptionInThai()
    {
        var exception = Assert.Throws<InvalidMeterReadingException>(() =>
            BillingCalculator.Calculate(
                monthlyRent: 2800m,
                previousWaterMeter: null,
                currentWaterMeter: 125m,
                previousElectricityMeter: 1000m,
                currentElectricityMeter: 1010m,
                waterRate: 30m,
                electricityRate: 4m));

        Assert.Contains("มิเตอร์น้ำ", exception.Message);
        Assert.Contains("ต้องระบุค่ามิเตอร์", exception.Message);
    }

    [Fact]
    public void Calculate_WhenElectricityMeterIsMissing_ThrowsInvalidMeterReadingExceptionInThai()
    {
        var exception = Assert.Throws<InvalidMeterReadingException>(() =>
            BillingCalculator.Calculate(
                monthlyRent: 2800m,
                previousWaterMeter: 120m,
                currentWaterMeter: 125m,
                previousElectricityMeter: 1000m,
                currentElectricityMeter: null,
                waterRate: 30m,
                electricityRate: 4m));

        Assert.Contains("มิเตอร์ไฟฟ้า", exception.Message);
        Assert.Contains("ต้องระบุค่ามิเตอร์", exception.Message);
    }
}
