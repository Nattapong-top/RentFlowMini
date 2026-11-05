using RentFlowApp.Application.Reports;
using RentFlowApp.Domain;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Tests.Application.Reports;

public class MonthlyReportBuilderTests
{
    private static readonly BillingPeriod October = BillingPeriod.Create("2026-10");
    private static readonly BillingPeriod September = BillingPeriod.Create("2026-09");

    // บิลรอบตุลาคมครบกำหนด 5 พ.ย. 2026
    private static readonly DateOnly BeforeOctoberDue = new(2026, 11, 5);

    private readonly List<Room> _rooms = [];
    private readonly List<Tenant> _tenants = [];
    private readonly List<Bill> _bills = [];

    private Room AddRoom(string number)
    {
        var room = new Room(RoomId.New(), new RoomNumber(number));
        _rooms.Add(room);
        return room;
    }

    private Tenant AddTenant(string firstName, Room? room = null)
    {
        var tenant = new Tenant(TenantId.New(), firstName, "ทดสอบ", roomId: room?.Id);
        _tenants.Add(tenant);
        return tenant;
    }

    private Bill AddBill(
        Room room,
        BillingPeriod period,
        decimal rent = 3000m,
        decimal water = 100m,
        decimal electricity = 200m,
        decimal motorcycle = 0m,
        decimal car = 0m,
        decimal cable = 0m,
        Tenant? tenant = null,
        PaymentMethod? paidBy = null)
    {
        var bill = Bill.Create(
            room.Id,
            period,
            new BillingCalculationResult(
                Money.Create(rent),
                Money.Create(water),
                Money.Create(electricity),
                Money.Create(motorcycle),
                Money.Create(car),
                Money.Create(cable),
                Money.Create(rent + water + electricity + motorcycle + car + cable)),
            tenant: tenant is null ? null : BillTenantSnapshot.From(tenant));

        if (paidBy is { } method)
        {
            bill.RecordPayment(new DateOnly(2026, 11, 1), method);
        }

        _bills.Add(bill);
        return bill;
    }

    private MonthlyReport Build(BillingPeriod? period = null, DateOnly? today = null) =>
        MonthlyReportBuilder.Build(_rooms, _tenants, _bills, period ?? October, today ?? BeforeOctoberDue);

    [Fact]
    public void Rooms_CountsOccupiedAndVacant_AndIgnoresTenantsWithoutRoom()
    {
        var occupied = AddRoom("101");
        AddRoom("102");
        AddRoom("103");
        AddTenant("มีห้อง", occupied);
        AddTenant("ไม่มีห้อง");

        var report = Build();

        Assert.Equal(3, report.TotalRooms);
        Assert.Equal(1, report.OccupiedRooms);
        Assert.Equal(2, report.VacantRooms);
    }

    [Fact]
    public void Rooms_WhenTenantMovedOut_RoomCountsAsVacant()
    {
        var room = AddRoom("101");
        _tenants.Add(new Tenant(TenantId.New(), "ย้ายแล้ว", "ทดสอบ", roomId: room.Id).MoveOut(new DateOnly(2026, 10, 31)));

        var report = Build();

        Assert.Equal(0, report.OccupiedRooms);
        Assert.Equal(1, report.VacantRooms);
    }

    [Fact]
    public void Totals_CountOnlySelectedPeriod_AndReceivedPlusUnpaidEqualsBilled()
    {
        AddBill(AddRoom("101"), October, rent: 3000m, water: 0m, electricity: 0m, paidBy: PaymentMethod.Cash);
        AddBill(AddRoom("102"), October, rent: 4000m, water: 0m, electricity: 0m);
        AddBill(AddRoom("103"), September, rent: 9000m, water: 0m, electricity: 0m);

        var report = Build();

        Assert.Equal(2, report.BilledCount);
        Assert.Equal(7000m, report.BilledTotal);
        Assert.Equal(1, report.ReceivedCount);
        Assert.Equal(3000m, report.ReceivedTotal);
        Assert.Equal(1, report.UnpaidCount);
        Assert.Equal(4000m, report.UnpaidTotal);
        Assert.Equal(report.BilledTotal, report.ReceivedTotal + report.UnpaidTotal);
    }

