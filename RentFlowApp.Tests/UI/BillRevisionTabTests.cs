using System.Globalization;
using RentFlowApp.Application.Billing;
using RentFlowApp.Application.Reports;
using RentFlowApp.Application.Rooms;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Forms;
using RentFlowApp.Infrastructure.Billing;
using RentFlowApp.Infrastructure.Rooms;
using RentFlowApp.Infrastructure.Tenants;
using static RentFlowApp.Tests.UI.InputTestHelper;

namespace RentFlowApp.Tests.UI;

public class BillRevisionTabTests
{
    private const string SaveEditButtonText = "บันทึกการแก้ไขบิล";

    private static readonly BillingPeriod ThisMonth =
        BillingPeriod.Create(DateTime.Now.ToString("yyyy-MM", CultureInfo.InvariantCulture));

    private static readonly string ThaiPeriod = ThaiCalendar.FormatPeriod(ThisMonth);

    // บิลออกด้วยค่าเช่าที่กรอกผิด 2800 และมิเตอร์น้ำ 100 → 110 ต่อมาแก้ต้นทางเป็นค่าเช่า 3000 และมิเตอร์น้ำ 100 → 115
    // ยอดเดิม 2800 + น้ำ 190 + ไฟ 160 + Cable TV 60 = 3210
    // ยอดใหม่ 3000 + น้ำ 285 + ไฟ 160 + Cable TV 60 = 3505
    private sealed class RevisionTab : IDisposable
    {
        private readonly Form _form = new();
        private readonly MonthlyBillTabControl _tab;

        public RevisionTab(bool billHasMeterSnapshot = true, bool billIsPaid = false)
        {
            var rooms = new InMemoryRoomRepository();
            var room = new CreateRoomService(rooms).Create("101", 3000m, false);
            var tenants = new InMemoryTenantRepository();
            TestTenants.MoveInto(tenants, room.Id);
            var readings = new InMemoryMonthlyMeterReadingRepository();
            var saveReading = new SaveMonthlyMeterReadingService(readings);
            saveReading.Save(room.Id, ThisMonth, Meter(100m), Meter(110m), Meter(200m), Meter(220m));

            Bill = billHasMeterSnapshot
                ? new CreateMonthlyBillService(Bills).Create(new CreateMonthlyBillRequest(
                    room.Id, ThisMonth, 2800m, Meter(100m), Meter(110m), Meter(200m), Meter(220m), 19m, 8m,
                    IsCableTvExempt: false,
                    CableTvRate: 60m,
                    Tenant: TestTenants.Snapshot()))
                : AddBillWithoutMeterSnapshot(room.Id);

            if (billIsPaid)
            {
                new BillPaymentService(Bills).RecordPayment(Bill.Id, DateOnly.FromDateTime(DateTime.Today), PaymentMethod.Cash);
            }

            saveReading.Save(room.Id, ThisMonth, Meter(100m), Meter(115m), Meter(200m), Meter(220m));

            _tab = new MonthlyBillTabControl(
                new CreateMonthlyBillService(Bills),
                rooms,
                monthlyMeterReadingRepository: readings,
                billRepository: Bills,
                tenantRepository: tenants,
                confirmBillRevision: message =>
                {
                    ConfirmMessages.Add(message);
                    return ConfirmAnswer;
                })
            {
                Dock = DockStyle.Fill
            };
            _form.Controls.Add(_tab);
            HiddenTestWindows.ShowHidden(_form);
        }

        public InMemoryBillRepository Bills { get; } = new();

        public Bill Bill { get; }

        public Bill StoredBill => Bills.Get(Bill.Id)!;

        public bool ConfirmAnswer { get; set; } = true;

        public List<string> ConfirmMessages { get; } = [];

        public Label Message => Find<Label>("BillMessageLabel");

        public Button PullButton => Find<Button>("PullRoomDataButton");

        public Button SaveButton => Find<Button>("CreateBillButton");

