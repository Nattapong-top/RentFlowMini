using ClosedXML.Excel;
using RentFlowApp.Application.Reports;

namespace RentFlowApp.Infrastructure.Reports;

public static class YearlyReportExcelExporter
{
    private const int HeaderRow = 3;
    private const int FirstAmountColumn = 3;
    private const string MoneyFormat = "#,##0.00";

    public static void Export(YearlyReport report, string filePath)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add($"ปี {report.BuddhistYear}");

        sheet.Cell(1, 1).Value = report.Title;
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 14;

        WriteHeader(sheet);

        int rowNumber = HeaderRow + 1;
        foreach (var row in report.AllRows)
        {
            WriteRow(sheet, rowNumber, row);
            rowNumber++;
        }

        int lastRow = rowNumber - 1;
        int lastColumn = FirstAmountColumn + YearlyReport.AmountHeaders.Count - 1;

        sheet.Row(lastRow).Style.Font.Bold = true;
        var table = sheet.Range(HeaderRow, 1, lastRow, lastColumn);
        table.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        table.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        sheet.Range(lastRow, 1, lastRow, lastColumn).Style.Fill.BackgroundColor = XLColor.LightGray;

        // กำหนดความกว้างเอง เพราะการวัดอัตโนมัติขึ้นกับฟอนต์ของแต่ละเครื่อง
        sheet.Column(1).Width = 14;
        sheet.Column(2).Width = 7;
        sheet.Columns(FirstAmountColumn, lastColumn).Width = 14;

        workbook.SaveAs(filePath);
    }

    private static void WriteHeader(IXLWorksheet sheet)
    {
        sheet.Cell(HeaderRow, 1).Value = YearlyReport.MonthHeader;
        sheet.Cell(HeaderRow, 2).Value = YearlyReport.BillCountHeader;
        for (int i = 0; i < YearlyReport.AmountHeaders.Count; i++)
        {
            sheet.Cell(HeaderRow, FirstAmountColumn + i).Value = YearlyReport.AmountHeaders[i];
        }

        sheet.Row(HeaderRow).Style.Font.Bold = true;
        sheet.Row(HeaderRow).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    private static void WriteRow(IXLWorksheet sheet, int rowNumber, YearlyReportRow row)
    {
        sheet.Cell(rowNumber, 1).Value = row.Label;
        sheet.Cell(rowNumber, 2).Value = row.BillCount;

        var amounts = row.Amounts;
        for (int i = 0; i < amounts.Count; i++)
        {
            var cell = sheet.Cell(rowNumber, FirstAmountColumn + i);
            // เก็บเป็นตัวเลขจริง เพื่อให้นำไปคำนวณต่อใน Excel ได้
            cell.Value = amounts[i];
            cell.Style.NumberFormat.Format = MoneyFormat;
        }
    }
}
