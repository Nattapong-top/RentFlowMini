using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Domain.Billing;

public sealed class BillingSettings
{
    private BillingSettings(
        Money waterRate,
        Money electricityRate,
        Money motorcycleParkingRate,
        Money carParkingRate,
        Money cableTvRate)
    {
        WaterRate = waterRate;
        ElectricityRate = electricityRate;
        MotorcycleParkingRate = motorcycleParkingRate;
        CarParkingRate = carParkingRate;
        CableTvRate = cableTvRate;
    }

    public Money WaterRate { get; }

    public Money ElectricityRate { get; }

    public Money MotorcycleParkingRate { get; }

    public Money CarParkingRate { get; }

    public Money CableTvRate { get; }

    public static BillingSettings Create(
        decimal waterRate,
        decimal electricityRate,
        decimal motorcycleParkingRate,
        decimal carParkingRate,
        decimal cableTvRate)
    {
        return new BillingSettings(
            Money.Create(waterRate),
            Money.Create(electricityRate),
            Money.Create(motorcycleParkingRate),
            Money.Create(carParkingRate),
            Money.Create(cableTvRate));
    }
}
