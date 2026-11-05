using RentFlowApp.Application.Billing;
using RentFlowApp.Application.Rooms;
using RentFlowApp.Application.Tenants;
using RentFlowApp.Domain;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Forms;
using RentFlowApp.Infrastructure.Billing;
using RentFlowApp.Infrastructure.Rooms;
using RentFlowApp.Infrastructure.Tenants;

namespace RentFlowApp.Tests.UI;

public class TenantTabTests
{
    private static void RunInSta(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception != null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }

    private sealed class TenantTab : IDisposable
    {
        private readonly Form _form = new();
        private readonly TenantTabControl _tab;
        private readonly InMemoryRoomRepository _rooms = new();

        public TenantTab()
        {
            _tab = new TenantTabControl(
                Tenants,
                _rooms,
                new CreateTenantService(Tenants),
                Bills,
                message =>
                {
                    ConfirmMessages.Add(message);
                    return ConfirmAnswer;
                })
            {
                Dock = DockStyle.Fill
            };
            _form.Controls.Add(_tab);
            _form.Show();
        }

        public InMemoryTenantRepository Tenants { get; } = new();

        public InMemoryBillRepository Bills { get; } = new();

        public bool ConfirmAnswer { get; set; } = true;

        public List<string> ConfirmMessages { get; } = [];

        public DataGridView Grid => Find<DataGridView>("TenantGrid");

        public Label Message => Find<Label>("TenantMessageLabel");

        public ComboBox RoomBox => Find<ComboBox>("TenantRoomComboBox");

        public T Find<T>(string name) where T : Control => (T)_tab.Controls.Find(name, true)[0];

        public void Click(string buttonName) => Find<Button>(buttonName).PerformClick();

        public RoomId AddRoom(string number)
        {
            var room = new CreateRoomService(_rooms).Create(number, 3000m, true);
            _tab.RefreshTab();
            return room.Id;
        }

        public Tenant AddTenant(string firstName, string? roomNumber = null, string? phone = null)
        {
            var roomId = roomNumber is null ? null : AddRoom(roomNumber);
            var tenant = new CreateTenantService(Tenants).Create(firstName, "ทดสอบ", phone, roomId);
            _tab.RefreshTab();
            return tenant;
        }

        public void AddUnpaidBill(Tenant tenant, RoomId roomId, string period, decimal rent = 3000m)
        {
            var meter = MeterReading.Create(100m);
            new CreateMonthlyBillService(Bills).Create(new CreateMonthlyBillRequest(
                roomId, BillingPeriod.Create(period), rent, meter, meter, meter, meter, 19m, 8m,
                Tenant: BillTenantSnapshot.From(tenant)));
            _tab.RefreshTab();
        }

        public void Refresh() => _tab.RefreshTab();

        public IEnumerable<string?> Column(string columnName) =>
            Grid.Rows.Cast<DataGridViewRow>().Select(row => row.Cells[columnName].Value?.ToString());

        public void SelectOnly(int rowIndex)
        {
            Grid.ClearSelection();
            Grid.Rows[rowIndex].Selected = true;
        }

        public void DoubleClickRow(int rowIndex)
        {
            var onCellDoubleClick = typeof(DataGridView).GetMethod(
                "OnCellDoubleClick",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            onCellDoubleClick!.Invoke(Grid, [new DataGridViewCellEventArgs(1, rowIndex)]);
        }

        public void Dispose() => _form.Dispose();
    }

