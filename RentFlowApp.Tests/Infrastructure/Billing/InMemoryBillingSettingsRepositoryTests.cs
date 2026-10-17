using RentFlowApp.Domain.Billing;
using RentFlowApp.Infrastructure.Billing;

namespace RentFlowApp.Tests.Infrastructure.Billing;

public class InMemoryBillingSettingsRepositoryTests
{
    [Fact]
    public void Constructor_ProvidesDefaultSeedData()
    {
        var repository = new InMemoryBillingSettingsRepository();
        var settings = repository.Get();

        Assert.Equal(19.00m, settings.WaterRate.Amount);
        Assert.Equal(8.00m, settings.ElectricityRate.Amount);
        Assert.Equal(100.00m, settings.MotorcycleParkingRate.Amount);
        Assert.Equal(500.00m, settings.CarParkingRate.Amount);
        Assert.Equal(60.00m, settings.CableTvRate.Amount);
    }

    [Fact]
    public void SaveThenGet_ReturnsLatestSettings()
    {
        var repository = new InMemoryBillingSettingsRepository();
        var settings = BillingSettings.Create(30m, 4m, 100m, 300m, 500m);

        repository.Save(settings);

        Assert.Same(settings, repository.Get());
    }

    [Fact]
    public void Save_WithNullSettings_ThrowsArgumentNullException()
    {
        var repository = new InMemoryBillingSettingsRepository();

        Assert.Throws<ArgumentNullException>(() => repository.Save(null!));
    }
}
