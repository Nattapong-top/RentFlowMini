using System.Drawing.Printing;
using RentFlowApp.Application.Reports;

namespace RentFlowApp.Forms;

public sealed class YearlyReportPrintDocument : PrintDocument
{
    private const string PdfPrinterName = "Microsoft Print to PDF";
    private const string FontFamilyName = "Tahoma";

    // หน่วยของ PrintDocument คือ 1/100 นิ้ว
    private const int A4Width = 827;
    private const int A4Height = 1169;
    private const int PageMargin = 40;
    private const int TitleHeight = 50;
    private const int RowHeight = 36;
    private const int CellPadding = 5;
    private const float MonthColumnWeight = 1.2f;
    private const float BillCountColumnWeight = 0.6f;

    private readonly YearlyReport _report;

    public YearlyReportPrintDocument(YearlyReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        _report = report;
        DocumentName = report.Title;
        DefaultPageSettings.Landscape = true;
        DefaultPageSettings.Margins = new Margins(PageMargin, PageMargin, PageMargin, PageMargin);
    }

    public static void ExportPdf(YearlyReport report, string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        using var document = new YearlyReportPrintDocument(report);
        document.PrinterSettings.PrinterName = PdfPrinterName;
        if (!document.PrinterSettings.IsValid)
        {
            throw new InvalidPrinterException(document.PrinterSettings);
        }

        // ไดรเวอร์เขียนไฟล์ภายหลังและไม่แจ้งกลับถ้าไฟล์เดิมเปิดค้าง จึงต้องตรวจก่อนสั่งพิมพ์
        if (File.Exists(filePath))
        {
            using var lockCheck = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }

        document.PrinterSettings.PrintToFile = true;
        document.PrinterSettings.PrintFileName = filePath;
        document.PrintController = new StandardPrintController();
        document.Print();
    }

    protected override void OnBeginPrint(PrintEventArgs e)
    {
        base.OnBeginPrint(e);
        DefaultPageSettings.PaperSize = FindA4PaperSize();
    }

    protected override void OnPrintPage(PrintPageEventArgs e)
    {
        base.OnPrintPage(e);

        var graphics = e.Graphics!;
        var area = e.MarginBounds;
        using var titleFont = new Font(FontFamilyName, 15f, FontStyle.Bold);
        using var normalFont = new Font(FontFamilyName, 9f);
        using var boldFont = new Font(FontFamilyName, 9f, FontStyle.Bold);

        graphics.DrawString(_report.Title, titleFont, Brushes.Black, area.Left, area.Top);

        float[] columnWidths = GetColumnWidths(area.Width);
        int top = area.Top + TitleHeight;

        string[] headers =
            [YearlyReport.MonthHeader, YearlyReport.BillCountHeader, .. YearlyReport.AmountHeaders];
        DrawRow(graphics, headers, columnWidths, area.Left, top, boldFont, StringAlignment.Center, Brushes.LightGray);
        top += RowHeight;

        foreach (var row in _report.Months)
        {
            DrawRow(graphics, GetCells(row), columnWidths, area.Left, top, normalFont, StringAlignment.Far, null);
            top += RowHeight;
        }

        DrawRow(graphics, GetCells(_report.Total), columnWidths, area.Left, top, boldFont, StringAlignment.Far, Brushes.Gainsboro);
    }

    private static float[] GetColumnWidths(int totalWidth)
    {
        int amountCount = YearlyReport.AmountHeaders.Count;
        float unit = totalWidth / (MonthColumnWeight + BillCountColumnWeight + amountCount);
        float[] widths = new float[amountCount + 2];
        widths[0] = unit * MonthColumnWeight;
        widths[1] = unit * BillCountColumnWeight;
        Array.Fill(widths, unit, 2, amountCount);
        return widths;
    }

    private static string[] GetCells(YearlyReportRow row) =>
        [row.Label, row.BillCount.ToString("N0"), .. row.Amounts.Select(amount => amount.ToString("N2"))];

    private static void DrawRow(
        Graphics graphics,
        string[] cells,
        float[] columnWidths,
        int left,
        int top,
        Font font,
        StringAlignment alignment,
        Brush? background)
    {
        float x = left;
        for (int i = 0; i < cells.Length; i++)
        {
            var bounds = new RectangleF(x, top, columnWidths[i], RowHeight);
            if (background is not null)
            {
                graphics.FillRectangle(background, bounds);
            }

            graphics.DrawRectangle(Pens.Black, bounds.X, bounds.Y, bounds.Width, bounds.Height);

            using var format = new StringFormat
            {
                // คอลัมน์เดือนชิดซ้าย ยกเว้นแถวหัวตารางที่จัดกลางทุกช่อง
                Alignment = i == 0 && alignment == StringAlignment.Far ? StringAlignment.Near : alignment,
                LineAlignment = StringAlignment.Center
            };
            var textBounds = RectangleF.Inflate(bounds, -CellPadding, 0);
            graphics.DrawString(cells[i], font, Brushes.Black, textBounds, format);

            x += columnWidths[i];
        }
    }

    private PaperSize FindA4PaperSize()
    {
        foreach (PaperSize paperSize in PrinterSettings.PaperSizes)
        {
            if (paperSize.Kind == PaperKind.A4)
            {
                return paperSize;
            }
        }

        return new PaperSize("A4", A4Width, A4Height);
    }
}
