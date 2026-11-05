using RentFlowApp.Application.Billing;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Infrastructure.Settings;

namespace RentFlowApp.Tests.Infrastructure.Settings;

public sealed class JsonFileSettingsRepositoryTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"RentFlowTests-{Guid.NewGuid():N}");

    private string FilePath => Path.Combine(_directory, "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void Constructor_WhenFileDoesNotExist_UsesDefaultsWithoutPropertyDetails()
    {
        var repository = new JsonFileSettingsRepository(FilePath);

        Assert.False(repository.LoadFailed);
        Assert.Equal(19m, ((IBillingSettingsRepository)repository).Get().WaterRate.Amount);
        Assert.False(((IInvoiceSettingsRepository)repository).Get().IsConfigured);
        Assert.False(File.Exists(FilePath));
    }

    [Fact]
    public void Save_KeepsBillingAndInvoiceSettingsAfterReopening()
    {
        var repository = new JsonFileSettingsRepository(FilePath);

        ((IBillingSettingsRepository)repository).Save(BillingSettings.Create(25m, 9m, 150m, 600m, 70m));
        ((IInvoiceSettingsRepository)repository).Save(InvoiceSettings.Create("หอทดสอบ", "ที่อยู่ทดสอบ", "ธนาคารทดสอบ"));

        var reopened = new JsonFileSettingsRepository(FilePath);
        var billing = ((IBillingSettingsRepository)reopened).Get();
        var invoice = ((IInvoiceSettingsRepository)reopened).Get();

        Assert.False(reopened.LoadFailed);
        Assert.Equal(25m, billing.WaterRate.Amount);
        Assert.Equal(9m, billing.ElectricityRate.Amount);
        Assert.Equal(150m, billing.MotorcycleParkingRate.Amount);
        Assert.Equal(600m, billing.CarParkingRate.Amount);
        Assert.Equal(70m, billing.CableTvRate.Amount);
        Assert.Equal("หอทดสอบ", invoice.PropertyName);
        Assert.Equal("ที่อยู่ทดสอบ", invoice.Address);
        Assert.Equal("ธนาคารทดสอบ", invoice.BankAccountDetails);
    }

    [Fact]
    public void Save_WritesReadableThaiText()
    {
        var repository = new JsonFileSettingsRepository(FilePath);

        ((IInvoiceSettingsRepository)repository).Save(InvoiceSettings.Create("หอทดสอบ", "ที่อยู่ทดสอบ", null));

        Assert.Contains("หอทดสอบ", File.ReadAllText(FilePath));
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("null")]
    [InlineData("{ \"WaterRate\": 19 }")]
    [InlineData("{ \"WaterRate\": -1, \"ElectricityRate\": 8, \"MotorcycleParkingRate\": 100, \"CarParkingRate\": 500, \"CableTvRate\": 60 }")]
    public void Constructor_WhenFileIsInvalid_ReportsFailureAndUsesDefaults(string content)
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(FilePath, content);

        var repository = new JsonFileSettingsRepository(FilePath);

        Assert.True(repository.LoadFailed);
        Assert.Equal(19m, ((IBillingSettingsRepository)repository).Get().WaterRate.Amount);
        Assert.Equal(content, File.ReadAllText(FilePath));
    }

    [Fact]
    public void Save_AfterInvalidFile_KeepsBackupOfOriginalContent()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(FilePath, "{ not json");
        var repository = new JsonFileSettingsRepository(FilePath);

        ((IInvoiceSettingsRepository)repository).Save(InvoiceSettings.Create("หอทดสอบ", "ที่อยู่ทดสอบ", null));

        Assert.Equal("{ not json", File.ReadAllText($"{FilePath}.bad"));
        Assert.False(repository.LoadFailed);
        Assert.False(new JsonFileSettingsRepository(FilePath).LoadFailed);
    }

    [Fact]
    public void Save_WhenFileCannotBeWritten_ThrowsThaiMessageAndKeepsCurrentSettings()
    {
        Directory.CreateDirectory(_directory);
        var repository = new JsonFileSettingsRepository(FilePath);

        // โฟลเดอร์ชื่อเดียวกับไฟล์ชั่วคราวทำให้เขียนไฟล์ไม่ได้
        Directory.CreateDirectory($"{FilePath}.tmp");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ((IInvoiceSettingsRepository)repository).Save(InvoiceSettings.Create("หอทดสอบ", "ที่อยู่ทดสอบ", null)));

        Assert.Contains("ไม่สามารถบันทึกการตั้งค่า", exception.Message);
        Assert.False(((IInvoiceSettingsRepository)repository).Get().IsConfigured);
    }
}
