using System.Drawing.Printing;
using RentFlowApp.Application.Billing;

namespace RentFlowApp.Forms;

public sealed class MonthlyInvoicePrintDocument : PrintDocument
{
    public const int InvoicesPerPage = 3;

    // หน่วยของ PrintDocument คือ 1/100 นิ้ว
    private const int PageMargin = 30;
    private const int SlotPadding = 14;
    private const int InfoBoxWidth = 235;
    private const int InfoBoxLabelWidth = 120;
    private const int InfoRowHeight = 21;
    private const int LineRowHeight = 21;
    private const int SummaryRowHeight = 52;
    private const string FontFamilyName = "Tahoma";

    private int _nextInvoiceIndex;

    public MonthlyInvoicePrintDocument(IReadOnlyList<MonthlyInvoice> invoices)
    {
        ArgumentNullException.ThrowIfNull(invoices);

        Invoices = invoices;
        DocumentName = "ใบแจ้งหนี้";
        DefaultPageSettings.Landscape = false;
        DefaultPageSettings.Margins = new Margins(PageMargin, PageMargin, PageMargin, PageMargin);
    }

    public IReadOnlyList<MonthlyInvoice> Invoices { get; }

    protected override void OnBeginPrint(PrintEventArgs e)
    {
        base.OnBeginPrint(e);
        _nextInvoiceIndex = 0;
        DefaultPageSettings.PaperSize = ReportPdfExporter.FindA4PaperSize(PrinterSettings);
    }

    protected override void OnPrintPage(PrintPageEventArgs e)
    {
        base.OnPrintPage(e);

        var graphics = e.Graphics!;
        var printArea = e.MarginBounds;
        int slotHeight = printArea.Height / InvoicesPerPage;
        int printedOnPage = 0;

        while (printedOnPage < InvoicesPerPage && _nextInvoiceIndex < Invoices.Count)
        {
            var slot = new Rectangle(
                printArea.Left,
                printArea.Top + printedOnPage * slotHeight,
                printArea.Width,
                slotHeight);

            DrawInvoice(graphics, Invoices[_nextInvoiceIndex], slot);

            if (printedOnPage < InvoicesPerPage - 1)
            {
                DrawCutLine(graphics, slot);
            }

            printedOnPage++;
            _nextInvoiceIndex++;
        }

        e.HasMorePages = _nextInvoiceIndex < Invoices.Count;
    }

