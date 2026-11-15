using RentFlowApp.Application.Billing;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Infrastructure.Billing;

namespace RentFlowApp.Tests.Application.Billing;

public class UpdateBillingSettingsServiceTests
{
    [Fact]
    public void Update_WithValidMoney_SavesAndReturnsSettings()
    {
        var repository = new InMemoryBillingSettingsRepository();
        var service = new UpdateBillingSettingsService(repository);

        var result = service.Update(
            Money.Create(20m),
            Money.Create(9m),
            Money.Create(150m),
            Money.Create(600m),
            Money.Create(70m));

        Assert.Equal(20m, result.WaterRate.Amount);
        Assert.Equal(9m, result.ElectricityRate.Amount);
        Assert.Equal(150m, result.MotorcycleParkingRate.Amount);
        Assert.Equal(600m, result.CarParkingRate.Amount);
        Assert.Equal(70m, result.CableTvRate.Amount);
        Assert.Same(result, repository.Get());
    }

    [Fact]
    public void Update_WithNullMoney_ThrowsArgumentNullException()
    {
        var repository = new InMemoryBillingSettingsRepository();
        var service = new UpdateBillingSettingsService(repository);

        Assert.Throws<ArgumentNullException>(() =>
            service.Update(null!, Money.Create(8m), Money.Create(100m), Money.Create(500m), Money.Create(60m)));
    }

    [Fact]
    public void Update_WithRateValues_SavesAllRates()
    {
        var repository = new InMemoryBillingSettingsRepository();
        var service = new UpdateBillingSettingsService(repository);

        var result = service.Update(new BillingRateValues(20m, 9m, 150m, 600m, 70m));

        var saved = repository.Get();
        Assert.Equal(result, saved);
        Assert.Equal(20m, saved.WaterRate.Amount);
        Assert.Equal(9m, saved.ElectricityRate.Amount);
        Assert.Equal(150m, saved.MotorcycleParkingRate.Amount);
        Assert.Equal(600m, saved.CarParkingRate.Amount);
        Assert.Equal(70m, saved.CableTvRate.Amount);
    }

    [Fact]
    public void GetCurrent_ReturnsLatestSavedSettings()
    {
        var repository = new InMemoryBillingSettingsRepository();
        var service = new UpdateBillingSettingsService(repository);
        var saved = service.Update(
            Money.Create(20m),
            Money.Create(9m),
            Money.Create(150m),
            Money.Create(600m),
            Money.Create(70m));

        Assert.Equal(saved, service.GetCurrent());
    }
}