    [Fact]
    public void CreatingBillInBillTab_StoresTenantSoOutstandingAndNameFollowThatTenant()
    {
        RunInSta(() =>
        {
            var roomRepo = new InMemoryRoomRepository();
            var billRepo = new InMemoryBillRepository();
            var tenantRepo = new InMemoryTenantRepository();
            var createRoomService = new CreateRoomService(roomRepo);
            var createTenantService = new CreateTenantService(tenantRepo);
            var room = createRoomService.Create("101");
            var oldTenant = createTenantService.Create("คนเก่า", "ทดสอบ", null, room.Id);

            using var form = new RoomForm(
                createRoomService,
                roomRepo,
                new CreateMonthlyBillService(billRepo),
                createTenantService,
                tenantRepo,
                billRepository: billRepo);
            form.Show();
            var tabs = form.Controls.OfType<TabControl>().First();
            var billingTab = tabs.TabPages[2];
            var tenantTab = tabs.TabPages[3];
            tabs.SelectedTab = billingTab;

            ((BillingPeriodSelector)billingTab.Controls.Find("BillingPeriodSelector", true)[0])
                .SetBillingPeriod(BillingPeriod.Create("2026-10"));
            ((ComboBox)billingTab.Controls.Find("BillRoomComboBox", true)[0]).SelectedIndex = 0;
            billingTab.Controls.Find("MonthlyRentTextBox", true)[0].Text = "3000";
            billingTab.Controls.Find("PrevWaterTextBox", true)[0].Text = "100";
            billingTab.Controls.Find("CurrWaterTextBox", true)[0].Text = "110";
            billingTab.Controls.Find("PrevElecTextBox", true)[0].Text = "500";
            billingTab.Controls.Find("CurrElecTextBox", true)[0].Text = "550";
            ((CheckBox)billingTab.Controls.Find("IsCableTvExemptCheckBox", true)[0]).Checked = true;
            ((Button)billingTab.Controls.Find("CreateBillButton", true)[0]).PerformClick();

            Assert.Equal(oldTenant.Id, Assert.Single(billRepo.GetAll()).Tenant!.TenantId);

            tabs.SelectedTab = tenantTab;
            var tenantGrid = (DataGridView)tenantTab.Controls.Find("TenantGrid", true)[0];
            Assert.Equal("3,590.00", tenantGrid.Rows[0].Cells["OutstandingAmount"].Value);

            new MoveOutTenantService(tenantRepo).MoveOut(oldTenant.Id, DateOnly.FromDateTime(DateTime.Now));
            createTenantService.Create("คนใหม่", "ทดสอบ", null, room.Id);

            tabs.SelectedTab = billingTab;
            var billGrid = (DataGridView)billingTab.Controls.Find("BillGrid", true)[0];
            Assert.Equal("คนเก่า ทดสอบ", billGrid.Rows[0].Cells["TenantName"].Value);
        });
    }

    [Fact]
    public void Grid_HidesIdAndListsTenantsInRoomOrderWithSequence()
    {
        RunInSta(() =>
        {
            using var tab = new TenantTab();
            tab.AddTenant("ไม่มีห้อง");
            tab.AddTenant("ห้องร้อยเอ็ด", "101");
            tab.AddTenant("ห้องเก้าเก้า", "99");

            Assert.False(tab.Grid.Columns["TenantId"]!.Visible);
            Assert.True(tab.Grid.ColumnHeadersDefaultCellStyle.Font!.Bold);
            Assert.Equal(["99", "101", "ไม่มีห้องพัก"], tab.Column("RoomNumber"));
            Assert.Equal(["1", "2", "3"], tab.Column("Sequence"));
        });
    }

    [Fact]
    public void AddTenant_SavesMoveInDateAndDepositAndClearsForm()
    {
        RunInSta(() =>
        {
            using var tab = new TenantTab();
            tab.AddRoom("101");
            tab.Find<TextBox>("TenantFirstNameTextBox").Text = "สมปอง";
            tab.Find<TextBox>("TenantLastNameTextBox").Text = "มีสุข";
            tab.RoomBox.SelectedIndex = 1;
            tab.Find<DateTimePicker>("TenantMoveInDatePicker").Value = new DateTime(2026, 10, 1);
            tab.Find<TextBox>("TenantSecurityDepositTextBox").Text = "5,000";

            tab.Click("AddTenantButton");

            var stored = Assert.Single(tab.Tenants.GetAll());
            Assert.Equal(new DateOnly(2026, 10, 1), stored.MoveInDate);
            Assert.Equal(5000m, stored.SecurityDeposit);
            Assert.Equal(["5,000.00"], tab.Column("SecurityDeposit"));
            Assert.Equal(["01/10/2569"], tab.Column("MoveInDate"));
            Assert.Empty(tab.Find<TextBox>("TenantSecurityDepositTextBox").Text);
            Assert.Equal(System.Drawing.Color.DarkGreen, tab.Message.ForeColor);
        });
    }

