using RentFlowApp.Application.Billing;
using RentFlowApp.Application.Rooms;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Forms;
using RentFlowApp.Infrastructure.Billing;
using RentFlowApp.Infrastructure.Rooms;

namespace RentFlowApp.Tests.UI;

public class BillPaymentTabTests
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

    private sealed class PaymentTab : IDisposable
    {
        private readonly Form _form = new();
        private readonly MonthlyBillTabControl _tab;
        private readonly CreateRoomService _createRoomService;
        private readonly CreateMonthlyBillService _createBillService;

        public PaymentTab(params string[] roomNumbers)
        {
            var roomRepo = new InMemoryRoomRepository();
            _createRoomService = new CreateRoomService(roomRepo);
            _createBillService = new CreateMonthlyBillService(Bills);
            var period = BillingPeriod.Create($"{DateTime.Now.Year}-10");

            foreach (var roomNumber in roomNumbers)
            {
                CreateBill(roomNumber, period);
            }

            var invoiceSettings = new InMemoryInvoiceSettingsRepository();
            invoiceSettings.Save(InvoiceSettings.Create("หอทดสอบ", "ที่อยู่ทดสอบ", null));

            _tab = new MonthlyBillTabControl(
                _createBillService,
                roomRepo,
                billRepository: Bills,
                invoiceSettingsRepository: invoiceSettings,
                showInvoicePreview: document => Printed = document.Invoices,
                confirmCancelPayment: message =>
                {
                    ConfirmMessages.Add(message);
                    return ConfirmAnswer;
                },
                // กำหนดวันนี้ตายตัว ก่อนครบกำหนดของรอบเดือน 10 เพื่อไม่ให้ผลเทสต์เปลี่ยนตามวันที่รัน
                today: () => new DateOnly(DateTime.Now.Year, 10, 15))
            {
                Dock = DockStyle.Fill
            };
            _form.Controls.Add(_tab);
            _form.Show();
            Find<BillingPeriodSelector>("BillingPeriodSelector").SetBillingPeriod(period);
        }

        public InMemoryBillRepository Bills { get; } = new();

        public bool ConfirmAnswer { get; set; } = true;

        public List<string> ConfirmMessages { get; } = [];

        public IReadOnlyList<MonthlyInvoice>? Printed { get; private set; }

        public void AddBill(string roomNumber, string monthOfThisYear)
        {
            CreateBill(roomNumber, BillingPeriod.Create($"{DateTime.Now.Year}-{monthOfThisYear}"));
            _tab.RefreshBillGrid();
        }

        private void CreateBill(string roomNumber, BillingPeriod period)
        {
            var meter = MeterReading.Create(100m);
            var room = _createRoomService.Create(roomNumber, 3000m, true);
            _createBillService.Create(new CreateMonthlyBillRequest(
                room.Id, period, 3000m, meter, meter, meter, meter, 19m, 8m));
        }

        public IEnumerable<string?> GridColumn(string columnName) =>
            Grid.Rows.Cast<DataGridViewRow>().Select(row => row.Cells[columnName].Value?.ToString());

        public DataGridView Grid => Find<DataGridView>("BillGrid");

        public Label Message => Find<Label>("BillMessageLabel");

        public T Find<T>(string name) where T : Control => (T)_tab.Controls.Find(name, true)[0];

        public void Click(string buttonName) => Find<Button>(buttonName).PerformClick();

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
    public void RecordPayment_StoresChosenDateAndMethodAndShowsStatusInGrid()
    {
        RunInSta(() =>
        {
            using var tab = new PaymentTab("101", "102");
            var paidOn = DateTime.Today.AddDays(-2);
            string thaiDate = $"{paidOn.Day:D2}/{paidOn.Month:D2}/{paidOn.Year + 543}";
            Assert.Equal("ยังไม่จ่าย", tab.Grid.Rows[1].Cells["PaymentStatus"].Value);

            tab.SelectOnly(1);
            tab.Find<DateTimePicker>("PaymentDatePicker").Value = paidOn;
            tab.Find<ComboBox>("PaymentMethodComboBox").SelectedIndex = 1;
            tab.Click("RecordPaymentButton");

            Assert.Equal($"จ่ายแล้ว {thaiDate} (โอนเข้าบัญชี)", tab.Grid.Rows[1].Cells["PaymentStatus"].Value);
            Assert.Equal("ยังไม่จ่าย", tab.Grid.Rows[0].Cells["PaymentStatus"].Value);
            Assert.True(tab.Grid.Rows[1].Selected);
            Assert.Equal(System.Drawing.Color.DarkGreen, tab.Message.ForeColor);
            Assert.Contains("ห้อง 102", tab.Message.Text);
            Assert.Equal(1, tab.Bills.GetAll().Count(bill => bill.IsPaid));
        });
    }

    [Fact]
    public void CancelPayment_ReturnsBillToUnpaidForTheSameRow()
    {
        RunInSta(() =>
        {
            using var tab = new PaymentTab("101", "102");
            tab.SelectOnly(1);
            tab.Click("RecordPaymentButton");

            tab.Click("CancelPaymentButton");

            Assert.Equal("ยังไม่จ่าย", tab.Grid.Rows[1].Cells["PaymentStatus"].Value);
            Assert.DoesNotContain(tab.Bills.GetAll(), bill => bill.IsPaid);
        });
    }

    [Fact]
    public void CancelPayment_WhenUserDeclines_KeepsBillPaid()
    {
        RunInSta(() =>
        {
            using var tab = new PaymentTab("101", "102");
            tab.SelectOnly(1);
            tab.Click("RecordPaymentButton");
            string statusBefore = (string)tab.Grid.Rows[1].Cells["PaymentStatus"].Value!;
            tab.ConfirmAnswer = false;

            tab.Click("CancelPaymentButton");

            string message = Assert.Single(tab.ConfirmMessages);
            Assert.Contains("ห้อง 102", message);
            Assert.Contains(statusBefore, message);
            Assert.Equal(statusBefore, tab.Grid.Rows[1].Cells["PaymentStatus"].Value);
            Assert.Equal(1, tab.Bills.GetAll().Count(bill => bill.IsPaid));
        });
    }

    [Fact]
    public void CancelPayment_WhenBillIsUnpaid_ShowsMessageWithoutAskingToConfirm()
    {
        RunInSta(() =>
        {
            using var tab = new PaymentTab("101");
            tab.SelectOnly(0);

            tab.Click("CancelPaymentButton");

            Assert.Empty(tab.ConfirmMessages);
            Assert.Contains("ยังไม่ได้บันทึกรับชำระ", tab.Message.Text);
        });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void RecordPayment_WhenNotExactlyOneBillIsSelected_ChangesNothing(int selectedCount)
    {
        RunInSta(() =>
        {
            using var tab = new PaymentTab("101", "102");
            tab.Grid.ClearSelection();
            for (int i = 0; i < selectedCount; i++)
            {
                tab.Grid.Rows[i].Selected = true;
            }

            tab.Click("RecordPaymentButton");

            Assert.Contains("กรุณาเลือกบิลในตาราง 1 รายการ", tab.Message.Text);
            Assert.DoesNotContain(tab.Bills.GetAll(), bill => bill.IsPaid);
        });
    }

    [Fact]
    public void DoubleClickPaidBill_DoesNotEnterEditModeUntilPaymentIsCancelled()
    {
        RunInSta(() =>
        {
            using var tab = new PaymentTab("101");
            var createButton = tab.Find<Button>("CreateBillButton");
            tab.SelectOnly(0);
            tab.Click("RecordPaymentButton");

            tab.DoubleClickRow(0);

            Assert.Equal("คำนวณและสร้างบิล", createButton.Text);
            Assert.Contains("กรุณายกเลิกการรับชำระก่อน", tab.Message.Text);

            tab.SelectOnly(0);
            tab.Click("CancelPaymentButton");
            tab.DoubleClickRow(0);

            Assert.Equal("บันทึกการแก้ไขบิล", createButton.Text);
        });
    }

    [Fact]
    public void Grid_ShowsTotalRightAfterPaymentStatusWithItsValue()
    {
        RunInSta(() =>
        {
            using var tab = new PaymentTab("101");

            Assert.Equal(
                tab.Grid.Columns["PaymentStatus"]!.DisplayIndex + 1,
                tab.Grid.Columns["TotalAmount"]!.DisplayIndex);
            Assert.Equal("3,000.00", tab.Grid.Rows[0].Cells["TotalAmount"].Value);
        });
    }

    [Fact]
    public void BillOfCurrentPeriod_WhenPastDueDate_ShowsOverdueDaysInStatusAndSummary()
    {
        RunInSta(() =>
        {
            using var tab = new PaymentTab("101");
            tab.AddBill("102", "09");

            tab.Find<BillingPeriodSelector>("BillingPeriodSelector")
                .SetBillingPeriod(BillingPeriod.Create($"{DateTime.Now.Year}-09"));

            Assert.Equal(["เกินกำหนด 10 วัน"], tab.GridColumn("PaymentStatus"));
            Assert.Equal(
                "ค้างชำระ 1 บิล รวม 3,000.00 บาท (เกินกำหนด 1 บิล)",
                tab.Find<Label>("OutstandingSummaryLabel").Text);
        });
    }

    [Fact]
    public void OutstandingFilter_ShowsUnpaidBillsOfAllMonthsOldestFirstAndCanBeTurnedOff()
    {
        RunInSta(() =>
        {
            using var tab = new PaymentTab("101", "102");
            tab.AddBill("103", "08");
            tab.AddBill("104", "09");
            tab.SelectOnly(1);
            tab.Click("RecordPaymentButton");
            var filter = tab.Find<CheckBox>("ShowOutstandingCheckBox");

            filter.Checked = true;

            Assert.Equal(["103", "104", "101"], tab.GridColumn("RoomNumber"));
            Assert.Equal(["เกินกำหนด 40 วัน", "เกินกำหนด 10 วัน", "ยังไม่จ่าย"], tab.GridColumn("PaymentStatus"));
            Assert.Equal(
                "ค้างชำระ 3 บิล รวม 9,000.00 บาท (เกินกำหนด 2 บิล)",
                tab.Find<Label>("OutstandingSummaryLabel").Text);

            filter.Checked = false;

            Assert.Equal(["101", "102"], tab.GridColumn("RoomNumber"));
        });
    }

    [Fact]
    public void OutstandingFilter_RecordingPaymentRemovesRowWithoutSelectingAnotherBill()
    {
        RunInSta(() =>
        {
            using var tab = new PaymentTab("101");
            tab.AddBill("103", "08");
            tab.Find<CheckBox>("ShowOutstandingCheckBox").Checked = true;
            tab.SelectOnly(0);

            tab.Click("RecordPaymentButton");

            Assert.Equal(["101"], tab.GridColumn("RoomNumber"));
            Assert.Empty(tab.Grid.SelectedRows.Cast<DataGridViewRow>());
            Assert.Equal(1, tab.Bills.GetAll().Count(bill => bill.IsPaid));
            Assert.Equal(
                "ค้างชำระ 1 บิล รวม 3,000.00 บาท (เกินกำหนด 0 บิล)",
                tab.Find<Label>("OutstandingSummaryLabel").Text);
        });
    }

    [Fact]
    public void OutstandingFilter_PrintAllUsesRowsInGridAndRenamesButton()
    {
        RunInSta(() =>
        {
            using var tab = new PaymentTab("101", "102");
            tab.AddBill("103", "08");
            tab.SelectOnly(1);
            tab.Click("RecordPaymentButton");
            var printAll = tab.Find<Button>("PrintAllInvoicesButton");
            Assert.Equal("พิมพ์ใบแจ้งหนี้ทั้งรอบ", printAll.Text);

            tab.Find<CheckBox>("ShowOutstandingCheckBox").Checked = true;
            tab.Click("PrintAllInvoicesButton");

            Assert.Equal("พิมพ์บิลค้างชำระทั้งหมด", printAll.Text);
            Assert.Equal(["103", "101"], tab.Printed!.Select(invoice => invoice.RoomNumber));
        });
    }

    [Fact]
    public void OutstandingFilter_DoubleClickOlderBillEditsItAndKeepsFilterOn()
    {
        RunInSta(() =>
        {
            using var tab = new PaymentTab("101");
            tab.AddBill("103", "08");
            var selector = tab.Find<BillingPeriodSelector>("BillingPeriodSelector");
            tab.Find<CheckBox>("ShowOutstandingCheckBox").Checked = true;

            tab.DoubleClickRow(0);

            Assert.Equal("บันทึกการแก้ไขบิล", tab.Find<Button>("CreateBillButton").Text);
            Assert.Equal(BillingPeriod.Create($"{DateTime.Now.Year}-08"), selector.GetBillingPeriod());
            Assert.Equal(["103", "101"], tab.GridColumn("RoomNumber"));
        });
    }

    [Fact]
    public void RecordPayment_WhileEditingSameBill_LeavesEditModeSoChargesCannotChange()
    {
        RunInSta(() =>
        {
            using var tab = new PaymentTab("101");
            var createButton = tab.Find<Button>("CreateBillButton");
            tab.DoubleClickRow(0);
            Assert.Equal("บันทึกการแก้ไขบิล", createButton.Text);

            tab.SelectOnly(0);
            tab.Click("RecordPaymentButton");

            Assert.Equal("คำนวณและสร้างบิล", createButton.Text);
            Assert.Equal(System.Drawing.Color.DarkGreen, tab.Message.ForeColor);
            Assert.True(tab.Bills.GetAll().Single().IsPaid);
        });
    }
}
