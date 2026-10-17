using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Tests.Domain.Billing;

public class MonthlyMeterReadingTests
{
    [Fact]
    public void Create_WithCompleteReadings_StoresRoomPeriodAndMeters()
    {
        var roomId = RoomId.New();
        var period = BillingPeriod.Create("2026-10");

        var reading = MonthlyMeterReading.Create(
            roomId,
            period,
            MeterReading.Create(100m),
            MeterReading.Create(125m),
            MeterReading.Create(1000m),
            MeterReading.Create(1010m));

        Assert.Equal(roomId, reading.RoomId);
        Assert.Equal(period, reading.BillingPeriod);
        Assert.Equal(100m, reading.PreviousWaterMeter.Value);
        Assert.Equal(125m, reading.CurrentWaterMeter.Value);
        Assert.Equal(1000m, reading.PreviousElectricityMeter.Value);
        Assert.Equal(1010m, reading.CurrentElectricityMeter.Value);
    }

    [Theory]
    [InlineData(125, 100, 1000, 1010)]
    [InlineData(100, 125, 1010, 1000)]
    public void Create_WhenCurrentMeterIsLowerThanPrevious_Throws(
        decimal previousWater,
        decimal currentWater,
        decimal previousElectricity,
        decimal currentElectricity)
    {
        Assert.Throws<InvalidMeterReadingException>(() => MonthlyMeterReading.Create(
            RoomId.New(),
            BillingPeriod.Create("2026-10"),
            MeterReading.Create(previousWater),
            MeterReading.Create(currentWater),
            MeterReading.Create(previousElectricity),
            MeterReading.Create(currentElectricity)));
    }

    [Fact]
    public void Create_WhenReadingIsMissing_ThrowsInvalidMeterReadingException()
    {
        Assert.Throws<InvalidMeterReadingException>(() => MonthlyMeterReading.Create(
            RoomId.New(),
            BillingPeriod.Create("2026-10"),
            MeterReading.Create(100m),
            null,
            MeterReading.Create(1000m),
            MeterReading.Create(1010m)));
    }
}
