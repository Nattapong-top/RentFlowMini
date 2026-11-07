using System.Globalization;
using Microsoft.Data.Sqlite;
using RentFlowApp.Application.Billing;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Infrastructure.Sqlite;

public sealed class SqliteBillRepository : IBillRepository
{
    private const int SqliteUniqueViolation = 2067;
    private const string DateFormat = "yyyy-MM-dd";

    private const string SelectColumns = """
        SELECT Id, RoomId, BillingPeriod, InvoiceNumber, CreatedAt, DueDate,
               RentAmount, WaterAmount, ElectricityAmount,
               MotorcycleParkingAmount, CarParkingAmount, CableTvAmount, TotalAmount,
               IsCableTvExempt,
               PreviousWaterMeter, CurrentWaterMeter,
               PreviousElectricityMeter, CurrentElectricityMeter,
               WaterRate, ElectricityRate,
               TenantId, TenantFullName,
               PaidOn, PaymentMethod
        FROM Bills
        """;

    private readonly SqliteDatabase _database;

    public SqliteBillRepository(SqliteDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);
        _database = database;
    }

    public void Add(Bill bill)
    {
        ArgumentNullException.ThrowIfNull(bill);

        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Bills (
                Id, RoomId, BillingPeriod, InvoiceNumber, CreatedAt, DueDate,
                RentAmount, WaterAmount, ElectricityAmount,
                MotorcycleParkingAmount, CarParkingAmount, CableTvAmount, TotalAmount,
                IsCableTvExempt,
                PreviousWaterMeter, CurrentWaterMeter,
                PreviousElectricityMeter, CurrentElectricityMeter,
                WaterRate, ElectricityRate,
                TenantId, TenantFullName,
                PaidOn, PaymentMethod)
            VALUES (
                $id, $roomId, $billingPeriod, $invoiceNumber, $createdAt, $dueDate,
                $rent, $water, $electricity,
                $motorcycleParking, $carParking, $cableTv, $total,
                $isCableTvExempt,
                $previousWater, $currentWater,
                $previousElectricity, $currentElectricity,
                $waterRate, $electricityRate,
                $tenantId, $tenantFullName,
                $paidOn, $paymentMethod);
            """;
        AddBillParameters(command, bill);

        try
        {
            command.ExecuteNonQuery();
        }
        // ตารางนี้มีกฎห้ามซ้ำ 2 ข้อที่ใช้รหัสเดียวกัน จึงต้องดูชื่อคอลัมน์เพื่อไม่ให้แจ้งผิดเรื่อง
        catch (SqliteException ex) when (
            ex.SqliteExtendedErrorCode == SqliteUniqueViolation && ex.Message.Contains("Bills.RoomId"))
        {
            throw new InvalidOperationException("มีบิลของห้องและรอบบิลนี้อยู่แล้ว", ex);
        }
    }

    public void Update(Bill bill)
    {
        throw new NotImplementedException();
    }

    public bool Exists(RoomId roomId, BillingPeriod billingPeriod)
    {
        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT EXISTS (
                SELECT 1 FROM Bills
                WHERE RoomId = $roomId AND BillingPeriod = $billingPeriod);
            """;
        command.Parameters.AddWithValue("$roomId", roomId.Value.ToString());
        command.Parameters.AddWithValue("$billingPeriod", billingPeriod.Value);

        return (long)command.ExecuteScalar()! == 1;
    }

    public IReadOnlyCollection<Bill> GetAll()
    {
        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"{SelectColumns};";

        var bills = new List<Bill>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            bills.Add(ReadBill(reader));
        }

        return bills.AsReadOnly();
    }

    public Bill? Get(BillId id)
    {
        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"{SelectColumns} WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id.Value.ToString());

        using var reader = command.ExecuteReader();

        return reader.Read() ? ReadBill(reader) : null;
    }

    private static void AddBillParameters(SqliteCommand command, Bill bill)
    {
        command.Parameters.AddWithValue("$id", bill.Id.Value.ToString());
        command.Parameters.AddWithValue("$roomId", bill.RoomId.Value.ToString());
        command.Parameters.AddWithValue("$billingPeriod", bill.BillingPeriod.Value);
        command.Parameters.AddWithValue("$invoiceNumber", bill.InvoiceNumber);
        command.Parameters.AddWithValue("$createdAt", bill.CreatedAt.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$dueDate", bill.DueDate.ToString(DateFormat, CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$rent", FormatDecimal(bill.Rent.Amount));
        command.Parameters.AddWithValue("$water", FormatDecimal(bill.Water.Amount));
        command.Parameters.AddWithValue("$electricity", FormatDecimal(bill.Electricity.Amount));
        command.Parameters.AddWithValue("$motorcycleParking", FormatDecimal(bill.MotorcycleParking.Amount));
        command.Parameters.AddWithValue("$carParking", FormatDecimal(bill.CarParking.Amount));
        command.Parameters.AddWithValue("$cableTv", FormatDecimal(bill.CableTv.Amount));
        command.Parameters.AddWithValue("$total", FormatDecimal(bill.Total.Amount));
        command.Parameters.AddWithValue("$isCableTvExempt", bill.IsCableTvExempt ? 1 : 0);
        command.Parameters.AddWithValue("$previousWater", ToDbValue(FormatMeter(bill.Meters?.PreviousWater)));
        command.Parameters.AddWithValue("$currentWater", ToDbValue(FormatMeter(bill.Meters?.CurrentWater)));
        command.Parameters.AddWithValue("$previousElectricity", ToDbValue(FormatMeter(bill.Meters?.PreviousElectricity)));
        command.Parameters.AddWithValue("$currentElectricity", ToDbValue(FormatMeter(bill.Meters?.CurrentElectricity)));
        command.Parameters.AddWithValue("$waterRate", ToDbValue(bill.Meters is null ? null : FormatDecimal(bill.Meters.WaterRate)));
        command.Parameters.AddWithValue("$electricityRate", ToDbValue(bill.Meters is null ? null : FormatDecimal(bill.Meters.ElectricityRate)));
        command.Parameters.AddWithValue("$tenantId", ToDbValue(bill.Tenant?.TenantId.Value.ToString()));
        command.Parameters.AddWithValue("$tenantFullName", ToDbValue(bill.Tenant?.FullName));
        command.Parameters.AddWithValue(
            "$paidOn",
            ToDbValue(bill.Payment?.PaidOn.ToString(DateFormat, CultureInfo.InvariantCulture)));
        command.Parameters.AddWithValue("$paymentMethod", ToDbValue(bill.Payment?.Method.ToString()));
    }

    private static object ToDbValue(string? value)
    {
        return value is null ? DBNull.Value : value;
    }

    // เก็บเงินเป็นข้อความ เพราะ SQLite เก็บทศนิยมแบบ REAL ซึ่งทำให้เศษสตางค์เพี้ยนได้
    private static string FormatDecimal(decimal value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string? FormatMeter(MeterReading? meter)
    {
        return meter is null ? null : FormatDecimal(meter.Value);
    }

    private static Bill ReadBill(SqliteDataReader reader)
    {
        return Bill.Restore(
            BillId.From(Guid.Parse(reader.GetString(0))),
            RoomId.From(Guid.Parse(reader.GetString(1))),
            BillingPeriod.Create(reader.GetString(2)),
            new BillingCalculationResult(
                ReadMoney(reader, 6),
                ReadMoney(reader, 7),
                ReadMoney(reader, 8),
                ReadMoney(reader, 9),
                ReadMoney(reader, 10),
                ReadMoney(reader, 11),
                ReadMoney(reader, 12)),
            reader.GetString(3),
            DateTime.Parse(reader.GetString(4), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            ReadDate(reader, 5),
            ReadMeters(reader),
            ReadTenant(reader),
            reader.GetInt64(13) == 1,
            ReadPayment(reader));
    }

    // ตาราง Bills บังคับให้แต่ละกลุ่มมีครบหรือไม่มีเลย จึงเช็กคอลัมน์แรกของกลุ่มก็พอ
    private static BillMeterSnapshot? ReadMeters(SqliteDataReader reader)
    {
        return reader.IsDBNull(14)
            ? null
            : new BillMeterSnapshot(
                ReadMeter(reader, 14),
                ReadMeter(reader, 15),
                ReadMeter(reader, 16),
                ReadMeter(reader, 17),
                ReadDecimal(reader, 18),
                ReadDecimal(reader, 19));
    }

    private static BillTenantSnapshot? ReadTenant(SqliteDataReader reader)
    {
        return reader.IsDBNull(20)
            ? null
            : new BillTenantSnapshot(TenantId.From(Guid.Parse(reader.GetString(20))), reader.GetString(21));
    }

    private static BillPayment? ReadPayment(SqliteDataReader reader)
    {
        return reader.IsDBNull(22)
            ? null
            : new BillPayment(ReadDate(reader, 22), Enum.Parse<PaymentMethod>(reader.GetString(23)));
    }

    private static decimal ReadDecimal(SqliteDataReader reader, int column)
    {
        return decimal.Parse(reader.GetString(column), CultureInfo.InvariantCulture);
    }

    private static Money ReadMoney(SqliteDataReader reader, int column)
    {
        return Money.Create(ReadDecimal(reader, column));
    }

    private static MeterReading ReadMeter(SqliteDataReader reader, int column)
    {
        return MeterReading.Create(ReadDecimal(reader, column));
    }

    private static DateOnly ReadDate(SqliteDataReader reader, int column)
    {
        return DateOnly.ParseExact(reader.GetString(column), DateFormat, CultureInfo.InvariantCulture);
    }
}
