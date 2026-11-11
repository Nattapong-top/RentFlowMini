using Microsoft.Data.Sqlite;
using RentFlowApp.Application.Billing;
using RentFlowApp.Application.Rooms;
using RentFlowApp.Application.Tenants;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Forms;
using RentFlowApp.Infrastructure.Sqlite;
using RentFlowApp.Tests.UI;
using static RentFlowApp.Tests.UI.InputTestHelper;

namespace RentFlowApp.Tests.Infrastructure.Sqlite;

public sealed class SqliteAppStorageTests : IDisposable
{
    private const string CorruptedFileMessage =
        "ไฟล์ข้อมูลเสียหาย ระบบจึงไม่เปิดใช้งานเพื่อไม่ให้ข้อมูลเดิมถูกทับ กรุณาอย่าลบไฟล์ และกู้คืนจากไฟล์สำรองหรือติดต่อผู้ดูแลระบบ";

    private const string FileInUseMessage =
        "ไฟล์ข้อมูลกำลังถูกใช้งานอยู่ กรุณาปิดโปรแกรม RentFlowMiniApp ที่เปิดค้างไว้แล้วลองใหม่อีกครั้ง";

    private const string CannotOpenMessage =
        "ไม่สามารถเปิดไฟล์ข้อมูลได้ กรุณาตรวจสอบสิทธิ์การเข้าถึงโฟลเดอร์และพื้นที่ว่างในเครื่อง แล้วลองใหม่อีกครั้ง";

    private static readonly BillingPeriod October = BillingPeriod.Create("2026-10");