        public T Find<T>(string name) where T : Control => (T)_tab.Controls.Find(name, true)[0];

        public void OpenBill() => DoubleClickRow(Find<DataGridView>("BillGrid"), 0);

        public void Dispose() => _form.Dispose();

        private Bill AddBillWithoutMeterSnapshot(RoomId roomId)
        {
            var bill = Bill.Create(
                roomId,
                ThisMonth,
                BillingCalculator.Calculate(2800m, Meter(100m), Meter(110m), Meter(200m), Meter(220m), 19m, 8m, cableTvRate: 60m),
                tenant: TestTenants.Snapshot());
            Bills.Add(bill);
            return bill;
        }
    }

    private static MeterReading Meter(decimal value) => MeterReading.Create(value);

    [Fact]
    public void PullRoomDataButton_IsAvailableOnlyWhileEditingUnpaidBill()
    {
        RunInSta(() =>
        {
            using var unpaid = new RevisionTab();
            Assert.False(unpaid.PullButton.Enabled);

            unpaid.OpenBill();
            Assert.True(unpaid.PullButton.Enabled);

            using var paid = new RevisionTab(billIsPaid: true);
            paid.OpenBill();
            Assert.False(paid.PullButton.Enabled);
        });
    }

    [Fact]
    public void PullRoomData_ShowsLatestValuesInFields_WithoutSavingBill()
    {
        RunInSta(() =>
        {
            using var tab = new RevisionTab();
            tab.OpenBill();

            tab.PullButton.PerformClick();

            Assert.Equal("3000", tab.Find<TextBox>("MonthlyRentTextBox").Text);
            Assert.Equal("100", tab.Find<TextBox>("PrevWaterTextBox").Text);
            Assert.Equal("115", tab.Find<TextBox>("CurrWaterTextBox").Text);
            Assert.Equal("ดึงข้อมูลห้องล่าสุดแล้ว ยังไม่ได้บันทึก กรุณาตรวจยอดแล้วกดบันทึกการแก้ไขบิล", tab.Message.Text);
            Assert.Equal(2800m, tab.StoredBill.Rent.Amount);
            Assert.Equal(3210m, tab.StoredBill.Total.Amount);
            Assert.Empty(tab.ConfirmMessages);
            Assert.Equal(SaveEditButtonText, tab.SaveButton.Text);
        });
    }

    [Fact]
    public void SaveAfterPull_WhenAnsweredNo_KeepsStoredBillAndStaysInEditMode()
    {
        RunInSta(() =>
        {
            using var tab = new RevisionTab { ConfirmAnswer = false };
            tab.OpenBill();
            tab.PullButton.PerformClick();

            tab.SaveButton.PerformClick();

            Assert.Equal(
                $"ยืนยันบันทึกการแก้ไขบิลห้อง 101 รอบ {ThaiPeriod}\n\nยอดเดิม 3,210.00 บาท\nยอดใหม่ 3,505.00 บาท",
                Assert.Single(tab.ConfirmMessages));
            Assert.Equal(2800m, tab.StoredBill.Rent.Amount);
            Assert.Equal(190m, tab.StoredBill.Water.Amount);
            Assert.Equal(3210m, tab.StoredBill.Total.Amount);
            Assert.Equal(Meter(110m), tab.StoredBill.Meters!.CurrentWater);
            Assert.Equal("ยกเลิกการบันทึก บิลยังเป็นยอดเดิม", tab.Message.Text);
            Assert.Equal(SaveEditButtonText, tab.SaveButton.Text);
        });
    }

