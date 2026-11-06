using System.Drawing.Printing;
using ClosedXML.Excel;
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

public class MonthlyReportExportTests
{
    // ตัวเลือกปีมีแค่รอบปีปัจจุบัน จึงต้องอิงรอบบิลกับวันนี้ ไม่ให้เทสต์หมดอายุ
    private static readonly BillingPeriod CurrentPeriod = BillingPeriod.Create(
        DateTime.Now.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture));

    private sealed class ReportTab : IDisposable
    {
        private readonly Form _form = new();
        private readonly ReportTabControl _tab;
        private readonly InMemoryRoomRepository _rooms = new();
        private readonly InMemoryBillRepository _bills = new();

        public ReportTab()
        {
            _tab = new ReportTabControl(
                _rooms,
                new InMemoryTenantRepository(),
                _bills,
                () => CurrentPeriod.GetPaymentDueDate(),
                (suggestedFileName, _) =>
                {
                    SuggestedFileName = suggestedFileName;
                    return SavePath;
                },
                (_, path) => YearlyPdfPath = path,
                (report, path) =>
                {
                    MonthlyPdfReport = report;
                    MonthlyPdfPath = path;
                });
            _form.Controls.Add(_tab);
            _form.Show();
            Find<BillingPeriodSelector>("ReportPeriodSelector").SetBillingPeriod(CurrentPeriod);
        }

        public string? SavePath { get; set; }

        public string? SuggestedFileName { get; private set; }

        public string? YearlyPdfPath { get; private set; }

        public MonthlyReport? MonthlyPdfReport { get; private set; }

        public string? MonthlyPdfPath { get; private set; }

        public Label Message => Find<Label>("ReportMonthlyMessageLabel");

        public T Find<T>(string name) where T : Control => (T)_tab.Controls.Find(name, true)[0];

        public void Click(string buttonName) => Find<Button>(buttonName).PerformClick();

        public void SetFormWidth(int width) => _form.Width = width;

        public void AddBill(BillingPeriod period, decimal rent)
        {
            var room = new CreateRoomService(_rooms).Create($"{_rooms.GetAll().Count + 101}", rent, true);
            var meter = MeterReading.Create(100m);
            new CreateMonthlyBillService(_bills).Create(new CreateMonthlyBillRequest(
                room.Id, period, rent, meter, meter, meter, meter, 19m, 8m));
        }

        public void Dispose() => _form.Dispose();
    }

    [Fact]
    public void ExportButtons_AreAvailableInMonthlyView()
    {
        RunInSta(() =>
        {
            using var tab = new ReportTab();

            Assert.True(tab.Find<Button>("ExportExcelButton").Visible);
            Assert.True(tab.Find<Button>("ExportPdfButton").Visible);
        });
    }

    [Fact]
    public void ExportExcel_InMonthlyView_WritesMonthlyWorkbook()
    {
        RunInSta(() =>
        {
            string filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xlsx");
            try
            {
                using var tab = new ReportTab { SavePath = filePath };
                tab.AddBill(CurrentPeriod, 1000m);

                tab.Click("ExportExcelButton");

                Assert.Equal($"รายงานประจำเดือน-{CurrentPeriod.ToBuddhistValue()}.xlsx", tab.SuggestedFileName);
                Assert.Equal($"ส่งออกรายงานแล้ว: {filePath}", tab.Message.Text);
                Assert.True(tab.Message.Visible);
                using var workbook = new XLWorkbook(filePath);
                Assert.Equal(["สรุป", "ค้างชำระ"], workbook.Worksheets.Select(sheet => sheet.Name));
            }
            finally
            {
                File.Delete(filePath);
            }
        });
    }

    [Fact]
    public void ExportPdf_InMonthlyView_PassesSelectedPeriodToMonthlyExporter()
    {
        RunInSta(() =>
        {
            using var tab = new ReportTab { SavePath = "รายงาน.pdf" };
            tab.AddBill(CurrentPeriod, 1000m);

            tab.Click("ExportPdfButton");

            Assert.Equal("รายงาน.pdf", tab.MonthlyPdfPath);
            Assert.Null(tab.YearlyPdfPath);
            Assert.Equal(CurrentPeriod, tab.MonthlyPdfReport!.Period);
        });
    }

    [Fact]
    public void Export_WhenPeriodHasNoBillsButOtherMonthsAreOutstanding_StillAllowed()
    {
        RunInSta(() =>
        {
            using var tab = new ReportTab { SavePath = "รายงาน.pdf" };
            tab.AddBill(CurrentPeriod.GetPrevious(), 1000m);

            tab.Click("ExportPdfButton");

            Assert.Equal("รายงาน.pdf", tab.MonthlyPdfPath);
            Assert.Single(tab.MonthlyPdfReport!.OutstandingBills);
        });
    }

    [Fact]
    public void Export_WhenNothingToExport_ShowsThaiMessageAndDoesNotAskForFile()
    {
        RunInSta(() =>
        {
            using var tab = new ReportTab { SavePath = "ไม่ควรถูกใช้.pdf" };

            tab.Click("ExportPdfButton");

            Assert.Null(tab.SuggestedFileName);
            Assert.Equal(
                $"ไม่มีบิลรอบ {ThaiCalendar.FormatPeriod(CurrentPeriod)} และไม่มีบิลค้างชำระ จึงไม่มีข้อมูลให้ส่งออก",
                tab.Message.Text);
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
                tab.AddBill(CurrentPeriod, 1000m);

                tab.Click("ExportExcelButton");

                Assert.StartsWith("บันทึกไฟล์ไม่สำเร็จ", tab.Message.Text);
            }
            finally
            {
                File.Delete(filePath);
            }
        });
    }

    [Fact]
    public void ChangingPeriod_ClearsPreviousExportMessage()
    {
        RunInSta(() =>
        {
            using var tab = new ReportTab { SavePath = "รายงาน.pdf" };
            tab.AddBill(CurrentPeriod, 1000m);
            tab.Click("ExportPdfButton");
            Assert.True(tab.Message.Visible);

            tab.Find<BillingPeriodSelector>("ReportPeriodSelector").SetBillingPeriod(CurrentPeriod.GetPrevious());

            Assert.False(tab.Message.Visible);
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Header_ShowsAllTextWithoutOverlap_AtMinimumFormWidth(bool isYearly)
    {
        RunInSta(() =>
        {
            const int roomFormMinimumWidth = 750;
            using var tab = new ReportTab();
            tab.SetFormWidth(roomFormMinimumWidth);
            var yearlyRadio = tab.Find<RadioButton>("ReportYearlyRadioButton");
            yearlyRadio.Checked = isYearly;

            var header = yearlyRadio.Parent!;
            var visibleControls = header.Controls.Cast<Control>().Where(control => control.Visible).ToList();

            foreach (var control in visibleControls.Where(control => control is Label))
            {
                // วัดด้วย TextRenderer เพราะแอปจริงวาดตัวอักษรด้วย GDI ซึ่งกว้างกว่าค่าเริ่มต้นตอนรันเทสต์
                int textWidth = TextRenderer.MeasureText(control.Text, control.Font).Width;
                Assert.True(control.Width >= textWidth, $"\"{control.Text}\" กว้าง {control.Width} แต่ตัวอักษรต้องการ {textWidth}");
            }

            for (int i = 0; i < visibleControls.Count; i++)
            {
                Assert.True(visibleControls[i].Right <= header.ClientSize.Width, $"{visibleControls[i].Name} ล้นขอบขวา");
                for (int j = i + 1; j < visibleControls.Count; j++)
                {
                    Assert.False(
                        visibleControls[i].Bounds.IntersectsWith(visibleControls[j].Bounds),
                        $"{visibleControls[i].Name} ทับ {visibleControls[j].Name}");
                }
            }
        });
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(5, 1)]
    [InlineData(40, 2)]
    [InlineData(100, 3)]
    public void PrintDocument_SplitsOutstandingRowsAcrossPages(int outstandingCount, int expectedPages)
    {
        // เครื่องที่ไม่ได้ติดตั้งเครื่องพิมพ์จะสร้าง print preview ไม่ได้
        if (PrinterSettings.InstalledPrinters.Count == 0)
        {
            return;
        }

        var report = CreateReportWithOutstanding(outstandingCount);
        var controller = new PreviewPrintController();
        using var document = new MonthlyReportPrintDocument(report) { PrintController = controller };

        document.Print();

        var pages = controller.GetPreviewPageInfo();
        Assert.Equal(expectedPages, pages.Length);
        foreach (var page in pages)
        {
            page.Image.Dispose();
        }
    }

    private static MonthlyReport CreateReportWithOutstanding(int count)
    {
        var bills = Enumerable.Range(0, count)
            .Select(_ => RentFlowApp.Domain.Billing.Bill.Create(
                RoomId.New(),
                BillingPeriod.Create("2026-10"),
                new RentFlowApp.Domain.Billing.BillingCalculationResult(
                    Money.Create(1000m),
                    Money.Create(0m),
                    Money.Create(0m),
                    Money.Create(0m),
                    Money.Create(0m),
                    Money.Create(0m),
                    Money.Create(1000m))))
            .ToList();

        return MonthlyReportBuilder.Build(
            [], [], bills, BillingPeriod.Create("2026-10"), new DateOnly(2026, 11, 8));
    }
}
