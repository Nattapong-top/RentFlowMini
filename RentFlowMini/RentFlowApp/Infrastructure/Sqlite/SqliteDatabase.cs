using Microsoft.Data.Sqlite;

namespace RentFlowApp.Infrastructure.Sqlite;

public sealed class SqliteDatabase
{
    private const string InitialSchemaResource = "RentFlowApp.Database.Sqlite.001_InitialSchema.sql";

    private readonly string _filePath;

    public SqliteDatabase(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = filePath;
    }

    public void Initialize()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_filePath))!);

        using var connection = OpenConnection();

        if (ReadSchemaVersion(connection) != 0)
        {
            return;
        }

        // ทำในธุรกรรมเดียว เพื่อไม่ให้เหลือตารางค้างครึ่งทางถ้าสร้างไม่สำเร็จ
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = ReadInitialSchema();
        command.ExecuteNonQuery();
        transaction.Commit();
    }

    public SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(
            new SqliteConnectionStringBuilder { DataSource = _filePath, ForeignKeys = true }.ToString());
        connection.Open();

        return connection;
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
