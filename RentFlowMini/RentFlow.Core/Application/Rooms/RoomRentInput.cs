using System.Diagnostics.CodeAnalysis;

namespace RentFlowApp.Application.Rooms;

public static class RoomRentInput
{
    public const string InvalidRentMessage = "กรุณาระบุค่าเช่ารายเดือนเป็นจำนวนเงินที่ถูกต้อง";

    // ค่าเช่า 0 บาทยอมรับได้ เช่น ห้องที่ให้อยู่ฟรี แต่ติดลบไม่ได้
    public static bool TryParse(
        string? text,
        out decimal monthlyRent,
        [NotNullWhen(false)] out string? error)
    {
        if (!decimal.TryParse(text?.Trim(), out monthlyRent) || monthlyRent < 0)
        {
            monthlyRent = 0m;
            error = InvalidRentMessage;
            return false;
        }

        error = null;
        return true;
    }
}
