using System.Drawing.Printing;
using RentFlowApp.Application.Billing;
using RentFlowApp.Application.Rooms;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Forms;
using RentFlowApp.Infrastructure.Billing;
using RentFlowApp.Infrastructure.Rooms;

namespace RentFlowApp.Tests.UI;

public class MonthlyInvoicePrintingTests
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

    private static MonthlyInvoice CreateInvoice(int index) => new(
        "หอทดสอบ",
        "ที่อยู่ทดสอบ",
        "ชำระด้วยเงินสด หรือโอนเข้าบัญชี",
        "ธนาคารทดสอบ",
        $"N.{index:D3}",
        $"{index}",
        "10/2569",
        "25/10/2569",
        "05/11/2569",
        "คุณ ทดสอบ ระบบ",
        [new MonthlyInvoiceLine("ค่าเช่าห้อง", string.Empty, string.Empty, string.Empty, "3,000.-")],
        "3,000.-",
        "ชำระแล้ว เมื่อวันที่ 03/11/2569 (เงินสด)");

    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 1)]
    [InlineData(4, 2)]
    [InlineData(7, 3)]
    public void Print_PutsThreeInvoicesOnEachPage(int invoiceCount, int expectedPages)
    {
        // เครื่องที่ไม่ได้ติดตั้งเครื่องพิมพ์จะสร้าง print preview ไม่ได้
        if (PrinterSettings.InstalledPrinters.Count == 0)
        {
            return;
        }

        var invoices = Enumerable.Range(1, invoiceCount).Select(CreateInvoice).ToList();
        var controller = new PreviewPrintController();
        using var document = new MonthlyInvoicePrintDocument(invoices) { PrintController = controller };

        document.Print();

        var pages = controller.GetPreviewPageInfo();
        Assert.Equal(expectedPages, pages.Length);
        foreach (var page in pages)
        {
            page.Image.Dispose();
        }
    }

    [Fact]
    public void PrintButtons_SendAllBillsOrOnlySelectedBillsInGridOrder()
    {
        RunInSta(() =>
        {
            // Arrange
            var roomRepo = new InMemoryRoomRepository();
            var billRepo = new InMemoryBillRepository();
            var createRoomService = new CreateRoomService(roomRepo);
            var createBillService = new CreateMonthlyBillService(billRepo);
            var period = BillingPeriod.Create($"{DateTime.Now.Year}-10");
            var meter = MeterReading.Create(100m);

            foreach (var roomNumber in new[] { "101", "102", "103" })
            {
                var room = createRoomService.Create(roomNumber, 3000m, true);
                createBillService.Create(new CreateMonthlyBillRequest(
                    room.Id, period, 3000m, meter, meter, meter, meter, 19m, 8m));
            }

            var settingsRepo = new InMemoryInvoiceSettingsRepository();
            IReadOnlyList<MonthlyInvoice>? printed = null;
            using var form = new Form();
            var tab = new MonthlyBillTabControl(
                createBillService,
                roomRepo,
                billRepository: billRepo,
                invoiceSettingsRepository: settingsRepo,
                showInvoicePreview: document => printed = document.Invoices)
            {
                Dock = DockStyle.Fill
            };
            form.Controls.Add(tab);
            form.Show();

            var periodSelector = (BillingPeriodSelector)tab.Controls.Find("BillingPeriodSelector", true).First();
            periodSelector.SetBillingPeriod(period);
            var grid = (DataGridView)tab.Controls.Find("BillGrid", true).First();
            var printAll = (Button)tab.Controls.Find("PrintAllInvoicesButton", true).First();
            var printSelected = (Button)tab.Controls.Find("PrintSelectedInvoicesButton", true).First();
            var messageLabel = (Label)tab.Controls.Find("BillMessageLabel", true).First();

            // Act & Assert: ยังไม่กรอกข้อมูลหอพัก ต้องไม่พิมพ์
            printAll.PerformClick();
            Assert.Null(printed);
            Assert.Contains("กรุณากรอกชื่อและที่อยู่หอพัก", messageLabel.Text);

            settingsRepo.Save(RentFlowApp.Domain.Billing.InvoiceSettings.Create("หอทดสอบ", "ที่อยู่ทดสอบ", null));

            // Act & Assert: ทั้งรอบ
            printAll.PerformClick();
            Assert.Equal(["101", "102", "103"], printed!.Select(invoice => invoice.RoomNumber));

            // Act & Assert: เฉพาะที่เลือก โดยเลือกแถวล่างก่อนแถวบน
            grid.ClearSelection();
            grid.Rows[2].Selected = true;
            grid.Rows[0].Selected = true;
            printSelected.PerformClick();
            Assert.Equal(["101", "103"], printed!.Select(invoice => invoice.RoomNumber));
        });
    }

    [Fact]
    public void PrintSelected_WhenNothingIsSelected_ShowsThaiMessage()
    {
        RunInSta(() =>
        {
            var roomRepo = new InMemoryRoomRepository();
            bool previewShown = false;
            using var form = new Form();
            var tab = new MonthlyBillTabControl(
                null,
                roomRepo,
                showInvoicePreview: _ => previewShown = true);
            form.Controls.Add(tab);
            form.Show();

            ((Button)tab.Controls.Find("PrintSelectedInvoicesButton", true).First()).PerformClick();

            var messageLabel = (Label)tab.Controls.Find("BillMessageLabel", true).First();
            Assert.False(previewShown);
            Assert.Contains("ไม่มีบิลสำหรับพิมพ์", messageLabel.Text);
        });
    }

    [Fact]
    public void SettingsTab_SavesPropertyDetailsUsedOnInvoices()
    {
        RunInSta(() =>
        {
            var roomRepo = new InMemoryRoomRepository();
            var settingsRepo = new InMemoryInvoiceSettingsRepository();
            using var form = new RoomForm(
                new CreateRoomService(roomRepo),
                roomRepo,
                invoiceSettingsRepository: settingsRepo);
            form.Show();
            var tabControl = form.Controls.OfType<TabControl>().First();
            var settingsTab = tabControl.TabPages[5];
            tabControl.SelectedTab = settingsTab;

            var nameBox = (TextBox)settingsTab.Controls.Find("InvoicePropertyNameTextBox", true).First();
            var addressBox = (TextBox)settingsTab.Controls.Find("InvoiceAddressTextBox", true).First();
            var bankBox = (TextBox)settingsTab.Controls.Find("InvoiceBankAccountTextBox", true).First();
            var saveButton = (Button)settingsTab.Controls.Find("SaveInvoiceSettingsButton", true).First();
            var messageLabel = (Label)settingsTab.Controls.Find("SettingsMessageLabel", true).First();

            Assert.Equal(settingsRepo.Get().PropertyName, nameBox.Text);

            nameBox.Text = "  ";
            saveButton.PerformClick();
            Assert.Equal("กรุณาระบุชื่อหอพัก", messageLabel.Text);

            nameBox.Text = "หอทดสอบ";
            addressBox.Text = "ที่อยู่ทดสอบ";
            bankBox.Text = "ธนาคารทดสอบ";
            saveButton.PerformClick();

            var saved = settingsRepo.Get();
            Assert.Equal("หอทดสอบ", saved.PropertyName);
            Assert.Equal("ที่อยู่ทดสอบ", saved.Address);
            Assert.Equal("ธนาคารทดสอบ", saved.BankAccountDetails);
            Assert.Equal(System.Drawing.Color.DarkGreen, messageLabel.ForeColor);
        });
    }
}