    [Theory]
    [InlineData("abc", "เงินประกันต้องเป็นตัวเลข")]
    [InlineData("-100", "เงินประกันต้องไม่ติดลบ")]
    public void AddTenant_WhenDepositIsInvalid_ShowsThaiMessageAndAddsNothing(string deposit, string expected)
    {
        RunInSta(() =>
        {
            using var tab = new TenantTab();
            tab.Find<TextBox>("TenantFirstNameTextBox").Text = "สมปอง";
            tab.Find<TextBox>("TenantLastNameTextBox").Text = "มีสุข";
            tab.Find<TextBox>("TenantSecurityDepositTextBox").Text = deposit;

            tab.Click("AddTenantButton");

            Assert.Equal(expected, tab.Message.Text);
            Assert.Empty(tab.Tenants.GetAll());
        });
    }

    [Fact]
    public void DoubleClick_LoadsTenantWithOwnRoomAndSaveUpdatesSameTenant()
    {
        RunInSta(() =>
        {
            using var tab = new TenantTab();
            var tenant = tab.AddTenant("ชื่อผิด", "101", "0812345678");

            tab.DoubleClickRow(0);

            Assert.Equal("ชื่อผิด", tab.Find<TextBox>("TenantFirstNameTextBox").Text);
            Assert.Equal("ห้อง 101", tab.RoomBox.SelectedItem!.ToString());
            Assert.Equal("บันทึกการแก้ไข", tab.Find<Button>("AddTenantButton").Text);
            Assert.False(tab.Find<DateTimePicker>("TenantMoveInDatePicker").Checked);

            tab.Find<TextBox>("TenantFirstNameTextBox").Text = "ชื่อถูก";
            tab.Click("AddTenantButton");

            var stored = Assert.Single(tab.Tenants.GetAll());
            Assert.Equal(tenant.Id, stored.Id);
            Assert.Equal("ชื่อถูก ทดสอบ", stored.FullName);
            Assert.Equal(tenant.RoomId, stored.RoomId);
            Assert.Null(stored.MoveInDate);
            Assert.Equal("เพิ่มผู้เช่า", tab.Find<Button>("AddTenantButton").Text);
            Assert.Empty(tab.Find<TextBox>("TenantFirstNameTextBox").Text);
        });
    }

    [Fact]
    public void Editing_CanMoveToFreeRoomButNotToOccupiedRoom()
    {
        RunInSta(() =>
        {
            using var tab = new TenantTab();
            tab.AddTenant("คนแรก", "101");
            tab.AddTenant("คนที่สอง", "102");
            tab.AddRoom("103");

            tab.DoubleClickRow(0);

            var roomOptions = tab.RoomBox.Items.Cast<object>().Select(item => item.ToString()).ToList();
            Assert.Contains("ห้อง 101", roomOptions);
            Assert.Contains("ห้อง 103", roomOptions);
            Assert.DoesNotContain("ห้อง 102", roomOptions);

            tab.RoomBox.SelectedItem = tab.RoomBox.Items.Cast<object>().First(item => item.ToString() == "ห้อง 103");
            tab.Click("AddTenantButton");

            Assert.Equal(["102", "103"], tab.Column("RoomNumber"));
        });
    }

