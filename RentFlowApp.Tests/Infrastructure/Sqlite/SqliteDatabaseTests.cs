using Microsoft.Data.Sqlite;
using RentFlowApp.Infrastructure.Sqlite;

namespace RentFlowApp.Tests.Infrastructure.Sqlite;

public sealed class SqliteDatabaseTests : IDisposable
{
    private const int SqliteForeignKeyViolation = 787;
    private const int FutureSchemaVersion = 999;

    // คำสั่งแรกสำเร็จแล้วคำสั่งที่สองพัง เพื่อตรวจว่าคำสั่งแรกถูกยกเลิกด้วย
    private static readonly SchemaUpdate FailingUpdate = new(
        2,
        "CREATE TABLE AddedByFailingUpdate (Id TEXT NOT NULL); INSERT INTO TableThatDoesNotExist VALUES (1);");

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

    [Fact]
    public void Initialize_WhenFolderDoesNotExist_CreatesFolderAndDatabase()
    {
        string path = Path.Combine(_folder, "RentFlow", "rentflow.db");

        new SqliteDatabase(path).Initialize();

        Assert.True(File.Exists(path));
    }

    [Fact]
    public void OpenConnection_RejectsReferenceToMissingRoom()
    {
        var database = new SqliteDatabase(DatabasePath);
        database.Initialize();

        using var connection = database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO MonthlyMeterReadings (
                RoomId, BillingPeriod,
                PreviousWaterMeter, CurrentWaterMeter,
                PreviousElectricityMeter, CurrentElectricityMeter)
            VALUES ('missing-room', '2026-11', '0', '10', '0', '10');
            """;

        var exception = Assert.Throws<SqliteException>(() => command.ExecuteNonQuery());
        Assert.Equal(SqliteForeignKeyViolation, exception.SqliteExtendedErrorCode);
    }

    [Fact]
    public void Initialize_OnNewerSchemaVersion_RejectsWithoutChangingFile()
    {
        var database = new SqliteDatabase(DatabasePath);
        database.Initialize();
        AddRoom("101");
        SetSchemaVersion(FutureSchemaVersion);
        SqliteConnection.ClearAllPools();
        byte[] fileBefore = File.ReadAllBytes(DatabasePath);

        var exception = Assert.Throws<InvalidOperationException>(database.Initialize);

        Assert.Equal(
            "ไฟล์ข้อมูลนี้มาจากโปรแกรมรุ่นใหม่กว่า กรุณาอัปเดตโปรแกรมก่อนเปิดใช้งาน",
            exception.Message);
        SqliteConnection.ClearAllPools();
        Assert.Equal(fileBefore, File.ReadAllBytes(DatabasePath));
    }

    [Fact]
    public void Initialize_OnOlderSchemaVersion_AppliesUpdateAndKeepsData()
    {
        new SqliteDatabase(DatabasePath).Initialize();
        AddRoom("101");
        var database = new SqliteDatabase(
            DatabasePath,
            [new SchemaUpdate(2, "ALTER TABLE Rooms ADD COLUMN Note TEXT;")]);

        database.Initialize();

        using var connection = new SqliteConnection($"Data Source={DatabasePath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT RoomNumber, Note FROM Rooms;";

        Assert.Equal("101", command.ExecuteScalar());
        Assert.Equal(2L, ReadSchemaVersion(connection));
    }

    [Fact]
    public void Initialize_WhenUpdateFails_LeavesDatabaseAsBeforeUpdate()
    {
        new SqliteDatabase(DatabasePath).Initialize();
        AddRoom("101");
        var database = new SqliteDatabase(DatabasePath, [FailingUpdate]);

        Assert.Throws<SqliteException>(database.Initialize);

        using var connection = new SqliteConnection($"Data Source={DatabasePath}");
        connection.Open();

        Assert.Equal(1L, ReadSchemaVersion(connection));
        Assert.DoesNotContain("AddedByFailingUpdate", ReadTableNames(connection));
        Assert.Equal("101", ReadFirstRoomNumber(connection));
    }

    private static string? ReadFirstRoomNumber(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT RoomNumber FROM Rooms;";

        return (string?)command.ExecuteScalar();
    }

    private void SetSchemaVersion(int version)
    {
        using var connection = new SqliteConnection($"Data Source={DatabasePath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA user_version = {version};";
        command.ExecuteNonQuery();
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
