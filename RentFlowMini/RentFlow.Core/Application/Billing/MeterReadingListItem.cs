using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Application.Billing;

public sealed record MeterReadingListItem(
    RoomId RoomId,
    int Sequence,
    string RoomNumber,
    BillingPeriod BillingPeriod,
    decimal PreviousWater,
    decimal CurrentWater,
    decimal WaterUsed,
    decimal PreviousElectricity,
    decimal CurrentElectricity,
    decimal ElectricityUsed);
