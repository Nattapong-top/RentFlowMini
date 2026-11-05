using System.Drawing.Printing;
using RentFlowApp.Application.Reports;

namespace RentFlowApp.Forms;

public sealed class MonthlyReportPrintDocument : PrintDocument
{
    private const string FontFamilyName = "Tahoma";

    // หน่วยของ PrintDocument คือ 1/100 นิ้ว
    private const int PageMargin = 40;
    private const int RowHeight = 26;
    private const int SectionTitleHeight = 28;
    private const int SectionGap = 14;
    private const int CellPadding = 5;
    private const int LineTableGap = 20;
    private const float LineLabelWeight = 0.68f;

    // เรียงตรงกับ MonthlyReport.OutstandingHeaders
    private static readonly float[] OutstandingColumnWeights = [0.09f, 0.35f, 0.11f, 0.15f, 0.15f, 0.15f];

    private static readonly StringAlignment[] OutstandingAlignments =
    [
        StringAlignment.Center,
        StringAlignment.Near,
        StringAlignment.Center,
        StringAlignment.Center,
        StringAlignment.Center,
        StringAlignment.Far
    ];

    private readonly MonthlyReport _report;
    private int _nextOutstandingRow;

    public MonthlyReportPrintDocument(MonthlyReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        _report = report;
        DocumentName = report.Title;
        DefaultPageSettings.Landscape = false;
        DefaultPageSettings.Margins = new Margins(PageMargin, PageMargin, PageMargin, PageMargin);
    }

    public static void ExportPdf(MonthlyReport report, string filePath)
    {
        using var document = new MonthlyReportPrintDocument(report);
        ReportPdfExporter.Export(document, filePath);
    }

    protected override void OnBeginPrint(PrintEventArgs e)
    {
        base.OnBeginPrint(e);
        _nextOutstandingRow = 0;
        DefaultPageSettings.PaperSize = ReportPdfExporter.FindA4PaperSize(PrinterSettings);
    }

    protected override void OnPrintPage(PrintPageEventArgs e)
    {
        base.OnPrintPage(e);

        var graphics = e.Graphics!;
        var area = e.MarginBounds;
        using var titleFont = new Font(FontFamilyName, 15f, FontStyle.Bold);
        using var normalFont = new Font(FontFamilyName, 9f);
        using var boldFont = new Font(FontFamilyName, 9f, FontStyle.Bold);

        int top = area.Top;
        if (_nextOutstandingRow == 0)
        {
            top = DrawSummary(graphics, area, top, titleFont, normalFont, boldFont);
        }

        graphics.DrawString(_report.OutstandingTitle, boldFont, Brushes.Black, area.Left, top);
        top += SectionTitleHeight;

        if (_report.OutstandingBills.Count == 0)
        {
            graphics.DrawString("ไม่มีบิลค้างชำระ", normalFont, Brushes.Black, area.Left, top);
            return;
        }

        e.HasMorePages = !DrawOutstandingRows(graphics, area, top, normalFont, boldFont);
    }

    private int DrawSummary(
        Graphics graphics,
        Rectangle area,
        int top,
        Font titleFont,
        Font normalFont,
        Font boldFont)
    {
        graphics.DrawString(_report.Title, titleFont, Brushes.Black, area.Left, top);
        top += 40;
        graphics.DrawString(_report.AsOfLabel, normalFont, Brushes.Black, area.Left, top);
        top += 22;
        graphics.DrawString(_report.RoomSummary, normalFont, Brushes.Black, area.Left, top);
        top += 22 + SectionGap;

        int tableWidth = (area.Width - LineTableGap) / 2;
        int rightLeft = area.Left + tableWidth + LineTableGap;

        int billingBottom = DrawLines(
            graphics, area.Left, top, tableWidth, MonthlyReport.BillingLinesTitle, _report.BillingLines, normalFont, boldFont);
        int paymentBottom = DrawLines(
            graphics, rightLeft, top, tableWidth, MonthlyReport.PaymentLinesTitle, _report.PaymentLines, normalFont, boldFont);
        top = Math.Max(billingBottom, paymentBottom) + SectionGap;

        return DrawLines(
            graphics, area.Left, top, tableWidth, MonthlyReport.ChargeLinesTitle, _report.ChargeLines, normalFont, boldFont)
            + SectionGap;
    }

