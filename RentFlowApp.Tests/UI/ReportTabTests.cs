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
using static RentFlowApp.Tests.UI.InputTestHelper;

namespace RentFlowApp.Tests.UI;

public class ReportTabTests
{
    // ตัวเลือกปีมีแค่รอบปีปัจจุบัน จึงต้องอิงรอบบิลกับวันนี้ ไม่ให้เทสต์หมดอายุ
    private static readonly BillingPeriod CurrentPeriod = BillingPeriod.Create(DateTime.Now.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture));
    private static readonly BillingPeriod PreviousPeriod = CurrentPeriod.GetPrevious();

    private sealed class ReportTab : IDisposable
    {
        private readonly Form _form = new();
        private readonly ReportTabControl _tab;
        private readonly InMemoryRoomRepository _rooms = new();
        private readonly InMemoryTenantRepository _tenants = new();
        private readonly InMemoryBillRepository _bills = new();

        public ReportTab()
        {
            _tab = new ReportTabControl(
                _rooms,
                _tenants,
                _bills,
                () => Today,
                (suggestedFileName, _) =>
                {
                    SuggestedFileName = suggestedFileName;
                    return SavePath;
                },
                (report, path) =>
                {
                    PdfExportReport = report;
                    PdfExportPath = path;
                });
            _form.Controls.Add(_tab);
            HiddenTestWindows.ShowHidden(_form);
            Selector.SetBillingPeriod(CurrentPeriod);
        }

        public DateOnly Today { get; set; } = CurrentPeriod.GetPaymentDueDate();

        public string? SavePath { get; set; }

        public string? SuggestedFileName { get; private set; }

        public RentFlowApp.Application.Reports.YearlyReport? PdfExportReport { get; private set; }

        public string? PdfExportPath { get; private set; }

        public BillingPeriodSelector Selector => Find<BillingPeriodSelector>("ReportPeriodSelector");

        public DataGridView Grid => Find<DataGridView>("ReportOutstandingGrid");

        public T Find<T>(string name) where T : Control => (T)_tab.Controls.Find(name, true)[0];

        public string Text(string labelName) => Find<Label>(labelName).Text;

        public IEnumerable<string?> Column(string columnName) =>
            Grid.Rows.Cast<DataGridViewRow>().Select(row => row.Cells[columnName].Value?.ToString());

        public RoomId AddRoom(string number) => new CreateRoomService(_rooms).Create(number, 3000m, true).Id;

        public Tenant AddTenant(string firstName, RoomId? roomId) =>
            new CreateTenantService(_tenants).Create(firstName, "ทดสอบ", null, roomId);

        public void AddBill(
            RoomId roomId,
            BillingPeriod period,
            decimal rent,
            Tenant? tenant = null,
            PaymentMethod? paidBy = null)
        {
            var meter = MeterReading.Create(100m);
            var bill = new CreateMonthlyBillService(_bills).Create(new CreateMonthlyBillRequest(
                roomId, period, rent, meter, meter, meter, meter, 19m, 8m,
                Tenant: tenant is null ? TestTenants.Snapshot() : BillTenantSnapshot.From(tenant)));

            if (paidBy is { } method)
            {
                bill.RecordPayment(Today, method);
                _bills.Update(bill);
            }
        }

        public void ClickExport(string buttonName)
        {
            // ปุ่มส่งออกแสดงเฉพาะมุมมองรายปี และ PerformClick ไม่ทำงานกับปุ่มที่ซ่อนอยู่
            Find<RadioButton>("ReportYearlyRadioButton").Checked = true;
            Find<Button>(buttonName).PerformClick();
        }

        public void Refresh() => _tab.RefreshReport();

        public void Dispose() => _form.Dispose();
    }

    [Fact]
    public void Refresh_ShowsSummaryCardsAndBreakdownsOfSelectedPeriod()
    {
        RunInSta(() =>
        {
            using var tab = new ReportTab();
            var room101 = tab.AddRoom("101");
            var room102 = tab.AddRoom("102");
            tab.AddRoom("103");
            tab.AddTenant("หนึ่ง", room101);
            tab.AddTenant("สอง", room102);
            tab.AddBill(room101, CurrentPeriod, 3000m, paidBy: PaymentMethod.Cash);
            tab.AddBill(room102, CurrentPeriod, 4000m);

            tab.Refresh();

            Assert.Equal("มีผู้เช่า 2/3", tab.Text("ReportRoomValueLabel"));
            Assert.Equal("ว่าง 1 ห้อง", tab.Text("ReportRoomDetailLabel"));
            Assert.Equal("2 บิล", tab.Text("ReportBilledDetailLabel"));
            Assert.Equal(7000m.ToString("N2"), tab.Text("ReportBilledValueLabel"));
            Assert.Equal(3000m.ToString("N2"), tab.Text("ReportReceivedValueLabel"));
            Assert.Equal("1 บิล", tab.Text("ReportReceivedDetailLabel"));
            Assert.Equal(4000m.ToString("N2"), tab.Text("ReportUnpaidValueLabel"));
            Assert.Equal(7000m.ToString("N2"), tab.Text("ReportRentLabel"));
            Assert.Equal(3000m.ToString("N2"), tab.Text("ReportCashLabel"));
            Assert.Equal(0m.ToString("N2"), tab.Text("ReportBankTransferLabel"));
        });
    }

    [Fact]
    public void ChangingPeriod_UpdatesCardsButKeepsOutstandingListAcrossMonths()
    {
        RunInSta(() =>
        {
            using var tab = new ReportTab();
            var room = tab.AddRoom("101");
            tab.AddBill(room, PreviousPeriod, 1000m);
            tab.AddBill(room, CurrentPeriod, 2000m);
            tab.Refresh();
            Assert.Equal(2000m.ToString("N2"), tab.Text("ReportBilledValueLabel"));

            tab.Selector.SetBillingPeriod(PreviousPeriod);

            Assert.Equal(1000m.ToString("N2"), tab.Text("ReportBilledValueLabel"));
            Assert.Equal(2, tab.Grid.Rows.Count);
            Assert.Equal(
                $"ค้างชำระทั้งหมด 2 บิล รวม {3000m:N2} บาท",
                tab.Text("ReportOutstandingSummaryLabel"));
        });
    }

    [Fact]
    public void OutstandingGrid_ListsOldestFirstWithTenantAndOverdueStatus()
    {
        RunInSta(() =>
        {
            using var tab = new ReportTab();
            var room = tab.AddRoom("101");
            var tenant = tab.AddTenant("ค้างจ่าย", room);
            tab.AddBill(room, CurrentPeriod, 2000m, tenant);
            tab.AddBill(room, PreviousPeriod, 1000m, tenant);

            // Today คือวันครบกำหนดของรอบปัจจุบัน รอบนี้จึงยังไม่เกินกำหนด แต่รอบก่อนเกินแล้ว
            tab.Refresh();

            int previousOverdueDays = tab.Today.DayNumber - PreviousPeriod.GetPaymentDueDate().DayNumber;
            Assert.Equal(
                [PreviousPeriod.ToBuddhistValue(), CurrentPeriod.ToBuddhistValue()],
                tab.Column("BillingPeriod"));
            Assert.Equal(["ค้างจ่าย ทดสอบ", "ค้างจ่าย ทดสอบ"], tab.Column("TenantName"));
            Assert.Equal([$"{previousOverdueDays} วัน", "ยังไม่ถึงกำหนด"], tab.Column("OverdueStatus"));
            Assert.Equal("1 บิล", tab.Text("ReportUnpaidDetailLabel"));
        });
    }

    [Fact]
    public void UnpaidCard_WhenPeriodHasOverdueBill_ShowsOverdueCount()
    {
        RunInSta(() =>
        {
            using var tab = new ReportTab();
            tab.AddBill(tab.AddRoom("101"), CurrentPeriod, 1000m);
            tab.AddBill(tab.AddRoom("102"), CurrentPeriod, 1000m);
            tab.Today = tab.Today.AddDays(1);

            tab.Refresh();

            Assert.Equal("2 บิล (เกินกำหนด 2)", tab.Text("ReportUnpaidDetailLabel"));
        });
    }

    [Fact]
    public void Refresh_WhenNoBills_ShowsEmptyMessages()
    {
        RunInSta(() =>
        {
            using var tab = new ReportTab();

            tab.Refresh();

            Assert.Equal("ยังไม่มีบิลในรอบนี้", tab.Text("ReportBilledDetailLabel"));
            Assert.Equal("ไม่มีบิลค้างชำระ", tab.Text("ReportOutstandingSummaryLabel"));
            Assert.Empty(tab.Column("RoomNumber"));
        });
    }

    [Fact]
    public void StartsInMonthlyView_AndSwitchingShowsYearlyControls()
    {
        RunInSta(() =>
        {
            using var tab = new ReportTab();
            var yearlyGrid = tab.Find<DataGridView>("ReportYearlyGrid");
            Assert.False(yearlyGrid.Visible);
            Assert.True(tab.Grid.Visible);

            tab.Find<RadioButton>("ReportYearlyRadioButton").Checked = true;

            Assert.True(yearlyGrid.Visible);
            Assert.True(tab.Find<Button>("ExportExcelButton").Visible);
            Assert.True(tab.Find<ComboBox>("ReportYearComboBox").Visible);
            Assert.False(tab.Grid.Visible);
            Assert.False(tab.Selector.Visible);
        });
    }

    [Fact]
    public void YearlyGrid_ShowsTwelveMonthsAndBoldTotal_AndChangesWithYear()
    {
        RunInSta(() =>
        {
            using var tab = new ReportTab();
            int year = tab.Today.Year;
            var room = tab.AddRoom("101");
            tab.AddBill(room, BillingPeriod.Create($"{year}-03"), 1000m);
            tab.AddBill(room, BillingPeriod.Create($"{year}-04"), 2000m, paidBy: PaymentMethod.Cash);
            tab.AddBill(room, BillingPeriod.Create($"{year - 1}-04"), 5000m);
            var yearBox = tab.Find<ComboBox>("ReportYearComboBox");
            var grid = tab.Find<DataGridView>("ReportYearlyGrid");
            yearBox.SelectedItem = year + 543;
            tab.Refresh();

            Assert.Equal(13, grid.Rows.Count);
            Assert.Equal("มีนาคม", grid.Rows[2].Cells[0].Value);
            Assert.Equal("1", grid.Rows[2].Cells[1].Value);
            Assert.Equal(1000m.ToString("N2"), grid.Rows[2].Cells[2].Value);
            var total = grid.Rows[12];
            Assert.Equal("รวมทั้งปี", total.Cells[0].Value);
            Assert.Equal("2", total.Cells[1].Value);
            Assert.Equal(3000m.ToString("N2"), total.Cells[7].Value);
            Assert.Equal(2000m.ToString("N2"), total.Cells[8].Value);
            Assert.Equal(1000m.ToString("N2"), total.Cells[9].Value);
            Assert.True(total.DefaultCellStyle.Font!.Bold);

            yearBox.SelectedItem = year - 1 + 543;

            Assert.Equal(5000m.ToString("N2"), grid.Rows[12].Cells[7].Value);
        });
    }

    [Fact]
    public void YearlyGrid_ReusesTotalRowFont_WhenRefreshed()
    {
        RunInSta(() =>
        {
            using var tab = new ReportTab();
            int thisYear = tab.Today.Year + 543;
            var yearBox = tab.Find<ComboBox>("ReportYearComboBox");
            var grid = tab.Find<DataGridView>("ReportYearlyGrid");
            yearBox.SelectedItem = thisYear;
            tab.Refresh();
            var firstFont = grid.Rows[12].DefaultCellStyle.Font;

            yearBox.SelectedItem = thisYear - 1;

            Assert.Same(firstFont, grid.Rows[12].DefaultCellStyle.Font);
        });
    }

    [Fact]
    public void ExportExcel_WritesChosenFileAndShowsSavedPath()
    {
        RunInSta(() =>
        {
            string filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xlsx");
            try
            {
                using var tab = new ReportTab { SavePath = filePath };
                tab.AddBill(tab.AddRoom("101"), BillingPeriod.Create($"{tab.Today.Year}-03"), 1000m);

                tab.ClickExport("ExportExcelButton");

                Assert.Equal($"รายงานประจำปี-{tab.Today.Year + 543}.xlsx", tab.SuggestedFileName);
                Assert.True(File.Exists(filePath));
                Assert.Equal($"ส่งออกรายงานแล้ว: {filePath}", tab.Text("ReportYearlyMessageLabel"));
            }
            finally
            {
                File.Delete(filePath);
            }
        });
    }

    [Fact]
    public void ExportPdf_PassesSelectedYearAndChosenPathToExporter()
    {
        RunInSta(() =>
        {
            using var tab = new ReportTab { SavePath = "รายงาน.pdf" };
            tab.AddBill(tab.AddRoom("101"), BillingPeriod.Create($"{tab.Today.Year}-03"), 1000m);

            tab.ClickExport("ExportPdfButton");

            Assert.Equal("รายงาน.pdf", tab.PdfExportPath);
            Assert.Equal(tab.Today.Year, tab.PdfExportReport!.Year);
            Assert.Equal(1000m, tab.PdfExportReport.Total.Rent);
        });
    }

    [Fact]
    public void Export_WhenYearHasNoBills_ShowsThaiMessageAndDoesNotAskForFile()
    {
        RunInSta(() =>
        {
            using var tab = new ReportTab { SavePath = "ไม่ควรถูกใช้.xlsx" };

            tab.ClickExport("ExportExcelButton");

            Assert.Null(tab.SuggestedFileName);
            Assert.Equal(
                $"ไม่มีบิลในปี {tab.Today.Year + 543} จึงไม่มีข้อมูลให้ส่งออก",
                tab.Text("ReportYearlyMessageLabel"));
        });
    }

    [Fact]
    public void Export_WhenSaveDialogIsCancelled_DoesNothing()
    {
        RunInSta(() =>
        {
            using var tab = new ReportTab { SavePath = null };
            tab.AddBill(tab.AddRoom("101"), BillingPeriod.Create($"{tab.Today.Year}-03"), 1000m);

            tab.ClickExport("ExportPdfButton");

            Assert.Null(tab.PdfExportPath);
            Assert.Equal(string.Empty, tab.Text("ReportYearlyMessageLabel"));
        });
    }

    [Fact]
    public void ExportExcel_WhenFileIsOpen_ShowsThaiMessage()
    {
        RunInSta(() =>
        {
            string filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xlsx");
            try
            {
                using var lockedFile = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                using var tab = new ReportTab { SavePath = filePath };
                tab.AddBill(tab.AddRoom("101"), BillingPeriod.Create($"{tab.Today.Year}-03"), 1000m);

                tab.ClickExport("ExportExcelButton");

                Assert.StartsWith("บันทึกไฟล์ไม่สำเร็จ", tab.Text("ReportYearlyMessageLabel"));
            }
            finally
            {
                File.Delete(filePath);
            }
        });
    }

    [Fact]
    public void PrintDocument_FitsYearlyReportOnOneLandscapePage()
    {
        // เครื่องที่ไม่ได้ติดตั้งเครื่องพิมพ์จะสร้าง print preview ไม่ได้
        if (System.Drawing.Printing.PrinterSettings.InstalledPrinters.Count == 0)
        {
            return;
        }

        var report = RentFlowApp.Application.Reports.YearlyReportBuilder.Build([], 2026);
        var controller = new System.Drawing.Printing.PreviewPrintController();
        using var document = new YearlyReportPrintDocument(report) { PrintController = controller };

        document.Print();

        var page = Assert.Single(controller.GetPreviewPageInfo());
        Assert.True(document.DefaultPageSettings.Landscape);
        page.Image.Dispose();
    }

    [Fact]
    public void OutstandingGrid_HasBoldHeaderThatGrowsToFitText()
    {
        RunInSta(() =>
        {
            using var tab = new ReportTab();

            Assert.Equal(DataGridViewColumnHeadersHeightSizeMode.AutoSize, tab.Grid.ColumnHeadersHeightSizeMode);
            Assert.True(tab.Grid.ColumnHeadersDefaultCellStyle.Font!.Bold);
        });
    }

    [Fact]
    public void RoomForm_HasReportTabInFifthPosition_AndRefreshesWhenOpened()
    {
        RunInSta(() =>
        {
            var rooms = new InMemoryRoomRepository();
            var bills = new InMemoryBillRepository();
            using var form = new RoomForm(new CreateRoomService(rooms), rooms, billRepository: bills);
            HiddenTestWindows.ShowHidden(form);
            var tabControl = form.Controls.OfType<TabControl>().First();
            var reportPage = tabControl.TabPages[4];
            Assert.Equal("รายงาน", reportPage.Text);

            var room = new CreateRoomService(rooms).Create("101", 3000m, true);
            var meter = MeterReading.Create(100m);
            new CreateMonthlyBillService(bills).Create(new CreateMonthlyBillRequest(
                room.Id, CurrentPeriod, 3000m, meter, meter, meter, meter, 19m, 8m, Tenant: TestTenants.Snapshot()));

            tabControl.SelectedTab = reportPage;

            var billedLabel = (Label)reportPage.Controls.Find("ReportBilledValueLabel", true)[0];
            Assert.Equal(3000m.ToString("N2"), billedLabel.Text);
        });
    }
}