    [Fact]
    public void Received_IsCountedByBillingPeriod_NotByPaymentDate()
    {
        // จ่ายบิลรอบกันยายนในเดือนตุลาคม ต้องนับเข้ารอบกันยายน
        var bill = AddBill(AddRoom("101"), September, rent: 3000m, water: 0m, electricity: 0m);
        bill.RecordPayment(new DateOnly(2026, 10, 20), PaymentMethod.Cash);

        Assert.Equal(0m, Build(October).ReceivedTotal);
        Assert.Equal(3000m, Build(September).ReceivedTotal);
    }

    [Fact]
    public void ChargeBreakdown_SumsEachTypeOfSelectedPeriod_AndAddsUpToBilledTotal()
    {
        var room = AddRoom("101");
        AddBill(room, October, rent: 3000m, water: 100m, electricity: 200m, motorcycle: 50m, car: 100m, cable: 60m);
        AddBill(AddRoom("102"), October, rent: 4000m, water: 10m, electricity: 20m, motorcycle: 50m, paidBy: PaymentMethod.Cash);
        AddBill(room, September, rent: 9999m);

        var report = Build();

        Assert.Equal(7000m, report.Rent);
        Assert.Equal(110m, report.Water);
        Assert.Equal(220m, report.Electricity);
        Assert.Equal(200m, report.Parking);
        Assert.Equal(60m, report.CableTv);
        Assert.Equal(
            report.BilledTotal,
            report.Rent + report.Water + report.Electricity + report.Parking + report.CableTv);
    }

    [Fact]
    public void ReceivedByMethod_SplitsPaidBillsOnly()
    {
        AddBill(AddRoom("101"), October, rent: 1000m, water: 0m, electricity: 0m, paidBy: PaymentMethod.Cash);
        AddBill(AddRoom("102"), October, rent: 2000m, water: 0m, electricity: 0m, paidBy: PaymentMethod.BankTransfer);
        AddBill(AddRoom("103"), October, rent: 4000m, water: 0m, electricity: 0m);

        var report = Build();

        Assert.Equal(1000m, report.ReceivedByCash);
        Assert.Equal(2000m, report.ReceivedByBankTransfer);
    }

    [Fact]
    public void OverdueCount_StartsTheDayAfterDueDate()
    {
        AddBill(AddRoom("101"), October);

        Assert.Equal(0, Build(today: new DateOnly(2026, 11, 5)).OverdueCount);
        Assert.Equal(1, Build(today: new DateOnly(2026, 11, 6)).OverdueCount);
    }

    [Fact]
    public void OutstandingBills_IncludeEveryPeriod_ExcludePaid_AndListOldestFirst()
    {
        var room101 = AddRoom("101");
        var room99 = AddRoom("99");
        AddBill(room101, October, rent: 1000m, water: 0m, electricity: 0m);
        AddBill(room101, September, rent: 2000m, water: 0m, electricity: 0m);
        AddBill(room99, October, rent: 3000m, water: 0m, electricity: 0m);
        AddBill(AddRoom("102"), September, paidBy: PaymentMethod.Cash);

        var rows = Build(today: new DateOnly(2026, 11, 10)).OutstandingBills;

        Assert.Equal(["101", "99", "101"], rows.Select(row => row.RoomNumber));
        Assert.Equal([September, October, October], rows.Select(row => row.BillingPeriod));
        Assert.Equal([36, 5, 5], rows.Select(row => row.OverdueDays));
        Assert.Equal(6000m, Build().OutstandingBillsTotal);
    }

    [Fact]
    public void OutstandingBills_ShowTenantOnTheBill_NotTheCurrentOccupant()
    {
        var room = AddRoom("101");
        var previousTenant = AddTenant("คนเก่า");
        AddBill(room, October, tenant: previousTenant);
        AddTenant("คนใหม่", room);

        var row = Assert.Single(Build().OutstandingBills);

        Assert.Equal("คนเก่า ทดสอบ", row.TenantName);
    }

    [Fact]
    public void OutstandingBills_WhenBillHasNoTenant_ShowsDash()
    {
        AddBill(AddRoom("101"), October);

        var row = Assert.Single(Build().OutstandingBills);

        Assert.Equal("-", row.TenantName);
    }

    [Fact]
    public void Build_WhenNoData_ReturnsZeros()
    {
        var report = Build();

        Assert.Equal(0, report.TotalRooms);
        Assert.Equal(0m, report.BilledTotal);
        Assert.Empty(report.OutstandingBills);
    }
}