    private static void DrawInvoice(Graphics graphics, MonthlyInvoice invoice, Rectangle slot)
    {
        using var titleFont = new Font(FontFamilyName, 14f, FontStyle.Bold);
        using var normalFont = new Font(FontFamilyName, 9f);
        using var noteBoldFont = new Font(FontFamilyName, 9f, FontStyle.Bold);
        using var boldFont = new Font(FontFamilyName, 10f, FontStyle.Bold);
        using var rowLinePen = new Pen(Color.DimGray) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
        using var totalFont = new Font(FontFamilyName, 13f, FontStyle.Bold);

        int left = slot.Left + SlotPadding;
        int right = slot.Right - SlotPadding;
        int top = slot.Top + SlotPadding;
        int infoBoxLeft = right - InfoBoxWidth;

        DrawText(graphics, invoice.PropertyName, titleFont, new Rectangle(left, top, infoBoxLeft - left - 8, 30));
        DrawText(graphics, invoice.Address, normalFont, new Rectangle(left, top + 30, infoBoxLeft - left - 8, 36));

        DrawCell(graphics, "ใบแจ้งหนี้", boldFont, new Rectangle(infoBoxLeft, top, InfoBoxWidth, InfoRowHeight), StringAlignment.Center);

        int infoRowTop = top + InfoRowHeight;
        DrawInfoRow(graphics, "เลขที่/No.", invoice.InvoiceNumber, normalFont, boldFont, infoBoxLeft, infoRowTop);
        infoRowTop += InfoRowHeight;
        DrawInfoRow(graphics, "เลขที่ห้องชุด/Room", invoice.RoomNumber, normalFont, boldFont, infoBoxLeft, infoRowTop);
        infoRowTop += InfoRowHeight;
        DrawInfoRow(graphics, "ประจำเดือน/Month", invoice.BillingMonth, normalFont, boldFont, infoBoxLeft, infoRowTop);
        infoRowTop += InfoRowHeight;
        DrawInfoRow(graphics, "วันที่/Date", invoice.IssueDate, normalFont, boldFont, infoBoxLeft, infoRowTop);

        var nameRow = new Rectangle(left, infoRowTop, infoBoxLeft - left, InfoRowHeight);
        graphics.DrawRectangle(Pens.Black, nameRow);
        DrawText(graphics, "ชื่อ/Name", normalFont, new Rectangle(left + 4, infoRowTop, 70, InfoRowHeight), StringAlignment.Near, StringAlignment.Center);
        DrawText(graphics, invoice.TenantName, boldFont, new Rectangle(left + 78, infoRowTop, nameRow.Width - 82, InfoRowHeight), StringAlignment.Near, StringAlignment.Center);

        int tableWidth = right - left;
        int amountWidth = 95;
        int unitPriceWidth = 85;
        int unitsWidth = 115;
        int descriptionWidth = tableWidth - unitsWidth - unitPriceWidth - amountWidth;
        int unitsLeft = left + descriptionWidth;
        int unitPriceLeft = unitsLeft + unitsWidth;
        int amountLeft = unitPriceLeft + unitPriceWidth;

        int headerTop = infoRowTop + InfoRowHeight;
        DrawCell(graphics, "รายละเอียด/Description", normalFont, new Rectangle(left, headerTop, descriptionWidth, InfoRowHeight), StringAlignment.Center);
        DrawCell(graphics, "จำนวนหน่วย/Units", normalFont, new Rectangle(unitsLeft, headerTop, unitsWidth, InfoRowHeight), StringAlignment.Center);
        DrawCell(graphics, "ราคาต่อหน่วย", normalFont, new Rectangle(unitPriceLeft, headerTop, unitPriceWidth, InfoRowHeight), StringAlignment.Center);
        DrawCell(graphics, "จำนวนเงิน", normalFont, new Rectangle(amountLeft, headerTop, amountWidth, InfoRowHeight), StringAlignment.Center);

        int linesTop = headerTop + InfoRowHeight;
        int linesHeight = invoice.Lines.Count * LineRowHeight;
        graphics.DrawRectangle(Pens.Black, left, linesTop, descriptionWidth, linesHeight);
        graphics.DrawRectangle(Pens.Black, unitsLeft, linesTop, unitsWidth, linesHeight);
        graphics.DrawRectangle(Pens.Black, unitPriceLeft, linesTop, unitPriceWidth, linesHeight);
        graphics.DrawRectangle(Pens.Black, amountLeft, linesTop, amountWidth, linesHeight);

        const int meterRangeWidth = 125;
        for (int i = 0; i < invoice.Lines.Count; i++)
        {
            var line = invoice.Lines[i];
            int rowTop = linesTop + i * LineRowHeight;

            if (i > 0)
            {
                graphics.DrawLine(rowLinePen, left, rowTop, right, rowTop);
            }

            DrawText(graphics, line.Description, normalFont, new Rectangle(left + 4, rowTop, descriptionWidth - meterRangeWidth - 4, LineRowHeight), StringAlignment.Near, StringAlignment.Center);
            DrawText(graphics, line.MeterRange, boldFont, new Rectangle(unitsLeft - meterRangeWidth, rowTop, meterRangeWidth - 4, LineRowHeight), StringAlignment.Near, StringAlignment.Center);
            DrawText(graphics, line.Units, boldFont, new Rectangle(unitsLeft, rowTop, unitsWidth - 6, LineRowHeight), StringAlignment.Far, StringAlignment.Center);
            DrawText(graphics, line.UnitPrice, boldFont, new Rectangle(unitPriceLeft, rowTop, unitPriceWidth - 6, LineRowHeight), StringAlignment.Far, StringAlignment.Center);
            DrawText(graphics, line.Amount, boldFont, new Rectangle(amountLeft, rowTop, amountWidth - 6, LineRowHeight), StringAlignment.Far, StringAlignment.Center);
        }

        int summaryTop = linesTop + linesHeight;
        graphics.DrawRectangle(Pens.Black, left, summaryTop, descriptionWidth, SummaryRowHeight);
        DrawText(graphics, $"* หมายเหตุ  {invoice.PaymentChannels}", normalFont, new Rectangle(left + 4, summaryTop + 3, descriptionWidth - 8, 16));
        // เผื่อความสูง 2 บรรทัด เพื่อไม่ให้เลขบัญชีที่ยาวถูกตัดเป็น "..."
        DrawText(graphics, invoice.BankAccountDetails, noteBoldFont, new Rectangle(left + 4, summaryTop + 19, descriptionWidth - 8, SummaryRowHeight - 21));

        int totalLabelWidth = unitsWidth + unitPriceWidth;
        graphics.DrawRectangle(Pens.Black, unitsLeft, summaryTop, totalLabelWidth, SummaryRowHeight);
        DrawText(graphics, "รวมเงินทั้งสิ้น/Grand Total", normalFont, new Rectangle(unitsLeft + 4, summaryTop, totalLabelWidth - 8, SummaryRowHeight), StringAlignment.Near, StringAlignment.Center);
        graphics.DrawRectangle(Pens.Black, amountLeft, summaryTop, amountWidth, SummaryRowHeight);
        DrawText(graphics, invoice.GrandTotal, totalFont, new Rectangle(amountLeft, summaryTop, amountWidth - 4, SummaryRowHeight), StringAlignment.Far, StringAlignment.Center);

        const string dueDateLabel = "กำหนดชำระเงินภายในวันที่";
        int dueDateTop = summaryTop + SummaryRowHeight + 3;
        int dueDateLabelWidth = (int)Math.Ceiling(graphics.MeasureString(dueDateLabel, normalFont).Width);
        DrawText(graphics, dueDateLabel, normalFont, new Rectangle(left, dueDateTop, dueDateLabelWidth, 20));
        DrawText(graphics, invoice.DueDate, boldFont, new Rectangle(left + dueDateLabelWidth, dueDateTop, tableWidth - dueDateLabelWidth, 20));
        DrawText(graphics, invoice.PaymentStatus, boldFont, new Rectangle(left, dueDateTop, tableWidth, 20), StringAlignment.Far);
    }

