using RentFlowApp.Application.Billing;
using RentFlowApp.Application.Rooms;
using RentFlowApp.Application.Tenants;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Forms;
using RentFlowApp.Infrastructure.Billing;
using RentFlowApp.Infrastructure.Rooms;
using RentFlowApp.Infrastructure.Tenants;
using static RentFlowApp.Tests.UI.InputTestHelper;


namespace RentFlowApp.Tests.UI;

public class RoomFormTests
{
    [Fact]
    public void RoomForm_InitializesWithSixTabsAndDefaultRates()
    {
        RunInSta(() =>
        {
            // Arrange
            var roomRepo = new InMemoryRoomRepository();
            var billRepo = new InMemoryBillRepository();
            var createRoomService = new CreateRoomService(roomRepo);
            var createBillService = new CreateMonthlyBillService(billRepo);

            // Act
            using var form = new RoomForm(createRoomService, roomRepo, createBillService);

            // Assert
            var tabControl = form.Controls.OfType<TabControl>().FirstOrDefault();
            Assert.NotNull(tabControl);
            Assert.Equal(6, tabControl.TabPages.Count);
            Assert.Equal("ห้องพัก (Rooms)", tabControl.TabPages[0].Text);
            Assert.Equal("บันทึกมิเตอร์น้ำประปา-ไฟฟ้า", tabControl.TabPages[1].Text);
            Assert.Equal("สร้างบิลประจำเดือน (Monthly Bill)", tabControl.TabPages[2].Text);
            Assert.Equal("ผู้เช่า (Tenants)", tabControl.TabPages[3].Text);
            Assert.Equal("รายงาน", tabControl.TabPages[4].Text);
            Assert.Equal("ตั้งค่าพื้นฐาน", tabControl.TabPages[5].Text);

            var billingTab = tabControl.TabPages[2];
            var waterRateTextBox = billingTab.Controls.Find("WaterRateTextBox", true).FirstOrDefault() as TextBox;
            Assert.NotNull(waterRateTextBox);
            Assert.Equal("19", waterRateTextBox.Text);

            var elecRateTextBox = billingTab.Controls.Find("ElecRateTextBox", true).FirstOrDefault() as TextBox;
            Assert.NotNull(elecRateTextBox);
            Assert.Equal("8", elecRateTextBox.Text);

            var settingsTab = tabControl.TabPages[5];
            var settingsWaterBox = settingsTab.Controls.Find("WaterRateTextBox", true).FirstOrDefault() as TextBox;
            Assert.NotNull(settingsWaterBox);
            Assert.Equal("19", settingsWaterBox.Text);

            var settingsCableBox = settingsTab.Controls.Find("CableTvRateTextBox", true).FirstOrDefault() as TextBox;
            Assert.NotNull(settingsCableBox);
            Assert.Equal("60", settingsCableBox.Text);
        });
    }

    [Fact]
    public void RoomForm_RoomTab_ContainsRentAndCableTvExemptionControls()
    {
        RunInSta(() =>
        {
            var roomRepo = new InMemoryRoomRepository();
            var createRoomService = new CreateRoomService(roomRepo);

            using var form = new RoomForm(createRoomService, roomRepo);
            var roomTab = form.Controls.OfType<TabControl>().First().TabPages[0];

            var rentBox = roomTab.Controls
                .Find("RoomMonthlyRentTextBox", true)
                .FirstOrDefault() as TextBox;
            var exemptCheckBox = roomTab.Controls
                .Find("RoomCableTvExemptCheckBox", true)
                .FirstOrDefault() as CheckBox;

            Assert.NotNull(rentBox);
            Assert.NotNull(exemptCheckBox);
        });
    }

    [Fact]
    public void RoomForm_RoomTab_UsesResponsiveLayoutAndHidesRoomId()
    {
        RunInSta(() =>
        {
            var roomRepo = new InMemoryRoomRepository();
            var createRoomService = new CreateRoomService(roomRepo);
            createRoomService.Create("201", 2800, false);
            createRoomService.Create("101", 2500, true);

            using var form = new RoomForm(createRoomService, roomRepo);
            var tabControl = form.Controls.OfType<TabControl>().First();
            var roomTab = tabControl.TabPages[0];
            var grid = (DataGridView)roomTab.Controls.Find("RoomGrid", true).First();

            Assert.Equal(FormWindowState.Maximized, form.WindowState);
            Assert.NotNull(roomTab.Controls.Find("RoomInputGroup", true).FirstOrDefault());
            Assert.NotNull(roomTab.Controls.Find("RoomListGroup", true).FirstOrDefault());
            Assert.Equal(6, grid.Columns.Count);
            Assert.Equal("Sequence", grid.Columns[0].Name);
            Assert.DoesNotContain(grid.Columns.Cast<DataGridViewColumn>(), column => column.Name == "RoomId");
            Assert.Equal("101", grid.Rows[0].Cells["RoomNumber"].Value);
            Assert.Equal("201", grid.Rows[1].Cells["RoomNumber"].Value);
            Assert.Equal(1, grid.Rows[0].Cells["Sequence"].Value);
            Assert.Equal("2,500.00", grid.Rows[0].Cells["MonthlyRent"].Value);
            Assert.Equal("ว่าง", grid.Rows[0].Cells["OccupancyStatus"].Value);
            Assert.Equal("-", grid.Rows[0].Cells["TenantName"].Value);
        });
    }

