using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace RentFlowApp.Infrastructure.Sqlite;

public sealed class SqliteBackupService
{
    private const int BackupsToKeep = 30;
    private const string TimestampFormat = "yyyyMMdd-HHmmss";

    private const string BackupFailedMessage =
        "ไม่สามารถสำรองข้อมูลได้ กรุณาตรวจสอบพื้นที่ว่างและสิทธิ์การเข้าถึงโฟลเดอร์ แล้วลองใหม่อีกครั้ง";

    private const string InvalidBackupMessage =
        "ไฟล์ที่เลือกไม่ใช่ไฟล์สำรองของ RentFlow หรือไฟล์เสียหาย ข้อมูลปัจจุบันยังอยู่ครบ กรุณาเลือกไฟล์อื่น";

    private const string NewerBackupMessage =
        "ไฟล์สำรองนี้มาจากโปรแกรมรุ่นใหม่กว่า กรุณาอัปเดตโปรแกรมก่อนกู้คืน ข้อมูลปัจจุบันยังอยู่ครบ";

    private static readonly string[] RequiredTables =
        ["Rooms", "Tenants", "MonthlyMeterReadings", "Bills", "BillingSettings", "InvoiceSettings"];

    private readonly SqliteDatabase _database;
    private readonly Func<DateTime> _now;
    private readonly string _filePrefix;
    private readonly Regex _regularBackupName;

    public SqliteBackupService(SqliteDatabase database, string backupFolder, Func<DateTime> now)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentException.ThrowIfNullOrWhiteSpace(backupFolder);
        ArgumentNullException.ThrowIfNull(now);

        _database = database;
        _now = now;
        BackupFolder = Path.GetFullPath(backupFolder);
        _filePrefix = Path.GetFileNameWithoutExtension(database.FilePath);
        // นับเฉพาะชุดที่สำรองตามปกติ สำเนาก่อนอัปเดตหรือก่อนกู้คืนมีชื่ออื่นจึงไม่ถูกลบตามรอบ
        _regularBackupName = new Regex(
            $"^{Regex.Escape(_filePrefix)}-(\\d{{8}}-\\d{{6}})\\.db$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    public string BackupFolder { get; }

    public DateTime? LastBackupTime =>
        FindRegularBackups().Select(backup => (DateTime?)backup.Time).Max();

    public string BackUpNow()
    {
        string backupPath = Path.Combine(BackupFolder, $"{_filePrefix}-{FormatNow()}.db");
        WriteBackup(backupPath);
        RemoveOldBackups();

        return backupPath;
    }

    public bool BackUpIfNoneToday()
    {
        DateTime today = _now().Date;
        if (FindRegularBackups().Any(backup => backup.Time.Date == today))
        {
            return false;
        }

        BackUpNow();
        return true;
    }

    public void RestoreFrom(string backupFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(backupFilePath);

        EnsureUsableBackup(backupFilePath);

        // เก็บข้อมูลปัจจุบันไว้ก่อน เผื่อเลือกไฟล์ผิดจะได้กู้กลับมาได้
        string safetyCopyPath = Path.Combine(BackupFolder, $"{_filePrefix}-before-restore-{FormatNow()}.db");
        WriteBackup(safetyCopyPath);

        try
        {
            using var source = OpenReadOnly(backupFilePath);
            using var current = _database.OpenConnection();
            // คัดลอกผ่านคำสั่งสำรองของ SQLite แทนการแทนที่ไฟล์ เพราะแอปยังเปิดไฟล์ข้อมูลค้างไว้
            source.BackupDatabase(current);
        }
        catch (SqliteException ex)
        {
            throw new InvalidOperationException(
                $"กู้คืนข้อมูลไม่สำเร็จ ข้อมูลก่อนกู้คืนเก็บไว้ที่ไฟล์ {Path.GetFileName(safetyCopyPath)} ในโฟลเดอร์ไฟล์สำรอง",
                ex);
        }
    }

    private void WriteBackup(string backupPath)
    {
        // เขียนไฟล์ชั่วคราวก่อน ถ้าสำรองไม่จบจะไม่มีไฟล์ครึ่ง ๆ ปนอยู่ในรายการไฟล์สำรอง
        string tempPath = backupPath + ".tmp";

        try
        {
            Directory.CreateDirectory(BackupFolder);

            using (var source = _database.OpenConnection())
            using (var target = new SqliteConnection(
                new SqliteConnectionStringBuilder { DataSource = tempPath, Pooling = false }.ToString()))
            {
                source.BackupDatabase(target);
            }

            File.Move(tempPath, backupPath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SqliteException)
        {
            DeleteIfExists(tempPath);
            throw new InvalidOperationException(BackupFailedMessage, ex);
        }
    }

    private void RemoveOldBackups()
    {
        var oldBackupPaths = FindRegularBackups()
            .OrderByDescending(backup => backup.Time)
            .Skip(BackupsToKeep)
            .Select(backup => backup.Path);

        foreach (string path in oldBackupPaths)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // การสำรองรอบนี้สำเร็จแล้ว ไฟล์เก่าที่ลบไม่ได้จะถูกลองลบอีกครั้งในรอบถัดไป
                System.Diagnostics.Trace.TraceWarning($"Could not delete old backup {path}: {ex.Message}");
            }
        }
    }

    private List<(string Path, DateTime Time)> FindRegularBackups()
    {
        if (!Directory.Exists(BackupFolder))
        {
            return [];
        }

        var backups = new List<(string Path, DateTime Time)>();
        foreach (string path in Directory.EnumerateFiles(BackupFolder, "*.db"))
        {
            var match = _regularBackupName.Match(Path.GetFileName(path));
            if (match.Success && DateTime.TryParseExact(
                    match.Groups[1].Value,
                    TimestampFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime time))
            {
                backups.Add((path, time));
            }
        }

        return backups;
    }

    private void EnsureUsableBackup(string backupFilePath)
    {
        try
        {
            using var connection = OpenReadOnly(backupFilePath);

            long schemaVersion = (long)ExecuteScalar(connection, "PRAGMA user_version;")!;
            if (schemaVersion > _database.LatestSchemaVersion)
            {
                throw new InvalidOperationException(NewerBackupMessage);
            }

            bool isIntact = ExecuteScalar(connection, "PRAGMA integrity_check;") is "ok";
            long tableCount = (long)ExecuteScalar(
                connection,
                $"SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name IN ({string.Join(", ", RequiredTables.Select(name => $"'{name}'"))});")!;

            if (!isIntact || schemaVersion < 1 || tableCount != RequiredTables.Length)
            {
                throw new InvalidOperationException(InvalidBackupMessage);
            }
        }
        catch (SqliteException ex)
        {
            throw new InvalidOperationException(InvalidBackupMessage, ex);
        }
    }

    private static SqliteConnection OpenReadOnly(string path)
    {
        // ปิด pool เพื่อไม่ให้แอปถือไฟล์สำรองค้างไว้ ผู้ใช้จะได้คัดลอกหรือย้ายไฟล์ได้
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false
        }.ToString());
        connection.Open();

        return connection;
    }

    private static object? ExecuteScalar(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;

        return command.ExecuteScalar();
    }

    private static void DeleteIfExists(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            System.Diagnostics.Trace.TraceWarning($"Could not delete temporary backup {path}: {ex.Message}");
        }
    }

    // ระบุ InvariantCulture เพราะเครื่องที่ตั้งค่าภาษาไทยจะได้ปี พ.ศ. ในชื่อไฟล์
    private string FormatNow() => _now().ToString(TimestampFormat, CultureInfo.InvariantCulture);
}
