namespace RentFlowApp.Application.Billing;

public sealed record BillingRateValues(
    decimal WaterRate,
    decimal ElectricityRate,
    decimal MotorcycleParkingRate,
    decimal CarParkingRate,
    decimal CableTvRate);
