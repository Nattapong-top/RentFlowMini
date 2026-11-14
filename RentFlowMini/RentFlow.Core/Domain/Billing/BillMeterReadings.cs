using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Domain.Billing;

public sealed record BillMeterReadings(
    MeterReading PreviousWater,
    MeterReading CurrentWater,
    MeterReading PreviousElectricity,
    MeterReading CurrentElectricity);