    [Fact]
    public void SaveAfterPull_WhenAnsweredYes_SavesNewTotalAndAsksToReprint()
    {
        RunInSta(() =>
        {
            using var tab = new RevisionTab();
            tab.OpenBill();
            tab.PullButton.PerformClick();

            tab.SaveButton.PerformClick();

            Assert.Single(tab.ConfirmMessages);
            Assert.Equal(3000m, tab.StoredBill.Rent.Amount);
            Assert.Equal(285m, tab.StoredBill.Water.Amount);
            Assert.Equal(3505m, tab.StoredBill.Total.Amount);
            Assert.Equal(Meter(115m), tab.StoredBill.Meters!.CurrentWater);
            Assert.Equal(tab.Bill.InvoiceNumber, tab.StoredBill.InvoiceNumber);
            Assert.Equal(
                $"บันทึกการแก้ไขบิลห้อง 101 รอบ {ThaiPeriod} สำเร็จเรียบร้อย หากพิมพ์บิลนี้ให้ผู้เช่าแล้ว กรุณาพิมพ์ใหม่",
                tab.Message.Text);
            Assert.Equal("3,505.00 บาท", tab.Find<Label>("ResultTotalLabel").Text);
        });
    }

    [Fact]
    public void SaveEdit_WhenTotalDoesNotChange_SavesWithoutAsking()
    {
        RunInSta(() =>
        {
            using var tab = new RevisionTab();
            tab.OpenBill();

            tab.SaveButton.PerformClick();

            Assert.Empty(tab.ConfirmMessages);
            Assert.Equal($"บันทึกการแก้ไขบิลห้อง 101 รอบ {ThaiPeriod} สำเร็จเรียบร้อย", tab.Message.Text);
        });
    }

    [Fact]
    public void SaveEdit_WhenParkingChangesTotal_AsksAndAnswerNoKeepsStoredBill()
    {
        RunInSta(() =>
        {
            using var tab = new RevisionTab { ConfirmAnswer = false };
            tab.OpenBill();
            tab.Find<CheckBox>("ChargeMotorcycleCheckBox").Checked = true;

            tab.SaveButton.PerformClick();

            Assert.Equal(
                $"ยืนยันบันทึกการแก้ไขบิลห้อง 101 รอบ {ThaiPeriod}\n\nยอดเดิม 3,210.00 บาท\nยอดใหม่ 3,310.00 บาท",
                Assert.Single(tab.ConfirmMessages));
            Assert.Equal(0m, tab.StoredBill.MotorcycleParking.Amount);
            Assert.Equal(3210m, tab.StoredBill.Total.Amount);
        });
    }

    private const string SummaryTitle = "สรุปรายละเอียดบิล";
    private const string DraftSummaryTitle = "สรุปรายละเอียดบิล (ร่าง ยังไม่บันทึก)";

    [Fact]
    public void OpenBill_ShowsSavedBillWithoutDraftTitle()
    {
        RunInSta(() =>
        {
            using var tab = new RevisionTab();

            tab.OpenBill();

            Assert.Equal(SummaryTitle, tab.Find<GroupBox>("ResultGroup").Text);
            Assert.Equal("3,210.00 บาท", tab.Find<Label>("ResultTotalLabel").Text);
        });
    }

    [Fact]
    public void PullRoomData_ShowsDraftInSummaryBeforeSaving()
    {
        RunInSta(() =>
        {
            using var tab = new RevisionTab();
            tab.OpenBill();

            tab.PullButton.PerformClick();

            Assert.Equal(DraftSummaryTitle, tab.Find<GroupBox>("ResultGroup").Text);
            Assert.Equal("3,000.00 บาท", tab.Find<Label>("ResultRentLabel").Text);
            Assert.Equal("└ มิเตอร์ 100 → 115 (15 หน่วย)", tab.Find<Label>("ResultWaterDetailsLabel").Text);
            Assert.Equal("285.00 บาท", tab.Find<Label>("ResultWaterAmountLabel").Text);
            Assert.Equal("3,505.00 บาท", tab.Find<Label>("ResultTotalLabel").Text);
            Assert.Equal($"ใบแจ้งหนี้ เลขที่ : {tab.Bill.InvoiceNumber}", tab.Find<Label>("ResultInvoiceNumberLabel").Text);
            Assert.Equal(3210m, tab.StoredBill.Total.Amount);
        });
    }

