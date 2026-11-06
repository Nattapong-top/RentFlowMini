using System.Globalization;
using Microsoft.Data.Sqlite;
using RentFlowApp.Application.Rooms;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Infrastructure.Sqlite;

public sealed class SqliteRoomRepository : IRoomEditor
{
    private const int SqliteUniqueViolation = 2067;
    private const int SqlitePrimaryKeyViolation = 1555;

    private readonly SqliteDatabase _database;

    public SqliteRoomRepository(SqliteDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);
        _database = database;
    }

    public void Add(Room room)
    {
        ArgumentNullException.ThrowIfNull(room);

        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Rooms (Id, RoomNumber, MonthlyRent, IsCableTvExempt)
            VALUES ($id, $roomNumber, $monthlyRent, $isCableTvExempt);
            """;
        command.Parameters.AddWithValue("$id", room.Id.Value.ToString());
        command.Parameters.AddWithValue("$roomNumber", room.RoomNumber.Value);
        // เก็บเงินเป็นข้อความ เพราะ SQLite เก็บทศนิยมแบบ REAL ซึ่งทำให้เศษสตางค์เพี้ยนได้
        command.Parameters.AddWithValue("$monthlyRent", room.MonthlyRent.Amount.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$isCableTvExempt", room.IsCableTvExempt ? 1 : 0);

        try
        {
            command.ExecuteNonQuery();
        }
        catch (SqliteException ex) when (ex.SqliteExtendedErrorCode is SqliteUniqueViolation or SqlitePrimaryKeyViolation)
        {
            throw new InvalidOperationException(
                $"หมายเลขห้อง {room.RoomNumber.Value} มีอยู่แล้ว กรุณาระบุหมายเลขอื่น",
                ex);
        }
    }

    public bool ExistsByRoomNumber(string roomNumber)
    {
        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT EXISTS (SELECT 1 FROM Rooms WHERE RoomNumber = $roomNumber);";
        command.Parameters.AddWithValue("$roomNumber", roomNumber);

        return (long)command.ExecuteScalar()! == 1;
    }

    public IReadOnlyList<Room> GetAll()
    {
        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, RoomNumber, MonthlyRent, IsCableTvExempt FROM Rooms;";

        var rooms = new List<Room>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            rooms.Add(ReadRoom(reader));
        }

        return rooms
            .OrderBy(room => room.RoomNumber.Value, RoomNumber.DisplayOrder)
            .ToList()
            .AsReadOnly();
    }

    public Room? GetById(RoomId roomId)
    {
        ArgumentNullException.ThrowIfNull(roomId);

        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, RoomNumber, MonthlyRent, IsCableTvExempt FROM Rooms WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", roomId.Value.ToString());

        using var reader = command.ExecuteReader();

        return reader.Read() ? ReadRoom(reader) : null;
    }

    public void Update(Room room)
    {
        ArgumentNullException.ThrowIfNull(room);

        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        // แก้เฉพาะค่าเช่า เพราะ Room เปลี่ยนได้แค่ค่านี้
        command.CommandText = "UPDATE Rooms SET MonthlyRent = $monthlyRent WHERE Id = $id;";
        command.Parameters.AddWithValue("$monthlyRent", room.MonthlyRent.Amount.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$id", room.Id.Value.ToString());

        command.ExecuteNonQuery();
    }

    private static Room ReadRoom(SqliteDataReader reader)
    {
        return new Room(
            RoomId.From(Guid.Parse(reader.GetString(0))),
            new RoomNumber(reader.GetString(1)),
            Money.Create(decimal.Parse(reader.GetString(2), CultureInfo.InvariantCulture)),
            reader.GetInt64(3) == 1);
    }
}
