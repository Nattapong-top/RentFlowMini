namespace RentFlowApp.Forms;

// สีที่หน้าจอกำหนดเองนอกเหนือจาก SystemColors ซึ่งไม่เปลี่ยนตามโหมดมืดให้เอง
// งานพิมพ์ไม่ใช้คลาสนี้ เพราะใบแจ้งหนี้และรายงานต้องเป็นพื้นขาวเสมอ
public sealed record ThemePalette(
    Color SuccessText,
    Color ErrorText,
    Color InfoText,
    Color OverdueText,
    Color MutedText,
    Color AlternatingRowBack,
    Color TotalRowBack,
    Color SummaryBack,
    Color SummaryHeaderBack,
    Color SummaryHeadingText,
    Color SummaryTenantText,
    Color SummaryPeriodText,
    Color SummaryDateText,
    Color SummaryDivider,
    Color SummaryAmountText,
    Color SummaryDetailText,
    Color SummaryTotalBack,
    Color SummaryTotalTitleText,
    Color SummaryTotalText)
{
    public static ThemePalette Light { get; } = new(
        SuccessText: Color.DarkGreen,
        ErrorText: Color.DarkRed,
        InfoText: Color.DarkBlue,
        OverdueText: Color.Firebrick,
        MutedText: Color.Gray,
        AlternatingRowBack: Color.FromArgb(245, 248, 252),
        TotalRowBack: Color.Gainsboro,
        SummaryBack: Color.White,
        SummaryHeaderBack: Color.FromArgb(246, 248, 251),
        SummaryHeadingText: Color.FromArgb(24, 76, 120),
        SummaryTenantText: Color.FromArgb(50, 50, 50),
        SummaryPeriodText: Color.FromArgb(70, 95, 125),
        SummaryDateText: Color.DimGray,
        SummaryDivider: Color.FromArgb(226, 232, 240),
        SummaryAmountText: Color.FromArgb(30, 41, 59),
        SummaryDetailText: Color.FromArgb(100, 116, 139),
        SummaryTotalBack: Color.FromArgb(238, 246, 255),
        SummaryTotalTitleText: Color.FromArgb(30, 58, 138),
        SummaryTotalText: Color.FromArgb(29, 78, 216));

    public static ThemePalette Dark { get; } = new(
        SuccessText: Color.FromArgb(110, 205, 125),
        ErrorText: Color.FromArgb(255, 125, 125),
        InfoText: Color.FromArgb(125, 175, 255),
        OverdueText: Color.FromArgb(255, 115, 115),
        MutedText: Color.FromArgb(160, 160, 160),
        AlternatingRowBack: Color.FromArgb(45, 48, 54),
        TotalRowBack: Color.FromArgb(70, 70, 70),
        SummaryBack: Color.FromArgb(32, 32, 32),
        SummaryHeaderBack: Color.FromArgb(42, 46, 54),
        SummaryHeadingText: Color.FromArgb(130, 185, 235),
        SummaryTenantText: Color.FromArgb(225, 225, 225),
        SummaryPeriodText: Color.FromArgb(155, 180, 210),
        SummaryDateText: Color.FromArgb(175, 175, 175),
        SummaryDivider: Color.FromArgb(70, 76, 86),
        SummaryAmountText: Color.FromArgb(226, 232, 240),
        SummaryDetailText: Color.FromArgb(160, 172, 190),
        SummaryTotalBack: Color.FromArgb(28, 44, 70),
        SummaryTotalTitleText: Color.FromArgb(160, 200, 255),
        SummaryTotalText: Color.FromArgb(110, 175, 255));

    // แอปตั้งโหมดสีครั้งเดียวตอนเริ่ม ใน Program.cs จึงอ่านค่าตอนสร้างหน้าจอได้เลย
    public static ThemePalette Current => System.Windows.Forms.Application.IsDarkModeEnabled ? Dark : Light;
}