    private static int DrawLines(
        Graphics graphics,
        int left,
        int top,
        int width,
        string title,
        IReadOnlyList<MonthlyReportLine> lines,
        Font normalFont,
        Font boldFont)
    {
        graphics.DrawString(title, boldFont, Brushes.Black, left, top);
        top += SectionTitleHeight;

        float labelWidth = width * LineLabelWeight;
        foreach (var line in lines)
        {
            string label = line.BillCount is { } billCount ? $"{line.Label}: {billCount} บิล" : line.Label;
            DrawCell(graphics, label, new RectangleF(left, top, labelWidth, RowHeight), normalFont, StringAlignment.Near, null);
            DrawCell(
                graphics,
                line.Amount.ToString("N2"),
                new RectangleF(left + labelWidth, top, width - labelWidth, RowHeight),
                normalFont,
                StringAlignment.Far,
                null);
            top += RowHeight;
        }

        return top;
    }

    // คืนค่า true เมื่อพิมพ์แถวครบแล้ว
    private bool DrawOutstandingRows(Graphics graphics, Rectangle area, int top, Font normalFont, Font boldFont)
    {
        float[] widths = OutstandingColumnWeights.Select(weight => weight * area.Width).ToArray();
        var bills = _report.OutstandingBills;

        DrawTableRow(graphics, [.. MonthlyReport.OutstandingHeaders], widths, area.Left, top, boldFont, Brushes.LightGray, centerAll: true);
        top += RowHeight;

        while (_nextOutstandingRow < bills.Count)
        {
            // แถวสุดท้ายต้องมีที่เหลือให้แถวรวมด้วย จะได้อยู่หน้าเดียวกัน
            bool isLastRow = _nextOutstandingRow == bills.Count - 1;
            int neededHeight = isLastRow ? RowHeight * 2 : RowHeight;
            if (top + neededHeight > area.Bottom)
            {
                return false;
            }

            var bill = bills[_nextOutstandingRow];
            string[] cells =
            [
                bill.RoomNumber,
                bill.TenantName,
                bill.BillingPeriod.ToBuddhistValue(),
                ThaiCalendar.FormatDate(bill.DueDate),
                bill.OverdueStatus,
                bill.Amount.ToString("N2")
            ];
            DrawTableRow(graphics, cells, widths, area.Left, top, normalFont, null, centerAll: false);
            top += RowHeight;
            _nextOutstandingRow++;
        }

        string[] totalCells =
        [
            string.Empty,
            $"รวม {bills.Count} บิล",
            string.Empty,
            string.Empty,
            string.Empty,
            _report.OutstandingBillsTotal.ToString("N2")
        ];
        DrawTableRow(graphics, totalCells, widths, area.Left, top, boldFont, Brushes.Gainsboro, centerAll: false);
        return true;
    }

    private static void DrawTableRow(
        Graphics graphics,
        string[] cells,
        float[] widths,
        int left,
        int top,
        Font font,
        Brush? background,
        bool centerAll)
    {
        float x = left;
        for (int i = 0; i < cells.Length; i++)
        {
            var alignment = centerAll ? StringAlignment.Center : OutstandingAlignments[i];
            DrawCell(graphics, cells[i], new RectangleF(x, top, widths[i], RowHeight), font, alignment, background);
            x += widths[i];
        }
    }

    private static void DrawCell(
        Graphics graphics,
        string text,
        RectangleF bounds,
        Font font,
        StringAlignment alignment,
        Brush? background)
    {
        if (background is not null)
        {
            graphics.FillRectangle(background, bounds);
        }

        graphics.DrawRectangle(Pens.Black, bounds.X, bounds.Y, bounds.Width, bounds.Height);

        using var format = new StringFormat
        {
            Alignment = alignment,
            LineAlignment = StringAlignment.Center,
            // ชื่อยาวเกินช่องให้ตัดท้ายด้วย ... แทนการขึ้นบรรทัดที่ถูกช่องบัง
            FormatFlags = StringFormatFlags.NoWrap,
            Trimming = StringTrimming.EllipsisCharacter
        };
        graphics.DrawString(text, font, Brushes.Black, RectangleF.Inflate(bounds, -CellPadding, 0), format);
    }
}
