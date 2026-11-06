using Microsoft.Data.Sqlite;
using RentFlowApp.Application.Billing;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Infrastructure.Sqlite;

namespace RentFlowApp.Tests.Infrastructure.Sqlite;

public sealed class SqliteMonthlyMeterReadingRepositoryTests : IDisposable
{
    private static readonly BillingPeriod October = BillingPeriod.Create("2026-10");
    private static readonly BillingPeriod November = BillingPeriod.Create("2026-11");

    private readonly string _folder =
        Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "RentFlowTests", Guid.NewGuid().ToString())).FullName;

    private readonly SqliteDatabase _database;

    public SqliteMonthlyMeterReadingRepositoryTests()
    {
        _database = new SqliteDatabase(Path.Combine(_folder, "rentflow.db"));
        _database.Initialize();
    }

    public void Dispose()
    {
        // pool ยังถือไฟล์ไว้แม้ปิด connection แล้ว ถ้าไม่ล้างจะลบไฟล์เทสต์ไม่ได้
        SqliteConnection.ClearAllPools();
        Directory.Delete(_folder, recursive: true);
    }

    [Fact]
    public void Get_FromNewRepository_ReturnsEverySavedValue()
    {
        var roomId = AddRoom("101");
        new SqliteMonthlyMeterReadingRepository(_database).Add(CreateReading(
            roomId,
            October,
            previousWater: 100.25m,
            currentWater: 125.5m,
            previousElectricity: 1000.75m,
            currentElectricity: 1010.125m));

        var saved = new SqliteMonthlyMeterReadingRepository(_database).Get(roomId, October)!;

        Assert.Equal(roomId, saved.RoomId);
        Assert.Equal(October, saved.BillingPeriod);
        Assert.Equal(100.25m, saved.PreviousWaterMeter.Value);
        Assert.Equal(125.5m, saved.CurrentWaterMeter.Value);
        Assert.Equal(1000.75m, saved.PreviousElectricityMeter.Value);
        Assert.Equal(1010.125m, saved.CurrentElectricityMeter.Value);
    }

    private static MonthlyMeterReading CreateReading(
        RoomId roomId,
        BillingPeriod billingPeriod,
        decimal previousWater = 100m,
        decimal currentWater = 125m,
        decimal previousElectricity = 1000m,
        decimal currentElectricity = 1010m)
    {
        return MonthlyMeterReading.Create(
            roomId,
            billingPeriod,
            MeterReading.Create(previousWater),
            MeterReading.Create(currentWater),
            MeterReading.Create(previousElectricity),
            MeterReading.Create(currentElectricity));
    }

    private RoomId AddRoom(string roomNumber)
    {
        var room = new Room(RoomId.New(), new RoomNumber(roomNumber));
        new SqliteRoomRepository(_database).Add(room);

        return room.Id;
    }
}
