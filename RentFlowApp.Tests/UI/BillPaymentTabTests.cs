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

        public PaymentTab(params string[] roomNumbers)
        {
            var roomRepo = new InMemoryRoomRepository();
            var createRoomService = new CreateRoomService(roomRepo);
            var createBillService = new CreateMonthlyBillService(Bills);
            var period = BillingPeriod.Create($"{DateTime.Now.Year}-10");
            var meter = MeterReading.Create(100m);

            foreach (var roomNumber in roomNumbers)
            {
                var room = createRoomService.Create(roomNumber, 3000m, true);
                createBillService.Create(new CreateMonthlyBillRequest(
                    room.Id, period, 3000m, meter, meter, meter, meter, 19m, 8m));
            }

            _tab = new MonthlyBillTabControl(
                createBillService,
                roomRepo,
                billRepository: Bills,
                confirmCancelPayment: message =>
                {
                    ConfirmMessages.Add(message);
                    return ConfirmAnswer;
                })
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
