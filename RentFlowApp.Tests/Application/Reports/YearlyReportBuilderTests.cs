using RentFlowApp.Application.Reports;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Tests.Application.Reports;

public class YearlyReportBuilderTests
{
    private readonly List<Bill> _bills = [];

    private Bill AddBill(
        string period,
        decimal rent = 3000m,
        decimal water = 100m,
        decimal electricity = 200m,
        decimal motorcycle = 0m,
        decimal car = 0m,
        decimal cable = 0m)
    {
        var bill = Bill.Create(
            RoomId.New(),
            BillingPeriod.Create(period),
            new BillingCalculationResult(
                Money.Create(rent),
                Money.Create(water),
                Money.Create(electricity),
                Money.Create(motorcycle),
                Money.Create(car),
                Money.Create(cable),
                Money.Create(rent + water + electricity + motorcycle + car + cable)));
        _bills.Add(bill);
        return bill;
    }

    [Fact]
    public void Build_AlwaysHasTwelveMonthsInOrder()
    {
        var report = YearlyReportBuilder.Build(_bills, 2026);

        Assert.Equal(12, report.Months.Count);
        Assert.Equal("มกราคม", report.Months[0].Label);
        Assert.Equal("ธันวาคม", report.Months[11].Label);
        Assert.Equal("รวมทั้งปี", report.Total.Label);
        Assert.Equal(13, report.AllRows.Count());
        Assert.False(report.HasBills);
    }

    [Fact]
    public void Build_SumsEachChargeIntoItsMonth_AndIgnoresOtherYears()
    {
        AddBill("2026-01", rent: 1000m, water: 10m, electricity: 20m, motorcycle: 30m, car: 40m, cable: 50m);
        AddBill("2026-01", rent: 2000m, water: 1m, electricity: 2m);
        AddBill("2026-03", rent: 4000m, water: 0m, electricity: 0m);
        AddBill("2025-01", rent: 9000m);
        AddBill("2027-01", rent: 9000m);

        var report = YearlyReportBuilder.Build(_bills, 2026);

        var january = report.Months[0];
        Assert.Equal(2, january.BillCount);
        Assert.Equal(3000m, january.Rent);
        Assert.Equal(11m, january.Water);
        Assert.Equal(22m, january.Electricity);
        Assert.Equal(70m, january.Parking);
        Assert.Equal(50m, january.CableTv);
        Assert.Equal(3153m, january.BilledTotal);
        Assert.Equal(0, report.Months[1].BillCount);
        Assert.Equal(4000m, report.Months[2].BilledTotal);
        Assert.Equal(7153m, report.Total.BilledTotal);
        Assert.Equal(3, report.Total.BillCount);
        Assert.True(report.HasBills);
    }

    [Fact]
    public void Total_EqualsSumOfMonths_AndReceivedPlusUnpaidEqualsBilled()
    {
        AddBill("2026-02", rent: 1000m, water: 0m, electricity: 0m)
            .RecordPayment(new DateOnly(2026, 3, 1), PaymentMethod.Cash);
        AddBill("2026-02", rent: 2000m, water: 0m, electricity: 0m);
        AddBill("2026-07", rent: 4000m, water: 0m, electricity: 0m);

        var report = YearlyReportBuilder.Build(_bills, 2026);

        Assert.Equal(1000m, report.Months[1].ReceivedTotal);
        Assert.Equal(2000m, report.Months[1].UnpaidTotal);
        Assert.Equal(1000m, report.Total.ReceivedTotal);
        Assert.Equal(6000m, report.Total.UnpaidTotal);
        Assert.Equal(report.Total.BilledTotal, report.Total.ReceivedTotal + report.Total.UnpaidTotal);
        Assert.Equal(report.Total.BilledTotal, report.Months.Sum(month => month.BilledTotal));
    }

    [Fact]
    public void Received_IsCountedByBillingPeriod_EvenWhenPaidInNextYear()
    {
        AddBill("2026-12", rent: 3000m, water: 0m, electricity: 0m)
            .RecordPayment(new DateOnly(2027, 1, 3), PaymentMethod.BankTransfer);

        Assert.Equal(3000m, YearlyReportBuilder.Build(_bills, 2026).Months[11].ReceivedTotal);
        Assert.Equal(0m, YearlyReportBuilder.Build(_bills, 2027).Total.ReceivedTotal);
    }

    [Fact]
    public void Amounts_FollowTheSameOrderAsHeaders()
    {
        AddBill("2026-01", rent: 1m, water: 2m, electricity: 3m, motorcycle: 4m, cable: 5m);

        var row = YearlyReportBuilder.Build(_bills, 2026).Months[0];

        Assert.Equal(YearlyReport.AmountHeaders.Count, row.Amounts.Count);
        Assert.Equal([1m, 2m, 3m, 4m, 5m, 15m, 0m, 15m], row.Amounts);
    }
}
