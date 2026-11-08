using RentFlowApp.Application.Billing;
using RentFlowApp.Application.Rooms;
using RentFlowApp.Application.Tenants;
using RentFlowApp.Infrastructure.Billing;
using RentFlowApp.Infrastructure.Sqlite;
using RentFlowApp.Infrastructure.Tenants;

namespace RentFlowApp.Forms;

public class RoomForm : Form
{
    private readonly RoomTabControl _roomTabControl;
    private readonly BillingSettingsTabControl _billingSettingsTabControl;
    private readonly MeterReadingTabControl _meterReadingTabControl;
    private readonly MonthlyBillTabControl _monthlyBillTabControl;
    private readonly TenantTabControl _tenantTabControl;
    private readonly ReportTabControl _reportTabControl;

    public RoomForm(
        CreateRoomService createRoomService,
        IRoomEditor roomRepository,
        CreateMonthlyBillService? createMonthlyBillService = null,
        CreateTenantService? createTenantService = null,
        ITenantRepository? tenantRepository = null,
        IMonthlyMeterReadingRepository? monthlyMeterReadingRepository = null,
        IBillRepository? billRepository = null,
        IBillingSettingsRepository? billingSettingsRepository = null,
        UpdateBillingSettingsService? updateBillingSettingsService = null,
        IInvoiceSettingsRepository? invoiceSettingsRepository = null,
        SqliteBackupService? backupService = null,
        Func<string, bool>? confirmBillRevision = null)
    {
        ArgumentNullException.ThrowIfNull(createRoomService);
        ArgumentNullException.ThrowIfNull(roomRepository);

        var monthlyMeterReadingRepo = monthlyMeterReadingRepository ?? new InMemoryMonthlyMeterReadingRepository();
        var tenantRepo = tenantRepository ?? new InMemoryTenantRepository();
        var billRepo = billRepository ?? new InMemoryBillRepository();
        var createTenantSvc = createTenantService ?? new CreateTenantService(tenantRepo);
        var billingSettingsRepo = billingSettingsRepository ?? new InMemoryBillingSettingsRepository();
        var updateBillingSettingsSvc = updateBillingSettingsService ?? new UpdateBillingSettingsService(billingSettingsRepo);
        var saveMonthlyMeterReadingSvc = new SaveMonthlyMeterReadingService(monthlyMeterReadingRepo);
        var invoiceSettingsRepo = invoiceSettingsRepository ?? new InMemoryInvoiceSettingsRepository();

        _roomTabControl = new RoomTabControl(
            createRoomService,
            roomRepository,
            tenantRepo);
        _roomTabControl.RoomsChanged += RoomTabControl_RoomsChanged;

        _billingSettingsTabControl = new BillingSettingsTabControl(
            billingSettingsRepo,
            updateBillingSettingsSvc,
            invoiceSettingsRepo,
            backupService);

        _meterReadingTabControl = new MeterReadingTabControl(
            roomRepository,
            monthlyMeterReadingRepo,
            saveMonthlyMeterReadingSvc);

        _monthlyBillTabControl = new MonthlyBillTabControl(
            createMonthlyBillService,
            roomRepository,
            monthlyMeterReadingRepository: monthlyMeterReadingRepo,
            billRepository: billRepo,
            tenantRepository: tenantRepo,
            billingSettingsRepository: billingSettingsRepo,
            invoiceSettingsRepository: invoiceSettingsRepo,
            confirmBillRevision: confirmBillRevision);

        _tenantTabControl = new TenantTabControl(
            tenantRepo,
            roomRepository,
            createTenantSvc,
            billRepo);
        _tenantTabControl.TenantsChanged += (s, e) => _roomTabControl.RefreshRoomList();

        _reportTabControl = new ReportTabControl(roomRepository, tenantRepo, billRepo);

        InitializeForm();

        _roomTabControl.RefreshRoomList();
        _meterReadingTabControl.RefreshRoomList();
        _monthlyBillTabControl.RefreshRoomList();
        _tenantTabControl.RefreshTab();
        _billingSettingsTabControl.LoadSettings();
        _reportTabControl.RefreshReport();
    }

    private void InitializeForm()
    {
        Text = $"{AppInfo.Name} - ระบบจัดการห้องพักและสร้างบิล";
        Icon = AppInfo.LoadIcon();
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        Width = 850;
        Height = 650;
        MinimumSize = new Size(750, 600);

        var tabControl = new TabControl
        {
            Dock = DockStyle.Fill
        };

        // Name ใช้หาแท็บโดยไม่ผูกกับลำดับ เผื่อเรียงแท็บใหม่ภายหลัง
        var reportTabPage = new TabPage("รายงานข้อมูล") { Name = "ReportTabPage" };
        var meterReadingTabPage = new TabPage("บันทึกมิเตอร์") { Name = "MeterReadingTabPage" };
        var billingTabPage = new TabPage("สร้างบิลประจำเดือน") { Name = "BillingTabPage" };
        var tenantTabPage = new TabPage("จัดการผู้เช่า") { Name = "TenantTabPage" };
        var roomTabPage = new TabPage("จัดการห้องพัก") { Name = "RoomTabPage" };
        var settingsTabPage = new TabPage("ตั้งค่าพื้นฐาน") { Name = "SettingsTabPage" };

        roomTabPage.Controls.Add(_roomTabControl);
        meterReadingTabPage.Controls.Add(_meterReadingTabControl);
        billingTabPage.Controls.Add(_monthlyBillTabControl);
        tenantTabPage.Controls.Add(_tenantTabControl);
        settingsTabPage.Controls.Add(_billingSettingsTabControl);
        reportTabPage.Controls.Add(_reportTabControl);

        // เรียงตามงานที่ใช้บ่อย: ดูรายงาน → จดมิเตอร์ → ออกบิล ส่วนงานตั้งต้นอยู่ท้าย
        tabControl.TabPages.Add(reportTabPage);
        tabControl.TabPages.Add(meterReadingTabPage);
        tabControl.TabPages.Add(billingTabPage);
        tabControl.TabPages.Add(tenantTabPage);
        tabControl.TabPages.Add(roomTabPage);
        tabControl.TabPages.Add(settingsTabPage);

        tabControl.SelectedIndexChanged += (s, e) =>
        {
            if (tabControl.SelectedTab == meterReadingTabPage)
            {
                _meterReadingTabControl.RefreshRoomList();
            }
            else if (tabControl.SelectedTab == billingTabPage)
            {
                _monthlyBillTabControl.RefreshRoomList();
                _monthlyBillTabControl.RefreshBillGrid();
            }
            else if (tabControl.SelectedTab == tenantTabPage)
            {
                _tenantTabControl.RefreshTab();
            }
            else if (tabControl.SelectedTab == settingsTabPage)
            {
                _billingSettingsTabControl.LoadSettings();
            }
            else if (tabControl.SelectedTab == reportTabPage)
            {
                _reportTabControl.RefreshReport();
            }
        };

        Controls.Add(tabControl);
    }

    private void RoomTabControl_RoomsChanged(object? sender, EventArgs e)
    {
        _monthlyBillTabControl.RefreshRoomList();
        _tenantTabControl.RefreshTab();
    }
}
