using System.Globalization;
using RentFlowApp.Application.Billing;
using RentFlowApp.Application.Reports;
using RentFlowApp.Application.Rooms;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Forms;
using RentFlowApp.Infrastructure.Billing;
using RentFlowApp.Infrastructure.Rooms;
using RentFlowApp.Infrastructure.Tenants;
using static RentFlowApp.Tests.UI.InputTestHelper;

namespace RentFlowApp.Tests.UI;

public class CreateBillDraftTabTests
{
    private const string SummaryTitle = "สรุปรายละเอียดบิล";
    private const string DraftSummaryTitle = "สรุปรายละเอียดบิล (ร่าง ยังไม่บันทึก)";
    private const string MissingMeterMessage = "ยังไม่มีข้อมูลบันทึกมิเตอร์ของรอบบิลนี้ กรุณาบันทึกมิเตอร์น้ำ-ไฟก่อนทำการสร้างบิล";

    private static readonly BillingPeriod ThisMonth =
        BillingPeriod.Create(DateTime.Now.ToString("yyyy-MM", CultureInfo.InvariantCulture));

    private static readonly string ThaiPeriod = ThaiCalendar.FormatPeriod(ThisMonth);

    // ห้อง 101: 3000 + น้ำ 10 หน่วย 190 + ไฟ 20 หน่วย 160 + Cable TV 60 = 3410
    // ห้อง 102: 3500 + น้ำ 10 หน่วย 190 + ไฟ 10 หน่วย 80 + Cable TV 60 = 3830
    private sealed class DraftTab : IDisposable
    {
        private readonly Form _form = new();
        private readonly MonthlyBillTabControl _tab;

        public DraftTab(bool room102HasMeter = true, bool allRoomsBilledLastMonth = false)
        {
            var rooms = new InMemoryRoomRepository();
            var room101 = new CreateRoomService(rooms).Create("101", 3000m, false);
            var room102 = new CreateRoomService(rooms).Create("102", 3500m, false);
            var tenants = new InMemoryTenantRepository();
            TestTenants.MoveInto(tenants, room101.Id);
            TestTenants.MoveInto(tenants, room102.Id);
            var readings = new InMemoryMonthlyMeterReadingRepository();
            var saveReading = new SaveMonthlyMeterReadingService(readings);
            saveReading.Save(room101.Id, ThisMonth, Meter(100m), Meter(110m), Meter(200m), Meter(220m));
            if (room102HasMeter)
            {
                saveReading.Save(room102.Id, ThisMonth, Meter(0m), Meter(10m), Meter(0m), Meter(10m));
            }

            if (allRoomsBilledLastMonth)
            {
                foreach (var roomId in new[] { room101.Id, room102.Id })
                {
                    new CreateMonthlyBillService(Bills).Create(new CreateMonthlyBillRequest(
                        roomId, ThisMonth.GetPrevious(), 3000m, Meter(0m), Meter(0m), Meter(0m), Meter(0m), 19m, 8m,
                        Tenant: TestTenants.Snapshot()));
                }
            }

            _tab = new MonthlyBillTabControl(
                new CreateMonthlyBillService(Bills),
                rooms,
                monthlyMeterReadingRepository: readings,
                billRepository: Bills,
                tenantRepository: tenants)
            {
                Dock = DockStyle.Fill
            };
            _form.Controls.Add(_tab);
            HiddenTestWindows.ShowHidden(_form);
        }

        public InMemoryBillRepository Bills { get; } = new();

        public Label Message => Find<Label>("BillMessageLabel");

        public ComboBox RoomBox => Find<ComboBox>("BillRoomComboBox");

        public string Title => Find<GroupBox>("ResultGroup").Text;

        public string Text(string labelName) => Find<Label>(labelName).Text;

        public T Find<T>(string name) where T : Control => (T)_tab.Controls.Find(name, true)[0];

        public void Create() => Find<Button>("CreateBillButton").PerformClick();

        public void Dispose() => _form.Dispose();
    }

    private static MeterReading Meter(decimal value) => MeterReading.Create(value);

    [Fact]
    public void OpeningTab_ShowsDraftOfSelectedRoom_WithoutInvoiceNumberOrSaving()
    {
        RunInSta(() =>
        {
            using var tab = new DraftTab();

            Assert.Equal("101", tab.RoomBox.Text);
            Assert.Equal(DraftSummaryTitle, tab.Title);
            Assert.Equal("ห้อง 101", tab.Text("ResultRoomLabel"));
            Assert.Equal("ใบแจ้งหนี้ เลขที่ : จะได้เลขตอนออกบิล", tab.Text("ResultInvoiceNumberLabel"));
            Assert.Equal("วันที่ออกบิล: -", tab.Text("ResultCreatedAtLabel"));
            Assert.Equal("ผู้เช่า: ผู้เช่า ทดสอบ", tab.Text("ResultTenantLabel"));
            Assert.Equal("3,000.00 บาท", tab.Text("ResultRentLabel"));
            Assert.Equal("└ มิเตอร์ 100 → 110 (10 หน่วย)", tab.Text("ResultWaterDetailsLabel"));
            Assert.Equal("3,410.00 บาท", tab.Text("ResultTotalLabel"));
            Assert.Equal($"โหลดข้อมูลมิเตอร์รอบบิล {ThaiPeriod} เรียบร้อย", tab.Message.Text);
            Assert.Empty(tab.Bills.GetAll());
        });
    }