    private static void DrawInfoRow(Graphics graphics, string label, string value, Font labelFont, Font valueFont, int left, int top)
    {
        DrawCell(graphics, label, labelFont, new Rectangle(left, top, InfoBoxLabelWidth, InfoRowHeight), StringAlignment.Near);
        DrawCell(graphics, value, valueFont, new Rectangle(left + InfoBoxLabelWidth, top, InfoBoxWidth - InfoBoxLabelWidth, InfoRowHeight), StringAlignment.Center);
    }

    private static void DrawCell(Graphics graphics, string text, Font font, Rectangle bounds, StringAlignment alignment)
    {
        graphics.DrawRectangle(Pens.Black, bounds);
        var textBounds = Rectangle.Inflate(bounds, -3, 0);
        DrawText(graphics, text, font, textBounds, alignment, StringAlignment.Center);
    }

    private static void DrawText(
        Graphics graphics,
        string text,
        Font font,
        Rectangle bounds,
        StringAlignment alignment = StringAlignment.Near,
        StringAlignment lineAlignment = StringAlignment.Near)
    {
        using var format = new StringFormat
        {
            Alignment = alignment,
            LineAlignment = lineAlignment,
            Trimming = StringTrimming.EllipsisCharacter
        };

        graphics.DrawString(text, font, Brushes.Black, bounds, format);
    }

    private static void DrawCutLine(Graphics graphics, Rectangle slot)
    {
        using var pen = new Pen(Color.Gray) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
        graphics.DrawLine(pen, slot.Left, slot.Bottom, slot.Right, slot.Bottom);
    }
}
