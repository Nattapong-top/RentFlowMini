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
