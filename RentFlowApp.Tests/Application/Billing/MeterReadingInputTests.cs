using System.Globalization;
using RentFlowApp.Application.Billing;
using RentFlowApp.Domain.Billing;

namespace RentFlowApp.Tests.Application.Billing;

public class MeterReadingInputTests
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

    [Theory]
    [InlineData("120", 120)]
    [InlineData(" 12.5 ", 12.5)]
    [InlineData("0", 0)]
    public void Parse_WithNumber_ReturnsMeterReading(string text, decimal expected)
    {
        WithThaiCulture(() => Assert.Equal(expected, MeterReadingInput.Parse(text, "มิเตอร์น้ำปัจจุบัน").Value));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Parse_WithBlankText_RejectsWithMeterName(string? text)
    {
        WithThaiCulture(() =>
        {
            var exception = Assert.Throws<InvalidMeterReadingException>(
                () => MeterReadingInput.Parse(text, "มิเตอร์น้ำปัจจุบัน"));

            Assert.Equal("ข้อมูลมิเตอร์น้ำปัจจุบันไม่ถูกต้อง: ต้องระบุค่ามิเตอร์", exception.Message);
        });
    }

    [Fact]
    public void Parse_WithText_RejectsAsInvalidNumber()
    {
        WithThaiCulture(() =>
        {
            var exception = Assert.Throws<InvalidMeterReadingException>(
                () => MeterReadingInput.Parse("abc", "มิเตอร์ไฟฟ้าก่อนหน้า"));

            Assert.Equal("ข้อมูลมิเตอร์ไฟฟ้าก่อนหน้าไม่ถูกต้อง: ต้องระบุเป็นตัวเลขที่ถูกต้อง", exception.Message);
        });
    }

    [Fact]
    public void Parse_WithNegativeNumber_RejectsLikeMeterReading()
    {
        WithThaiCulture(() =>
        {
            var exception = Assert.Throws<InvalidMeterReadingException>(
                () => MeterReadingInput.Parse("-1", "มิเตอร์น้ำปัจจุบัน"));

            Assert.Equal("ข้อมูลมิเตอร์ไม่ถูกต้อง: ค่ามิเตอร์ต้องไม่ติดลบ", exception.Message);
        });
    }
}
