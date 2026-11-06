using System.Globalization;
using Microsoft.Data.Sqlite;
using RentFlowApp.Application.Billing;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Infrastructure.Sqlite;

public sealed class SqliteMonthlyMeterReadingRepository : IMonthlyMeterReadingRepository
{
    private const string InsertSql = """
        INSERT INTO MonthlyMeterReadings (
            RoomId, BillingPeriod,
            PreviousWaterMeter, CurrentWaterMeter,
            PreviousElectricityMeter, CurrentElectricityMeter)
        VALUES (
            $roomId, $billingPeriod,
            $previousWater, $currentWater,
            $previousElectricity, $currentElectricity)
        """;

    private const string SelectColumns = """
        SELECT RoomId, BillingPeriod,
               PreviousWaterMeter, CurrentWaterMeter,
               PreviousElectricityMeter, CurrentElectricityMeter
        FROM MonthlyMeterReadings
        """;

    private readonly SqliteDatabase _database;

    public SqliteMonthlyMeterReadingRepository(SqliteDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);
        _database = database;
    }

    public void Add(MonthlyMeterReading reading)
    {
        ArgumentNullException.ThrowIfNull(reading);

        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"{InsertSql};";
        AddReadingParameters(command, reading);

        command.ExecuteNonQuery();
    }

    public void Update(MonthlyMeterReading reading)
    {
        throw new NotImplementedException();
    }

    public MonthlyMeterReading? Get(RoomId roomId, BillingPeriod billingPeriod)
    {
        ArgumentNullException.ThrowIfNull(roomId);
        ArgumentNullException.ThrowIfNull(billingPeriod);

        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"{SelectColumns} WHERE RoomId = $roomId AND BillingPeriod = $billingPeriod;";
        command.Parameters.AddWithValue("$roomId", roomId.Value.ToString());
        command.Parameters.AddWithValue("$billingPeriod", billingPeriod.Value);

        using var reader = command.ExecuteReader();

        return reader.Read() ? ReadReading(reader) : null;
    }

    public bool Exists(RoomId roomId, BillingPeriod billingPeriod)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<MonthlyMeterReading> GetAll()
    {
        throw new NotImplementedException();
    }

    private static void AddReadingParameters(SqliteCommand command, MonthlyMeterReading reading)
    {
        command.Parameters.AddWithValue("$roomId", reading.RoomId.Value.ToString());
        command.Parameters.AddWithValue("$billingPeriod", reading.BillingPeriod.Value);
        command.Parameters.AddWithValue("$previousWater", FormatMeter(reading.PreviousWaterMeter));
        command.Parameters.AddWithValue("$currentWater", FormatMeter(reading.CurrentWaterMeter));
        command.Parameters.AddWithValue("$previousElectricity", FormatMeter(reading.PreviousElectricityMeter));
        command.Parameters.AddWithValue("$currentElectricity", FormatMeter(reading.CurrentElectricityMeter));
    }

    // เก็บเป็นข้อความ เพราะ SQLite เก็บทศนิยมแบบ REAL ซึ่งทำให้ค่าเพี้ยนและยอดเงินผิดได้
    private static string FormatMeter(MeterReading meter)
    {
        return meter.Value.ToString(CultureInfo.InvariantCulture);
    }

    private static MonthlyMeterReading ReadReading(SqliteDataReader reader)
    {
        return MonthlyMeterReading.Create(
            RoomId.From(Guid.Parse(reader.GetString(0))),
            BillingPeriod.Create(reader.GetString(1)),
            ReadMeter(reader, 2),
            ReadMeter(reader, 3),
            ReadMeter(reader, 4),
            ReadMeter(reader, 5));
    }

    private static MeterReading ReadMeter(SqliteDataReader reader, int column)
    {
        return MeterReading.Create(decimal.Parse(reader.GetString(column), CultureInfo.InvariantCulture));
    }
}
