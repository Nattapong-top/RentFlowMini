using ClosedXML.Excel;
using RentFlowApp.Application.Reports;
using RentFlowApp.Domain;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Infrastructure.Reports;

namespace RentFlowApp.Tests.Infrastructure.Reports;

public sealed class MonthlyReportExcelExporterTests : IDisposable
{
    private readonly string _filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xlsx");

    public void Dispose() => File.Delete(_filePath);

    private static MonthlyReport CreateReport()
    {
        var room = new Room(RoomId.New(), new RoomNumber("101"));
        var tenant = new Tenant(TenantId.New(), "สมมติ", "ทดสอบ", roomId: room.Id);
        var bill = Bill.Create(
            room.Id,
            BillingPeriod.Create("2026-10"),
            new BillingCalculationResult(
                Money.Create(3000m),
                Money.Create(100.50m),
                Money.Create(200m),
                Money.Create(50m),
                Money.Create(0m),
                Money.Create(60m),
                Money.Create(3410.50m)),
            tenant: BillTenantSnapshot.From(tenant));

        return MonthlyReportBuilder.Build(
            [room], [tenant], [bill], BillingPeriod.Create("2026-10"), new DateOnly(2026, 11, 8));
    }

    private static int FindRow(IXLWorksheet sheet, string label) =>
        sheet.Column(1).CellsUsed().First(cell => cell.GetString() == label).Address.RowNumber;

    [Fact]
    public void Export_WritesSummarySheetWithRealNumbers()
    {
        MonthlyReportExcelExporter.Export(CreateReport(), _filePath);

        using var workbook = new XLWorkbook(_filePath);
        var sheet = workbook.Worksheet("สรุป");
        Assert.Equal("รายงานประจำเดือน ตุลาคม 2569", sheet.Cell(1, 1).GetString());
        Assert.Equal("ข้อมูล ณ วันที่ 08/11/2569", sheet.Cell(2, 1).GetString());
        Assert.Equal("ห้องพัก (ปัจจุบัน): มีผู้เช่า 1/1 ว่าง 0 ห้อง", sheet.Cell(3, 1).GetString());

        var billed = sheet.Row(FindRow(sheet, "ยอดเรียกเก็บ"));
        Assert.Equal(1, billed.Cell(2).GetValue<int>());
        Assert.Equal(XLDataType.Number, billed.Cell(3).DataType);
        Assert.Equal(3410.50m, billed.Cell(3).GetValue<decimal>());
        Assert.Equal(100.50m, sheet.Row(FindRow(sheet, "ค่าน้ำ")).Cell(3).GetValue<decimal>());
        Assert.Equal(0m, sheet.Row(FindRow(sheet, "เงินสด")).Cell(3).GetValue<decimal>());
        Assert.Equal(
            3410.50m,
            sheet.Row(FindRow(sheet, "ค้างชำระ (เกินกำหนด 1 บิล)")).Cell(3).GetValue<decimal>());
    }

    [Fact]
    public void Export_WritesOutstandingSheetWithTenantAndTotal()
    {
        MonthlyReportExcelExporter.Export(CreateReport(), _filePath);

        using var workbook = new XLWorkbook(_filePath);
        var sheet = workbook.Worksheet("ค้างชำระ");
        Assert.Equal("รายการค้างชำระทุกเดือน ณ วันที่ 08/11/2569", sheet.Cell(1, 1).GetString());
        Assert.Equal("ห้อง", sheet.Cell(3, 1).GetString());
        Assert.Equal("ยอดค้าง", sheet.Cell(3, 6).GetString());
        Assert.Equal("101", sheet.Cell(4, 1).GetString());
        Assert.Equal("สมมติ ทดสอบ", sheet.Cell(4, 2).GetString());
        Assert.Equal("2569-10", sheet.Cell(4, 3).GetString());
        Assert.Equal("05/11/2569", sheet.Cell(4, 4).GetString());
        Assert.Equal("3 วัน", sheet.Cell(4, 5).GetString());
        Assert.Equal(3410.50m, sheet.Cell(4, 6).GetValue<decimal>());
        Assert.Equal("รวม 1 บิล", sheet.Cell(5, 1).GetString());
        Assert.Equal(3410.50m, sheet.Cell(5, 6).GetValue<decimal>());
    }

    [Fact]
    public void Export_WhenFileIsOpenElsewhere_ThrowsIOException()
    {
        using var lockedFile = new FileStream(_filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);

        Assert.ThrowsAny<IOException>(() => MonthlyReportExcelExporter.Export(CreateReport(), _filePath));
    }
}
