using System.Globalization;
using Microsoft.Data.Sqlite;
using RentFlowApp.Application.Billing;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Infrastructure.Billing;
using RentFlowApp.Infrastructure.Settings;

namespace RentFlowApp.Infrastructure.Sqlite;

public sealed class SqliteSettingsRepository : IBillingSettingsRepository, IInvoiceSettingsRepository
{
    private const string SaveFailedMessage = "ไม่สามารถบันทึกการตั้งค่าได้ กรุณาปิดแล้วเปิดโปรแกรมใหม่อีกครั้ง";

    private readonly SqliteDatabase _database;

    public SqliteSettingsRepository(SqliteDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);
        _database = database;
    }

    BillingSettings IBillingSettingsRepository.Get()
    {
        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT WaterRate, ElectricityRate, MotorcycleParkingRate, CarParkingRate, CableTvRate
            FROM BillingSettings;
            """;

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return new InMemoryBillingSettingsRepository().Get();
        }

        return BillingSettings.Create(
            ReadDecimal(reader, 0),
            ReadDecimal(reader, 1),
            ReadDecimal(reader, 2),
            ReadDecimal(reader, 3),
            ReadDecimal(reader, 4));
    }

    void IBillingSettingsRepository.Save(BillingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        try
        {
            using var connection = _database.OpenConnection();
            WriteBillingSettings(connection, null, settings);
        }
        catch (SqliteException ex)
        {
            throw new InvalidOperationException(SaveFailedMessage, ex);
        }
    }

    InvoiceSettings IInvoiceSettingsRepository.Get()
    {
        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT PropertyName, Address, BankAccountDetails FROM InvoiceSettings;";

        using var reader = command.ExecuteReader();
        if (!reader.Read() || reader.GetString(0).Length == 0)
        {
            return InvoiceSettings.Empty;
        }

        return InvoiceSettings.Create(reader.GetString(0), reader.GetString(1), reader.GetString(2));
    }

    void IInvoiceSettingsRepository.Save(InvoiceSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        try
        {
            using var connection = _database.OpenConnection();
            WriteInvoiceSettings(connection, null, settings);
        }
        catch (SqliteException ex)
        {
            throw new InvalidOperationException(SaveFailedMessage, ex);
        }
    }

    // คืน false เมื่อไฟล์อ่านไม่ได้ เพื่อให้แอปแจ้งเตือนได้ทุกครั้งที่เปิดจนกว่าจะบันทึกการตั้งค่าใหม่
    public bool TryImportOnce(string settingsFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsFilePath);

        using var connection = _database.OpenConnection();

        // มีค่าในฐานข้อมูลแล้วถือว่าใหม่กว่าไฟล์เสมอ จึงไม่อ่านไฟล์ซ้ำเพื่อไม่ให้ทับค่าที่แก้ไว้
        if (HasSavedSettings(connection) || !File.Exists(settingsFilePath))
        {
            return true;
        }

        var settingsFile = new JsonFileSettingsRepository(settingsFilePath);
        if (settingsFile.LoadFailed)
        {
            return false;
        }

        // ทำในธุรกรรมเดียว เพราะถ้านำเข้าได้ครึ่งเดียว ครั้งต่อไปจะไม่นำเข้าส่วนที่เหลืออีก
        using var transaction = connection.BeginTransaction();
        WriteBillingSettings(connection, transaction, ((IBillingSettingsRepository)settingsFile).Get());

        var invoiceSettings = ((IInvoiceSettingsRepository)settingsFile).Get();
        if (invoiceSettings.IsConfigured)
        {
            WriteInvoiceSettings(connection, transaction, invoiceSettings);
        }

        transaction.Commit();

        return true;
    }

    private static bool HasSavedSettings(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT EXISTS (SELECT 1 FROM BillingSettings)
                OR EXISTS (SELECT 1 FROM InvoiceSettings);
            """;

        return (long)command.ExecuteScalar()! == 1;
    }

    private static void WriteBillingSettings(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        BillingSettings settings)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO BillingSettings (
                Id, WaterRate, ElectricityRate, MotorcycleParkingRate, CarParkingRate, CableTvRate)
            VALUES (1, $water, $electricity, $motorcycleParking, $carParking, $cableTv)
            ON CONFLICT (Id) DO UPDATE SET
                WaterRate = excluded.WaterRate,
                ElectricityRate = excluded.ElectricityRate,
                MotorcycleParkingRate = excluded.MotorcycleParkingRate,
                CarParkingRate = excluded.CarParkingRate,
                CableTvRate = excluded.CableTvRate;
            """;
        command.Parameters.AddWithValue("$water", FormatDecimal(settings.WaterRate.Amount));
        command.Parameters.AddWithValue("$electricity", FormatDecimal(settings.ElectricityRate.Amount));
        command.Parameters.AddWithValue("$motorcycleParking", FormatDecimal(settings.MotorcycleParkingRate.Amount));
        command.Parameters.AddWithValue("$carParking", FormatDecimal(settings.CarParkingRate.Amount));
        command.Parameters.AddWithValue("$cableTv", FormatDecimal(settings.CableTvRate.Amount));
        command.ExecuteNonQuery();
    }

    private static void WriteInvoiceSettings(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        InvoiceSettings settings)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO InvoiceSettings (Id, PropertyName, Address, BankAccountDetails)
            VALUES (1, $propertyName, $address, $bankAccountDetails)
            ON CONFLICT (Id) DO UPDATE SET
                PropertyName = excluded.PropertyName,
                Address = excluded.Address,
                BankAccountDetails = excluded.BankAccountDetails;
            """;
        command.Parameters.AddWithValue("$propertyName", settings.PropertyName);
        command.Parameters.AddWithValue("$address", settings.Address);
        command.Parameters.AddWithValue("$bankAccountDetails", settings.BankAccountDetails);
        command.ExecuteNonQuery();
    }

    // เก็บเงินเป็นข้อความ เพราะ SQLite เก็บทศนิยมแบบ REAL ซึ่งทำให้เศษสตางค์เพี้ยนได้
    private static string FormatDecimal(decimal value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static decimal ReadDecimal(SqliteDataReader reader, int column)
    {
        return decimal.Parse(reader.GetString(column), CultureInfo.InvariantCulture);
    }
}
