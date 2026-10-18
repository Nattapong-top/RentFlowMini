using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Application.Billing;

public sealed record CreateMonthlyBillRequest(
    RoomId RoomId,
    BillingPeriod BillingPeriod,
    decimal MonthlyRent,
    MeterReading? PreviousWaterMeter,
    MeterReading? CurrentWaterMeter,
    MeterReading? PreviousElectricityMeter,
    MeterReading? CurrentElectricityMeter,
    decimal WaterRate,
    decimal ElectricityRate,
    bool ChargeMotorcycleParking = false,
    decimal MotorcycleParkingRate = 0m,
    bool ChargeCarParking = false,
    decimal CarParkingRate = 0m,
    bool IsCableTvExempt = false,
    decimal CableTvRate = 0m);
