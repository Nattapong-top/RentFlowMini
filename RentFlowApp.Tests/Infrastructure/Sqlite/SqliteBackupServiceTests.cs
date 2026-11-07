using Microsoft.Data.Sqlite;
using RentFlowApp.Application.Billing;
using RentFlowApp.Application.Rooms;
using RentFlowApp.Application.Tenants;
using RentFlowApp.Forms;
using RentFlowApp.Infrastructure.Sqlite;
using RentFlowApp.Tests.UI;
using static RentFlowApp.Tests.UI.InputTestHelper;

namespace RentFlowApp.Tests.Infrastructure.Sqlite;

public sealed class SqliteBackupServiceTests : IDisposable
{
    private const string BackupFailedMessage =
        "ไม่สามารถสำรองข้อมูลได้ กรุณาตรวจสอบพื้นที่ว่างและสิทธิ์การเข้าถึงโฟลเดอร์ แล้วลองใหม่อีกครั้ง";

    private const string InvalidBackupMessage =
        "ไฟล์ที่เลือกไม่ใช่ไฟล์สำรองของ RentFlow หรือไฟล์เสียหาย ข้อมูลปัจจุบันยังอยู่ครบ กรุณาเลือกไฟล์อื่น";

    private const string NewerBackupMessage =
        "ไฟล์สำรองนี้มาจากโปรแกรมรุ่นใหม่กว่า กรุณาอัปเดตโปรแกรมก่อนกู้คืน ข้อมูลปัจจุบันยังอยู่ครบ";

