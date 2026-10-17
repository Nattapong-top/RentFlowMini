using System.Data;
using Microsoft.Data.SqlClient;
using RentFlowApp.Application.Tenants;
using RentFlowApp.Domain;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Infrastructure.Tenants;

public sealed class SqlServerTenantRepository : ITenantRepository
{
    private readonly string _connectionString;

    public SqlServerTenantRepository(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    public void Add(Tenant tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        const string sql = """
            INSERT INTO dbo.Tenants (Id, FirstName, LastName, PhoneNumber, RoomId)
            VALUES (@Id, @FirstName, @LastName, @PhoneNumber, @RoomId);
            """;

        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier)
                .Value = tenant.Id.Value;
            command.Parameters.Add("@FirstName", SqlDbType.NVarChar, 100)
                .Value = tenant.FirstName;
            command.Parameters.Add("@LastName", SqlDbType.NVarChar, 100)
                .Value = tenant.LastName;
            command.Parameters.Add("@PhoneNumber", SqlDbType.NVarChar, 20)
                .Value = (object?)tenant.PhoneNumber ?? DBNull.Value;
            command.Parameters.Add("@RoomId", SqlDbType.UniqueIdentifier)
                .Value = (object?)tenant.RoomId?.Value ?? DBNull.Value;

            connection.Open();
            command.ExecuteNonQuery();
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            if (ex.Message.Contains(
                    "UQ_Tenants_RoomId",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("ห้องพักนี้มีผู้เช่าอยู่แล้ว ไม่สามารถผูกผู้เช่าซ้ำได้", ex);
            }

            throw new InvalidOperationException(
                "ผู้เช่ารายนี้มีอยู่แล้ว ไม่สามารถเพิ่มข้อมูลซ้ำได้",
                ex);
        }
    }

    public Tenant? GetById(TenantId tenantId)
    {
        ArgumentNullException.ThrowIfNull(tenantId);

        const string sql = """
            SELECT Id, FirstName, LastName, PhoneNumber, RoomId
            FROM dbo.Tenants
            WHERE Id = @Id;
            """;

        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier)
            .Value = tenantId.Value;

        connection.Open();

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return MapTenant(reader);
    }

    public bool ExistsByRoomId(RoomId roomId)
    {
        ArgumentNullException.ThrowIfNull(roomId);

        const string sql = """
            SELECT CASE
                WHEN EXISTS (
                    SELECT 1
                    FROM dbo.Tenants
                    WHERE RoomId = @RoomId
                )
                THEN 1
                ELSE 0
            END;
            """;

        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@RoomId", SqlDbType.UniqueIdentifier)
            .Value = roomId.Value;

        connection.Open();

        return (int)command.ExecuteScalar()! == 1;
    }

    public IReadOnlyList<Tenant> GetAll()
    {
        const string sql = """
            SELECT Id, FirstName, LastName, PhoneNumber, RoomId
            FROM dbo.Tenants
            ORDER BY FirstName, LastName;
            """;

        var tenants = new List<Tenant>();

        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(sql, connection);

        connection.Open();

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            tenants.Add(MapTenant(reader));
        }

        return tenants.AsReadOnly();
    }

    private static Tenant MapTenant(SqlDataReader reader)
    {
        var id = TenantId.From(reader.GetGuid(0));
        string firstName = reader.GetString(1);
        string lastName = reader.GetString(2);
        string? phoneNumber = reader.IsDBNull(3) ? null : reader.GetString(3);
        RoomId? roomId = reader.IsDBNull(4) ? null : RoomId.From(reader.GetGuid(4));

        return new Tenant(id, firstName, lastName, phoneNumber, roomId);
    }
}
