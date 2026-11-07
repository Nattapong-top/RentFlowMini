using Microsoft.Data.Sqlite;

namespace RentFlowApp.Infrastructure.Sqlite;

public sealed class SqliteAppStorage
{
    private const string DatabaseFileName = "rentflow.db";
    private const string SettingsFileName = "settings.json";
    private const string BackupFolderName = "Backups";

    private const int SqliteBusy = 5;
    private const int SqliteLocked = 6;
    private const int SqliteCorrupt = 11;
    private const int SqliteNotADatabase = 26;

    private const string CorruptedFileMessage =
        "ไฟล์ข้อมูลเสียหาย ระบบจึงไม่เปิดใช้งานเพื่อไม่ให้ข้อมูลเดิมถูกทับ กรุณาอย่าลบไฟล์ และกู้คืนจากไฟล์สำรองหรือติดต่อผู้ดูแลระบบ";

    private const string FileInUseMessage =
        "ไฟล์ข้อมูลกำลังถูกใช้งานอยู่ กรุณาปิดโปรแกรม RentFlow ที่เปิดค้างไว้แล้วลองใหม่อีกครั้ง";

    private const string CannotOpenMessage =
        "ไม่สามารถเปิดไฟล์ข้อมูลได้ กรุณาตรวจสอบสิทธิ์การเข้าถึงโฟลเดอร์และพื้นที่ว่างในเครื่อง แล้วลองใหม่อีกครั้ง";

    private SqliteAppStorage(
        SqliteDatabase database,
        SqliteSettingsRepository settings,
        bool settingsImportFailed,
        string folder)
    {
        Rooms = new SqliteRoomRepository(database);
        Tenants = new SqliteTenantRepository(database);
        MeterReadings = new SqliteMonthlyMeterReadingRepository(database);
        Bills = new SqliteBillRepository(database);
        Settings = settings;
        SettingsImportFailed = settingsImportFailed;
        Backups = new SqliteBackupService(database, Path.Combine(folder, BackupFolderName), () => DateTime.Now);
    }

    public SqliteRoomRepository Rooms { get; }

    public SqliteTenantRepository Tenants { get; }

    public SqliteMonthlyMeterReadingRepository MeterReadings { get; }

    public SqliteBillRepository Bills { get; }

    public SqliteSettingsRepository Settings { get; }

    public bool SettingsImportFailed { get; }

    public SqliteBackupService Backups { get; }

    public static SqliteAppStorage Open(string folder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);

        try
        {
            var database = new SqliteDatabase(Path.Combine(folder, DatabaseFileName));
            database.Initialize();

            var settings = new SqliteSettingsRepository(database);
            bool imported = settings.TryImportOnce(Path.Combine(folder, SettingsFileName));

            return new SqliteAppStorage(database, settings, settingsImportFailed: !imported, folder);
        }
        catch (SqliteException ex)
        {
            throw new InvalidOperationException(DescribeOpenFailure(ex), ex);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException(CannotOpenMessage, ex);
        }
    }

    public static string DescribeOpenFailure(SqliteException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.SqliteErrorCode switch
        {
            SqliteCorrupt or SqliteNotADatabase => CorruptedFileMessage,
            SqliteBusy or SqliteLocked => FileInUseMessage,
            _ => CannotOpenMessage
        };
    }
}
