using RentFlowApp.Application.Billing;
using RentFlowApp.Application.Rooms;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Forms;
using RentFlowApp.Infrastructure.Billing;
using RentFlowApp.Infrastructure.Rooms;
using static RentFlowApp.Tests.UI.InputTestHelper;

namespace RentFlowApp.Tests.UI;

public class InputBehaviorTests
{
    private static void DoubleClickFirstRow(DataGridView grid)
    {
        var onCellDoubleClick = typeof(DataGridView).GetMethod(
            "OnCellDoubleClick",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        onCellDoubleClick!.Invoke(grid, [new DataGridViewCellEventArgs(1, 0)]);
    }

    private static IEnumerable<DataGridView> FindGrids(Control parent) =>
        parent.Controls.Cast<Control>().SelectMany(child =>
            child is DataGridView grid ? [grid] : FindGrids(child));

    private static RoomForm CreateForm(InMemoryRoomRepository rooms) =>
        new(new CreateRoomService(rooms), rooms);

    [Fact]
    public void AllGrids_GrowHeaderToFitWrappedText()
    {
        RunInSta(() =>
        {
            using var form = CreateForm(new InMemoryRoomRepository());
            form.Show();

            var grids = FindGrids(form).ToList();

            Assert.Equal(6, grids.Count);
            Assert.All(grids, grid =>
                Assert.Equal(DataGridViewColumnHeadersHeightSizeMode.AutoSize, grid.ColumnHeadersHeightSizeMode));
        });
    }

    [Fact]
    public void RoomRent_AcceptsOnlyNumbersIncludingWhenPasted()
    {
        RunInSta(() =>
        {
            using var form = CreateForm(new InMemoryRoomRepository());
            form.Show();
            var rent = (TextBox)form.Controls.Find("RoomMonthlyRentTextBox", true)[0];

            rent.Text = "3,500บาท";
            Assert.Equal("3,500", rent.Text);

            rent.Text = "๑๒๓";
            Assert.Empty(rent.Text);

            Assert.True(InputTestHelper.IsKeyBlocked(rent, 'a'));
            Assert.True(InputTestHelper.IsKeyBlocked(rent, '-'));
            Assert.True(InputTestHelper.IsKeyBlocked(rent, '๑'));
            Assert.False(InputTestHelper.IsKeyBlocked(rent, '5'));
            Assert.False(InputTestHelper.IsKeyBlocked(rent, '.'));
        });
    }

    [Fact]
    public void NumericBox_AllowsSecondDotOnlyWhenFirstDotIsBeingReplaced()
    {
        RunInSta(() =>
        {
            using var form = CreateForm(new InMemoryRoomRepository());
            form.Show();
            var rent = (TextBox)form.Controls.Find("RoomMonthlyRentTextBox", true)[0];
            rent.Text = "12.5";

            rent.Select(4, 0);
            Assert.True(InputTestHelper.IsKeyBlocked(rent, '.'));

            rent.SelectAll();
            Assert.False(InputTestHelper.IsKeyBlocked(rent, '.'));
        });
    }

    [Fact]
    public void MeterAndSettingsBoxes_RejectLettersAndThousandsSeparator()
    {
        RunInSta(() =>
        {
            using var form = CreateForm(new InMemoryRoomRepository());
            form.Show();
            var tabs = form.Controls.OfType<TabControl>().First();
            var meterBox = tabs.TabPages[1].Controls.OfType<Control>()
                .SelectMany(control => FindTextBoxes(control))
                .First(box => box.Enabled && !box.ReadOnly);
            var settingsRate = (TextBox)tabs.TabPages[5].Controls.Find("WaterRateTextBox", true)[0];

            meterBox.Text = "1,2a3.4.5";
            settingsRate.Text = "1,2a3.4.5";

            Assert.Equal("123.45", meterBox.Text);
            Assert.Equal("123.45", settingsRate.Text);
        });
    }

    private static IEnumerable<TextBox> FindTextBoxes(Control parent) =>
        (parent is TextBox box ? new[] { box } : [])
            .Concat(parent.Controls.Cast<Control>().SelectMany(FindTextBoxes));

    [Fact]
    public void RoomRent_EnterSavesOnlyWhileEditing()
    {
        RunInSta(() =>
        {
            var rooms = new InMemoryRoomRepository();
            new CreateRoomService(rooms).Create("101", 3000m, false);
            using var form = CreateForm(rooms);
            form.Show();
            var grid = FindGrids(form).First(item => item.Name == "RoomGrid");
            var rent = (TextBox)form.Controls.Find("RoomMonthlyRentTextBox", true)[0];
            var editButton = (Button)form.Controls.Find("EditRoomRentButton", true)[0];

            rent.Text = "4000";
            InputTestHelper.PressEnter(rent);
            Assert.Equal("4000", rent.Text);

            DoubleClickFirstRow(grid);
            Assert.Equal("บันทึกค่าเช่า", editButton.Text);
            rent.Text = "4500";

            InputTestHelper.PressEnter(rent);

            Assert.Equal("แก้ไขค่าเช่า", editButton.Text);
            Assert.Equal(4500m, rooms.GetAll().Single().MonthlyRent.Amount);
        });
    }

    [Fact]
    public void BillEdit_EnterOnCheckBoxSavesOnlyWhileEditing()
    {
        RunInSta(() =>
        {
            var rooms = new InMemoryRoomRepository();
            var bills = new InMemoryBillRepository();
            var createBill = new CreateMonthlyBillService(bills);
            var room = new CreateRoomService(rooms).Create("101", 3000m, true);
            var period = BillingPeriod.Create($"{DateTime.Now.Year}-10");
            var meter = MeterReading.Create(100m);
            var bill = createBill.Create(new CreateMonthlyBillRequest(
                room.Id, period, 3000m, meter, meter, meter, meter, 19m, 8m, Tenant: TestTenants.Snapshot()));

            using var form = new Form();
            var tab = new MonthlyBillTabControl(createBill, rooms, billRepository: bills) { Dock = DockStyle.Fill };
            form.Controls.Add(tab);
            form.Show();
            ((BillingPeriodSelector)tab.Controls.Find("BillingPeriodSelector", true)[0]).SetBillingPeriod(period);
            var motorcycle = (CheckBox)tab.Controls.Find("ChargeMotorcycleCheckBox", true)[0];
            var grid = (DataGridView)tab.Controls.Find("BillGrid", true)[0];

            motorcycle.Checked = true;
            InputTestHelper.PressEnter(motorcycle);
            Assert.Equal(0m, bill.MotorcycleParking.Amount);

            DoubleClickFirstRow(grid);
            motorcycle.Checked = true;

            InputTestHelper.PressEnter(motorcycle);

            Assert.True(bill.MotorcycleParking.Amount > 0m);
            Assert.Single(bills.GetAll());
        });
    }
}
