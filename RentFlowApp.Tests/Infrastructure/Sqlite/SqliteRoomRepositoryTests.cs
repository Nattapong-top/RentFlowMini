using Microsoft.Data.Sqlite;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Infrastructure.Sqlite;

namespace RentFlowApp.Tests.Infrastructure.Sqlite;

public sealed class SqliteRoomRepositoryTests : IDisposable
{
    private readonly string _folder =
        Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "RentFlowTests", Guid.NewGuid().ToString())).FullName;

    private readonly SqliteDatabase _database;

    public SqliteRoomRepositoryTests()
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
    public void GetAll_FromNewRepository_ReturnsEverySavedValue()
    {
        var room = new Room(
            RoomId.New(),
            new RoomNumber("101"),
            Money.Create(3500.75m),
            isCableTvExempt: true);
        new SqliteRoomRepository(_database).Add(room);

        var savedRoom = Assert.Single(new SqliteRoomRepository(_database).GetAll());

        Assert.Equal(room.Id, savedRoom.Id);
        Assert.Equal("101", savedRoom.RoomNumber.Value);
        Assert.Equal(3500.75m, savedRoom.MonthlyRent.Amount);
        Assert.True(savedRoom.IsCableTvExempt);
    }

    [Fact]
    public void Add_WithDuplicateRoomNumber_ThrowsThaiMessage()
    {
        var repository = new SqliteRoomRepository(_database);
        repository.Add(new Room(RoomId.New(), new RoomNumber("101")));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            repository.Add(new Room(RoomId.New(), new RoomNumber("101"))));

        Assert.Equal("หมายเลขห้อง 101 มีอยู่แล้ว กรุณาระบุหมายเลขอื่น", exception.Message);
    }

    [Fact]
    public void Add_WithDuplicateRoomId_ThrowsInvalidOperation()
    {
        var repository = new SqliteRoomRepository(_database);
        var roomId = RoomId.New();
        repository.Add(new Room(roomId, new RoomNumber("101")));

        Assert.Throws<InvalidOperationException>(() =>
            repository.Add(new Room(roomId, new RoomNumber("102"))));
    }
}