    private readonly string _folder =
        Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "RentFlowTests", Guid.NewGuid().ToString())).FullName;

    private DateTime _now = new(2026, 11, 7, 10, 15, 30);

    private string DatabasePath => Path.Combine(_folder, "rentflow.db");

    private string BackupFolder => Path.Combine(_folder, "Backups");

    public void Dispose()
    {
        // pool ยังถือไฟล์ไว้แม้ปิด connection แล้ว ถ้าไม่ล้างจะลบไฟล์เทสต์ไม่ได้
        SqliteConnection.ClearAllPools();
        Directory.Delete(_folder, recursive: true);
    }

    [Fact]
    public void BackUpNow_CreatesFileWithEveryValue()
    {
        var storage = SqliteAppStorage.Open(_folder);
        var room = new CreateRoomService(storage.Rooms).Create("101", 2800.25m, true);
        new CreateTenantService(storage.Tenants).Create("Anna", "Example", null, room.Id);

        string backupPath = CreateService().BackUpNow();

        Assert.Equal(Path.Combine(BackupFolder, "rentflow-20261107-101530.db"), backupPath);
        var database = new SqliteDatabase(backupPath);
        var savedRoom = Assert.Single(new SqliteRoomRepository(database).GetAll());
        Assert.Equal(2800.25m, savedRoom.MonthlyRent.Amount);
        Assert.True(savedRoom.IsCableTvExempt);
        Assert.Equal(room.Id, Assert.Single(new SqliteTenantRepository(database).GetAll()).RoomId);
    }

    [Fact]
    public void BackUpNow_KeepsLatestThirtyAndEverySafetyCopy()
    {
        SqliteAppStorage.Open(_folder);
        Directory.CreateDirectory(BackupFolder);
        string beforeUpdate = Path.Combine(BackupFolder, "rentflow-before-update-v1-20260101-090000.db");
        string beforeRestore = Path.Combine(BackupFolder, "rentflow-before-restore-20260102-090000.db");
        File.WriteAllText(beforeUpdate, "safety copy");
        File.WriteAllText(beforeRestore, "safety copy");
        var service = CreateService();
        var start = _now;

        for (int day = 0; day < 31; day++)
        {
            _now = start.AddDays(day);
            service.BackUpNow();
        }

        var regularBackups = Directory.GetFiles(BackupFolder, "rentflow-2*.db").Select(Path.GetFileName).ToList();
        Assert.Equal(30, regularBackups.Count);
        Assert.DoesNotContain("rentflow-20261107-101530.db", regularBackups);
        Assert.Contains("rentflow-20261207-101530.db", regularBackups);
        Assert.True(File.Exists(beforeUpdate));
        Assert.True(File.Exists(beforeRestore));
    }

    [Fact]
    public void BackUpIfNoneToday_WhenTodayIsBackedUp_DoesNotBackUpAgain()
    {
        SqliteAppStorage.Open(_folder);
        var service = CreateService();
        service.BackUpNow();
        _now = _now.AddHours(5);

        bool backedUp = service.BackUpIfNoneToday();

        Assert.False(backedUp);
        Assert.Single(Directory.GetFiles(BackupFolder));
    }

    [Fact]
    public void BackUpIfNoneToday_WhenOnlyYesterdayIsBackedUp_BacksUp()
    {
        SqliteAppStorage.Open(_folder);
        var service = CreateService();
        service.BackUpNow();
        _now = _now.AddDays(1);

        bool backedUp = service.BackUpIfNoneToday();

        Assert.True(backedUp);
        Assert.True(File.Exists(Path.Combine(BackupFolder, "rentflow-20261108-101530.db")));
    }

    [Fact]
    public void BackUpNow_WhenFolderCannotBeCreated_ThrowsThaiMessage()
    {
        SqliteAppStorage.Open(_folder);
        File.WriteAllText(BackupFolder, "not a folder");

        var exception = Assert.Throws<InvalidOperationException>(() => CreateService().BackUpNow());

        Assert.Equal(BackupFailedMessage, exception.Message);
    }

    [Fact]
    public void BackUpNow_WhenFileCannotBeSaved_LeavesNoPartialFile()
    {
        SqliteAppStorage.Open(_folder);
        Directory.CreateDirectory(Path.Combine(BackupFolder, "rentflow-20261107-101530.db"));

        var exception = Assert.Throws<InvalidOperationException>(() => CreateService().BackUpNow());

        Assert.Equal(BackupFailedMessage, exception.Message);
        Assert.Empty(Directory.GetFiles(BackupFolder));
    }

    [Fact]
    public void LastBackupTime_ReadsNewestRegularBackup()
    {
        SqliteAppStorage.Open(_folder);
        var service = CreateService();
        Assert.Null(service.LastBackupTime);

        service.BackUpNow();
        _now = _now.AddDays(1);
        service.BackUpNow();
        File.WriteAllText(Path.Combine(BackupFolder, "rentflow-before-restore-20261230-090000.db"), "safety copy");

        Assert.Equal(new DateTime(2026, 11, 8, 10, 15, 30), service.LastBackupTime);
    }

    [Fact]
    public void RestoreFrom_ValidBackup_ReturnsDataAndKeepsCopyOfCurrentData()
    {
        var storage = SqliteAppStorage.Open(_folder);
        new CreateRoomService(storage.Rooms).Create("101", 3000m, false);
        var service = CreateService();
        string backupPath = service.BackUpNow();
        new CreateRoomService(storage.Rooms).Create("102", 3500m, false);
        _now = _now.AddHours(1);

        service.RestoreFrom(backupPath);

        var restoredRoom = Assert.Single(new SqliteRoomRepository(new SqliteDatabase(DatabasePath)).GetAll());
        Assert.Equal("101", restoredRoom.RoomNumber.Value);
        string safetyCopy = Path.Combine(BackupFolder, "rentflow-before-restore-20261107-111530.db");
        Assert.Equal(
            ["101", "102"],
            new SqliteRoomRepository(new SqliteDatabase(safetyCopy)).GetAll().Select(room => room.RoomNumber.Value));
    }

    [Theory]
    [InlineData("text")]
    [InlineData("corrupted")]
    [InlineData("corrupted last page")]
    [InlineData("empty database")]
    [InlineData("missing table")]
    public void RestoreFrom_InvalidFile_ThrowsThaiMessageAndKeepsCurrentData(string kind)
    {
        var storage = SqliteAppStorage.Open(_folder);
        new CreateRoomService(storage.Rooms).Create("101", 3000m, false);
        var service = CreateService();
        string invalidPath = Path.Combine(_folder, "invalid.db");
        switch (kind)
        {
            case "text":
                File.WriteAllText(invalidPath, "not a database");
                break;
            case "corrupted":
                File.Copy(service.BackUpNow(), invalidPath);
                CorruptAfterFirstPage(invalidPath);
                break;
            case "corrupted last page":
                File.Copy(service.BackUpNow(), invalidPath);
                CorruptLastPage(invalidPath);
                break;
            case "missing table":
                File.Copy(service.BackUpNow(), invalidPath);
                using (var connection = new SqliteConnection($"Data Source={invalidPath};Pooling=False"))
                {
                    connection.Open();
                    using var command = connection.CreateCommand();
                    command.CommandText = "DROP TABLE Bills;";
                    command.ExecuteNonQuery();
                }

                break;
            case "empty database":
                using (var connection = new SqliteConnection($"Data Source={invalidPath};Pooling=False"))
                {
                    connection.Open();
                }

                break;
        }

        var exception = Assert.Throws<InvalidOperationException>(() => service.RestoreFrom(invalidPath));

        Assert.Equal(InvalidBackupMessage, exception.Message);
        AssertCurrentDataKept();
    }

    [Fact]
    public void RestoreFrom_BackupFromNewerVersion_ThrowsThaiMessageAndKeepsCurrentData()
    {
        var storage = SqliteAppStorage.Open(_folder);
        new CreateRoomService(storage.Rooms).Create("101", 3000m, false);
        var service = CreateService();
        string newerPath = Path.Combine(_folder, "newer.db");
        File.Copy(service.BackUpNow(), newerPath);
        using (var connection = new SqliteConnection($"Data Source={newerPath};Pooling=False"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA user_version = 999;";
            command.ExecuteNonQuery();
        }

        var exception = Assert.Throws<InvalidOperationException>(() => service.RestoreFrom(newerPath));

        Assert.Equal(NewerBackupMessage, exception.Message);
        AssertCurrentDataKept();
    }

    [Fact]
    public void SettingsTab_ShowsLastBackupTimeAndBacksUpOnClick()
    {
        RunInSta(() =>
        {
            var storage = SqliteAppStorage.Open(_folder);
            using var form = CreateForm(storage, CreateService());
            HiddenTestWindows.ShowHidden(form);
            var tabs = form.Controls.OfType<TabControl>().First();
            var settingsTab = tabs.TabPages[5];
            tabs.SelectedTab = settingsTab;
            var lastBackupLabel = (Label)settingsTab.Controls.Find("LastBackupLabel", true)[0];
            Assert.Equal("ยังไม่มีไฟล์สำรอง", lastBackupLabel.Text);

            ((Button)settingsTab.Controls.Find("BackUpNowButton", true)[0]).PerformClick();

            Assert.True(File.Exists(Path.Combine(BackupFolder, "rentflow-20261107-101530.db")));
            Assert.Equal("สำรองล่าสุด: 07/11/2569 10:15", lastBackupLabel.Text);
            Assert.Equal("สำรองข้อมูลเรียบร้อยแล้ว", settingsTab.Controls.Find("SettingsMessageLabel", true)[0].Text);
        });
    }

    [Fact]
    public void SettingsTab_WithoutBackupService_HidesBackupGroup()
    {
        RunInSta(() =>
        {
            using var form = CreateForm(SqliteAppStorage.Open(_folder), backupService: null);
            var settingsTab = form.Controls.OfType<TabControl>().First().TabPages[5];

            Assert.Empty(settingsTab.Controls.Find("BackupGroup", true));
        });
    }

    private SqliteBackupService CreateService()
    {
        return new SqliteBackupService(new SqliteDatabase(DatabasePath), BackupFolder, () => _now);
    }

    private void AssertCurrentDataKept()
    {
        var room = Assert.Single(new SqliteRoomRepository(new SqliteDatabase(DatabasePath)).GetAll());
        Assert.Equal("101", room.RoomNumber.Value);
        Assert.Empty(Directory.Exists(BackupFolder)
            ? Directory.GetFiles(BackupFolder, "rentflow-before-restore-*.db")
            : []);
    }

    private static void CorruptAfterFirstPage(string path)
    {
        SqliteConnection.ClearAllPools();
        byte[] bytes = File.ReadAllBytes(path);
        for (int i = 4096; i < bytes.Length; i++)
        {
            bytes[i] = 0xFF;
        }

        File.WriteAllBytes(path, bytes);
    }

    // ทำให้เสียเฉพาะหน้าสุดท้าย ไฟล์ยังเปิดและอ่านรายชื่อตารางได้ จึงต้องพึ่งการตรวจความสมบูรณ์ของไฟล์
    private static void CorruptLastPage(string path)
    {
        SqliteConnection.ClearAllPools();
        byte[] bytes = File.ReadAllBytes(path);
        for (int i = bytes.Length - 4096; i < bytes.Length; i++)
        {
            bytes[i] = 0xFF;
        }

        File.WriteAllBytes(path, bytes);
    }

    private static RoomForm CreateForm(SqliteAppStorage storage, SqliteBackupService? backupService)
    {
        return new RoomForm(
            new CreateRoomService(storage.Rooms),
            storage.Rooms,
            new CreateMonthlyBillService(storage.Bills),
            new CreateTenantService(storage.Tenants),
            storage.Tenants,
            storage.MeterReadings,
            storage.Bills,
            billingSettingsRepository: storage.Settings,
            invoiceSettingsRepository: storage.Settings,
            backupService: backupService);
    }
}
