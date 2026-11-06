using System.Globalization;
using Microsoft.Data.Sqlite;
using RentFlowApp.Application.Tenants;
using RentFlowApp.Domain;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Infrastructure.Sqlite;

public sealed class SqliteTenantRepository : ITenantRepository
{
    private const int SqliteUniqueViolation = 2067;
    private const int SqlitePrimaryKeyViolation = 1555;
    private const string DateFormat = "yyyy-MM-dd";
    private const string RoomOccupiedMessage = "ห้องพักนี้มีผู้เช่าอยู่แล้ว ไม่สามารถผูกผู้เช่าซ้ำได้";

    private const string SelectColumns = """
        SELECT Id, FirstName, LastName, PhoneNumber, RoomId,
               MoveInDate, SecurityDeposit, MovedOutOn, MovedOutFromRoomId
        FROM Tenants
        """;

    private readonly SqliteDatabase _database;

    public SqliteTenantRepository(SqliteDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);
        _database = database;
    }

    public void Add(Tenant tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Tenants (
                Id, FirstName, LastName, PhoneNumber, RoomId,
                MoveInDate, SecurityDeposit, MovedOutOn, MovedOutFromRoomId)
            VALUES (
                $id, $firstName, $lastName, $phoneNumber, $roomId,
                $moveInDate, $securityDeposit, $movedOutOn, $movedOutFromRoomId);
            """;
        AddTenantParameters(command, tenant);

        try
        {
            command.ExecuteNonQuery();
        }
        catch (SqliteException ex) when (ex.SqliteExtendedErrorCode == SqlitePrimaryKeyViolation)
        {
            throw new InvalidOperationException("ผู้เช่ารายนี้มีอยู่แล้ว ไม่สามารถเพิ่มข้อมูลซ้ำได้", ex);
        }
        catch (SqliteException ex) when (ex.SqliteExtendedErrorCode == SqliteUniqueViolation)
        {
            throw new InvalidOperationException(RoomOccupiedMessage, ex);
        }
    }

    public void Update(Tenant tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Tenants
            SET FirstName = $firstName,
                LastName = $lastName,
                PhoneNumber = $phoneNumber,
                RoomId = $roomId,
                MoveInDate = $moveInDate,
                SecurityDeposit = $securityDeposit,
                MovedOutOn = $movedOutOn,
                MovedOutFromRoomId = $movedOutFromRoomId
            WHERE Id = $id;
            """;
        AddTenantParameters(command, tenant);

        try
        {
            if (command.ExecuteNonQuery() == 0)
            {
                throw new InvalidOperationException("ไม่พบผู้เช่าที่ต้องการแก้ไข");
            }
        }
        catch (SqliteException ex) when (ex.SqliteExtendedErrorCode == SqliteUniqueViolation)
        {
            throw new InvalidOperationException(RoomOccupiedMessage, ex);
        }
    }

    public Tenant? GetById(TenantId tenantId)
    {
        ArgumentNullException.ThrowIfNull(tenantId);

        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"{SelectColumns} WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", tenantId.Value.ToString());

        using var reader = command.ExecuteReader();

        return reader.Read() ? ReadTenant(reader) : null;
    }

    public bool ExistsByRoomId(RoomId roomId)
    {
        ArgumentNullException.ThrowIfNull(roomId);

        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT EXISTS (SELECT 1 FROM Tenants WHERE RoomId = $roomId);";
        command.Parameters.AddWithValue("$roomId", roomId.Value.ToString());

        return (long)command.ExecuteScalar()! == 1;
    }

    public IReadOnlyList<Tenant> GetAll()
    {
        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"{SelectColumns};";

        var tenants = new List<Tenant>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            tenants.Add(ReadTenant(reader));
        }

        return tenants
            .OrderBy(tenant => tenant.FirstName)
            .ThenBy(tenant => tenant.LastName)
            .ToList()
            .AsReadOnly();
    }

    private static void AddTenantParameters(SqliteCommand command, Tenant tenant)
    {
        command.Parameters.AddWithValue("$id", tenant.Id.Value.ToString());
        command.Parameters.AddWithValue("$firstName", tenant.FirstName);
        command.Parameters.AddWithValue("$lastName", tenant.LastName);
        command.Parameters.AddWithValue("$phoneNumber", ToDbValue(tenant.PhoneNumber));
        command.Parameters.AddWithValue("$roomId", ToDbValue(tenant.RoomId?.Value.ToString()));
        command.Parameters.AddWithValue("$moveInDate", ToDbValue(FormatDate(tenant.MoveInDate)));
        // เก็บเงินเป็นข้อความ เพราะ SQLite เก็บทศนิยมแบบ REAL ซึ่งทำให้เศษสตางค์เพี้ยนได้
        command.Parameters.AddWithValue(
            "$securityDeposit",
            ToDbValue(tenant.SecurityDeposit?.ToString(CultureInfo.InvariantCulture)));
        command.Parameters.AddWithValue("$movedOutOn", ToDbValue(FormatDate(tenant.MovedOutOn)));
        command.Parameters.AddWithValue("$movedOutFromRoomId", ToDbValue(tenant.MovedOutFromRoomId?.Value.ToString()));
    }

    private static object ToDbValue(string? value)
    {
        return value is null ? DBNull.Value : value;
    }

    // ระบุ InvariantCulture เพราะเครื่องที่ตั้งค่าภาษาไทยจะได้ปี พ.ศ.
    private static string? FormatDate(DateOnly? date)
    {
        return date?.ToString(DateFormat, CultureInfo.InvariantCulture);
    }

    private static Tenant ReadTenant(SqliteDataReader reader)
    {
        return new Tenant(
            TenantId.From(Guid.Parse(reader.GetString(0))),
            reader.GetString(1),
            reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            ReadRoomId(reader, 4),
            ReadDate(reader, 5),
            reader.IsDBNull(6) ? null : decimal.Parse(reader.GetString(6), CultureInfo.InvariantCulture),
            ReadDate(reader, 7),
            ReadRoomId(reader, 8));
    }

    private static RoomId? ReadRoomId(SqliteDataReader reader, int column)
    {
        return reader.IsDBNull(column) ? null : RoomId.From(Guid.Parse(reader.GetString(column)));
    }

    private static DateOnly? ReadDate(SqliteDataReader reader, int column)
    {
        return reader.IsDBNull(column)
            ? null
            : DateOnly.ParseExact(reader.GetString(column), DateFormat, CultureInfo.InvariantCulture);
    }
}
