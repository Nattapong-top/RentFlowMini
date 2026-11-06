using System.Globalization;
using Microsoft.Data.Sqlite;
using RentFlowApp.Application.Billing;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Infrastructure.Sqlite;

public sealed class SqliteMonthlyMeterReadingRepository : IMonthlyMeterReadingRepository
{
    private const int SqlitePrimaryKeyViolation = 1555;

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

        try
        {
            command.ExecuteNonQuery();
        }
        catch (SqliteException ex) when (ex.SqliteExtendedErrorCode == SqlitePrimaryKeyViolation)
        {
            throw new InvalidOperationException("มีข้อมูลมิเตอร์ของห้องและรอบบิลนี้อยู่แล้ว", ex);
        }
    }

    public void Update(MonthlyMeterReading reading)
    {
        ArgumentNullException.ThrowIfNull(reading);

        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        // ถ้ายังไม่มีรายการจะเพิ่มให้ ตามพฤติกรรมเดิมของ InMemoryMonthlyMeterReadingRepository
        command.CommandText = $"""
            {InsertSql}
            ON CONFLICT (RoomId, BillingPeriod) DO UPDATE SET
                PreviousWaterMeter = excluded.PreviousWaterMeter,
                CurrentWaterMeter = excluded.CurrentWaterMeter,
                PreviousElectricityMeter = excluded.PreviousElectricityMeter,
                CurrentElectricityMeter = excluded.CurrentElectricityMeter;
            """;
        AddReadingParameters(command, reading);
        command.ExecuteNonQuery();
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
        return Get(roomId, billingPeriod) is not null;
    }

    public IEnumerable<MonthlyMeterReading> GetAll()
    {
        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"{SelectColumns};";

        var readings = new List<MonthlyMeterReading>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            readings.Add(ReadReading(reader));
        }

        return readings.AsReadOnly();
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
