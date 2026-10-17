using Microsoft.Data.SqlClient;
using RentFlowApp.Domain;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Infrastructure.Tenants;

namespace RentFlowApp.Tests.Infrastructure.Tenants;

public class SqlServerTenantRepositoryTests
{
    private static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("RENTFLOW_TEST_CONNECTION")
        ?? @"Server=.\SQLEXPRESS;
            Database=RentFlowDb;
            Integrated Security=True;
            Encrypt=False;
            TrustServerCertificate=True;";

    [Fact]
    public void Add_WithValidTenant_StoresTenantInDatabaseAndCanBeReadById()
    {
        var repository = new SqlServerTenantRepository(ConnectionString);
        var tenant = new Tenant(
            TenantId.New(),
            "สมชาย",
            "ใจดี",
            "0812345678",
            RoomId.New());
        var roomNumber = $"TEST-{Guid.NewGuid():N}";
        var roomInserted = false;
        var tenantInserted = false;

        try
        {
            InsertTestRoom(tenant.RoomId!, roomNumber);
            roomInserted = true;

            repository.Add(tenant);
            tenantInserted = true;

            var storedTenant = repository.GetById(tenant.Id);

            Assert.NotNull(storedTenant);
            Assert.Equal(tenant.Id, storedTenant.Id);
            Assert.Equal(tenant.FirstName, storedTenant.FirstName);
            Assert.Equal(tenant.LastName, storedTenant.LastName);
            Assert.Equal(tenant.PhoneNumber, storedTenant.PhoneNumber);
            Assert.Equal(tenant.RoomId, storedTenant.RoomId);
        }
        finally
        {
            if (tenantInserted)
            {
                DeleteTestTenant(tenant.Id);
            }

            if (roomInserted)
            {
                DeleteTestRoom(tenant.RoomId!);
            }
        }
    }

    [Fact]
    public void Add_WithDuplicateId_ThrowsInvalidOperationException()
    {
        var repository = new SqlServerTenantRepository(ConnectionString);
        var tenantId = TenantId.New();
        var firstTenant = new Tenant(tenantId, "สมชาย", "ใจดี");
        var duplicateTenant = new Tenant(tenantId, "สมหญิง", "ใจดี");

        try
        {
            repository.Add(firstTenant);

            var exception = Assert.Throws<InvalidOperationException>(
                () => repository.Add(duplicateTenant));

            Assert.Contains("ผู้เช่ารายนี้มีอยู่แล้ว", exception.Message);
        }
        finally
        {
            DeleteTestTenant(firstTenant.Id);
        }
    }

    [Fact]
    public void Add_WithOccupiedRoom_ThrowsInvalidOperationException()
    {
        var repository = new SqlServerTenantRepository(ConnectionString);
        var roomId = RoomId.New();
        var firstTenant = new Tenant(
            TenantId.New(),
            "สมชาย",
            "ใจดี",
            roomId: roomId);
        var secondTenant = new Tenant(
            TenantId.New(),
            "สมหญิง",
            "ใจดี",
            roomId: roomId);
        var roomNumber = $"TEST-{Guid.NewGuid():N}";
        var roomInserted = false;

        try
        {
            InsertTestRoom(roomId, roomNumber);
            roomInserted = true;
            repository.Add(firstTenant);

            var exception = Assert.Throws<InvalidOperationException>(
                () => repository.Add(secondTenant));

            Assert.Contains("ห้องพักนี้มีผู้เช่าอยู่แล้ว", exception.Message);
        }
        finally
        {
            DeleteTestTenant(firstTenant.Id);
            DeleteTestTenant(secondTenant.Id);

            if (roomInserted)
            {
                DeleteTestRoom(roomId);
            }
        }
    }

    [Fact]
    public void Add_MultipleTenantsWithoutRoom_IsAllowed()
    {
        var repository = new SqlServerTenantRepository(ConnectionString);
        var firstTenant = new Tenant(TenantId.New(), "สมชาย", "ใจดี");
        var secondTenant = new Tenant(TenantId.New(), "สมหญิง", "ใจดี");

        try
        {
            repository.Add(firstTenant);
            repository.Add(secondTenant);

            Assert.NotNull(repository.GetById(firstTenant.Id));
            Assert.NotNull(repository.GetById(secondTenant.Id));
        }
        finally
        {
            DeleteTestTenant(firstTenant.Id);
            DeleteTestTenant(secondTenant.Id);
        }
    }

    [Fact]
    public void ExistsByRoomId_WhenRoomHasTenant_ReturnsTrue()
    {
        var repository = new SqlServerTenantRepository(ConnectionString);
        var roomId = RoomId.New();
        var tenant = new Tenant(
            TenantId.New(),
            "สมชาย",
            "ใจดี",
            roomId: roomId);
        var roomNumber = $"TEST-{Guid.NewGuid():N}";
        var roomInserted = false;

        try
        {
            InsertTestRoom(roomId, roomNumber);
            roomInserted = true;
            repository.Add(tenant);

            Assert.True(repository.ExistsByRoomId(roomId));
        }
        finally
        {
            DeleteTestTenant(tenant.Id);

            if (roomInserted)
            {
                DeleteTestRoom(roomId);
            }
        }
    }

    [Fact]
    public void ExistsByRoomId_WhenRoomIsEmpty_ReturnsFalse()
    {
        var repository = new SqlServerTenantRepository(ConnectionString);

        Assert.False(repository.ExistsByRoomId(RoomId.New()));
    }

    [Fact]
    public void GetAll_ReturnsAllSavedTenants()
    {
        var repository = new SqlServerTenantRepository(ConnectionString);
        var firstTenant = new Tenant(TenantId.New(), "สมชาย", "ใจดี");
        var secondTenant = new Tenant(TenantId.New(), "สมหญิง", "ใจดี");

        try
        {
            repository.Add(firstTenant);
            repository.Add(secondTenant);

            var tenants = repository.GetAll();

            Assert.Contains(tenants, tenant => tenant.Id == firstTenant.Id);
            Assert.Contains(tenants, tenant => tenant.Id == secondTenant.Id);
        }
        finally
        {
            DeleteTestTenant(firstTenant.Id);
            DeleteTestTenant(secondTenant.Id);
        }
    }

    private static void InsertTestRoom(RoomId roomId, string roomNumber)
    {
        const string sql = """
            INSERT INTO dbo.Rooms (Id, RoomNumber)
            VALUES (@Id, @RoomNumber);
            """;

        using var connection = new SqlConnection(ConnectionString);
        using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@Id", System.Data.SqlDbType.UniqueIdentifier)
            .Value = roomId.Value;
        command.Parameters.Add("@RoomNumber", System.Data.SqlDbType.NVarChar, 50)
            .Value = roomNumber;

        connection.Open();
        command.ExecuteNonQuery();
    }

    private static void DeleteTestTenant(TenantId tenantId)
    {
        const string sql = """
            DELETE FROM dbo.Tenants
            WHERE Id = @Id;
            """;

        using var connection = new SqlConnection(ConnectionString);
        using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@Id", System.Data.SqlDbType.UniqueIdentifier)
            .Value = tenantId.Value;

        connection.Open();
        command.ExecuteNonQuery();
    }

    private static void DeleteTestRoom(RoomId roomId)
    {
        const string sql = """
            DELETE FROM dbo.Rooms
            WHERE Id = @Id;
            """;

        using var connection = new SqlConnection(ConnectionString);
        using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@Id", System.Data.SqlDbType.UniqueIdentifier)
            .Value = roomId.Value;

        connection.Open();
        command.ExecuteNonQuery();
    }
}
