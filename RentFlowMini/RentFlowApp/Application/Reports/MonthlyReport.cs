using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Application.Reports;

public sealed record OutstandingBillRow(
    string RoomNumber,
    string TenantName,
    BillingPeriod BillingPeriod,
    DateOnly DueDate,
    int OverdueDays,
    decimal Amount)
{
    public string OverdueStatus => OverdueDays > 0 ? $"{OverdueDays} วัน" : "ยังไม่ถึงกำหนด";
}

public sealed record MonthlyReportLine(string Label, decimal Amount, int? BillCount = null);

public sealed record MonthlyReport(
    BillingPeriod Period,
    DateOnly AsOf,
    int TotalRooms,
    int OccupiedRooms,
    int BilledCount,
    decimal BilledTotal,
    int ReceivedCount,
    decimal ReceivedTotal,
    int UnpaidCount,
    decimal UnpaidTotal,
    int OverdueCount,
    decimal Rent,
    decimal Water,
    decimal Electricity,
    decimal Parking,
    decimal CableTv,
    decimal ReceivedByCash,
    decimal ReceivedByBankTransfer,
    IReadOnlyList<OutstandingBillRow> OutstandingBills)
{
    public const string BillingLinesTitle = "สรุปยอดของรอบบิล";
    public const string ChargeLinesTitle = "ยอดเรียกเก็บแยกประเภท";
    public const string PaymentLinesTitle = "รับชำระแยกช่องทาง";

    // เรียงตรงกับคอลัมน์ห้อง ผู้เช่า รอบบิล ครบกำหนด เกินกำหนด ยอดค้าง
    public static readonly IReadOnlyList<string> OutstandingHeaders =
        ["ห้อง", "ผู้เช่า", "รอบบิล", "ครบกำหนด", "เกินกำหนด", "ยอดค้าง"];

    public int VacantRooms => TotalRooms - OccupiedRooms;

    public decimal OutstandingBillsTotal => OutstandingBills.Sum(bill => bill.Amount);

    public bool HasBills => BilledCount > 0 || OutstandingBills.Count > 0;

    public string Title => $"รายงานประจำเดือน {ThaiCalendar.FormatPeriod(Period)}";

    public string AsOfLabel => $"ข้อมูล ณ วันที่ {ThaiCalendar.FormatDate(AsOf)}";

    public string RoomSummary => $"ห้องพัก (ปัจจุบัน): มีผู้เช่า {OccupiedRooms}/{TotalRooms} ว่าง {VacantRooms} ห้อง";

    // จำนวนวันเกินกำหนดเปลี่ยนทุกวัน หัวข้อจึงต้องบอกวันที่ของข้อมูล
    public string OutstandingTitle => $"รายการค้างชำระทุกเดือน ณ วันที่ {ThaiCalendar.FormatDate(AsOf)}";

    public IReadOnlyList<MonthlyReportLine> BillingLines =>
    [
        new("ยอดเรียกเก็บ", BilledTotal, BilledCount),
        new("รับแล้ว", ReceivedTotal, ReceivedCount),
        new(OverdueCount > 0 ? $"ค้างชำระ (เกินกำหนด {OverdueCount} บิล)" : "ค้างชำระ", UnpaidTotal, UnpaidCount)
    ];

    public IReadOnlyList<MonthlyReportLine> ChargeLines =>
    [
        new("ค่าเช่า", Rent),
        new("ค่าน้ำ", Water),
        new("ค่าไฟ", Electricity),
        new("ค่าจอดรถ", Parking),
        new("Cable TV", CableTv)
    ];

    public IReadOnlyList<MonthlyReportLine> PaymentLines =>
    [
        new("เงินสด", ReceivedByCash),
        new("โอนเข้าบัญชี", ReceivedByBankTransfer)
    ];
}