    [Fact]
    public void Draft_FollowsCheckBoxesWhileEditing()
    {
        RunInSta(() =>
        {
            using var tab = new RevisionTab();
            tab.OpenBill();
            tab.PullButton.PerformClick();

            tab.Find<CheckBox>("ChargeMotorcycleCheckBox").Checked = true;

            Assert.Equal(DraftSummaryTitle, tab.Find<GroupBox>("ResultGroup").Text);
            Assert.Equal("100.00 บาท", tab.Find<Label>("ResultMotorcycleParkingLabel").Text);
            Assert.Equal("3,605.00 บาท", tab.Find<Label>("ResultTotalLabel").Text);
            Assert.Equal(0m, tab.StoredBill.MotorcycleParking.Amount);
        });
    }

    [Fact]
    public void Draft_StaysAfterAnswerNo_AndClearsAfterAnswerYes()
    {
        RunInSta(() =>
        {
            using var tab = new RevisionTab { ConfirmAnswer = false };
            tab.OpenBill();
            tab.PullButton.PerformClick();

            tab.SaveButton.PerformClick();
            Assert.Equal(DraftSummaryTitle, tab.Find<GroupBox>("ResultGroup").Text);
            Assert.Equal("3,505.00 บาท", tab.Find<Label>("ResultTotalLabel").Text);

            tab.ConfirmAnswer = true;
            tab.SaveButton.PerformClick();
            Assert.Equal(SummaryTitle, tab.Find<GroupBox>("ResultGroup").Text);
            Assert.Equal("3,505.00 บาท", tab.Find<Label>("ResultTotalLabel").Text);
        });
    }

    [Fact]
    public void LeavingEditWithoutSaving_DropsRevisionDraftAndFollowsNewSelection()
    {
        RunInSta(() =>
        {
            using var tab = new RevisionTab();
            tab.OpenBill();
            tab.PullButton.PerformClick();

            // ออกจากโหมดแก้ไขแล้วกลับเป็นโหมดออกบิลของห้อง 101 รอบก่อน ซึ่งยังไม่มีมิเตอร์ จึงคำนวณร่างไม่ได้
            tab.Find<BillingPeriodSelector>("BillingPeriodSelector").SetBillingPeriod(ThisMonth.GetPrevious());

            Assert.NotEqual(SaveEditButtonText, tab.SaveButton.Text);
            Assert.Equal(SummaryTitle, tab.Find<GroupBox>("ResultGroup").Text);
            Assert.Equal("-", tab.Find<Label>("ResultTotalLabel").Text);
            Assert.Equal(
                "ยังไม่มีข้อมูลบันทึกมิเตอร์ของรอบบิลนี้ กรุณาบันทึกมิเตอร์น้ำ-ไฟก่อนทำการสร้างบิล",
                tab.Message.Text);
            Assert.Equal(3210m, tab.StoredBill.Total.Amount);
        });
    }

    [Fact]
    public void PullRoomData_OnBillWithoutMeterSnapshot_ChangesRentOnlyAndShowsNotice()
    {
        RunInSta(() =>
        {
            using var tab = new RevisionTab(billHasMeterSnapshot: false);
            tab.OpenBill();

            tab.PullButton.PerformClick();
            Assert.Equal(
                "ดึงข้อมูลห้องล่าสุดแล้ว ยังไม่ได้บันทึก กรุณาตรวจยอดแล้วกดบันทึกการแก้ไขบิล "
                + "บิลนี้ไม่ได้เก็บอัตราค่าน้ำและค่าไฟไว้ จึงดึงได้เฉพาะค่าเช่าและสถานะยกเว้น Cable TV",
                tab.Message.Text);

            tab.SaveButton.PerformClick();

            Assert.Equal(3000m, tab.StoredBill.Rent.Amount);
            Assert.Equal(190m, tab.StoredBill.Water.Amount);
            Assert.Equal(3410m, tab.StoredBill.Total.Amount);
        });
    }
}
