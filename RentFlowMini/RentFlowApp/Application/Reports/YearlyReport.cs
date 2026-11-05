namespace RentFlowApp.Application.Reports;

public sealed record YearlyReportRow(
    string Label,
    int BillCount,
    decimal Rent,
    decimal Water,
    decimal Electricity,
    decimal Parking,
    decimal CableTv,
    decimal BilledTotal,
    decimal ReceivedTotal,
    decimal UnpaidTotal)
{
    // เรียงตรงกับ YearlyReport.AmountHeaders
    public IReadOnlyList<decimal> Amounts =>
        [Rent, Water, Electricity, Parking, CableTv, BilledTotal, ReceivedTotal, UnpaidTotal];
}

public sealed record YearlyReport(int Year, IReadOnlyList<YearlyReportRow> Months, YearlyReportRow Total)
{
    public const string MonthHeader = "เดือน";
    public const string BillCountHeader = "บิล";

    public static readonly IReadOnlyList<string> AmountHeaders =
        ["ค่าเช่า", "ค่าน้ำ", "ค่าไฟ", "จอดรถ", "Cable TV", "เรียกเก็บ", "รับแล้ว", "ค้างชำระ"];

    public int BuddhistYear => Year + 543;

    public string Title => $"สรุปรายงานประจำปี {BuddhistYear}";

    public bool HasBills => Total.BillCount > 0;

    public IEnumerable<YearlyReportRow> AllRows => Months.Append(Total);
}
