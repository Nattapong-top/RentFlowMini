using RentFlowApp.Domain.Billing;

namespace RentFlowApp.Application.Billing;

public sealed class CreateMonthlyBillService
{
    private readonly IBillRepository _repository;

    public CreateMonthlyBillService(IBillRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public Bill Create(CreateMonthlyBillRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var calculation = BillingCalculator.Calculate(
            monthlyRent: request.MonthlyRent,
            previousWaterMeter: request.PreviousWaterMeter,
            currentWaterMeter: request.CurrentWaterMeter,
            previousElectricityMeter: request.PreviousElectricityMeter,
            currentElectricityMeter: request.CurrentElectricityMeter,
            waterRate: request.WaterRate,
            electricityRate: request.ElectricityRate,
            chargeMotorcycleParking: request.ChargeMotorcycleParking,
            motorcycleParkingRate: request.MotorcycleParkingRate,
            chargeCarParking: request.ChargeCarParking,
            carParkingRate: request.CarParkingRate,
            isCableTvExempt: request.IsCableTvExempt,
            cableTvRate: request.CableTvRate);

        var bill = Bill.Create(
            request.RoomId,
            request.BillingPeriod,
            calculation);

        _repository.Add(bill);

        return bill;
    }
}
