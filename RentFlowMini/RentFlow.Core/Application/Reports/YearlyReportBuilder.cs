using RentFlowApp.Domain.Billing;

namespace RentFlowApp.Application.Reports;

public static class YearlyReportBuilder
{
    private const string TotalLabel = "รวมทั้งปี";

    public static YearlyReport Build(IReadOnlyCollection<Bill> bills, int year)
    {
        ArgumentNullException.ThrowIfNull(bills);

        // นับตามรอบบิลเหมือนรายงานรายเดือน บิลธันวาคมที่จ่ายเดือนมกราคมจึงยังอยู่ในเดือนธันวาคม
        var yearBills = bills.Where(bill => bill.BillingPeriod.Year == year).ToList();

        var months = ThaiCalendar.MonthNames
            .Select((name, index) => BuildRow(name, yearBills.Where(bill => bill.BillingPeriod.Month == index + 1)))
            .ToList();

        return new YearlyReport(year, months, BuildRow(TotalLabel, yearBills));
    }

    private static YearlyReportRow BuildRow(string label, IEnumerable<Bill> rowBills)
    {
        var bills = rowBills.ToList();

        return new YearlyReportRow(
            label,
            bills.Count,
            bills.Sum(bill => bill.Rent.Amount),
            bills.Sum(bill => bill.Water.Amount),
            bills.Sum(bill => bill.Electricity.Amount),
            bills.Sum(bill => bill.MotorcycleParking.Amount + bill.CarParking.Amount),
            bills.Sum(bill => bill.CableTv.Amount),
            bills.Sum(bill => bill.Total.Amount),
            bills.Where(bill => bill.IsPaid).Sum(bill => bill.Total.Amount),
            bills.Where(bill => !bill.IsPaid).Sum(bill => bill.Total.Amount));
    }
}
