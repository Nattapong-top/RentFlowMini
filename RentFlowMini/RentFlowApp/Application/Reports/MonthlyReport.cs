using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Application.Reports;

public sealed record OutstandingBillRow(
    string RoomNumber,
    string TenantName,
    BillingPeriod BillingPeriod,
    DateOnly DueDate,
    int OverdueDays,
    decimal Amount);

public sealed record MonthlyReport(
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
    public int VacantRooms => TotalRooms - OccupiedRooms;

    public decimal OutstandingBillsTotal => OutstandingBills.Sum(bill => bill.Amount);
}
