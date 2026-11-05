using System.Drawing.Printing;

namespace RentFlowApp.Forms;

internal static class ReportPdfExporter
{
    private const string PdfPrinterName = "Microsoft Print to PDF";

    // หน่วยของ PrintDocument คือ 1/100 นิ้ว
    private const int A4Width = 827;
    private const int A4Height = 1169;

    public static void Export(PrintDocument document, string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

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

    public static PaperSize FindA4PaperSize(PrinterSettings printerSettings)
    {
        foreach (PaperSize paperSize in printerSettings.PaperSizes)
        {
            if (paperSize.Kind == PaperKind.A4)
            {
                return paperSize;
            }
        }

        return new PaperSize("A4", A4Width, A4Height);
    }
}
