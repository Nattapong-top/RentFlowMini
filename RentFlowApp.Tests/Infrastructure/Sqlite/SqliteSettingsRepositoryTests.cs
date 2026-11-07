using Microsoft.Data.Sqlite;
using RentFlowApp.Application.Billing;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Infrastructure.Sqlite;

namespace RentFlowApp.Tests.Infrastructure.Sqlite;

public sealed class SqliteSettingsRepositoryTests : IDisposable
{
    private const string ValidSettingsFile = """
        {
          "WaterRate": 19.25,
          "ElectricityRate": 8.5,
          "MotorcycleParkingRate": 150.75,
          "CarParkingRate": 600.1,
          "CableTvRate": 70.05,
          "PropertyName": "หอทดสอบ",
          "Address": "ที่อยู่ทดสอบ",
          "BankAccountDetails": "บัญชีทดสอบ"
        }
        """;

    private readonly string _folder =
        Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "RentFlowTests", Guid.NewGuid().ToString())).FullName;

    private readonly SqliteDatabase _database;

    public SqliteSettingsRepositoryTests()
    {
        _database = new SqliteDatabase(Path.Combine(_folder, "rentflow.db"));
        _database.Initialize();
    }

    private string SettingsFilePath => Path.Combine(_folder, "settings.json");

    public void Dispose()
    {
        // pool ยังถือไฟล์ไว้แม้ปิด connection แล้ว ถ้าไม่ล้างจะลบไฟล์เทสต์ไม่ได้
        SqliteConnection.ClearAllPools();
        Directory.Delete(_folder, recursive: true);
    }

    [Fact]
    public void Get_WithoutSavedSettings_ReturnsDefaultsAndNoPropertyDetails()
    {
        var repository = new SqliteSettingsRepository(_database);

        AssertDefaults(repository);
    }

    [Fact]
    public void Save_KeepsEveryValueForNewRepository()
    {
        var repository = new SqliteSettingsRepository(_database);

        ((IBillingSettingsRepository)repository).Save(BillingSettings.Create(25.25m, 9.5m, 150.75m, 600.1m, 70.05m));
        ((IInvoiceSettingsRepository)repository).Save(InvoiceSettings.Create("หอทดสอบ", "ที่อยู่ทดสอบ", "บัญชีทดสอบ"));

        var reopened = new SqliteSettingsRepository(_database);
        var billing = ((IBillingSettingsRepository)reopened).Get();
        var invoice = ((IInvoiceSettingsRepository)reopened).Get();

        Assert.Equal(25.25m, billing.WaterRate.Amount);
        Assert.Equal(9.5m, billing.ElectricityRate.Amount);
        Assert.Equal(150.75m, billing.MotorcycleParkingRate.Amount);
        Assert.Equal(600.1m, billing.CarParkingRate.Amount);
        Assert.Equal(70.05m, billing.CableTvRate.Amount);
        Assert.Equal(InvoiceSettings.Create("หอทดสอบ", "ที่อยู่ทดสอบ", "บัญชีทดสอบ"), invoice);
    }

    [Fact]
    public void Save_Twice_KeepsLatestValues()
    {
        var repository = new SqliteSettingsRepository(_database);

        ((IBillingSettingsRepository)repository).Save(BillingSettings.Create(1m, 1m, 1m, 1m, 1m));
        ((IBillingSettingsRepository)repository).Save(BillingSettings.Create(2m, 2m, 2m, 2m, 2m));
        ((IInvoiceSettingsRepository)repository).Save(InvoiceSettings.Create("หอเดิม", "ที่อยู่เดิม", null));
        ((IInvoiceSettingsRepository)repository).Save(InvoiceSettings.Create("หอใหม่", "ที่อยู่ใหม่", null));

        Assert.Equal(2m, ((IBillingSettingsRepository)repository).Get().WaterRate.Amount);
        Assert.Equal("หอใหม่", ((IInvoiceSettingsRepository)repository).Get().PropertyName);
    }

    [Fact]
    public void Save_WithEmptyInvoiceSettings_ReadsBackAsEmpty()
    {
        var repository = new SqliteSettingsRepository(_database);

        ((IInvoiceSettingsRepository)repository).Save(InvoiceSettings.Empty);

        Assert.Equal(InvoiceSettings.Empty, ((IInvoiceSettingsRepository)repository).Get());
    }

    [Fact]
    public void Save_WhenDatabaseFails_ThrowsThaiMessage()
    {
        var repository = new SqliteSettingsRepository(_database);
        DropTable("BillingSettings");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ((IBillingSettingsRepository)repository).Save(BillingSettings.Create(1m, 1m, 1m, 1m, 1m)));

        Assert.Equal("ไม่สามารถบันทึกการตั้งค่าได้ กรุณาปิดแล้วเปิดโปรแกรมใหม่อีกครั้ง", exception.Message);
    }

    [Fact]
    public void TryImportOnce_CopiesEveryValueAndKeepsFile()
    {
        File.WriteAllText(SettingsFilePath, ValidSettingsFile);
        byte[] fileBefore = File.ReadAllBytes(SettingsFilePath);
        var repository = new SqliteSettingsRepository(_database);

        bool succeeded = repository.TryImportOnce(SettingsFilePath);

        var billing = ((IBillingSettingsRepository)repository).Get();
        Assert.True(succeeded);
        Assert.Equal(19.25m, billing.WaterRate.Amount);
        Assert.Equal(8.5m, billing.ElectricityRate.Amount);
        Assert.Equal(150.75m, billing.MotorcycleParkingRate.Amount);
        Assert.Equal(600.1m, billing.CarParkingRate.Amount);
        Assert.Equal(70.05m, billing.CableTvRate.Amount);
        Assert.Equal(
            InvoiceSettings.Create("หอทดสอบ", "ที่อยู่ทดสอบ", "บัญชีทดสอบ"),
            ((IInvoiceSettingsRepository)repository).Get());
        Assert.Equal(fileBefore, File.ReadAllBytes(SettingsFilePath));
    }

    [Fact]
    public void TryImportOnce_AfterImporting_DoesNotReadFileAgain()
    {
        File.WriteAllText(SettingsFilePath, ValidSettingsFile);
        var repository = new SqliteSettingsRepository(_database);
        repository.TryImportOnce(SettingsFilePath);
        File.WriteAllText(SettingsFilePath, ValidSettingsFile.Replace("19.25", "99"));

        repository.TryImportOnce(SettingsFilePath);

        Assert.Equal(19.25m, ((IBillingSettingsRepository)repository).Get().WaterRate.Amount);
    }

    [Fact]
    public void TryImportOnce_AfterSavingOnlyInvoiceSettings_KeepsSavedValuesAndSkipsFile()
    {
        File.WriteAllText(SettingsFilePath, ValidSettingsFile);
        var repository = new SqliteSettingsRepository(_database);
        ((IInvoiceSettingsRepository)repository).Save(InvoiceSettings.Create("หอใหม่", "ที่อยู่ใหม่", null));

        bool succeeded = repository.TryImportOnce(SettingsFilePath);

        Assert.True(succeeded);
        Assert.Equal("หอใหม่", ((IInvoiceSettingsRepository)repository).Get().PropertyName);
        Assert.Equal(19m, ((IBillingSettingsRepository)repository).Get().WaterRate.Amount);
    }

    [Fact]
    public void TryImportOnce_WithoutPropertyDetails_ImportsRatesOnly()
    {
        File.WriteAllText(
            SettingsFilePath,
            """{ "WaterRate": 20, "ElectricityRate": 9, "MotorcycleParkingRate": 100, "CarParkingRate": 500, "CableTvRate": 60 }""");
        var repository = new SqliteSettingsRepository(_database);

        bool succeeded = repository.TryImportOnce(SettingsFilePath);

        Assert.True(succeeded);
        Assert.Equal(20m, ((IBillingSettingsRepository)repository).Get().WaterRate.Amount);
        Assert.False(((IInvoiceSettingsRepository)repository).Get().IsConfigured);
    }

    [Fact]
    public void TryImportOnce_WhenFileDoesNotExist_SucceedsWithDefaults()
    {
        var repository = new SqliteSettingsRepository(_database);

        bool succeeded = repository.TryImportOnce(SettingsFilePath);

        Assert.True(succeeded);
        AssertDefaults(repository);
    }

    [Fact]
    public void TryImportOnce_WhenFileIsUnreadable_FailsAndKeepsFileAndDefaults()
    {
        File.WriteAllText(SettingsFilePath, "{ not json");
        var repository = new SqliteSettingsRepository(_database);

        bool succeeded = repository.TryImportOnce(SettingsFilePath);

        Assert.False(succeeded);
        AssertDefaults(repository);
        Assert.Equal("{ not json", File.ReadAllText(SettingsFilePath));
    }

    [Fact]
    public void TryImportOnce_WhenFileIsUnreadable_StopsFailingAfterSaving()
    {
        File.WriteAllText(SettingsFilePath, "{ not json");
        var repository = new SqliteSettingsRepository(_database);
        Assert.False(repository.TryImportOnce(SettingsFilePath));

        ((IBillingSettingsRepository)repository).Save(BillingSettings.Create(2m, 2m, 2m, 2m, 2m));

        Assert.True(repository.TryImportOnce(SettingsFilePath));
    }

    private static void AssertDefaults(SqliteSettingsRepository repository)
    {
        var billing = ((IBillingSettingsRepository)repository).Get();

        Assert.Equal(19m, billing.WaterRate.Amount);
        Assert.Equal(8m, billing.ElectricityRate.Amount);
        Assert.Equal(100m, billing.MotorcycleParkingRate.Amount);
        Assert.Equal(500m, billing.CarParkingRate.Amount);
        Assert.Equal(60m, billing.CableTvRate.Amount);
        Assert.Equal(InvoiceSettings.Empty, ((IInvoiceSettingsRepository)repository).Get());
    }

    private void DropTable(string tableName)
    {
        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"DROP TABLE {tableName};";
        command.ExecuteNonQuery();
    }
}