    [Fact]
    public void CancelEdit_ClearsFormAndNextSaveAddsNewTenant()
    {
        RunInSta(() =>
        {
            using var tab = new TenantTab();
            tab.AddTenant("คนแรก", "101");
            var cancel = tab.Find<Button>("CancelTenantEditButton");
            Assert.False(cancel.Enabled);

            tab.DoubleClickRow(0);
            Assert.True(cancel.Enabled);
            tab.Click("CancelTenantEditButton");

            Assert.False(cancel.Enabled);
            Assert.Equal("เพิ่มผู้เช่า", tab.Find<Button>("AddTenantButton").Text);
            Assert.Equal(0, tab.RoomBox.SelectedIndex);
            Assert.Equal("คนแรก ทดสอบ", Assert.Single(tab.Tenants.GetAll()).FullName);
        });
    }

    [Theory]
    [InlineData("สมชาย", "101")]
    [InlineData("102", "102")]
    [InlineData("0822222222", "102")]
    public void Search_FiltersByNameRoomOrPhone(string search, string expectedRoom)
    {
        RunInSta(() =>
        {
            using var tab = new TenantTab();
            tab.AddTenant("สมชาย", "101", "0811111111");
            tab.AddTenant("สมหญิง", "102", "0822222222");

            tab.Find<TextBox>("TenantSearchTextBox").Text = search;

            Assert.Equal([expectedRoom], tab.Column("RoomNumber"));
            Assert.Equal(["1"], tab.Column("Sequence"));
        });
    }

    [Fact]
    public void Outstanding_ShowsOnlyUnpaidBillsOfThatTenant()
    {
        RunInSta(() =>
        {
            using var tab = new TenantTab();
            var tenant = tab.AddTenant("ค้างจ่าย", "101");
            tab.AddTenant("ไม่ค้าง", "102");
            tab.AddUnpaidBill(tenant, tenant.RoomId!, "2026-08");
            tab.AddUnpaidBill(tenant, tenant.RoomId!, "2026-09");

            Assert.Equal(["6,000.00", "-"], tab.Column("OutstandingAmount"));

            tab.Bills.GetAll().First().RecordPayment(new DateOnly(2026, 9, 1), PaymentMethod.Cash);
            tab.Refresh();

            Assert.Equal(["3,000.00", "-"], tab.Column("OutstandingAmount"));
        });
    }

    [Fact]
    public void Outstanding_StaysWithOldTenantWhenNewTenantTakesRoom()
    {
        RunInSta(() =>
        {
            using var tab = new TenantTab();
            var oldTenant = tab.AddTenant("คนเก่า", "101");
            var room = oldTenant.RoomId!;
            tab.AddUnpaidBill(oldTenant, room, "2026-08");
            new MoveOutTenantService(tab.Tenants).MoveOut(oldTenant.Id, DateOnly.FromDateTime(DateTime.Now));
            new CreateTenantService(tab.Tenants).Create("คนใหม่", "ทดสอบ", null, room);
            tab.Find<CheckBox>("ShowMovedOutTenantsCheckBox").Checked = true;

            var outstandingByName = tab.Grid.Rows.Cast<DataGridViewRow>().ToDictionary(
                row => row.Cells["FullName"].Value!.ToString()!,
                row => row.Cells["OutstandingAmount"].Value!.ToString());

            Assert.Equal("-", outstandingByName["คนใหม่ ทดสอบ"]);
            Assert.Equal("3,000.00", outstandingByName["คนเก่า ทดสอบ"]);
        });
    }

