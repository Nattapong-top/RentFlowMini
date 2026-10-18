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
            previousWaterMeter: MeterReading.Create(120m),
            currentWaterMeter: MeterReading.Create(125m),
            previousElectricityMeter: MeterReading.Create(1000m),
            currentElectricityMeter: MeterReading.Create(1010m),
            waterRate: 30m,
            electricityRate: 4m);

        Assert.Equal(2800m, result.Rent.Amount);
        Assert.Equal(150m, result.Water.Amount);
        Assert.Equal(40m, result.Electricity.Amount);
        Assert.Equal(0m, result.MotorcycleParking.Amount);
        Assert.Equal(0m, result.CarParking.Amount);
        Assert.Equal(0m, result.CableTv.Amount);
        Assert.Equal(2990m, result.Total.Amount);
    }

    [Fact]
    public void Calculate_WithAllChargesAndParking_ReturnsAllAmountsAndTotal()
    {
        var result = BillingCalculator.Calculate(
            monthlyRent: 3000m,
            previousWaterMeter: MeterReading.Create(100m),
            currentWaterMeter: MeterReading.Create(110m),
            previousElectricityMeter: MeterReading.Create(500m),
            currentElectricityMeter: MeterReading.Create(550m),
            waterRate: 19m,
            electricityRate: 8m,
            chargeMotorcycleParking: true,
            motorcycleParkingRate: 300m,
            chargeCarParking: true,
            carParkingRate: 500m,
            isCableTvExempt: false,
            cableTvRate: 60m);

        Assert.Equal(3000m, result.Rent.Amount);
        Assert.Equal(190m, result.Water.Amount);
        Assert.Equal(400m, result.Electricity.Amount);
        Assert.Equal(300m, result.MotorcycleParking.Amount);
        Assert.Equal(500m, result.CarParking.Amount);
        Assert.Equal(60m, result.CableTv.Amount);
        Assert.Equal(4450m, result.Total.Amount);
    }

    [Fact]
    public void Calculate_WhenCableTvExempt_ExcludesCableTv()
    {
        var result = BillingCalculator.Calculate(
            monthlyRent: 3000m,
            previousWaterMeter: MeterReading.Create(100m),
            currentWaterMeter: MeterReading.Create(100m),
            previousElectricityMeter: MeterReading.Create(500m),
            currentElectricityMeter: MeterReading.Create(500m),
            waterRate: 19m,
            electricityRate: 8m,
            chargeMotorcycleParking: false,
            motorcycleParkingRate: 300m,
            chargeCarParking: false,
            carParkingRate: 500m,
            isCableTvExempt: true,
            cableTvRate: 60m);

        Assert.Equal(3000m, result.Rent.Amount);
        Assert.Equal(0m, result.Water.Amount);
        Assert.Equal(0m, result.Electricity.Amount);
        Assert.Equal(0m, result.MotorcycleParking.Amount);
        Assert.Equal(0m, result.CarParking.Amount);
        Assert.Equal(0m, result.CableTv.Amount);
        Assert.Equal(3000m, result.Total.Amount);
    }

    [Fact]
    public void Calculate_WhenCurrentWaterMeterIsLower_ThrowsInvalidMeterReadingException()
    {
        Assert.Throws<InvalidMeterReadingException>(() =>
            BillingCalculator.Calculate(
                monthlyRent: 2800m,
                previousWaterMeter: MeterReading.Create(120m),
                currentWaterMeter: MeterReading.Create(119m),
                previousElectricityMeter: MeterReading.Create(1000m),
                currentElectricityMeter: MeterReading.Create(1010m),
                waterRate: 30m,
                electricityRate: 4m));
    }

    [Fact]
    public void Calculate_WhenCurrentElectricityMeterIsLower_ThrowsInvalidMeterReadingException()
    {
        Assert.Throws<InvalidMeterReadingException>(() =>
            BillingCalculator.Calculate(
                monthlyRent: 2800m,
                previousWaterMeter: MeterReading.Create(120m),
                currentWaterMeter: MeterReading.Create(125m),
                previousElectricityMeter: MeterReading.Create(1000m),
                currentElectricityMeter: MeterReading.Create(999m),
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
                currentWaterMeter: MeterReading.Create(125m),
                previousElectricityMeter: MeterReading.Create(1000m),
                currentElectricityMeter: MeterReading.Create(1010m),
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
                previousWaterMeter: MeterReading.Create(120m),
                currentWaterMeter: MeterReading.Create(125m),
                previousElectricityMeter: MeterReading.Create(1000m),
                currentElectricityMeter: null,
                waterRate: 30m,
                electricityRate: 4m));

        Assert.Contains("มิเตอร์ไฟฟ้า", exception.Message);
        Assert.Contains("ต้องระบุค่ามิเตอร์", exception.Message);
    }
}