    [Fact]
    public void Draft_FollowsCheckBoxesBeforeCreating()
    {
        RunInSta(() =>
        {
            using var tab = new DraftTab();

            tab.Find<CheckBox>("ChargeMotorcycleCheckBox").Checked = true;
            Assert.Equal("100.00 บาท", tab.Text("ResultMotorcycleParkingLabel"));
            Assert.Equal("3,510.00 บาท", tab.Text("ResultTotalLabel"));

            tab.Find<CheckBox>("IsCableTvExemptCheckBox").Checked = true;
            Assert.Equal("0.00 บาท", tab.Text("ResultCableTvLabel"));
            Assert.Equal("3,450.00 บาท", tab.Text("ResultTotalLabel"));

            Assert.Equal(DraftSummaryTitle, tab.Title);
            Assert.Empty(tab.Bills.GetAll());
        });
    }

    [Fact]
    public void SelectingAnotherRoom_ShowsDraftOfThatRoom()
    {
        RunInSta(() =>
        {
            using var tab = new DraftTab();

            tab.RoomBox.SelectedIndex = 1;

            Assert.Equal(DraftSummaryTitle, tab.Title);
            Assert.Equal("ห้อง 102", tab.Text("ResultRoomLabel"));
            Assert.Equal("3,830.00 บาท", tab.Text("ResultTotalLabel"));
        });
    }

    [Fact]
    public void SelectingRoomWithoutMeter_ClearsSummaryAndShowsReason()
    {
        RunInSta(() =>
        {
            using var tab = new DraftTab(room102HasMeter: false);

            tab.RoomBox.SelectedIndex = 1;

            Assert.Equal(SummaryTitle, tab.Title);
            Assert.Equal("ห้อง -", tab.Text("ResultRoomLabel"));
            Assert.Equal("ใบแจ้งหนี้ เลขที่ : -", tab.Text("ResultInvoiceNumberLabel"));
            Assert.Equal("ผู้เช่า: -", tab.Text("ResultTenantLabel"));
            Assert.Equal("-", tab.Text("ResultRentLabel"));
            Assert.Equal("-", tab.Text("ResultWaterDetailsLabel"));
            Assert.Equal("-", tab.Text("ResultTotalLabel"));
            Assert.Equal(MissingMeterMessage, tab.Message.Text);
        });
    }

    [Fact]
    public void ChangingPeriod_RecalculatesDraft()
    {
        RunInSta(() =>
        {
            using var tab = new DraftTab();
            var periodSelector = tab.Find<BillingPeriodSelector>("BillingPeriodSelector");

            periodSelector.SetBillingPeriod(ThisMonth.GetPrevious());
            Assert.Equal("-", tab.Text("ResultTotalLabel"));
            Assert.Equal(MissingMeterMessage, tab.Message.Text);

            periodSelector.SetBillingPeriod(ThisMonth);
            Assert.Equal(DraftSummaryTitle, tab.Title);
            Assert.Equal("3,410.00 บาท", tab.Text("ResultTotalLabel"));
        });
    }

    [Fact]
    public void ChangingToPeriodWithNoRoomLeftToBill_ClearsDraft()
    {
        RunInSta(() =>
        {
            using var tab = new DraftTab(allRoomsBilledLastMonth: true);
            Assert.Equal(DraftSummaryTitle, tab.Title);

            tab.Find<BillingPeriodSelector>("BillingPeriodSelector").SetBillingPeriod(ThisMonth.GetPrevious());

            Assert.Equal(0, tab.RoomBox.Items.Count);
            Assert.Equal(SummaryTitle, tab.Title);
            Assert.Equal("ห้อง -", tab.Text("ResultRoomLabel"));
            Assert.Equal("-", tab.Text("ResultTotalLabel"));
        });
    }

    [Fact]
    public void AfterCreating_ShowsDraftOfNextRoomImmediately()
    {
        RunInSta(() =>
        {
            using var tab = new DraftTab();

            tab.Create();

            var created = Assert.Single(tab.Bills.GetAll());
            Assert.Equal("N001", created.InvoiceNumber);
            Assert.Equal(3410m, created.Total.Amount);
            Assert.Equal("102", tab.RoomBox.Text);
            Assert.Equal(DraftSummaryTitle, tab.Title);
            Assert.Equal("ห้อง 102", tab.Text("ResultRoomLabel"));
            Assert.Equal("ใบแจ้งหนี้ เลขที่ : จะได้เลขตอนออกบิล", tab.Text("ResultInvoiceNumberLabel"));
            Assert.Equal("3,830.00 บาท", tab.Text("ResultTotalLabel"));
            Assert.Equal($"สร้างบิลสำหรับห้อง 101 รอบ {ThaiPeriod} สำเร็จเรียบร้อย", tab.Message.Text);
        });
    }

    [Fact]
    public void AfterCreating_WhenNextRoomHasNoMeter_ClearsSummaryAndKeepsSuccessMessage()
    {
        RunInSta(() =>
        {
            using var tab = new DraftTab(room102HasMeter: false);

            tab.Create();

            Assert.Single(tab.Bills.GetAll());
            Assert.Equal("102", tab.RoomBox.Text);
            Assert.Equal(SummaryTitle, tab.Title);
            Assert.Equal("ห้อง -", tab.Text("ResultRoomLabel"));
            Assert.Equal("-", tab.Text("ResultTotalLabel"));
            Assert.Equal($"สร้างบิลสำหรับห้อง 101 รอบ {ThaiPeriod} สำเร็จเรียบร้อย", tab.Message.Text);
        });
    }
}
