using Microsoft.Data.Sqlite;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Infrastructure.Sqlite;
using RentFlowApp.Domain;

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
    public void Update_ChangesRentAndKeepsOtherValues()
    {
        var repository = new SqliteRoomRepository(_database);
        var room = new Room(RoomId.New(), new RoomNumber("101"), Money.Create(3500.75m), isCableTvExempt: true);
        var otherRoom = new Room(RoomId.New(), new RoomNumber("102"), Money.Create(4000m), isCableTvExempt: false);
        repository.Add(room);
        repository.Add(otherRoom);

        var editableRoom = repository.GetById(room.Id)!;
        editableRoom.ChangeMonthlyRent(Money.Create(4200.50m));
        repository.Update(editableRoom);

        var savedRoom = new SqliteRoomRepository(_database).GetById(room.Id)!;
        Assert.Equal(4200.50m, savedRoom.MonthlyRent.Amount);
        Assert.Equal("101", savedRoom.RoomNumber.Value);
        Assert.True(savedRoom.IsCableTvExempt);
        Assert.Equal(4000m, repository.GetById(otherRoom.Id)!.MonthlyRent.Amount);
    }

    [Fact]
    public void Update_SavesCableTvExemption()
    {
        var repository = new SqliteRoomRepository(_database);
        var room = new Room(RoomId.New(), new RoomNumber("101"), Money.Create(3000m), isCableTvExempt: false);
        repository.Add(room);

        var editableRoom = repository.GetById(room.Id)!;
        editableRoom.ChangeCableTvExemption(true);
        repository.Update(editableRoom);

        Assert.True(new SqliteRoomRepository(_database).GetById(room.Id)!.IsCableTvExempt);
    }

    [Fact]
    public void GetById_WithUnknownRoom_ReturnsNull()
    {
        var repository = new SqliteRoomRepository(_database);

        Assert.Null(repository.GetById(RoomId.New()));
    }

    [Fact]
    public void Update_WithUnknownRoom_ThrowsThaiMessage()
    {
        var repository = new SqliteRoomRepository(_database);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            repository.Update(new Room(RoomId.New(), new RoomNumber("101"))));

        Assert.Equal("ไม่พบห้องพักที่ต้องการแก้ไข", exception.Message);
    }

    [Fact]
    public void GetAll_ReturnsRoomsInDisplayOrder()
    {
        var repository = new SqliteRoomRepository(_database);
        repository.Add(new Room(RoomId.New(), new RoomNumber("102")));
        repository.Add(new Room(RoomId.New(), new RoomNumber("101")));
        repository.Add(new Room(RoomId.New(), new RoomNumber("99")));

        var roomNumbers = repository.GetAll().Select(room => room.RoomNumber.Value);

        Assert.Equal(["99", "101", "102"], roomNumbers);
    }

    [Fact]
    public void ExistsByRoomNumber_ReturnsTrueOnlyForSavedRoomNumber()
    {
        var repository = new SqliteRoomRepository(_database);
        repository.Add(new Room(RoomId.New(), new RoomNumber("101")));

        Assert.True(repository.ExistsByRoomNumber("101"));
        Assert.False(repository.ExistsByRoomNumber("999"));
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
