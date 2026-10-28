using Microsoft.Data.SqlClient;
using RentFlowApp.Application.Billing;
using RentFlowApp.Application.Rooms;
using RentFlowApp.Application.Tenants;
using RentFlowApp.Infrastructure.Billing;
using RentFlowApp.Infrastructure.Tenants;

namespace RentFlowApp.Forms;

public class RoomForm : Form
{
    private readonly RoomTabControl _roomTabControl;
    private readonly BillingSettingsTabControl _billingSettingsTabControl;
    private readonly MeterReadingTabControl _meterReadingTabControl;
    private readonly MonthlyBillTabControl _monthlyBillTabControl;
    private readonly TenantTabControl _tenantTabControl;

    public RoomForm(
        CreateRoomService createRoomService,
        IRoomEditor roomRepository,
        CreateMonthlyBillService? createMonthlyBillService = null,
        CreateTenantService? createTenantService = null,
        ITenantRepository? tenantRepository = null,
        IMonthlyMeterReadingRepository? monthlyMeterReadingRepository = null,
        IBillingSettingsRepository? billingSettingsRepository = null,
        UpdateBillingSettingsService? updateBillingSettingsService = null)
    {
        ArgumentNullException.ThrowIfNull(createRoomService);
        ArgumentNullException.ThrowIfNull(roomRepository);

        var monthlyMeterReadingRepo = monthlyMeterReadingRepository ?? new InMemoryMonthlyMeterReadingRepository();
        var tenantRepo = tenantRepository ?? new InMemoryTenantRepository();
        var createTenantSvc = createTenantService ?? new CreateTenantService(tenantRepo);
        var billingSettingsRepo = billingSettingsRepository ?? new InMemoryBillingSettingsRepository();
        var updateBillingSettingsSvc = updateBillingSettingsService ?? new UpdateBillingSettingsService(billingSettingsRepo);
        var saveMonthlyMeterReadingSvc = new SaveMonthlyMeterReadingService(monthlyMeterReadingRepo);

        _roomTabControl = new RoomTabControl(
            createRoomService,
            roomRepository,
            tenantRepo);
        _roomTabControl.RoomsChanged += RoomTabControl_RoomsChanged;

        _billingSettingsTabControl = new BillingSettingsTabControl(
            billingSettingsRepo,
            updateBillingSettingsSvc);

        _meterReadingTabControl = new MeterReadingTabControl(
            roomRepository,
            monthlyMeterReadingRepo,
            saveMonthlyMeterReadingSvc);

        _monthlyBillTabControl = new MonthlyBillTabControl(
            createMonthlyBillService,
            roomRepository,
            monthlyMeterReadingRepo,
            saveMonthlyMeterReadingSvc);

        _tenantTabControl = new TenantTabControl(
            tenantRepo,
            roomRepository,
            createTenantSvc);
        _tenantTabControl.TenantAdded += (s, e) => _roomTabControl.RefreshRoomList();

        InitializeForm();

        try
        {
            _roomTabControl.RefreshRoomList();
            _meterReadingTabControl.RefreshRoomList();
            _monthlyBillTabControl.RefreshRoomList();
            _tenantTabControl.RefreshTab();
            _billingSettingsTabControl.LoadSettings();
        }
        catch (SqlException)
        {
            _roomTabControl.ShowError(
                "ไม่สามารถเชื่อมต่อฐานข้อมูลได้ กรุณาตรวจสอบ SQL Server และลองใหม่อีกครั้ง");
        }
    }

    private void InitializeForm()
    {
        Text = "RentFlow - ระบบจัดการห้องพักและสร้างบิล";
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        Width = 850;
        Height = 650;
        MinimumSize = new Size(750, 600);

        var tabControl = new TabControl
        {
            Dock = DockStyle.Fill
        };

        var roomTabPage = new TabPage("ห้องพัก (Rooms)");
        var meterReadingTabPage = new TabPage("บันทึกมิเตอร์น้ำประปา-ไฟฟ้า");
        var billingTabPage = new TabPage("สร้างบิลประจำเดือน (Monthly Bill)");
        var tenantTabPage = new TabPage("ผู้เช่า (Tenants)");
        var settingsTabPage = new TabPage("ตั้งค่าราคากลาง");

        roomTabPage.Controls.Add(_roomTabControl);
        meterReadingTabPage.Controls.Add(_meterReadingTabControl);
        billingTabPage.Controls.Add(_monthlyBillTabControl);
        tenantTabPage.Controls.Add(_tenantTabControl);
        settingsTabPage.Controls.Add(_billingSettingsTabControl);

        tabControl.TabPages.Add(roomTabPage);
        tabControl.TabPages.Add(meterReadingTabPage);
        tabControl.TabPages.Add(billingTabPage);
        tabControl.TabPages.Add(tenantTabPage);
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
            }
            else if (tabControl.SelectedTab == tenantTabPage)
            {
                _tenantTabControl.RefreshTab();
            }
            else if (tabControl.SelectedTab == settingsTabPage)
            {
                _billingSettingsTabControl.LoadSettings();
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