    [Fact]
    public void MoveOut_ConfirmsThenFreesRoomAndHidesTenantUntilAskedFor()
    {
        RunInSta(() =>
        {
            using var tab = new TenantTab();
            var tenant = tab.AddTenant("ย้ายออก", "101");
            tab.SelectOnly(0);

            tab.Click("MoveOutTenantButton");

            string confirm = Assert.Single(tab.ConfirmMessages);
            Assert.Contains("ย้ายออก ทดสอบ", confirm);
            Assert.Contains("ห้อง 101", confirm);
            Assert.DoesNotContain("ค้างชำระ", confirm);
            Assert.Empty(tab.Column("FullName"));
            Assert.False(tab.Tenants.ExistsByRoomId(tenant.RoomId!));
            Assert.Contains("ห้อง 101", tab.RoomBox.Items.Cast<object>().Select(item => item.ToString()));
            Assert.Equal(System.Drawing.Color.DarkGreen, tab.Message.ForeColor);

            tab.Find<CheckBox>("ShowMovedOutTenantsCheckBox").Checked = true;

            Assert.True(tab.Grid.Columns["MovedOutDate"]!.Visible);
            Assert.Equal(["101 (เดิม)"], tab.Column("RoomNumber"));
            var today = DateTime.Now;
            Assert.Equal([$"{today.Day:D2}/{today.Month:D2}/{today.Year + 543}"], tab.Column("MovedOutDate"));
        });
    }

    [Fact]
    public void MoveOut_WhenTenantHasOutstanding_WarnsAndStillAllows()
    {
        RunInSta(() =>
        {
            using var tab = new TenantTab();
            var tenant = tab.AddTenant("ค้างจ่าย", "101");
            tab.AddUnpaidBill(tenant, tenant.RoomId!, "2026-08");
            tab.SelectOnly(0);

            tab.Click("MoveOutTenantButton");

            Assert.Contains("ค้างชำระ 1 บิล รวม 3,000.00 บาท", Assert.Single(tab.ConfirmMessages));
            Assert.True(tab.Tenants.GetById(tenant.Id)!.IsMovedOut);
            Assert.False(Assert.Single(tab.Bills.GetAll()).IsPaid);
        });
    }

    [Fact]
    public void MoveOut_WhenUserDeclines_ChangesNothing()
    {
        RunInSta(() =>
        {
            using var tab = new TenantTab();
            var tenant = tab.AddTenant("ไม่ย้าย", "101");
            tab.ConfirmAnswer = false;
            tab.SelectOnly(0);

            tab.Click("MoveOutTenantButton");

            Assert.False(tab.Tenants.GetById(tenant.Id)!.IsMovedOut);
            Assert.True(tab.Tenants.ExistsByRoomId(tenant.RoomId!));
        });
    }

    [Fact]
    public void MoveOut_WhenNoRowIsSelected_DoesNotPickFirstTenant()
    {
        RunInSta(() =>
        {
            using var tab = new TenantTab();
            var tenant = tab.AddTenant("แถวแรก", "101");

            tab.Click("MoveOutTenantButton");

            Assert.Empty(tab.ConfirmMessages);
            Assert.Contains("กรุณาเลือกผู้เช่า", tab.Message.Text);
            Assert.False(tab.Tenants.GetById(tenant.Id)!.IsMovedOut);
        });
    }

    [Fact]
    public void MoveOut_WhileEditingSameTenant_LeavesEditMode()
    {
        RunInSta(() =>
        {
            using var tab = new TenantTab();
            tab.AddTenant("กำลังแก้", "101");
            tab.DoubleClickRow(0);
            tab.SelectOnly(0);

            tab.Click("MoveOutTenantButton");

            Assert.Equal("เพิ่มผู้เช่า", tab.Find<Button>("AddTenantButton").Text);
            Assert.Empty(tab.Find<TextBox>("TenantFirstNameTextBox").Text);
        });
    }

    [Fact]
    public void DoubleClickMovedOutTenant_DoesNotEnterEditMode()
    {
        RunInSta(() =>
        {
            using var tab = new TenantTab();
            var tenant = tab.AddTenant("ย้ายแล้ว", "101");
            new MoveOutTenantService(tab.Tenants).MoveOut(tenant.Id, DateOnly.FromDateTime(DateTime.Now));
            tab.Find<CheckBox>("ShowMovedOutTenantsCheckBox").Checked = true;

            tab.DoubleClickRow(0);

            Assert.Equal("เพิ่มผู้เช่า", tab.Find<Button>("AddTenantButton").Text);
            Assert.Contains("ย้ายออกแล้ว", tab.Message.Text);
        });
    }
}
