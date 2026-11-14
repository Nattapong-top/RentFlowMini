using System.Globalization;
using RentFlowApp.Application.Billing;

namespace RentFlowApp.Tests.Application.Billing;

public class BillingRatesInputTests
{
    // การแปลงตัวเลขขึ้นกับภาษาของเครื่อง จึงกำหนดเป็นภาษาไทยที่แอปใช้จริง ผลจะได้ไม่เปลี่ยนตามเครื่องที่รันเทสต์
    private static void WithThaiCulture(Action action)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("th-TH");
        try
        {
            action();
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void TryParse_WithValidRates_ReturnsAllRatesInOrder()
    {
        WithThaiCulture(() =>
        {
            var isValid = BillingRatesInput.TryParse(" 19 ", "8.5", "100", "0", "60", out var rates, out var error);

            Assert.True(isValid);
            Assert.Null(error);
            Assert.Equal(new BillingRateValues(19m, 8.5m, 100m, 0m, 60m), rates);
        });
    }

    [Theory]
    [InlineData(0, "abc", "กรุณาระบุค่าน้ำประปาเป็นตัวเลขที่ไม่ติดลบ")]
    [InlineData(1, "-1", "กรุณาระบุค่าไฟฟ้าเป็นตัวเลขที่ไม่ติดลบ")]
    [InlineData(2, "", "กรุณาระบุค่าจอดรถมอเตอร์ไซค์เป็นตัวเลขที่ไม่ติดลบ")]
    [InlineData(3, "   ", "กรุณาระบุค่าจอดรถยนต์เป็นตัวเลขที่ไม่ติดลบ")]
    [InlineData(4, null, "กรุณาระบุค่า Cable TV เป็นตัวเลขที่ไม่ติดลบ")]
    public void TryParse_WithOneInvalidRate_ReturnsMessageOfThatRate(int invalidIndex, string? invalidText, string expected)
    {
        var texts = new[] { "19", "8", "100", "500", "60" };
        texts[invalidIndex] = invalidText!;

        WithThaiCulture(() =>
        {
            var isValid = BillingRatesInput.TryParse(
                texts[0], texts[1], texts[2], texts[3], texts[4], out var rates, out var error);

            Assert.False(isValid);
            Assert.Null(rates);
            Assert.Equal(expected, error);
        });
    }

    [Fact]
    public void TryParse_WithSeveralInvalidRates_ReturnsMessageOfFirstInvalidRate()
    {
        WithThaiCulture(() =>
        {
            var isValid = BillingRatesInput.TryParse("19", "abc", "100", "-5", "x", out _, out var error);

            Assert.False(isValid);
            Assert.Equal("กรุณาระบุค่าไฟฟ้าเป็นตัวเลขที่ไม่ติดลบ", error);
        });
    }
}