    [Fact]
    public void RoomForm_RoomTab_ShowsOccupancyAndTenantName()
    {
        RunInSta(() =>
        {
            var roomRepo = new InMemoryRoomRepository();
            var tenantRepo = new InMemoryTenantRepository();
            var createRoomService = new CreateRoomService(roomRepo);
            var createTenantService = new CreateTenantService(tenantRepo);
            var room = createRoomService.Create("101", 2500, false);
            createTenantService.Create("สมชาย", "ใจดี", null, room.Id);

            using var form = new RoomForm(
                createRoomService,
                roomRepo,
                tenantRepository: tenantRepo);
            var roomTab = form.Controls.OfType<TabControl>().First().TabPages[0];
            var grid = (DataGridView)roomTab.Controls.Find("RoomGrid", true).First();

            Assert.Equal("ไม่ว่าง", grid.Rows[0].Cells["OccupancyStatus"].Value);
            Assert.Equal("สมชาย ใจดี", grid.Rows[0].Cells["TenantName"].Value);
        });
    }

    [Fact]
    public void RoomForm_AddRoom_WithRentAndExemption_SavesRoom()
    {
        RunInSta(() =>
        {
            var roomRepo = new InMemoryRoomRepository();
            var createRoomService = new CreateRoomService(roomRepo);

            using var form = new RoomForm(createRoomService, roomRepo);
            form.Show();
            var roomTab = form.Controls.OfType<TabControl>().First().TabPages[0];

            var roomNumberBox = (TextBox)roomTab.Controls
                .Find("RoomNumberTextBox", true).First();
            var rentBox = (TextBox)roomTab.Controls
                .Find("RoomMonthlyRentTextBox", true).First();
            var exemptCheckBox = (CheckBox)roomTab.Controls
                .Find("RoomCableTvExemptCheckBox", true).First();
            var addButton = (Button)roomTab.Controls
                .Find("AddRoomButton", true).First();

            roomNumberBox.Text = "101";
            rentBox.Text = "5000";
            exemptCheckBox.Checked = true;

            addButton.PerformClick();

            var room = Assert.Single(roomRepo.GetAll());
            Assert.Equal(5000, room.MonthlyRent.Amount);
            Assert.True(room.IsCableTvExempt);
        });
    }

    [Fact]
    public void RoomForm_EditSelectedRoomRent_WithZero_UpdatesRoom()
    {
        RunInSta(() =>
        {
            var roomRepo = new InMemoryRoomRepository();
            var createRoomService = new CreateRoomService(roomRepo);
            var room = createRoomService.Create("101", 5000, true);

            using var form = new RoomForm(createRoomService, roomRepo);
            form.Show();
            var roomTab = form.Controls.OfType<TabControl>().First().TabPages[0];
            var grid = (DataGridView)roomTab.Controls
                .Find("RoomGrid", true).First();
            var rentBox = (TextBox)roomTab.Controls
                .Find("RoomMonthlyRentTextBox", true).First();
            var editButton = (Button)roomTab.Controls
                .Find("EditRoomRentButton", true).First();

            grid.Rows[0].Selected = true;
            editButton.PerformClick();
            rentBox.Text = "0";
            editButton.PerformClick();

            var updatedRoom = roomRepo.GetById(room.Id)!;
            Assert.Equal(0, updatedRoom.MonthlyRent.Amount);
            Assert.True(updatedRoom.IsCableTvExempt);
        });
    }

