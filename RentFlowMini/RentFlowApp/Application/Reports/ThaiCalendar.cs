using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Application.Reports;

public static class ThaiCalendar
{
    public static readonly IReadOnlyList<string> MonthNames =
    [
        "มกราคม", "กุมภาพันธ์", "มีนาคม", "เมษายน", "พฤษภาคม", "มิถุนายน",
        "กรกฎาคม", "สิงหาคม", "กันยายน", "ตุลาคม", "พฤศจิกายน", "ธันวาคม"
    ];

    public static string FormatDate(DateOnly date) =>
        $"{date.Day:D2}/{date.Month:D2}/{date.Year + 543}";

    public static string FormatPeriod(BillingPeriod period) =>
        $"{MonthNames[period.Month - 1]} {period.Year + 543}";
}
