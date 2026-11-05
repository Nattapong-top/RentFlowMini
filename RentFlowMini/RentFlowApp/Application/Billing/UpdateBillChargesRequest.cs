using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Application.Billing;

public sealed record UpdateBillChargesRequest(
    BillId BillId,
    bool ChargeMotorcycleParking,
    decimal MotorcycleParkingRate,
    bool ChargeCarParking,
    decimal CarParkingRate,
    bool IsCableTvExempt,
    decimal CableTvRate);