    private readonly string _folder =
        Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "RentFlowTests", Guid.NewGuid().ToString())).FullName;

    private string DatabasePath => Path.Combine(_folder, "rentflow.db");

    private string SettingsFilePath => Path.Combine(_folder, "settings.json");

    public void Dispose()
    {
        // pool ยังถือไฟล์ไว้แม้ปิด connection แล้ว ถ้าไม่ล้างจะลบไฟล์เทสต์ไม่ได้
        SqliteConnection.ClearAllPools();
        Directory.Delete(_folder, recursive: true);
    }

    [Fact]
    public void Open_OnNewFolder_CreatesEmptyDatabase()
    {
        string newFolder = Path.Combine(_folder, "RentFlow");

        var storage = SqliteAppStorage.Open(newFolder);

        Assert.True(File.Exists(Path.Combine(newFolder, "rentflow.db")));
        Assert.Empty(storage.Rooms.GetAll());
        Assert.Empty(storage.Tenants.GetAll());
        Assert.Empty(storage.MeterReadings.GetAll());
        Assert.Empty(storage.Bills.GetAll());
        Assert.False(storage.SettingsImportFailed);
    }

    [Fact]
    public void Open_WithSettingsFile_ImportsSettings()
    {
        File.WriteAllText(
            SettingsFilePath,
            """{ "WaterRate": 25.5, "ElectricityRate": 9, "MotorcycleParkingRate": 100, "CarParkingRate": 500, "CableTvRate": 60 }""");

        var storage = SqliteAppStorage.Open(_folder);

        Assert.False(storage.SettingsImportFailed);
        Assert.Equal(25.5m, ((IBillingSettingsRepository)storage.Settings).Get().WaterRate.Amount);
    }

    [Fact]
    public void Open_WithUnreadableSettingsFile_ReportsFailureAndStillOpens()
    {
        File.WriteAllText(SettingsFilePath, "{ not json");

        var storage = SqliteAppStorage.Open(_folder);

        Assert.True(storage.SettingsImportFailed);
        Assert.Equal(19m, ((IBillingSettingsRepository)storage.Settings).Get().WaterRate.Amount);
    }

    [Fact]
    public void Open_OnCorruptedFile_ThrowsThaiMessageAndKeepsFile()
    {
        byte[] corrupted = new byte[8192];
        Array.Fill(corrupted, (byte)0x5A);
        File.WriteAllBytes(DatabasePath, corrupted);

        var exception = Assert.Throws<InvalidOperationException>(() => SqliteAppStorage.Open(_folder));

        Assert.Equal(CorruptedFileMessage, exception.Message);
        SqliteConnection.ClearAllPools();
        Assert.Equal(corrupted, File.ReadAllBytes(DatabasePath));
    }

    [Fact]
    public void Open_WhenFolderCannotBeCreated_ThrowsThaiMessage()
    {
        string pathUsedByFile = Path.Combine(_folder, "occupied");
        File.WriteAllText(pathUsedByFile, "not a folder");

        var exception = Assert.Throws<InvalidOperationException>(() => SqliteAppStorage.Open(pathUsedByFile));

        Assert.Equal(CannotOpenMessage, exception.Message);
    }

    [Fact]
    public void Open_OnNewerSchemaVersion_KeepsUpdateMessage()
    {
        SqliteAppStorage.Open(_folder);
        using (var connection = new SqliteConnection($"Data Source={DatabasePath}"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA user_version = 999;";
            command.ExecuteNonQuery();
        }

        var exception = Assert.Throws<InvalidOperationException>(() => SqliteAppStorage.Open(_folder));

        Assert.Equal("ไฟล์ข้อมูลนี้มาจากโปรแกรมรุ่นใหม่กว่า กรุณาอัปเดตโปรแกรมก่อนเปิดใช้งาน", exception.Message);
    }

    [Theory]
    [InlineData(11, CorruptedFileMessage)]
    [InlineData(26, CorruptedFileMessage)]
    [InlineData(5, FileInUseMessage)]
    [InlineData(6, FileInUseMessage)]
    [InlineData(8, CannotOpenMessage)]
    [InlineData(13, CannotOpenMessage)]
    [InlineData(14, CannotOpenMessage)]
    public void DescribeOpenFailure_ChoosesMessageFromErrorCode(int errorCode, string expectedMessage)
    {
        Assert.Equal(
            expectedMessage,
            SqliteAppStorage.DescribeOpenFailure(new SqliteException("raw error", errorCode)));
    }

    [Fact]
    public void ReopeningApp_KeepsEveryKindOfData()
    {
        var firstRun = SqliteAppStorage.Open(_folder);
        var room = new CreateRoomService(firstRun.Rooms).Create("101", 2800.25m, false);
        var tenant = new CreateTenantService(firstRun.Tenants).Create("Anna", "Example", "0800000000", room.Id);
        new SaveMonthlyMeterReadingService(firstRun.MeterReadings).Save(
            room.Id,
            October,
            MeterReading.Create(100m),
            MeterReading.Create(110.5m),
            MeterReading.Create(1000m),
            MeterReading.Create(1020.25m));
        var bill = new CreateMonthlyBillService(firstRun.Bills).Create(new CreateMonthlyBillRequest(
            room.Id,
            October,
            room.MonthlyRent.Amount,
            MeterReading.Create(100m),
            MeterReading.Create(110.5m),
            MeterReading.Create(1000m),
            MeterReading.Create(1020.25m),
            19.5m,
            8.25m,
            CableTvRate: 60m,
            Tenant: BillTenantSnapshot.From(tenant)));
        new UpdateBillChargesService(firstRun.Bills).Update(new UpdateBillChargesRequest(
            bill.Id,
            ChargeMotorcycleParking: true,
            MotorcycleParkingRate: 100m,
            ChargeCarParking: false,
            CarParkingRate: 500m,
            IsCableTvExempt: false,
            CableTvRate: 60m));
        var paidOn = DateOnly.FromDateTime(DateTime.Now);
        new BillPaymentService(firstRun.Bills).RecordPayment(bill.Id, paidOn, PaymentMethod.BankTransfer);
        decimal totalBeforeClosing = firstRun.Bills.Get(bill.Id)!.Total.Amount;
        ((IBillingSettingsRepository)firstRun.Settings).Save(BillingSettings.Create(25m, 9m, 150m, 600m, 70m));
        ((IInvoiceSettingsRepository)firstRun.Settings).Save(InvoiceSettings.Create("หอทดสอบ", "ที่อยู่ทดสอบ", null));

        // ล้าง pool เพื่อให้เหมือนปิดโปรแกรมจริง โดยไม่มี connection เดิมค้างอยู่
        SqliteConnection.ClearAllPools();
        var secondRun = SqliteAppStorage.Open(_folder);

        var savedRoom = Assert.Single(secondRun.Rooms.GetAll());
        Assert.Equal("101", savedRoom.RoomNumber.Value);
        Assert.Equal(2800.25m, savedRoom.MonthlyRent.Amount);
        var savedTenant = Assert.Single(secondRun.Tenants.GetAll());
        Assert.Equal(tenant.Id, savedTenant.Id);
        Assert.Equal(room.Id, savedTenant.RoomId);
        var savedReading = Assert.Single(secondRun.MeterReadings.GetAll());
        Assert.Equal(110.5m, savedReading.CurrentWaterMeter.Value);
        var savedBill = Assert.Single(secondRun.Bills.GetAll());
        Assert.Equal(bill.Id, savedBill.Id);
        Assert.Equal(bill.InvoiceNumber, savedBill.InvoiceNumber);
        Assert.Equal(100m, savedBill.MotorcycleParking.Amount);
        Assert.Equal(totalBeforeClosing, savedBill.Total.Amount);
        Assert.Equal(new BillPayment(paidOn, PaymentMethod.BankTransfer), savedBill.Payment);
        Assert.Equal(25m, ((IBillingSettingsRepository)secondRun.Settings).Get().WaterRate.Amount);
        Assert.Equal("หอทดสอบ", ((IInvoiceSettingsRepository)secondRun.Settings).Get().PropertyName);
    }

    [Fact]
    public void ReopeningApp_ShowsSavedRoomOnRoomTab()
    {
        RunInSta(() =>
        {
            var firstRun = SqliteAppStorage.Open(_folder);
            var room = new CreateRoomService(firstRun.Rooms).Create("101", 2800m, false);
            new CreateTenantService(firstRun.Tenants).Create("Anna", "Example", null, room.Id);

            SqliteConnection.ClearAllPools();
            var secondRun = SqliteAppStorage.Open(_folder);

            using var form = CreateForm(secondRun);
            var roomTab = form.Controls.OfType<TabControl>().First().TabPages["RoomTabPage"]!;
            var grid = (DataGridView)roomTab.Controls.Find("RoomGrid", true).First();

            Assert.Equal("ไม่ว่าง", grid.Rows[0].Cells["OccupancyStatus"].Value);
            Assert.Equal("Anna Example", grid.Rows[0].Cells["TenantName"].Value);
        });
    }

    [Fact]
    public void MainForm_OnNewDatabase_OpensEmptyWithDefaultRates()
    {
        RunInSta(() =>
        {
            using var form = CreateForm(SqliteAppStorage.Open(_folder));
            var tabs = form.Controls.OfType<TabControl>().First();
            var grid = (DataGridView)tabs.TabPages["RoomTabPage"]!.Controls.Find("RoomGrid", true)[0];
            var waterRate = (TextBox)tabs.TabPages["SettingsTabPage"]!.Controls.Find("WaterRateTextBox", true)[0];

            Assert.Equal(0, grid.Rows.Count);
            Assert.Equal("19", waterRate.Text);
        });
    }

    [Fact]
    public void MainForm_DataEnteredOnScreen_IsStillThereAfterReopening()
    {
        RunInSta(() =>
        {
            var period = BillingPeriod.Create("2026-10");

            var firstStorage = SqliteAppStorage.Open(_folder);
            using (var firstRun = CreateForm(firstStorage))
            {
                HiddenTestWindows.ShowHidden(firstRun);
                var tabs = firstRun.Controls.OfType<TabControl>().First();

                var roomTab = tabs.TabPages["RoomTabPage"]!;
                tabs.SelectedTab = roomTab;
                ((TextBox)roomTab.Controls.Find("RoomNumberTextBox", true)[0]).Text = "101";
                ((TextBox)roomTab.Controls.Find("RoomMonthlyRentTextBox", true)[0]).Text = "3000";
                ((Button)roomTab.Controls.Find("AddRoomButton", true)[0]).PerformClick();
                TestTenants.MoveInto(firstStorage.Tenants, Assert.Single(firstStorage.Rooms.GetAll()).Id);

                var billTab = tabs.TabPages["BillingTabPage"]!;
                tabs.SelectedTab = billTab;
                ((BillingPeriodSelector)billTab.Controls.Find("BillingPeriodSelector", true)[0]).SetBillingPeriod(period);
                ((ComboBox)billTab.Controls.Find("BillRoomComboBox", true)[0]).SelectedIndex = 0;
                ((TextBox)billTab.Controls.Find("PrevWaterTextBox", true)[0]).Text = "100";
                ((TextBox)billTab.Controls.Find("CurrWaterTextBox", true)[0]).Text = "110";
                ((TextBox)billTab.Controls.Find("PrevElecTextBox", true)[0]).Text = "500";
                ((TextBox)billTab.Controls.Find("CurrElecTextBox", true)[0]).Text = "550";
                ((CheckBox)billTab.Controls.Find("IsCableTvExemptCheckBox", true)[0]).Checked = true;
                ((Button)billTab.Controls.Find("CreateBillButton", true)[0]).PerformClick();
            }

            SqliteConnection.ClearAllPools();
            var storage = SqliteAppStorage.Open(_folder);

            using var secondRun = CreateForm(storage);
            HiddenTestWindows.ShowHidden(secondRun);
            var reopenedTabs = secondRun.Controls.OfType<TabControl>().First();
            var roomGrid = (DataGridView)reopenedTabs.TabPages["RoomTabPage"]!.Controls.Find("RoomGrid", true)[0];
            var reopenedBillTab = reopenedTabs.TabPages["BillingTabPage"]!;
            reopenedTabs.SelectedTab = reopenedBillTab;
            ((BillingPeriodSelector)reopenedBillTab.Controls.Find("BillingPeriodSelector", true)[0]).SetBillingPeriod(period);
            var billGrid = (DataGridView)reopenedBillTab.Controls.Find("BillGrid", true)[0];

            Assert.Equal(1, roomGrid.Rows.Count);
            Assert.Equal(1, billGrid.Rows.Count);
            var savedBill = Assert.Single(storage.Bills.GetAll());
            Assert.Equal(3590m, savedBill.Total.Amount);
            Assert.Equal("N001", savedBill.InvoiceNumber);
        });
    }

    [Fact]
    public void MainForm_WhenDatabaseCannotBeRead_RaisesErrorThatHasThaiMessage()
    {
        // Program ดัก error นี้ตอนสร้างฟอร์ม แล้วแสดงข้อความภาษาไทยก่อนปิดแอป
        RunInSta(() =>
        {
            var storage = SqliteAppStorage.Open(_folder);
            using (var connection = new SqliteConnection($"Data Source={DatabasePath}"))
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "DROP TABLE Rooms;";
                command.ExecuteNonQuery();
            }

            var exception = Assert.Throws<SqliteException>(() => CreateForm(storage));

            Assert.Equal(CannotOpenMessage, SqliteAppStorage.DescribeOpenFailure(exception));
        });
    }

    private static MainForm CreateForm(SqliteAppStorage storage)
    {
        return new MainForm(
            new CreateRoomService(storage.Rooms),
            storage.Rooms,
            new CreateMonthlyBillService(storage.Bills),
            new CreateTenantService(storage.Tenants),
            storage.Tenants,
            storage.MeterReadings,
            storage.Bills,
            billingSettingsRepository: storage.Settings,
            invoiceSettingsRepository: storage.Settings);
    }
}
