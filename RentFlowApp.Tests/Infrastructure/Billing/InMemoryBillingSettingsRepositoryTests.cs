using RentFlowApp.Domain.Billing;
using RentFlowApp.Infrastructure.Billing;

namespace RentFlowApp.Tests.Infrastructure.Billing;

public class InMemoryBillingSettingsRepositoryTests
{
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
