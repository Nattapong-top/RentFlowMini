
using System.Data;
using Microsoft.Data.SqlClient;
using RentFlowApp.Application.Rooms;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Infrastructure.Rooms;

public sealed class SqlServerRoomRepository : IRoomRepository
{
    private readonly string _connectionString;

    public SqlServerRoomRepository(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    public void Add(Room room)
    {
        ArgumentNullException.ThrowIfNull(room);

        const string sql = """
            INSERT INTO dbo.Rooms (Id, RoomNumber)
            VALUES (@Id, @RoomNumber);
            """;

        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier)
                .Value = room.Id.Value;
            command.Parameters.Add(
                "@RoomNumber", SqlDbType.NVarChar, 50)
                .Value = room.RoomNumber.Value;

            connection.Open();
            command.ExecuteNonQuery();
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            throw new InvalidOperationException(
                $"Room number '{room.RoomNumber.Value}' already exists.",
                ex);
        }
    }

    public bool ExistsByRoomNumber(string roomNumber)
    {
        const string sql = """
            SELECT CASE
                WHEN EXISTS (
                    SELECT 1
                    FROM dbo.Rooms
                    WHERE RoomNumber = @RoomNumber
                )
                THEN 1
                ELSE 0
            END;
            """;

        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(sql, connection);

        command.Parameters.Add(
            "@RoomNumber", SqlDbType.NVarChar, 50)
            .Value = roomNumber;

        connection.Open();

        return (int)command.ExecuteScalar()! == 1;
    }

    public IReadOnlyList<Room> GetAll()
    {
        const string sql = """
            SELECT Id, RoomNumber
            FROM dbo.Rooms
            ORDER BY RoomNumber;
            """;

        var rooms = new List<Room>();

        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(sql, connection);

        connection.Open();

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            var id = RoomId.From(reader.GetGuid(0));
            var roomNumber = new RoomNumber(reader.GetString(1));

            rooms.Add(new Room(id, roomNumber));
        }

        return rooms.AsReadOnly();
    }
}
