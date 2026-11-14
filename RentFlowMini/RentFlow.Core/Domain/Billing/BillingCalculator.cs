using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Domain.Billing;

public sealed record BillingCalculationResult(
    Money Rent,
    Money Water,
    Money Electricity,
    Money MotorcycleParking,
    Money CarParking,
    Money CableTv,
    Money Total);

public static class BillingCalculator
{
    public static BillingCalculationResult Calculate(
        decimal monthlyRent,
        MeterReading? previousWaterMeter,
        MeterReading? currentWaterMeter,
        MeterReading? previousElectricityMeter,
        MeterReading? currentElectricityMeter,
        decimal waterRate,
        decimal electricityRate,
        bool chargeMotorcycleParking = false,
        decimal motorcycleParkingRate = 0m,
        bool chargeCarParking = false,
        decimal carParkingRate = 0m,
        bool isCableTvExempt = false,
        decimal cableTvRate = 0m)
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
        decimal motorcycleParkingAmount = chargeMotorcycleParking ? motorcycleParkingRate : 0m;
        decimal carParkingAmount = chargeCarParking ? carParkingRate : 0m;
        decimal cableTvAmount = isCableTvExempt ? 0m : cableTvRate;

        decimal total = monthlyRent + waterAmount + electricityAmount + motorcycleParkingAmount + carParkingAmount + cableTvAmount;

        return new BillingCalculationResult(
            Money.Create(monthlyRent),
            Money.Create(waterAmount),
            Money.Create(electricityAmount),
            Money.Create(motorcycleParkingAmount),
            Money.Create(carParkingAmount),
            Money.Create(cableTvAmount),
            Money.Create(total));
    }

    private static (decimal Previous, decimal Current) ValidateMeterReading(
        string meterType,
        MeterReading? previousReading,
        MeterReading? currentReading)
    {
        if (previousReading is null || currentReading is null)
        {
            throw new InvalidMeterReadingException(
                meterType,
                "ต้องระบุค่ามิเตอร์ก่อนหน้าและค่าปัจจุบัน");
        }

        if (currentReading.Value < previousReading.Value)
        {
            throw new InvalidMeterReadingException(
                meterType,
                "ค่าปัจจุบันต้องไม่น้อยกว่าค่าก่อนหน้า");
        }

        return (previousReading.Value, currentReading.Value);
    }
}
