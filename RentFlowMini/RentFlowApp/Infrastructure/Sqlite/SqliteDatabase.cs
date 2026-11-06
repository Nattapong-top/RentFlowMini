using System.Globalization;
using Microsoft.Data.Sqlite;

namespace RentFlowApp.Infrastructure.Sqlite;

public sealed class SqliteDatabase
{
    private const string InitialSchemaResource = "RentFlowApp.Database.Sqlite.001_InitialSchema.sql";
    private const string BackupFolderName = "Backups";
    private const string NewerSchemaMessage = "ไฟล์ข้อมูลนี้มาจากโปรแกรมรุ่นใหม่กว่า กรุณาอัปเดตโปรแกรมก่อนเปิดใช้งาน";

    private readonly string _filePath;
    private readonly IReadOnlyList<SchemaUpdate> _updates;

    public SqliteDatabase(string filePath)
        : this(filePath, [new SchemaUpdate(1, ReadInitialSchema())])
    {
    }

    public SqliteDatabase(string filePath, IReadOnlyList<SchemaUpdate> updates)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = filePath;
        _updates = updates;
    }

    public void Initialize()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_filePath))!);

        using var connection = OpenConnection();

        long schemaVersion = ReadSchemaVersion(connection);

        if (schemaVersion > _updates.Max(update => update.Version))
        {
            throw new InvalidOperationException(NewerSchemaMessage);
        }

        var pendingUpdates = _updates
            .Where(update => update.Version > schemaVersion)
            .OrderBy(update => update.Version)
            .ToList();

        if (pendingUpdates.Count == 0)
        {
            return;
        }

        // ไฟล์ใหม่ยังไม่มีข้อมูลให้สำรอง
        if (schemaVersion > 0)
        {
            BackUp(connection, schemaVersion);
        }

        // ทำในธุรกรรมเดียว เพื่อไม่ให้เหลือตารางค้างครึ่งทางถ้าอัปเดตไม่สำเร็จ
        using var transaction = connection.BeginTransaction();
        foreach (var update in pendingUpdates)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $"{update.Sql}\nPRAGMA user_version = {update.Version};";
            command.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(
            new SqliteConnectionStringBuilder { DataSource = _filePath, ForeignKeys = true }.ToString());
        connection.Open();

        return connection;
    }

    private void BackUp(SqliteConnection connection, long schemaVersion)
    {
        string fullPath = Path.GetFullPath(_filePath);
        string folder = Directory.CreateDirectory(
            Path.Combine(Path.GetDirectoryName(fullPath)!, BackupFolderName)).FullName;
        // ระบุ InvariantCulture เพราะเครื่องที่ตั้งค่าภาษาไทยจะได้ปี พ.ศ. ในชื่อไฟล์
        string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        string backupPath = Path.Combine(
            folder,
            $"{Path.GetFileNameWithoutExtension(fullPath)}-before-update-v{schemaVersion}-{timestamp}.db");

        // ปิด pool เพื่อไม่ให้แอปถือไฟล์สำรองค้างไว้ ผู้ใช้จะได้คัดลอกหรือย้ายไฟล์ได้
        using var backup = new SqliteConnection(
            new SqliteConnectionStringBuilder { DataSource = backupPath, Pooling = false }.ToString());
        connection.BackupDatabase(backup);
    }

    private static long ReadSchemaVersion(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";

        return (long)command.ExecuteScalar()!;
    }

    private static string ReadInitialSchema()
    {
        using var stream = typeof(SqliteDatabase).Assembly.GetManifestResourceStream(InitialSchemaResource)
            ?? throw new InvalidOperationException($"Missing embedded resource: {InitialSchemaResource}");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
