using System.Globalization;
using RentFlowApp.Application.Rooms;

namespace RentFlowApp.Tests.Application.Rooms;

public class RoomRentInputTests
{
    private const string InvalidRentMessage = "กรุณาระบุค่าเช่ารายเดือนเป็นจำนวนเงินที่ถูกต้อง";

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
    [InlineData("3000", 3000)]
    [InlineData(" 3500.50 ", 3500.50)]
    [InlineData("0", 0)]
    // ตอนแก้ค่าเช่า หน้าจอใส่ค่าเดิมกลับในรูปแบบ N2 ซึ่งมีจุลภาคคั่นหลักพัน
    [InlineData("3,000.00", 3000)]
    public void TryParse_WithValidRent_ReturnsRentWithoutError(string text, decimal expected)
    {
        WithThaiCulture(() =>
        {
            var isValid = RoomRentInput.TryParse(text, out var monthlyRent, out var error);

            Assert.True(isValid);
            Assert.Equal(expected, monthlyRent);
            Assert.Null(error);
        });
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("abc")]
    [InlineData("-1")]
    public void TryParse_WithInvalidOrNegativeRent_ReturnsError(string? text)
    {
        WithThaiCulture(() =>
        {
            var isValid = RoomRentInput.TryParse(text, out _, out var error);

            Assert.False(isValid);
            Assert.Equal(InvalidRentMessage, error);
        });
    }
}