    [Fact]
    public void RoomForm_DoubleClickRoomRow_StartsRentEditingImmediately()
    {
        RunInSta(() =>
        {
            var roomRepo = new InMemoryRoomRepository();
            var createRoomService = new CreateRoomService(roomRepo);
            var room = createRoomService.Create("101", 5000, true);

            using var form = new RoomForm(createRoomService, roomRepo);
            form.Show();
            var roomTab = form.Controls.OfType<TabControl>().First().TabPages[0];
            var grid = (DataGridView)roomTab.Controls.Find("RoomGrid", true).First();
            var roomNumberBox = (TextBox)roomTab.Controls.Find("RoomNumberTextBox", true).First();
            var rentBox = (TextBox)roomTab.Controls.Find("RoomMonthlyRentTextBox", true).First();
            var exemptCheckBox = (CheckBox)roomTab.Controls.Find("RoomCableTvExemptCheckBox", true).First();
            var addButton = (Button)roomTab.Controls.Find("AddRoomButton", true).First();
            var editButton = (Button)roomTab.Controls.Find("EditRoomRentButton", true).First();

            var onCellDoubleClick = typeof(DataGridView).GetMethod(
                "OnCellDoubleClick",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            onCellDoubleClick!.Invoke(grid, [new DataGridViewCellEventArgs(1, 0)]);

            Assert.Equal(room.RoomNumber.Value, roomNumberBox.Text);
            Assert.Equal("5,000.00", rentBox.Text);
            Assert.True(exemptCheckBox.Checked);
            Assert.True(roomNumberBox.ReadOnly);
            Assert.False(exemptCheckBox.Enabled);
            Assert.False(addButton.Enabled);
            Assert.True(editButton.Enabled);
            Assert.Equal("บันทึกค่าเช่า", editButton.Text);
        });
    }

    [Fact]
    public void RoomForm_InitializesWithTenantTabAndControls()
    {
        RunInSta(() =>
        {
            // Arrange
            var roomRepo = new InMemoryRoomRepository();
            var billRepo = new InMemoryBillRepository();
            var createRoomService = new CreateRoomService(roomRepo);
            var createBillService = new CreateMonthlyBillService(billRepo);

            // Act
            using var form = new RoomForm(createRoomService, roomRepo, createBillService);

            // Assert
            var tabControl = form.Controls.OfType<TabControl>().FirstOrDefault();
            Assert.NotNull(tabControl);
            Assert.Equal(6, tabControl.TabPages.Count);

            var tenantTab = tabControl.TabPages[3];
            Assert.Equal("ผู้เช่า (Tenants)", tenantTab.Text);

            var firstNameBox = tenantTab.Controls.Find("TenantFirstNameTextBox", true).FirstOrDefault() as TextBox;
            Assert.NotNull(firstNameBox);

            var lastNameBox = tenantTab.Controls.Find("TenantLastNameTextBox", true).FirstOrDefault() as TextBox;
            Assert.NotNull(lastNameBox);

            var phoneBox = tenantTab.Controls.Find("TenantPhoneTextBox", true).FirstOrDefault() as TextBox;
            Assert.NotNull(phoneBox);

            var roomComboBox = tenantTab.Controls.Find("TenantRoomComboBox", true).FirstOrDefault() as ComboBox;
            Assert.NotNull(roomComboBox);

            var addTenantButton = tenantTab.Controls.Find("AddTenantButton", true).FirstOrDefault() as Button;
            Assert.NotNull(addTenantButton);

            var messageLabel = tenantTab.Controls.Find("TenantMessageLabel", true).FirstOrDefault() as Label;
            Assert.NotNull(messageLabel);

            var tenantGrid = tenantTab.Controls.Find("TenantGrid", true).FirstOrDefault() as DataGridView;
            Assert.NotNull(tenantGrid);
            Assert.False(tenantGrid.Columns["TenantId"]!.Visible);
            Assert.True(tenantGrid.Columns["FullName"]!.Visible);
        });
    }

    [Fact]
    public void RoomForm_WhenRoomsExist_PopulatesRoomSelectionInBillingTab()
    {
        RunInSta(() =>
        {
            // Arrange
            var roomRepo = new InMemoryRoomRepository();
            var billRepo = new InMemoryBillRepository();
            var createRoomService = new CreateRoomService(roomRepo);
            var createBillService = new CreateMonthlyBillService(billRepo);

            createRoomService.Create("101");
            createRoomService.Create("102");

            // Act
            using var form = new RoomForm(createRoomService, roomRepo, createBillService);
            var billingTab = form.Controls.OfType<TabControl>().First().TabPages[2];
            var comboBox = billingTab.Controls.Find("BillRoomComboBox", true).First() as ComboBox;
            Assert.NotNull(comboBox);

            // Assert
            Assert.Equal(2, comboBox.Items.Count);
            Assert.Equal("101", comboBox.Items[0]?.ToString());
            Assert.Equal("102", comboBox.Items[1]?.ToString());
        });
    }

    [Fact]
    public void RoomForm_CreateBill_WhenInputValid_CalculatesAndSavesBill()
    {
        RunInSta(() =>
        {
            // Arrange
            var roomRepo = new InMemoryRoomRepository();
            var billRepo = new InMemoryBillRepository();
            var createRoomService = new CreateRoomService(roomRepo);
            var createBillService = new CreateMonthlyBillService(billRepo);

            createRoomService.Create("101");

            using var form = new RoomForm(createRoomService, roomRepo, createBillService);
            form.Show();
            var tabControl = form.Controls.OfType<TabControl>().First();
            var billingTab = tabControl.TabPages[2];
            tabControl.SelectedTab = billingTab;
            var comboBox = (ComboBox)billingTab.Controls.Find("BillRoomComboBox", true).First();
            comboBox.SelectedIndex = 0;

            var periodSelector = (BillingPeriodSelector)billingTab.Controls.Find("BillingPeriodSelector", true).First();
            periodSelector.SetBillingPeriod(BillingPeriod.Create("2026-10"));

            var rentBox = (TextBox)billingTab.Controls.Find("MonthlyRentTextBox", true).First();
            var prevWaterBox = (TextBox)billingTab.Controls.Find("PrevWaterTextBox", true).First();
            var currWaterBox = (TextBox)billingTab.Controls.Find("CurrWaterTextBox", true).First();
            var prevElecBox = (TextBox)billingTab.Controls.Find("PrevElecTextBox", true).First();
            var currElecBox = (TextBox)billingTab.Controls.Find("CurrElecTextBox", true).First();
            var createButton = (Button)billingTab.Controls.Find("CreateBillButton", true).First();
            var totalLabel = (Label)billingTab.Controls.Find("ResultTotalLabel", true).First();
            var statusLabel = (Label)billingTab.Controls.Find("BillMessageLabel", true).First();

            rentBox.Text = "3000";
            prevWaterBox.Text = "100";
            currWaterBox.Text = "110"; // 10 units * 19 = 190
            prevElecBox.Text = "500";
            currElecBox.Text = "550"; // 50 units * 8 = 400
            
            var cableTvExemptBox = (CheckBox)billingTab.Controls.Find("IsCableTvExemptCheckBox", true).First();
            cableTvExemptBox.Checked = true;

            // Act
            createButton.PerformClick();

            // Assert: Total should be 3000 + 190 + 400 = 3590.00
            Assert.Contains("3,590.00", totalLabel.Text);
            Assert.Equal(System.Drawing.Color.DarkGreen, statusLabel.ForeColor);
            Assert.Contains("สำเร็จเรียบร้อย", statusLabel.Text);
        });
    }

    [Fact]
    public void RoomForm_CreateBill_WhenCurrentMeterLowerThanPrevious_DisplaysErrorMessage()
    {
        RunInSta(() =>
        {
            // Arrange
            var roomRepo = new InMemoryRoomRepository();
            var billRepo = new InMemoryBillRepository();
            var createRoomService = new CreateRoomService(roomRepo);
            var createBillService = new CreateMonthlyBillService(billRepo);

            createRoomService.Create("101");

            using var form = new RoomForm(createRoomService, roomRepo, createBillService);
            form.Show();
            var tabControl = form.Controls.OfType<TabControl>().First();
            var billingTab = tabControl.TabPages[2];
            tabControl.SelectedTab = billingTab;
            var comboBox = (ComboBox)billingTab.Controls.Find("BillRoomComboBox", true).First();
            comboBox.SelectedIndex = 0;

            var periodSelector = (BillingPeriodSelector)billingTab.Controls.Find("BillingPeriodSelector", true).First();
            periodSelector.SetBillingPeriod(BillingPeriod.Create("2026-10"));

            var rentBox = (TextBox)billingTab.Controls.Find("MonthlyRentTextBox", true).First();
            var prevWaterBox = (TextBox)billingTab.Controls.Find("PrevWaterTextBox", true).First();
            var currWaterBox = (TextBox)billingTab.Controls.Find("CurrWaterTextBox", true).First();
            var prevElecBox = (TextBox)billingTab.Controls.Find("PrevElecTextBox", true).First();
            var currElecBox = (TextBox)billingTab.Controls.Find("CurrElecTextBox", true).First();
            var createButton = (Button)billingTab.Controls.Find("CreateBillButton", true).First();
            var statusLabel = (Label)billingTab.Controls.Find("BillMessageLabel", true).First();

            rentBox.Text = "3000";
            prevWaterBox.Text = "100";
            currWaterBox.Text = "90"; // Invalid: current < previous
            prevElecBox.Text = "500";
            currElecBox.Text = "550";

            // Act
            createButton.PerformClick();

            // Assert
            Assert.Equal(System.Drawing.Color.DarkRed, statusLabel.ForeColor);
            Assert.Contains("มิเตอร์", statusLabel.Text);
        });
    }

    [Fact]
    public void RoomForm_TenantTab_PopulatesOnlyAvailableRooms()
    {
        RunInSta(() =>
        {
            // Arrange
            var roomRepo = new InMemoryRoomRepository();
            var billRepo = new InMemoryBillRepository();
            var tenantRepo = new InMemoryTenantRepository();
            var createRoomService = new CreateRoomService(roomRepo);
            var createBillService = new CreateMonthlyBillService(billRepo);
            var createTenantService = new CreateTenantService(tenantRepo);

            var room1 = createRoomService.Create("101");
            var room2 = createRoomService.Create("102");

            createTenantService.Create("สมชาย", "ใจดี", null, room1.Id);

            // Act
            using var form = new RoomForm(createRoomService, roomRepo, createBillService, createTenantService, tenantRepo);
            form.Show();
            var tabControl = form.Controls.OfType<TabControl>().First();
            var tenantTab = tabControl.TabPages[3];
            tabControl.SelectedTab = tenantTab;

            var comboBox = (ComboBox)tenantTab.Controls.Find("TenantRoomComboBox", true).First();

            // Assert: Should contain "-- ไม่ระบุห้องพัก --" and "ห้อง 102"
            Assert.Equal(2, comboBox.Items.Count);
            Assert.Equal("-- ไม่ระบุห้องพัก --", comboBox.Items[0]?.ToString());
            Assert.Equal("ห้อง 102", comboBox.Items[1]?.ToString());
        });
    }

    [Fact]
    public void RoomForm_TenantTab_AddTenant_WhenValidInput_AddsTenantToGrid()
    {
        RunInSta(() =>
        {
            // Arrange
            var roomRepo = new InMemoryRoomRepository();
            var billRepo = new InMemoryBillRepository();
            var tenantRepo = new InMemoryTenantRepository();
            var createRoomService = new CreateRoomService(roomRepo);
            var createBillService = new CreateMonthlyBillService(billRepo);
            var createTenantService = new CreateTenantService(tenantRepo);

            createRoomService.Create("101");

            using var form = new RoomForm(createRoomService, roomRepo, createBillService, createTenantService, tenantRepo);
            form.Show();
            var tabControl = form.Controls.OfType<TabControl>().First();
            var tenantTab = tabControl.TabPages[3];
            tabControl.SelectedTab = tenantTab;

            var firstNameBox = (TextBox)tenantTab.Controls.Find("TenantFirstNameTextBox", true).First();
            var lastNameBox = (TextBox)tenantTab.Controls.Find("TenantLastNameTextBox", true).First();
            var phoneBox = (TextBox)tenantTab.Controls.Find("TenantPhoneTextBox", true).First();
            var roomComboBox = (ComboBox)tenantTab.Controls.Find("TenantRoomComboBox", true).First();
            var addButton = (Button)tenantTab.Controls.Find("AddTenantButton", true).First();
            var messageLabel = (Label)tenantTab.Controls.Find("TenantMessageLabel", true).First();
            var grid = (DataGridView)tenantTab.Controls.Find("TenantGrid", true).First();

            firstNameBox.Text = "สมปอง";
            lastNameBox.Text = "มีสุข";
            phoneBox.Text = "0812345678";
            roomComboBox.SelectedIndex = 1; // "ห้อง 101"

            // Act
            addButton.PerformClick();

            // Assert
            Assert.Equal(System.Drawing.Color.DarkGreen, messageLabel.ForeColor);
            Assert.Contains("สมปอง มีสุข", messageLabel.Text);
            Assert.Contains("สำเร็จแล้ว", messageLabel.Text);

            var dataSource = grid.DataSource as System.Collections.IList;
            Assert.NotNull(dataSource);
            Assert.Single(dataSource);
        });
    }

    [Fact]
    public void RoomForm_TenantTab_AddTenant_WhenMissingFirstName_DisplaysErrorMessage()
    {
        RunInSta(() =>
        {
            // Arrange
            var roomRepo = new InMemoryRoomRepository();
            var billRepo = new InMemoryBillRepository();
            var tenantRepo = new InMemoryTenantRepository();
            var createRoomService = new CreateRoomService(roomRepo);
            var createBillService = new CreateMonthlyBillService(billRepo);
            var createTenantService = new CreateTenantService(tenantRepo);

            using var form = new RoomForm(createRoomService, roomRepo, createBillService, createTenantService, tenantRepo);
            form.Show();
            var tabControl = form.Controls.OfType<TabControl>().First();
            var tenantTab = tabControl.TabPages[3];
            tabControl.SelectedTab = tenantTab;

            var firstNameBox = (TextBox)tenantTab.Controls.Find("TenantFirstNameTextBox", true).First();
            var lastNameBox = (TextBox)tenantTab.Controls.Find("TenantLastNameTextBox", true).First();
            var addButton = (Button)tenantTab.Controls.Find("AddTenantButton", true).First();
            var messageLabel = (Label)tenantTab.Controls.Find("TenantMessageLabel", true).First();

            firstNameBox.Text = "  "; // Empty
            lastNameBox.Text = "มีสุข";

            // Act
            addButton.PerformClick();

            // Assert
            Assert.Equal(System.Drawing.Color.DarkRed, messageLabel.ForeColor);
            Assert.Contains("ต้องระบุชื่อ", messageLabel.Text);
        });
    }

    [Fact]
    public void RoomForm_TenantTab_AddTenant_WhenInvalidPhoneNumber_DisplaysErrorMessage()
    {
        RunInSta(() =>
        {
            // Arrange
            var roomRepo = new InMemoryRoomRepository();
            var billRepo = new InMemoryBillRepository();
            var tenantRepo = new InMemoryTenantRepository();
            var createRoomService = new CreateRoomService(roomRepo);
            var createBillService = new CreateMonthlyBillService(billRepo);
            var createTenantService = new CreateTenantService(tenantRepo);

            using var form = new RoomForm(createRoomService, roomRepo, createBillService, createTenantService, tenantRepo);
            form.Show();
            var tabControl = form.Controls.OfType<TabControl>().First();
            var tenantTab = tabControl.TabPages[3];
            tabControl.SelectedTab = tenantTab;

            var firstNameBox = (TextBox)tenantTab.Controls.Find("TenantFirstNameTextBox", true).First();
            var lastNameBox = (TextBox)tenantTab.Controls.Find("TenantLastNameTextBox", true).First();
            var phoneBox = (TextBox)tenantTab.Controls.Find("TenantPhoneTextBox", true).First();
            var addButton = (Button)tenantTab.Controls.Find("AddTenantButton", true).First();
            var messageLabel = (Label)tenantTab.Controls.Find("TenantMessageLabel", true).First();

            Assert.Equal(10, phoneBox.MaxLength);

            firstNameBox.Text = "สมปอง";
            lastNameBox.Text = "มีสุข";
            phoneBox.Text = "12345"; // Invalid: less than 9 digits

            // Act
            addButton.PerformClick();

            // Assert
            Assert.Equal(System.Drawing.Color.DarkRed, messageLabel.ForeColor);
            Assert.Contains("เบอร์โทรศัพท์ต้องเป็นตัวเลข 9-10 หลัก", messageLabel.Text);
        });
    }

    [Fact]
    public void RoomForm_BillingTab_WhenNoMeterReadingExists_DisplaysWarningMessage()
    {
        RunInSta(() =>
        {
            // Arrange
            var roomRepo = new InMemoryRoomRepository();
            var meterRepo = new InMemoryMonthlyMeterReadingRepository();
            var createRoomService = new CreateRoomService(roomRepo);
            createRoomService.Create("101");

            using var form = new RoomForm(createRoomService, roomRepo, null, null, null, meterRepo, null);
            form.Show();
            var tabControl = form.Controls.OfType<TabControl>().First();
            var billingTab = tabControl.TabPages[2];
            tabControl.SelectedTab = billingTab;

            var statusLabel = (Label)billingTab.Controls.Find("BillMessageLabel", true).First();

            // Assert
            Assert.Equal(System.Drawing.Color.DarkRed, statusLabel.ForeColor);
            Assert.Contains("ยังไม่มีข้อมูลบันทึกมิเตอร์ของรอบบิลนี้ กรุณาบันทึกมิเตอร์น้ำ-ไฟก่อนทำการสร้างบิล", statusLabel.Text);
        });
    }

    [Fact]
    public void RoomForm_BillingTab_WhenMeterReadingExists_AutoLoadsFromRepository()
    {
        RunInSta(() =>
        {
            // Arrange
            var roomRepo = new InMemoryRoomRepository();
            var meterRepo = new InMemoryMonthlyMeterReadingRepository();
            var createRoomService = new CreateRoomService(roomRepo);
            var room = createRoomService.Create("101");

            var period = BillingPeriod.Create("2026-10");
            var reading = MonthlyMeterReading.Create(
                room.Id,
                period,
                MeterReading.Create(100m),
                MeterReading.Create(120m),
                MeterReading.Create(500m),
                MeterReading.Create(560m));
            meterRepo.Add(reading);

            using var form = new RoomForm(createRoomService, roomRepo, null, null, null, meterRepo, null);
            form.Show();
            var tabControl = form.Controls.OfType<TabControl>().First();
            var billingTab = tabControl.TabPages[2];
            tabControl.SelectedTab = billingTab;

            var periodSelector = (BillingPeriodSelector)billingTab.Controls.Find("BillingPeriodSelector", true).First();
            periodSelector.SetBillingPeriod(period);

            var prevWaterBox = (TextBox)billingTab.Controls.Find("PrevWaterTextBox", true).First();
            var currWaterBox = (TextBox)billingTab.Controls.Find("CurrWaterTextBox", true).First();
            var prevElecBox = (TextBox)billingTab.Controls.Find("PrevElecTextBox", true).First();
            var currElecBox = (TextBox)billingTab.Controls.Find("CurrElecTextBox", true).First();
            var statusLabel = (Label)billingTab.Controls.Find("BillMessageLabel", true).First();

            // Assert
            Assert.Equal("100", prevWaterBox.Text);
            Assert.Equal("120", currWaterBox.Text);
            Assert.Equal("500", prevElecBox.Text);
            Assert.Equal("560", currElecBox.Text);
            Assert.Equal(System.Drawing.Color.DarkGreen, statusLabel.ForeColor);
            Assert.Contains("โหลดข้อมูลมิเตอร์รอบบิล", statusLabel.Text);
        });
    }

    [Fact]
    public void RoomForm_BillingTab_WhenBillCreated_AutoAdvancesToNextRoom()
    {
        RunInSta(() =>
        {
            // Arrange
            var roomRepo = new InMemoryRoomRepository();
            var billRepo = new InMemoryBillRepository();
            var meterRepo = new InMemoryMonthlyMeterReadingRepository();
            var createRoomService = new CreateRoomService(roomRepo);
            var createBillService = new CreateMonthlyBillService(billRepo);

            var room1 = createRoomService.Create("101");
            var room2 = createRoomService.Create("102");

            using var form = new RoomForm(createRoomService, roomRepo, createBillService, null, null, meterRepo, billRepo);
            form.Show();
            var tabControl = form.Controls.OfType<TabControl>().First();
            var billingTab = tabControl.TabPages[2];
            tabControl.SelectedTab = billingTab;

            var comboBox = (ComboBox)billingTab.Controls.Find("BillRoomComboBox", true).First();
            Assert.Equal(2, comboBox.Items.Count);

            var periodSelector = (BillingPeriodSelector)billingTab.Controls.Find("BillingPeriodSelector", true).First();
            periodSelector.SetBillingPeriod(BillingPeriod.Create("2026-10"));

            var rentBox = (TextBox)billingTab.Controls.Find("MonthlyRentTextBox", true).First();
            var prevWaterBox = (TextBox)billingTab.Controls.Find("PrevWaterTextBox", true).First();
            var currWaterBox = (TextBox)billingTab.Controls.Find("CurrWaterTextBox", true).First();
            var prevElecBox = (TextBox)billingTab.Controls.Find("PrevElecTextBox", true).First();
            var currElecBox = (TextBox)billingTab.Controls.Find("CurrElecTextBox", true).First();
            var createButton = (Button)billingTab.Controls.Find("CreateBillButton", true).First();

            rentBox.Text = "3000";
            prevWaterBox.Text = "100";
            currWaterBox.Text = "110";
            prevElecBox.Text = "500";
            currElecBox.Text = "550";

            // Act: Create bill for Room 101
            createButton.PerformClick();

            // Assert: Room 101 should be filtered out, leaving Room 102 selected
            Assert.Single(comboBox.Items);
            var remainingRoom = comboBox.Items[0]?.ToString();
            Assert.Equal("102", remainingRoom);
        });
    }

    private static void AddMeterReading(
        InMemoryMonthlyMeterReadingRepository meterRepo,
        RoomId roomId,
        BillingPeriod period)
    {
        meterRepo.Add(MonthlyMeterReading.Create(
            roomId,
            period,
            MeterReading.Create(100m),
            MeterReading.Create(110m),
            MeterReading.Create(500m),
            MeterReading.Create(550m)));
    }

    private static void DoubleClickFirstRow(DataGridView grid)
    {
        var onCellDoubleClick = typeof(DataGridView).GetMethod(
            "OnCellDoubleClick",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        onCellDoubleClick!.Invoke(grid, [new DataGridViewCellEventArgs(1, 0)]);
    }

    [Fact]
    public void RoomForm_BillingTab_WhenExistingBillIsEdited_UpdatesSameBillWithoutDuplicateError()
    {
        RunInSta(() =>
        {
            // Arrange
            var roomRepo = new InMemoryRoomRepository();
            var billRepo = new InMemoryBillRepository();
            var meterRepo = new InMemoryMonthlyMeterReadingRepository();
            var createRoomService = new CreateRoomService(roomRepo);
            var createBillService = new CreateMonthlyBillService(billRepo);
            var period = BillingPeriod.Create($"{DateTime.Now.Year}-10");

            var room = createRoomService.Create("101", 3000m, true);
            AddMeterReading(meterRepo, room.Id, period);

            using var form = new RoomForm(createRoomService, roomRepo, createBillService, null, null, meterRepo, billRepo);
            form.Show();
            var tabControl = form.Controls.OfType<TabControl>().First();
            var billingTab = tabControl.TabPages[2];
            tabControl.SelectedTab = billingTab;

            var periodSelector = (BillingPeriodSelector)billingTab.Controls.Find("BillingPeriodSelector", true).First();
            periodSelector.SetBillingPeriod(period);

            var createButton = (Button)billingTab.Controls.Find("CreateBillButton", true).First();
            var motorcycleCheckBox = (CheckBox)billingTab.Controls.Find("ChargeMotorcycleCheckBox", true).First();
            var messageLabel = (Label)billingTab.Controls.Find("BillMessageLabel", true).First();
            var grid = (DataGridView)billingTab.Controls.Find("BillGrid", true).First();

            createButton.PerformClick();
            var bill = Assert.Single(billRepo.GetAll());
            Assert.Equal(0m, bill.MotorcycleParking.Amount);
            decimal originalTotal = bill.Total.Amount;
            string invoiceNumber = bill.InvoiceNumber;

            // Act
            DoubleClickFirstRow(grid);
            Assert.Equal("บันทึกการแก้ไขบิล", createButton.Text);
            motorcycleCheckBox.Checked = true;
            createButton.PerformClick();

            // Assert
            Assert.Equal(System.Drawing.Color.DarkGreen, messageLabel.ForeColor);
            Assert.DoesNotContain("อยู่แล้ว", messageLabel.Text);
            var updatedBill = Assert.Single(billRepo.GetAll());
            Assert.Equal(invoiceNumber, updatedBill.InvoiceNumber);
            Assert.True(updatedBill.MotorcycleParking.Amount > 0m);
            Assert.Equal(originalTotal + updatedBill.MotorcycleParking.Amount, updatedBill.Total.Amount);
            Assert.Equal("คำนวณและสร้างบิล", createButton.Text);
        });
    }

    [Fact]
    public void RoomForm_BillingTab_WhenMeterChangesAfterBillCreated_ShowsMetersFromBill()
    {
        RunInSta(() =>
        {
            // Arrange
            var roomRepo = new InMemoryRoomRepository();
            var billRepo = new InMemoryBillRepository();
            var meterRepo = new InMemoryMonthlyMeterReadingRepository();
            var createRoomService = new CreateRoomService(roomRepo);
            var createBillService = new CreateMonthlyBillService(billRepo);
            var saveMeterService = new SaveMonthlyMeterReadingService(meterRepo);
            int year = DateTime.Now.Year;
            var period = BillingPeriod.Create($"{year}-10");

            var room = createRoomService.Create("101", 3000m, true);
            AddMeterReading(meterRepo, room.Id, period);

            using var form = new RoomForm(createRoomService, roomRepo, createBillService, null, null, meterRepo, billRepo);
            form.Show();
            var tabControl = form.Controls.OfType<TabControl>().First();
            var billingTab = tabControl.TabPages[2];
            tabControl.SelectedTab = billingTab;

            var periodSelector = (BillingPeriodSelector)billingTab.Controls.Find("BillingPeriodSelector", true).First();
            periodSelector.SetBillingPeriod(period);

            var createButton = (Button)billingTab.Controls.Find("CreateBillButton", true).First();
            var grid = (DataGridView)billingTab.Controls.Find("BillGrid", true).First();
            var currWaterBox = (TextBox)billingTab.Controls.Find("CurrWaterTextBox", true).First();
            var waterDetailsLabel = (Label)billingTab.Controls.Find("ResultWaterDetailsLabel", true).First();
            var dueDateLabel = (Label)billingTab.Controls.Find("ResultDueDateLabel", true).First();

            createButton.PerformClick();

            // Act: แก้มิเตอร์หลังออกบิล แล้วเปิดบิลเดิม
            saveMeterService.Save(
                room.Id,
                period,
                MeterReading.Create(100m),
                MeterReading.Create(150m),
                MeterReading.Create(500m),
                MeterReading.Create(550m));
            periodSelector.SetBillingPeriod(BillingPeriod.Create($"{year}-09"));
            periodSelector.SetBillingPeriod(period);
            DoubleClickFirstRow(grid);

            // Assert
            Assert.Equal("100 -> 110", grid.Rows[0].Cells["WaterReading"].Value);
            Assert.Equal("10", grid.Rows[0].Cells["WaterUsed"].Value);
            Assert.Equal("110", currWaterBox.Text);
            Assert.Contains("100 → 110", waterDetailsLabel.Text);
            Assert.Equal($"ครบกำหนด: 05/11/{year + 543}", dueDateLabel.Text);
            Assert.Equal($"05/11/{year + 543}", grid.Rows[0].Cells["DueDateDisplay"].Value);
        });
    }

    [Fact]
    public void RoomForm_WhenPriceChangesInSettingsTab_NewBillUsesNewPriceAndIssuedBillStaysTheSame()
    {
        RunInSta(() =>
        {
            // Arrange
            var roomRepo = new InMemoryRoomRepository();
            var billRepo = new InMemoryBillRepository();
            var meterRepo = new InMemoryMonthlyMeterReadingRepository();
            var createRoomService = new CreateRoomService(roomRepo);
            var createBillService = new CreateMonthlyBillService(billRepo);
            var period = BillingPeriod.Create($"{DateTime.Now.Year}-10");

            var room1 = createRoomService.Create("101", 3000m, true);
            var room2 = createRoomService.Create("102", 3000m, true);
            AddMeterReading(meterRepo, room1.Id, period);
            AddMeterReading(meterRepo, room2.Id, period);

            using var form = new RoomForm(createRoomService, roomRepo, createBillService, null, null, meterRepo, billRepo);
            form.Show();
            var tabControl = form.Controls.OfType<TabControl>().First();
            var billingTab = tabControl.TabPages[2];
            var settingsTab = tabControl.TabPages[5];
            tabControl.SelectedTab = billingTab;

            var periodSelector = (BillingPeriodSelector)billingTab.Controls.Find("BillingPeriodSelector", true).First();
            periodSelector.SetBillingPeriod(period);
            var createButton = (Button)billingTab.Controls.Find("CreateBillButton", true).First();
            var billingWaterRateBox = (TextBox)billingTab.Controls.Find("WaterRateTextBox", true).First();

            createButton.PerformClick();
            var firstBill = billRepo.GetAll().Single();
            decimal firstWaterAmount = firstBill.Water.Amount;

            // Act
            tabControl.SelectedTab = settingsTab;
            ((TextBox)settingsTab.Controls.Find("WaterRateTextBox", true).First()).Text = "25";
            ((Button)settingsTab.Controls.Find("SaveSettingsButton", true).First()).PerformClick();
            tabControl.SelectedTab = billingTab;
            createButton.PerformClick();

            // Assert
            Assert.Equal("25", billingWaterRateBox.Text);
            var secondBill = billRepo.GetAll().Single(bill => bill.RoomId == room2.Id);
            Assert.Equal(250m, secondBill.Water.Amount);
            Assert.Equal(25m, secondBill.Meters!.WaterRate);
            Assert.Equal(firstWaterAmount, firstBill.Water.Amount);
            Assert.Equal(19m, firstBill.Meters!.WaterRate);
        });
    }

    [Fact]
    public void RoomForm_BillingTab_WhenAnotherRoomIsSelectedDuringEdit_CreatesNewBillForThatRoom()
    {
        RunInSta(() =>
        {
            // Arrange
            var roomRepo = new InMemoryRoomRepository();
            var billRepo = new InMemoryBillRepository();
            var meterRepo = new InMemoryMonthlyMeterReadingRepository();
            var createRoomService = new CreateRoomService(roomRepo);
            var createBillService = new CreateMonthlyBillService(billRepo);
            var period = BillingPeriod.Create($"{DateTime.Now.Year}-10");

            var room1 = createRoomService.Create("101", 3000m, true);
            var room2 = createRoomService.Create("102", 3000m, true);
            AddMeterReading(meterRepo, room1.Id, period);
            AddMeterReading(meterRepo, room2.Id, period);

            using var form = new RoomForm(createRoomService, roomRepo, createBillService, null, null, meterRepo, billRepo);
            form.Show();
            var tabControl = form.Controls.OfType<TabControl>().First();
            var billingTab = tabControl.TabPages[2];
            tabControl.SelectedTab = billingTab;

            var periodSelector = (BillingPeriodSelector)billingTab.Controls.Find("BillingPeriodSelector", true).First();
            periodSelector.SetBillingPeriod(period);

            var comboBox = (ComboBox)billingTab.Controls.Find("BillRoomComboBox", true).First();
            var createButton = (Button)billingTab.Controls.Find("CreateBillButton", true).First();
            var grid = (DataGridView)billingTab.Controls.Find("BillGrid", true).First();

            createButton.PerformClick();
            DoubleClickFirstRow(grid);

            // Act
            comboBox.SelectedItem = comboBox.Items.Cast<object>().First(item => item.ToString() == "102");
            createButton.PerformClick();

            // Assert
            Assert.Equal("คำนวณและสร้างบิล", createButton.Text);
            Assert.Equal(2, billRepo.GetAll().Count);
            Assert.True(billRepo.Exists(room2.Id, period));
        });
    }
}
