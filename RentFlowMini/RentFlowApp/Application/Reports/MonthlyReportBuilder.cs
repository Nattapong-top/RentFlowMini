using RentFlowApp.Domain;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Application.Reports;

public static class MonthlyReportBuilder
{
    private const string NoValue = "-";

    public static MonthlyReport Build(
        IReadOnlyCollection<Room> rooms,
        IReadOnlyCollection<Tenant> tenants,
        IReadOnlyCollection<Bill> bills,
        BillingPeriod period,
        DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(rooms);
        ArgumentNullException.ThrowIfNull(tenants);
        ArgumentNullException.ThrowIfNull(bills);
        ArgumentNullException.ThrowIfNull(period);

        var periodBills = bills.Where(bill => bill.BillingPeriod == period).ToList();
        // "รับแล้ว" นับตามรอบบิล ไม่ใช่วันที่รับเงิน รับแล้วกับค้างชำระจึงรวมกันเท่ายอดเรียกเก็บเสมอ
        var paidBills = periodBills.Where(bill => bill.IsPaid).ToList();
        var unpaidBills = periodBills.Where(bill => !bill.IsPaid).ToList();

        return new MonthlyReport(
            TotalRooms: rooms.Count,
            OccupiedRooms: rooms.Count(room => tenants.Any(tenant => tenant.RoomId == room.Id)),
            BilledCount: periodBills.Count,
            BilledTotal: periodBills.Sum(bill => bill.Total.Amount),
            ReceivedCount: paidBills.Count,
            ReceivedTotal: paidBills.Sum(bill => bill.Total.Amount),
            UnpaidCount: unpaidBills.Count,
            UnpaidTotal: unpaidBills.Sum(bill => bill.Total.Amount),
            OverdueCount: unpaidBills.Count(bill => bill.IsOverdue(today)),
            Rent: periodBills.Sum(bill => bill.Rent.Amount),
            Water: periodBills.Sum(bill => bill.Water.Amount),
            Electricity: periodBills.Sum(bill => bill.Electricity.Amount),
            Parking: periodBills.Sum(bill => bill.MotorcycleParking.Amount + bill.CarParking.Amount),
            CableTv: periodBills.Sum(bill => bill.CableTv.Amount),
            ReceivedByCash: SumReceivedBy(paidBills, PaymentMethod.Cash),
            ReceivedByBankTransfer: SumReceivedBy(paidBills, PaymentMethod.BankTransfer),
            OutstandingBills: BuildOutstandingBills(rooms, bills, today));
    }

    private static decimal SumReceivedBy(List<Bill> paidBills, PaymentMethod method) =>
        paidBills.Where(bill => bill.Payment!.Method == method).Sum(bill => bill.Total.Amount);

    private static List<OutstandingBillRow> BuildOutstandingBills(
        IReadOnlyCollection<Room> rooms,
        IReadOnlyCollection<Bill> bills,
        DateOnly today)
    {
        var roomNumbers = rooms.ToDictionary(room => room.Id, room => room.RoomNumber.Value);

        return bills
            .Where(bill => !bill.IsPaid)
            .Select(bill => new OutstandingBillRow(
                roomNumbers.GetValueOrDefault(bill.RoomId, NoValue),
                bill.Tenant?.FullName ?? NoValue,
                bill.BillingPeriod,
                bill.DueDate,
                bill.GetOverdueDays(today),
                bill.Total.Amount))
            .OrderBy(row => row.DueDate)
            // เรียงเลขห้องให้ 99 มาก่อน 101
            .ThenBy(row => row.RoomNumber.Length)
            .ThenBy(row => row.RoomNumber, StringComparer.Ordinal)
            .ToList();
    }
}
