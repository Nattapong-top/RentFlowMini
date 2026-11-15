using System.Diagnostics.CodeAnalysis;

namespace RentFlowApp.Application.Billing;

public static class BillingRatesInput
{
    // ตรวจตามลำดับช่องบนหน้าจอ และบอกเฉพาะช่องแรกที่ผิด ผู้ใช้จะได้แก้ทีละช่องจากบนลงล่าง
    public static bool TryParse(
        string? waterRate,
        string? electricityRate,
        string? motorcycleParkingRate,
        string? carParkingRate,
        string? cableTvRate,
        [NotNullWhen(true)] out BillingRateValues? rates,
        [NotNullWhen(false)] out string? error)
    {
        rates = null;

        if (!TryParseRate(waterRate, "กรุณาระบุค่าน้ำประปาเป็นตัวเลขที่ไม่ติดลบ", out var water, out error) ||
            !TryParseRate(electricityRate, "กรุณาระบุค่าไฟฟ้าเป็นตัวเลขที่ไม่ติดลบ", out var electricity, out error) ||
            !TryParseRate(motorcycleParkingRate, "กรุณาระบุค่าจอดรถมอเตอร์ไซค์เป็นตัวเลขที่ไม่ติดลบ", out var motorcycleParking, out error) ||
            !TryParseRate(carParkingRate, "กรุณาระบุค่าจอดรถยนต์เป็นตัวเลขที่ไม่ติดลบ", out var carParking, out error) ||
            !TryParseRate(cableTvRate, "กรุณาระบุค่า Cable TV เป็นตัวเลขที่ไม่ติดลบ", out var cableTv, out error))
        {
            return false;
        }

        rates = new BillingRateValues(water, electricity, motorcycleParking, carParking, cableTv);
        return true;
    }

    private static bool TryParseRate(
        string? text,
        string invalidMessage,
        out decimal rate,
        [NotNullWhen(false)] out string? error)
    {
        if (!decimal.TryParse(text?.Trim(), out rate) || rate < 0)
        {
            error = invalidMessage;
            return false;
        }

        error = null;
        return true;
    }
}
