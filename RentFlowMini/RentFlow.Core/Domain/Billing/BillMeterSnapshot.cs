using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Domain.Billing;

public sealed record BillMeterSnapshot(
    MeterReading PreviousWater,
    MeterReading CurrentWater,
    MeterReading PreviousElectricity,
    MeterReading CurrentElectricity,
    decimal WaterRate,
    decimal ElectricityRate)
{
    public decimal WaterUsage => CurrentWater.Value - PreviousWater.Value;

    public decimal ElectricityUsage => CurrentElectricity.Value - PreviousElectricity.Value;
}
