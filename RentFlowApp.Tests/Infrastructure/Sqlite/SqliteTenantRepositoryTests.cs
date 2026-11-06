using System.Globalization;
using Microsoft.Data.Sqlite;
using RentFlowApp.Domain;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Infrastructure.Sqlite;

namespace RentFlowApp.Tests.Infrastructure.Sqlite;

public sealed class SqliteTenantRepositoryTests : IDisposable
{
    private const string RoomOccupiedMessage = "ห้องพักนี้มีผู้เช่าอยู่แล้ว ไม่สามารถผูกผู้เช่าซ้ำได้";

    private static readonly DateOnly MoveInDate = new(2026, 1, 15);

    private readonly string _folder =
        Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "RentFlowTests", Guid.NewGuid().ToString())).FullName;

    private readonly SqliteDatabase _database;

    public SqliteTenantRepositoryTests()
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
        var roomId = AddRoom("101");
        var tenant = new Tenant(
            TenantId.New(),
            "Anna",
            "Example",
            "0800000000",
            roomId,
            MoveInDate,
            5000.50m);
        new SqliteTenantRepository(_database).Add(tenant);

        var saved = Assert.Single(new SqliteTenantRepository(_database).GetAll());

        Assert.Equal(tenant.Id, saved.Id);
        Assert.Equal("Anna", saved.FirstName);
        Assert.Equal("Example", saved.LastName);
        Assert.Equal("0800000000", saved.PhoneNumber);
        Assert.Equal(roomId, saved.RoomId);
        Assert.Equal(MoveInDate, saved.MoveInDate);
        Assert.Equal(5000.50m, saved.SecurityDeposit);
        Assert.Null(saved.MovedOutOn);
        Assert.Null(saved.MovedOutFromRoomId);
    }

    [Fact]
    public void GetAll_ReturnsMovedOutValues()
    {
        var roomId = AddRoom("101");
        var moveOutDate = new DateOnly(2026, 6, 30);
        var tenant = new Tenant(
            TenantId.New(),
            "Anna",
            "Example",
            movedOutOn: moveOutDate,
            movedOutFromRoomId: roomId);
        new SqliteTenantRepository(_database).Add(tenant);

        var saved = Assert.Single(new SqliteTenantRepository(_database).GetAll());

        Assert.Null(saved.RoomId);
        Assert.Equal(moveOutDate, saved.MovedOutOn);
        Assert.Equal(roomId, saved.MovedOutFromRoomId);
    }

    [Fact]
    public void Add_OnThaiCultureMachine_StoresGregorianDate()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("th-TH");
        try
        {
            new SqliteTenantRepository(_database).Add(new Tenant(
                TenantId.New(),
                "Anna",
                "Example",
                moveInDate: MoveInDate));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }

        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT MoveInDate FROM Tenants;";

        Assert.Equal("2026-01-15", command.ExecuteScalar());
    }

    [Fact]
    public void GetAll_WithOnlyRequiredValues_ReturnsEmptyOptionalValues()
    {
        new SqliteTenantRepository(_database).Add(new Tenant(TenantId.New(), "Anna", "Example"));

        var saved = Assert.Single(new SqliteTenantRepository(_database).GetAll());

        Assert.Null(saved.PhoneNumber);
        Assert.Null(saved.RoomId);
        Assert.Null(saved.MoveInDate);
        Assert.Null(saved.SecurityDeposit);
        Assert.Null(saved.MovedOutOn);
        Assert.Null(saved.MovedOutFromRoomId);
    }

    [Fact]
    public void GetAll_WithZeroDeposit_KeepsZero()
    {
        new SqliteTenantRepository(_database).Add(new Tenant(
            TenantId.New(),
            "Anna",
            "Example",
            securityDeposit: 0m));

        var saved = Assert.Single(new SqliteTenantRepository(_database).GetAll());

        Assert.Equal(0m, saved.SecurityDeposit);
    }

    [Fact]
    public void Add_WithDuplicateTenantId_ThrowsThaiMessage()
    {
        var repository = new SqliteTenantRepository(_database);
        var tenantId = TenantId.New();
        repository.Add(new Tenant(tenantId, "Anna", "Example"));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            repository.Add(new Tenant(tenantId, "Ben", "Example")));

        Assert.Equal("ผู้เช่ารายนี้มีอยู่แล้ว ไม่สามารถเพิ่มข้อมูลซ้ำได้", exception.Message);
    }

    [Fact]
    public void Add_ToOccupiedRoom_ThrowsThaiMessage()
    {
        var repository = new SqliteTenantRepository(_database);
        var roomId = AddRoom("101");
        repository.Add(new Tenant(TenantId.New(), "Anna", "Example", roomId: roomId));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            repository.Add(new Tenant(TenantId.New(), "Ben", "Example", roomId: roomId)));

        Assert.Equal(RoomOccupiedMessage, exception.Message);
    }

    [Fact]
    public void ExistsByRoomId_ReturnsTrueOnlyForOccupiedRoom()
    {
        var repository = new SqliteTenantRepository(_database);
        var occupiedRoomId = AddRoom("101");
        var emptyRoomId = AddRoom("102");
        repository.Add(new Tenant(TenantId.New(), "Anna", "Example", roomId: occupiedRoomId));

        Assert.True(repository.ExistsByRoomId(occupiedRoomId));
        Assert.False(repository.ExistsByRoomId(emptyRoomId));
    }

    [Fact]
    public void GetAll_ReturnsTenantsOrderedByFirstNameThenLastName()
    {
        var repository = new SqliteTenantRepository(_database);
        repository.Add(new Tenant(TenantId.New(), "Ben", "Adams"));
        repository.Add(new Tenant(TenantId.New(), "Anna", "Young"));
        repository.Add(new Tenant(TenantId.New(), "Anna", "Baker"));

        var names = repository.GetAll().Select(tenant => tenant.FullName);

        Assert.Equal(["Anna Baker", "Anna Young", "Ben Adams"], names);
    }

    [Fact]
    public void Update_ChangesDetailsOfThatTenantOnly()
    {
        var repository = new SqliteTenantRepository(_database);
        var firstRoomId = AddRoom("101");
        var newRoomId = AddRoom("102");
        var otherRoomId = AddRoom("103");
        var tenant = new Tenant(TenantId.New(), "Anna", "Example", "0800000000", firstRoomId, MoveInDate, 5000m);
        var otherTenant = new Tenant(TenantId.New(), "Ben", "Other", roomId: otherRoomId);
        repository.Add(tenant);
        repository.Add(otherTenant);
        var newMoveInDate = new DateOnly(2026, 2, 1);

        repository.Update(repository.GetById(tenant.Id)!.WithDetails(
            "Anne",
            "Changed",
            "0811111111",
            newRoomId,
            newMoveInDate,
            6000.25m));

        var saved = new SqliteTenantRepository(_database).GetById(tenant.Id)!;
        Assert.Equal("Anne", saved.FirstName);
        Assert.Equal("Changed", saved.LastName);
        Assert.Equal("0811111111", saved.PhoneNumber);
        Assert.Equal(newRoomId, saved.RoomId);
        Assert.Equal(newMoveInDate, saved.MoveInDate);
        Assert.Equal(6000.25m, saved.SecurityDeposit);
        Assert.Equal("Ben", repository.GetById(otherTenant.Id)!.FirstName);
    }

    [Fact]
    public void Update_WithMoveOut_FreesRoomForNewTenant()
    {
        var repository = new SqliteTenantRepository(_database);
        var roomId = AddRoom("101");
        var tenant = new Tenant(TenantId.New(), "Anna", "Example", roomId: roomId, moveInDate: MoveInDate);
        repository.Add(tenant);
        var moveOutDate = new DateOnly(2026, 6, 30);

        repository.Update(repository.GetById(tenant.Id)!.MoveOut(moveOutDate));

        var saved = repository.GetById(tenant.Id)!;
        Assert.Null(saved.RoomId);
        Assert.Equal(moveOutDate, saved.MovedOutOn);
        Assert.Equal(roomId, saved.MovedOutFromRoomId);
        Assert.False(repository.ExistsByRoomId(roomId));

        var newTenant = new Tenant(TenantId.New(), "Ben", "Example", roomId: roomId);
        repository.Add(newTenant);

        Assert.Equal(newTenant.Id, repository.GetById(newTenant.Id)!.Id);
    }

    [Fact]
    public void GetById_WithUnknownTenant_ReturnsNull()
    {
        var repository = new SqliteTenantRepository(_database);

        Assert.Null(repository.GetById(TenantId.New()));
    }

    [Fact]
    public void Update_WithUnknownTenant_ThrowsThaiMessage()
    {
        var repository = new SqliteTenantRepository(_database);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            repository.Update(new Tenant(TenantId.New(), "Anna", "Example")));

        Assert.Equal("ไม่พบผู้เช่าที่ต้องการแก้ไข", exception.Message);
    }

    [Fact]
    public void Update_ToOccupiedRoom_ThrowsThaiMessageAndKeepsTenant()
    {
        var repository = new SqliteTenantRepository(_database);
        var occupiedRoomId = AddRoom("101");
        var currentRoomId = AddRoom("102");
        repository.Add(new Tenant(TenantId.New(), "Anna", "Example", roomId: occupiedRoomId));
        var tenant = new Tenant(TenantId.New(), "Ben", "Example", roomId: currentRoomId);
        repository.Add(tenant);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            repository.Update(tenant.WithDetails("Ben", "Example", null, occupiedRoomId, null, null)));

        Assert.Equal(RoomOccupiedMessage, exception.Message);
        Assert.Equal(currentRoomId, repository.GetById(tenant.Id)!.RoomId);
    }

    private RoomId AddRoom(string roomNumber)
    {
        var room = new Room(RoomId.New(), new RoomNumber(roomNumber));
        new SqliteRoomRepository(_database).Add(room);

        return room.Id;
    }
}
