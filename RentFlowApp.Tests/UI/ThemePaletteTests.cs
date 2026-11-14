using RentFlowApp.Forms;

namespace RentFlowApp.Tests.UI;

public class ThemePaletteTests
{
    // เกณฑ์ขั้นต่ำของ WCAG สำหรับตัวอักษรขนาดปกติ
    private const double MinimumContrast = 4.5;

    public static TheoryData<string, Color, Color> DarkTextOnBackground()
    {
        var dark = ThemePalette.Dark;
        // ข้อความทั่วไปวางบนพื้นของหน้าจอ ใช้สีแถวสลับซึ่งสว่างกว่าพื้นปกติเป็นกรณีที่แย่ที่สุด
        var screenBack = dark.AlternatingRowBack;
        return new TheoryData<string, Color, Color>
        {
            { nameof(dark.SuccessText), dark.SuccessText, screenBack },
            { nameof(dark.ErrorText), dark.ErrorText, screenBack },
            { nameof(dark.InfoText), dark.InfoText, screenBack },
            { nameof(dark.OverdueText), dark.OverdueText, screenBack },
            { nameof(dark.MutedText), dark.MutedText, screenBack },
            { nameof(dark.SummaryHeadingText), dark.SummaryHeadingText, dark.SummaryHeaderBack },
            { nameof(dark.SummaryTenantText), dark.SummaryTenantText, dark.SummaryHeaderBack },
            { nameof(dark.SummaryPeriodText), dark.SummaryPeriodText, dark.SummaryHeaderBack },
            { nameof(dark.SummaryDateText), dark.SummaryDateText, dark.SummaryHeaderBack },
            { nameof(dark.SummaryAmountText), dark.SummaryAmountText, dark.SummaryBack },
            { nameof(dark.SummaryDetailText), dark.SummaryDetailText, dark.SummaryBack },
            { nameof(dark.SummaryTotalTitleText), dark.SummaryTotalTitleText, dark.SummaryTotalBack },
            { nameof(dark.SummaryTotalText), dark.SummaryTotalText, dark.SummaryTotalBack },
        };
    }

    [Theory]
    [MemberData(nameof(DarkTextOnBackground))]
    public void DarkPalette_TextIsReadableOnItsBackground(string name, Color text, Color background)
    {
        var contrast = ContrastRatio(text, background);

        Assert.True(contrast >= MinimumContrast, $"{name} contrast {contrast:F2} is below {MinimumContrast}");
    }

    [Fact]
    public void Current_InTests_UsesLightPalette()
    {
        // เทสต์ไม่ได้เรียก SetColorMode หน้าจอในเทสต์เดิมจึงยังเป็นสีโหมดสว่างเหมือนก่อนมีโหมดมืด
        Assert.Same(ThemePalette.Light, ThemePalette.Current);
    }

    private static double ContrastRatio(Color a, Color b)
    {
        var lighter = Math.Max(RelativeLuminance(a), RelativeLuminance(b));
        var darker = Math.Min(RelativeLuminance(a), RelativeLuminance(b));
        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double RelativeLuminance(Color color)
    {
        static double Channel(byte value)
        {
            var c = value / 255.0;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Channel(color.R) + 0.7152 * Channel(color.G) + 0.0722 * Channel(color.B);
    }
}
