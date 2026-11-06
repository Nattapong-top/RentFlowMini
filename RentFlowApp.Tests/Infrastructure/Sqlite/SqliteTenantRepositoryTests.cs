using System.Globalization;
using Microsoft.Data.Sqlite;
using RentFlowApp.Domain;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Infrastructure.Sqlite;

namespace RentFlowApp.Tests.Infrastructure.Sqlite;

public sealed class SqliteTenantRepositoryTests : IDisposable
{
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

    private RoomId AddRoom(string roomNumber)
    {
        var room = new Room(RoomId.New(), new RoomNumber(roomNumber));
        new SqliteRoomRepository(_database).Add(room);

        return room.Id;
    }
}
