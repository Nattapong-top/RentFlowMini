using RentFlowApp.Domain.Billing;

namespace RentFlowApp.Application.Billing;

public sealed class CreateMonthlyBillService
{
    public Bill Create(CreateMonthlyBillRequest request)
    {
        var calculation = BillingCalculator.Calculate(
            monthlyRent: request.MonthlyRent,
            previousWaterMeter: request.PreviousWaterMeter,
            currentWaterMeter: request.CurrentWaterMeter,
            previousElectricityMeter: request.PreviousElectricityMeter,
            currentElectricityMeter: request.CurrentElectricityMeter,
            waterRate: request.WaterRate,
            electricityRate: request.ElectricityRate);

        return Bill.Create(
            request.RoomId,
            request.BillingPeriod,
            calculation);
    }
}
