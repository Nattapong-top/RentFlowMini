using RentFlowApp.Application.Billing;
using RentFlowApp.Application.Reports;
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

public class BillSummaryTests
{
    private static T Find<T>(Control parent, string name) where T : Control =>
        (T)parent.Controls.Find(name, true)[0];

    private static readonly string[] SummaryLabelNames =
    [
        "ResultRoomLabel",
        "ResultInvoiceNumberLabel",
        "ResultTenantLabel",
        "ResultPeriodLabel",
        "ResultCreatedAtLabel",
        "ResultDueDateLabel",
        "ResultRentLabel",
        "ResultWaterAmountLabel",
        "ResultWaterDetailsLabel",
        "ResultElecAmountLabel",
        "ResultElecDetailsLabel",
        "ResultMotorcycleParkingLabel",
        "ResultCarParkingLabel",
        "ResultCableTvLabel",
        "ResultTotalLabel"
    ];

    private static Dictionary<string, string> ReadSummary(Control tab) =>
        SummaryLabelNames.ToDictionary(name => name, name => Find<Label>(tab, name).Text);

    [Theory]
    [InlineData(true, "ผู้เช่า: ผู้เช่าทดสอบ นามสกุลสมมติ")]
    public void Summary_ShowsSameValues_WhenCreatedOpenedAndEdited(bool hasTenant, string expectedTenant)
    {
        RunInSta(() =>
        {
            var rooms = new InMemoryRoomRepository();
            var room = new CreateRoomService(rooms).Create("101", 3000m, false);
            var tenants = new InMemoryTenantRepository();
            if (hasTenant)
            {
                new CreateTenantService(tenants).Create("ผู้เช่าทดสอบ", "นามสกุลสมมติ", null, room.Id);
            }

            var meters = new InMemoryMonthlyMeterReadingRepository();
            new SaveMonthlyMeterReadingService(meters).Save(
                room.Id,
                BillingPeriod.Create(DateTime.Now.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture)),
                MeterReading.Create(100m),
                MeterReading.Create(110m),
                MeterReading.Create(200m),
                MeterReading.Create(220m));
            var bills = new InMemoryBillRepository();
            using var form = new Form();
            var tab = new MonthlyBillTabControl(
                new CreateMonthlyBillService(bills),
                rooms,
                monthlyMeterReadingRepository: meters,
                billRepository: bills,
                tenantRepository: tenants);
            form.Controls.Add(tab);
            HiddenTestWindows.ShowHidden(form);
            Find<CheckBox>(tab, "ChargeMotorcycleCheckBox").Checked = true;
            var createButton = Find<Button>(tab, "CreateBillButton");

            createButton.PerformClick();
            var created = ReadSummary(tab);

            Assert.Equal("ห้อง 101", created["ResultRoomLabel"]);
            Assert.Equal("ใบแจ้งหนี้ เลขที่ : N001", created["ResultInvoiceNumberLabel"]);
            Assert.Equal(expectedTenant, created["ResultTenantLabel"]);
            Assert.Equal($"(รอบเดือนที่ {DateTime.Now.Month}/{DateTime.Now.Year + 543})", created["ResultPeriodLabel"]);
            Assert.StartsWith("วันที่ออกบิล: ", created["ResultCreatedAtLabel"]);
            Assert.StartsWith("ครบกำหนด: ", created["ResultDueDateLabel"]);
            Assert.Equal("3,000.00 บาท", created["ResultRentLabel"]);
            Assert.Equal("└ มิเตอร์ 100 → 110 (10 หน่วย)", created["ResultWaterDetailsLabel"]);
            Assert.Equal("190.00 บาท", created["ResultWaterAmountLabel"]);
            Assert.Equal("└ มิเตอร์ 200 → 220 (20 หน่วย)", created["ResultElecDetailsLabel"]);
            Assert.Equal("160.00 บาท", created["ResultElecAmountLabel"]);
            Assert.Equal("100.00 บาท", created["ResultMotorcycleParkingLabel"]);
            Assert.Equal("-", created["ResultCarParkingLabel"]);
            Assert.Equal("60.00 บาท", created["ResultCableTvLabel"]);
            Assert.Equal("3,510.00 บาท", created["ResultTotalLabel"]);

            DoubleClickRow(Find<DataGridView>(tab, "BillGrid"), 0);
            Assert.Equal(created, ReadSummary(tab));

            createButton.PerformClick();
            Assert.Equal(created, ReadSummary(tab));
        });
    }

    [Fact]
    public void Summary_OfOldBillWithoutTenant_ShowsNoTenantText_WhenOpened()
    {
        RunInSta(() =>
        {
            var rooms = new InMemoryRoomRepository();
            var room = new CreateRoomService(rooms).Create("101", 3000m, false);
            var bills = new InMemoryBillRepository();
            var meter = MeterReading.Create(100m);
            // Bills saved before tenant names were kept on the bill have no tenant.
            bills.Add(Bill.Create(
                room.Id,
                BillingPeriod.Create(DateTime.Now.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture)),
                BillingCalculator.Calculate(3000m, meter, meter, meter, meter, 19m, 8m)));
            using var form = new Form();
            var tab = new MonthlyBillTabControl(new CreateMonthlyBillService(bills), rooms, billRepository: bills);
            form.Controls.Add(tab);
            HiddenTestWindows.ShowHidden(form);

            DoubleClickRow(Find<DataGridView>(tab, "BillGrid"), 0);

            Assert.Equal("ผู้เช่า: ไม่มีข้อมูลผู้เช่า", Find<Label>(tab, "ResultTenantLabel").Text);
        });
    }

    [Fact]
    public void ZeroRateBill_StaysNotExemptAndKeepsAmount_WhenOpenedAndSavedWithoutChanges()
    {
        RunInSta(() =>
        {
            var rooms = new InMemoryRoomRepository();
            var room = new CreateRoomService(rooms).Create("101", 3000m, false);
            var bills = new InMemoryBillRepository();
            var meter = MeterReading.Create(100m);
            // ออกบิลตอนราคากลาง Cable TV เป็น 0 ส่วนราคากลางปัจจุบันของแท็บคือ 60
            var bill = new CreateMonthlyBillService(bills).Create(new CreateMonthlyBillRequest(
                room.Id,
                BillingPeriod.Create(DateTime.Now.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture)),
                3000m,
                meter,
                meter,
                meter,
                meter,
                19m,
                8m,
                IsCableTvExempt: false,
                CableTvRate: 0m,
                Tenant: TestTenants.Snapshot()));
            using var form = new Form();
            var tab = new MonthlyBillTabControl(
                new CreateMonthlyBillService(bills),
                rooms,
                billRepository: bills);
            form.Controls.Add(tab);
            HiddenTestWindows.ShowHidden(form);

            DoubleClickRow(Find<DataGridView>(tab, "BillGrid"), 0);

            Assert.False(Find<CheckBox>(tab, "IsCableTvExemptCheckBox").Checked);

            Find<Button>(tab, "CreateBillButton").PerformClick();

            Assert.False(bill.IsCableTvExempt);
            Assert.Equal(0m, bill.CableTv.Amount);
        });
    }

    [Fact]
    public void Messages_ShowPeriodInBuddhistYear_WhenLoadedCreatedOpenedAndEdited()
    {
        RunInSta(() =>
        {
            var period = BillingPeriod.Create(DateTime.Now.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture));
            string thaiPeriod = ThaiCalendar.FormatPeriod(period);
            var rooms = new InMemoryRoomRepository();
            var room = new CreateRoomService(rooms).Create("101", 3000m, false);
            var meters = new InMemoryMonthlyMeterReadingRepository();
            new SaveMonthlyMeterReadingService(meters).Save(
                room.Id,
                period,
                MeterReading.Create(0m),
                MeterReading.Create(10m),
                MeterReading.Create(0m),
                MeterReading.Create(10m));
            var bills = new InMemoryBillRepository();
            var tenants = new InMemoryTenantRepository();
            TestTenants.MoveInto(tenants, room.Id);
            using var form = new Form();
            var tab = new MonthlyBillTabControl(
                new CreateMonthlyBillService(bills),
                rooms,
                monthlyMeterReadingRepository: meters,
                billRepository: bills,
                tenantRepository: tenants);
            form.Controls.Add(tab);
            HiddenTestWindows.ShowHidden(form);
            var button = Find<Button>(tab, "CreateBillButton");
            var message = Find<Label>(tab, "BillMessageLabel");

            Assert.Equal($"โหลดข้อมูลมิเตอร์รอบบิล {thaiPeriod} เรียบร้อย", message.Text);

            button.PerformClick();
            Assert.Equal($"สร้างบิลสำหรับห้อง 101 รอบ {thaiPeriod} สำเร็จเรียบร้อย", message.Text);

            DoubleClickRow(Find<DataGridView>(tab, "BillGrid"), 0);
            Assert.Equal($"แก้ไขบิลห้อง 101 รอบ {thaiPeriod}: แก้ได้เฉพาะค่าจอดรถและ Cable TV", message.Text);

            button.PerformClick();
            Assert.Equal($"บันทึกการแก้ไขบิลห้อง 101 รอบ {thaiPeriod} สำเร็จเรียบร้อย", message.Text);
        });
    }

    [Fact]
    public void BillGrid_SortsRoomsByNumberWithinTheSameDueDate()
    {
        RunInSta(() =>
        {
            var period = BillingPeriod.Create(DateTime.Now.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture));
            var rooms = new InMemoryRoomRepository();
            var bills = new InMemoryBillRepository();
            var meter = MeterReading.Create(100m);
            foreach (string number in new[] { "101", "99" })
            {
                var room = new CreateRoomService(rooms).Create(number, 3000m, false);
                new CreateMonthlyBillService(bills).Create(new CreateMonthlyBillRequest(
                    room.Id, period, 3000m, meter, meter, meter, meter, 19m, 8m, Tenant: TestTenants.Snapshot()));
            }

            using var form = new Form();
            var tab = new MonthlyBillTabControl(new CreateMonthlyBillService(bills), rooms, billRepository: bills);
            form.Controls.Add(tab);
            HiddenTestWindows.ShowHidden(form);

            var shownRooms = Find<DataGridView>(tab, "BillGrid").Rows.Cast<DataGridViewRow>()
                .Select(row => row.Cells["RoomNumber"].Value?.ToString());

            Assert.Equal(["99", "101"], shownRooms);
        });
    }

    [Fact]
    public void ExemptBill_ShowsZeroCableTvAmount_WhenCreatedOpenedAndEdited()
    {
        RunInSta(() =>
        {
            var rooms = new InMemoryRoomRepository();
            var room = new CreateRoomService(rooms).Create("101", 3000m, true);
            var meters = new InMemoryMonthlyMeterReadingRepository();
            new SaveMonthlyMeterReadingService(meters).Save(
                room.Id,
                BillingPeriod.Create(DateTime.Now.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture)),
                MeterReading.Create(0m),
                MeterReading.Create(10m),
                MeterReading.Create(0m),
                MeterReading.Create(10m));
            var bills = new InMemoryBillRepository();
            var tenants = new InMemoryTenantRepository();
            TestTenants.MoveInto(tenants, room.Id);
            using var form = new Form();
            var tab = new MonthlyBillTabControl(
                new CreateMonthlyBillService(bills),
                rooms,
                monthlyMeterReadingRepository: meters,
                billRepository: bills,
                tenantRepository: tenants);
            form.Controls.Add(tab);
            HiddenTestWindows.ShowHidden(form);
            var createButton = Find<Button>(tab, "CreateBillButton");
            var cableTvLabel = Find<Label>(tab, "ResultCableTvLabel");

            createButton.PerformClick();
            Assert.Equal("0.00 บาท", cableTvLabel.Text);

            DoubleClickRow(Find<DataGridView>(tab, "BillGrid"), 0);
            Assert.Equal("0.00 บาท", cableTvLabel.Text);

            createButton.PerformClick();
            Assert.Equal("0.00 บาท", cableTvLabel.Text);
        });
    }
}
