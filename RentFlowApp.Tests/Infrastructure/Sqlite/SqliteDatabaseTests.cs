using Microsoft.Data.Sqlite;
using RentFlowApp.Infrastructure.Sqlite;

namespace RentFlowApp.Tests.Infrastructure.Sqlite;

public sealed class SqliteDatabaseTests : IDisposable
{
    private readonly string _folder =
        Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "RentFlowTests", Guid.NewGuid().ToString())).FullName;

    private string DatabasePath => Path.Combine(_folder, "rentflow.db");

    public void Dispose()
    {
        // pool ยังถือไฟล์ไว้แม้ปิด connection แล้ว ถ้าไม่ล้างจะลบไฟล์เทสต์ไม่ได้
        SqliteConnection.ClearAllPools();
        Directory.Delete(_folder, recursive: true);
    }

    [Fact]
    public void Initialize_OnNewFile_CreatesAllTablesAtVersionOne()
    {
        new SqliteDatabase(DatabasePath).Initialize();

        using var connection = new SqliteConnection($"Data Source={DatabasePath}");
        connection.Open();

        Assert.Equal(
            ["BillingSettings", "Bills", "InvoiceSettings", "MonthlyMeterReadings", "Rooms", "Tenants"],
            ReadTableNames(connection));
        Assert.Equal(1L, ReadSchemaVersion(connection));
    }

    [Fact]
    public void Initialize_OnExistingDatabase_KeepsSavedData()
    {
        var database = new SqliteDatabase(DatabasePath);
        database.Initialize();
        AddRoom("101");

        database.Initialize();

        using var connection = new SqliteConnection($"Data Source={DatabasePath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT RoomNumber FROM Rooms;";

        Assert.Equal("101", command.ExecuteScalar());
    }

    private void AddRoom(string roomNumber)
    {
        using var connection = new SqliteConnection($"Data Source={DatabasePath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Rooms (Id, RoomNumber, MonthlyRent, IsCableTvExempt)
            VALUES ($id, $roomNumber, '3000', 0);
            """;
        command.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
        command.Parameters.AddWithValue("$roomNumber", roomNumber);
        command.ExecuteNonQuery();
    }

    private static List<string> ReadTableNames(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT name
            FROM sqlite_master
            WHERE type = 'table' AND name NOT LIKE 'sqlite_%'
            ORDER BY name;
            """;

        var names = new List<string>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }

    private static long ReadSchemaVersion(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";

        return (long)command.ExecuteScalar()!;
    }
}
