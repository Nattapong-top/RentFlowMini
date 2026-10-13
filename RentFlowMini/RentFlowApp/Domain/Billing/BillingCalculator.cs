using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Domain.Billing;

public sealed record BillingCalculationResult(
    Money Rent,
    Money Water,
    Money Electricity,
    Money Total);

public static class BillingCalculator
{
    public static BillingCalculationResult Calculate(
        decimal monthlyRent,
        decimal? previousWaterMeter,
        decimal? currentWaterMeter,
        decimal? previousElectricityMeter,
        decimal? currentElectricityMeter,
        decimal waterRate,
        decimal electricityRate)
    {
        var waterReadings = ValidateMeterReading(
            "มิเตอร์น้ำ",
            previousWaterMeter,
            currentWaterMeter);
        var electricityReadings = ValidateMeterReading(
            "มิเตอร์ไฟฟ้า",
            previousElectricityMeter,
            currentElectricityMeter);

        decimal waterUsage =
            waterReadings.Current - waterReadings.Previous;
        decimal electricityUsage =
            electricityReadings.Current - electricityReadings.Previous;

        decimal waterAmount = waterUsage * waterRate;
        decimal electricityAmount = electricityUsage * electricityRate;
        decimal total = monthlyRent + waterAmount + electricityAmount;

        return new BillingCalculationResult(
            Money.Create(monthlyRent),
            Money.Create(waterAmount),
            Money.Create(electricityAmount),
            Money.Create(total));
    }

    private static (decimal Previous, decimal Current) ValidateMeterReading(
        string meterType,
        decimal? previousReading,
        decimal? currentReading)
    {
        if (!previousReading.HasValue || !currentReading.HasValue)
        {
            throw new InvalidMeterReadingException(
                meterType,
                "ต้องระบุค่ามิเตอร์ก่อนหน้าและค่าปัจจุบัน");
        }

        if (currentReading < previousReading)
        {
            throw new InvalidMeterReadingException(
                meterType,
                "ค่าปัจจุบันต้องไม่น้อยกว่าค่าก่อนหน้า");
        }

        return (previousReading.Value, currentReading.Value);
    }
}
