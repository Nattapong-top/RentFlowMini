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

    [Fact]
    public void GetAndExists_MatchBothRoomAndPeriod()
    {
        var repository = new SqliteMonthlyMeterReadingRepository(_database);
        var roomId = AddRoom("101");
        var otherRoomId = AddRoom("102");
        repository.Add(CreateReading(roomId, October));

        Assert.True(repository.Exists(roomId, October));
        Assert.False(repository.Exists(roomId, November));
        Assert.False(repository.Exists(otherRoomId, October));
        Assert.Null(repository.Get(roomId, November));
        Assert.Null(repository.Get(otherRoomId, October));
    }

    [Fact]
    public void Add_WhenRoomAndPeriodAlreadyExist_ThrowsThaiMessage()
    {
        var repository = new SqliteMonthlyMeterReadingRepository(_database);
        var roomId = AddRoom("101");
        repository.Add(CreateReading(roomId, October));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            repository.Add(CreateReading(roomId, October)));

        Assert.Equal("มีข้อมูลมิเตอร์ของห้องและรอบบิลนี้อยู่แล้ว", exception.Message);
    }

    [Fact]
    public void GetAll_ReturnsEveryRoomAndPeriod()
    {
        var repository = new SqliteMonthlyMeterReadingRepository(_database);
        var roomId = AddRoom("101");
        var otherRoomId = AddRoom("102");
        repository.Add(CreateReading(roomId, October));
        repository.Add(CreateReading(roomId, November));
        repository.Add(CreateReading(otherRoomId, October));

        var saved = repository.GetAll()
            .Select(reading => (reading.RoomId, reading.BillingPeriod))
            .ToList();

        Assert.Equal(3, saved.Count);
        Assert.Contains((roomId, October), saved);
        Assert.Contains((roomId, November), saved);
        Assert.Contains((otherRoomId, October), saved);
    }

    [Fact]
    public void Update_ReplacesOnlyThatRoomAndPeriod()
    {
        var repository = new SqliteMonthlyMeterReadingRepository(_database);
        var roomId = AddRoom("101");
        var otherRoomId = AddRoom("102");
        repository.Add(CreateReading(roomId, October));
        repository.Add(CreateReading(roomId, November));
        repository.Add(CreateReading(otherRoomId, October));

        repository.Update(CreateReading(
            roomId,
            October,
            previousWater: 110m,
            currentWater: 130.5m,
            previousElectricity: 1005m,
            currentElectricity: 1020.5m));

        var saved = repository.Get(roomId, October)!;
        Assert.Equal(110m, saved.PreviousWaterMeter.Value);
        Assert.Equal(130.5m, saved.CurrentWaterMeter.Value);
        Assert.Equal(1005m, saved.PreviousElectricityMeter.Value);
        Assert.Equal(1020.5m, saved.CurrentElectricityMeter.Value);
        Assert.Equal(125m, repository.Get(roomId, November)!.CurrentWaterMeter.Value);
        Assert.Equal(125m, repository.Get(otherRoomId, October)!.CurrentWaterMeter.Value);
        Assert.Equal(3, repository.GetAll().Count());
    }

    [Fact]
    public void Update_WhenReadingDoesNotExist_AddsReading()
    {
        var repository = new SqliteMonthlyMeterReadingRepository(_database);
        var roomId = AddRoom("101");

        repository.Update(CreateReading(roomId, October));

        Assert.True(repository.Exists(roomId, October));
    }

    [Fact]
    public void SaveServiceTwiceForSamePeriod_KeepsOneUpdatedReading()
    {
        var repository = new SqliteMonthlyMeterReadingRepository(_database);
        var service = new SaveMonthlyMeterReadingService(repository);
        var roomId = AddRoom("101");

        service.Save(
            roomId,
            October,
            MeterReading.Create(100m),
            MeterReading.Create(125m),
            MeterReading.Create(1000m),
            MeterReading.Create(1010m));
        service.Save(
            roomId,
            October,
            MeterReading.Create(100m),
            MeterReading.Create(140m),
            MeterReading.Create(1000m),
            MeterReading.Create(1030m));

        var saved = Assert.Single(new SqliteMonthlyMeterReadingRepository(_database).GetAll());
        Assert.Equal(140m, saved.CurrentWaterMeter.Value);
        Assert.Equal(1030m, saved.CurrentElectricityMeter.Value);
    }

    [Fact]
    public void Add_WithMissingRoom_ThrowsThaiMessage()
    {
        var repository = new SqliteMonthlyMeterReadingRepository(_database);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            repository.Add(CreateReading(RoomId.New(), October)));

        Assert.Equal("ไม่พบห้องพักที่เลือก กรุณาเลือกห้องจากรายการอีกครั้ง", exception.Message);
    }

    [Fact]
    public void Update_WithMissingRoom_ThrowsThaiMessage()
    {
        var repository = new SqliteMonthlyMeterReadingRepository(_database);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            repository.Update(CreateReading(RoomId.New(), October)));

        Assert.Equal("ไม่พบห้องพักที่เลือก กรุณาเลือกห้องจากรายการอีกครั้ง", exception.Message);
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
