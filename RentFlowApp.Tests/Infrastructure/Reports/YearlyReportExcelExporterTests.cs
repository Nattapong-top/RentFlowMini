using ClosedXML.Excel;
using RentFlowApp.Application.Reports;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Infrastructure.Reports;

namespace RentFlowApp.Tests.Infrastructure.Reports;

public sealed class YearlyReportExcelExporterTests : IDisposable
{
    private const int HeaderRow = 3;
    private const int JanuaryRow = HeaderRow + 1;
    private const int TotalRow = JanuaryRow + 12;

    private readonly string _filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xlsx");

    public void Dispose() => File.Delete(_filePath);

    private static YearlyReport CreateReport()
    {
        var bill = Bill.Create(
            RoomId.New(),
            BillingPeriod.Create("2026-01"),
            new BillingCalculationResult(
                Money.Create(3000m),
                Money.Create(100.50m),
                Money.Create(200m),
                Money.Create(50m),
                Money.Create(0m),
                Money.Create(60m),
                Money.Create(3410.50m)));

        return YearlyReportBuilder.Build([bill], 2026);
    }

    [Fact]
    public void Export_WritesTitleHeadersAndAllRows()
    {
        YearlyReportExcelExporter.Export(CreateReport(), _filePath);

        using var workbook = new XLWorkbook(_filePath);
        var sheet = workbook.Worksheet("ปี 2569");
        Assert.Equal("สรุปรายงานประจำปี 2569", sheet.Cell(1, 1).GetString());
        Assert.Equal("เดือน", sheet.Cell(HeaderRow, 1).GetString());
        Assert.Equal("ค้างชำระ", sheet.Cell(HeaderRow, 10).GetString());
        Assert.Equal("มกราคม", sheet.Cell(JanuaryRow, 1).GetString());
        Assert.Equal("รวมทั้งปี", sheet.Cell(TotalRow, 1).GetString());
    }

    [Fact]
    public void Export_StoresAmountsAsRealNumbers()
    {
        YearlyReportExcelExporter.Export(CreateReport(), _filePath);

        using var workbook = new XLWorkbook(_filePath);
        var sheet = workbook.Worksheet(1);
        var water = sheet.Cell(JanuaryRow, 4);
        Assert.Equal(XLDataType.Number, water.DataType);
        Assert.Equal(100.50m, water.GetValue<decimal>());
        Assert.Equal(1, sheet.Cell(TotalRow, 2).GetValue<int>());
        Assert.Equal(3410.50m, sheet.Cell(TotalRow, 8).GetValue<decimal>());
        Assert.Equal("#,##0.00", water.Style.NumberFormat.Format);
    }

    [Fact]
    public void Export_WhenFileIsOpenElsewhere_ThrowsIOException()
    {
        using var lockedFile = new FileStream(_filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);

        Assert.ThrowsAny<IOException>(() => YearlyReportExcelExporter.Export(CreateReport(), _filePath));
    }
}
