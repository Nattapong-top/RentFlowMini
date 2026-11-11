using ClosedXML.Excel;
using RentFlowApp.Application.Reports;

namespace RentFlowApp.Infrastructure.Reports;

public static class MonthlyReportExcelExporter
{
    private const string MoneyFormat = "#,##0.00";

    public static void Export(MonthlyReport report, string filePath)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        using var workbook = new XLWorkbook();
        WriteSummarySheet(workbook.Worksheets.Add("สรุป"), report);
        WriteOutstandingSheet(workbook.Worksheets.Add("ค้างชำระ"), report);
        workbook.SaveAs(filePath);
    }

    private static void WriteSummarySheet(IXLWorksheet sheet, MonthlyReport report)
    {
        sheet.Cell(1, 1).Value = report.Title;
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 14;
        sheet.Cell(2, 1).Value = report.AsOfLabel;
        sheet.Cell(3, 1).Value = report.RoomSummary;

        int nextRow = WriteLines(sheet, 5, MonthlyReport.BillingLinesTitle, report.BillingLines);
        nextRow = WriteLines(sheet, nextRow + 1, MonthlyReport.ChargeLinesTitle, report.ChargeLines);
        WriteLines(sheet, nextRow + 1, MonthlyReport.PaymentLinesTitle, report.PaymentLines);

        // กำหนดความกว้างเอง เพราะการวัดอัตโนมัติขึ้นกับฟอนต์ของแต่ละเครื่อง
        sheet.Column(1).Width = 32;
        sheet.Column(2).Width = 8;
        sheet.Column(3).Width = 16;
    }

    private static int WriteLines(
        IXLWorksheet sheet,
        int startRow,
        string title,
        IReadOnlyList<MonthlyReportLine> lines)
    {
        sheet.Cell(startRow, 1).Value = title;
        sheet.Cell(startRow, 2).Value = "บิล";
        sheet.Cell(startRow, 3).Value = "ยอดเงิน";
        sheet.Row(startRow).Style.Font.Bold = true;

        int row = startRow + 1;
        foreach (var line in lines)
        {
            sheet.Cell(row, 1).Value = line.Label;
            if (line.BillCount is { } billCount)
            {
                sheet.Cell(row, 2).Value = billCount;
            }

            // เก็บเป็นตัวเลขจริง เพื่อให้นำไปคำนวณต่อใน Excel ได้
            sheet.Cell(row, 3).Value = line.Amount;
            sheet.Cell(row, 3).Style.NumberFormat.Format = MoneyFormat;
            row++;
        }

        ApplyTableBorders(sheet.Range(startRow, 1, row - 1, 3));
        return row;
    }

    private static void WriteOutstandingSheet(IXLWorksheet sheet, MonthlyReport report)
    {
        const int headerRow = 3;
        var headers = MonthlyReport.OutstandingHeaders;
        int columnCount = headers.Count;

        sheet.Cell(1, 1).Value = report.OutstandingTitle;
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 14;

        for (int i = 0; i < columnCount; i++)
        {
            sheet.Cell(headerRow, i + 1).Value = headers[i];
        }

        sheet.Row(headerRow).Style.Font.Bold = true;
        sheet.Row(headerRow).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        int row = headerRow + 1;
        foreach (var bill in report.OutstandingBills)
        {
            sheet.Cell(row, 1).Value = bill.RoomNumber;
            sheet.Cell(row, 2).Value = bill.TenantName;
            sheet.Cell(row, 3).Value = bill.BillingPeriod.ToBuddhistValue();
            sheet.Cell(row, 4).Value = ThaiCalendar.FormatDate(bill.DueDate);
            sheet.Cell(row, 5).Value = bill.OverdueStatus;
            sheet.Cell(row, columnCount).Value = bill.Amount;
            row++;
        }

        sheet.Cell(row, 1).Value = $"รวม {report.OutstandingBills.Count} บิล";
        sheet.Cell(row, columnCount).Value = report.OutstandingBillsTotal;
        sheet.Row(row).Style.Font.Bold = true;
        sheet.Range(row, 1, row, columnCount).Style.Fill.BackgroundColor = XLColor.LightGray;
        sheet.Range(headerRow + 1, columnCount, row, columnCount).Style.NumberFormat.Format = MoneyFormat;
        ApplyTableBorders(sheet.Range(headerRow, 1, row, columnCount));

        sheet.Column(1).Width = 12;
        sheet.Column(2).Width = 32;
        sheet.Column(3).Width = 12;
        sheet.Column(4).Width = 14;
        sheet.Column(5).Width = 16;
        sheet.Column(6).Width = 14;
    }

    private static void ApplyTableBorders(IXLRange range)
    {
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }
}
