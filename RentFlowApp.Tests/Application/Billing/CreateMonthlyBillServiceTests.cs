using RentFlowApp.Application.Billing;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Infrastructure.Billing;

namespace RentFlowApp.Tests.Application.Billing;

public class CreateMonthlyBillServiceTests
{
    [Fact]
    public void Create_WithCompleteRequest_ReturnsMonthlyBill()
    {
        var repository = new InMemoryBillRepository();
        var service = new CreateMonthlyBillService(repository);
        var request = new CreateMonthlyBillRequest(
            RoomId: RoomId.New(),
            BillingPeriod: BillingPeriod.Create("2026-09"),
            MonthlyRent: 2800m,
            PreviousWaterMeter: MeterReading.Create(120m),
            CurrentWaterMeter: MeterReading.Create(125m),
            PreviousElectricityMeter: MeterReading.Create(1000m),
            CurrentElectricityMeter: MeterReading.Create(1010m),
            WaterRate: 30m,
            ElectricityRate: 4m,
            Tenant: TestTenants.Snapshot());

        var bill = service.Create(request);

        Assert.NotEqual(Guid.Empty, bill.Id.Value);
        Assert.Equal(request.RoomId, bill.RoomId);
        Assert.Equal(request.BillingPeriod, bill.BillingPeriod);
        Assert.Equal(2990m, bill.Total.Amount);
        Assert.True(
            repository.Exists(request.RoomId, request.BillingPeriod));
    }

    [Fact]
    public void Create_KeepsMeterReadingsUsedForCalculationOnBill()
    {
        var repository = new InMemoryBillRepository();
        var service = new CreateMonthlyBillService(repository);

        var bill = service.Create(new CreateMonthlyBillRequest(
            RoomId: RoomId.New(),
            BillingPeriod: BillingPeriod.Create("2026-09"),
            MonthlyRent: 2800m,
            PreviousWaterMeter: MeterReading.Create(120m),
            CurrentWaterMeter: MeterReading.Create(125m),
            PreviousElectricityMeter: MeterReading.Create(1000m),
            CurrentElectricityMeter: MeterReading.Create(1010m),
            WaterRate: 30m,
            ElectricityRate: 4m,
            Tenant: TestTenants.Snapshot()));

        Assert.NotNull(bill.Meters);
        Assert.Equal(120m, bill.Meters.PreviousWater.Value);
        Assert.Equal(125m, bill.Meters.CurrentWater.Value);
        Assert.Equal(1000m, bill.Meters.PreviousElectricity.Value);
        Assert.Equal(1010m, bill.Meters.CurrentElectricity.Value);
        Assert.Equal(30m, bill.Meters.WaterRate);
        Assert.Equal(4m, bill.Meters.ElectricityRate);
    }

    [Fact]
    public void Create_WithParkingAndCableTv_ReturnsBillWithAllCharges()
    {
        var repository = new InMemoryBillRepository();
        var service = new CreateMonthlyBillService(repository);
        var request = new CreateMonthlyBillRequest(
            RoomId: RoomId.New(),
            BillingPeriod: BillingPeriod.Create("2026-09"),
            MonthlyRent: 3000m,
            PreviousWaterMeter: MeterReading.Create(100m),
            CurrentWaterMeter: MeterReading.Create(110m),
            PreviousElectricityMeter: MeterReading.Create(500m),
            CurrentElectricityMeter: MeterReading.Create(550m),
            WaterRate: 19m,
            ElectricityRate: 8m,
            ChargeMotorcycleParking: true,
            MotorcycleParkingRate: 300m,
            ChargeCarParking: true,
            CarParkingRate: 500m,
            IsCableTvExempt: false,
            CableTvRate: 60m,
            Tenant: TestTenants.Snapshot());

        var bill = service.Create(request);

        Assert.Equal(3000m, bill.Rent.Amount);
        Assert.Equal(190m, bill.Water.Amount);
        Assert.Equal(400m, bill.Electricity.Amount);
        Assert.Equal(300m, bill.MotorcycleParking.Amount);
        Assert.Equal(500m, bill.CarParking.Amount);
        Assert.Equal(60m, bill.CableTv.Amount);
        Assert.Equal(4450m, bill.Total.Amount);
    }

    [Fact]
    public void Create_WhenRoomAndPeriodAlreadyHaveBill_ThrowsInvalidOperationException()
    {
        var repository = new InMemoryBillRepository();
        var service = new CreateMonthlyBillService(repository);
        var request = new CreateMonthlyBillRequest(
            RoomId: RoomId.New(),
            BillingPeriod: BillingPeriod.Create("2026-09"),
            MonthlyRent: 2800m,
            PreviousWaterMeter: MeterReading.Create(120m),
            CurrentWaterMeter: MeterReading.Create(125m),
            PreviousElectricityMeter: MeterReading.Create(1000m),
            CurrentElectricityMeter: MeterReading.Create(1010m),
            WaterRate: 30m,
            ElectricityRate: 4m,
            Tenant: TestTenants.Snapshot());

        service.Create(request);

        Assert.Throws<InvalidOperationException>(() =>
            service.Create(request));
    }

    [Fact]
    public void Create_WhenRoomHasNoTenant_IsRejectedAndSavesNothing()
    {
        var repository = new InMemoryBillRepository();
        var service = new CreateMonthlyBillService(repository);
        var meter = MeterReading.Create(100m);

        var exception = Assert.Throws<InvalidOperationException>(() => service.Create(new CreateMonthlyBillRequest(
            RoomId.New(), BillingPeriod.Create("2026-10"), 3000m, meter, meter, meter, meter, 19m, 8m)));

        Assert.Equal("ห้องนี้ไม่มีผู้เช่า จึงออกบิลไม่ได้ กรุณาเพิ่มผู้เช่าให้ห้องนี้ก่อน", exception.Message);
        Assert.Empty(repository.GetAll());
    }

    [Fact]
    public void Create_AssignsSequentialInvoiceNumbers_AndResetsEachMonth()
    {
        var repository = new InMemoryBillRepository();
        var service = new CreateMonthlyBillService(repository);

        var room1 = RoomId.New();
        var room2 = RoomId.New();

        var periodSep = BillingPeriod.Create("2026-09");
        var periodOct = BillingPeriod.Create("2026-10");

        var m0 = MeterReading.Create(100m);
        var m1 = MeterReading.Create(110m);

        var bill1Sep = service.Create(new CreateMonthlyBillRequest(
            room1, periodSep, 3000m, m0, m1, m0, m1, 19m, 8m, Tenant: TestTenants.Snapshot()));
        var bill2Sep = service.Create(new CreateMonthlyBillRequest(
            room2, periodSep, 3000m, m0, m1, m0, m1, 19m, 8m, Tenant: TestTenants.Snapshot()));

        var bill1Oct = service.Create(new CreateMonthlyBillRequest(
            room1, periodOct, 3000m, m0, m1, m0, m1, 19m, 8m, Tenant: TestTenants.Snapshot()));

        Assert.Equal("N001", bill1Sep.InvoiceNumber);
        Assert.Equal("N002", bill2Sep.InvoiceNumber);
        Assert.Equal("N001", bill1Oct.InvoiceNumber);
    }

    // ค่าเช่า 3000 + น้ำ 10 หน่วย x 19 = 190 + ไฟ 20 หน่วย x 8 = 160 + มอเตอร์ไซค์ 100 + Cable TV 60 = 3510
    private static CreateMonthlyBillRequest DraftRequest(
        MeterReading? currentWater = null,
        bool hasCurrentElectricity = true,
        bool hasTenant = true) =>
        new(
            RoomId.New(),
            BillingPeriod.Create("2026-10"),
            3000m,
            MeterReading.Create(100m),
            currentWater ?? MeterReading.Create(110m),
            MeterReading.Create(200m),
            hasCurrentElectricity ? MeterReading.Create(220m) : null,
            19m,
            8m,
            ChargeMotorcycleParking: true,
            MotorcycleParkingRate: 100m,
            IsCableTvExempt: false,
            CableTvRate: 60m,
            Tenant: hasTenant ? TestTenants.Snapshot() : null);

    [Fact]
    public void Preview_ReturnsDraftWithAllCharges_WithoutSavingOrTakingInvoiceNumber()
    {
        var repository = new InMemoryBillRepository();
        var service = new CreateMonthlyBillService(repository);
        var request = DraftRequest();

        var draft = service.Preview(request);

        Assert.Null(draft.Reason);
        Assert.NotNull(draft.Bill);
        Assert.Equal(request.RoomId, draft.Bill.RoomId);
        Assert.Equal(3000m, draft.Bill.Rent.Amount);
        Assert.Equal(190m, draft.Bill.Water.Amount);
        Assert.Equal(160m, draft.Bill.Electricity.Amount);
        Assert.Equal(100m, draft.Bill.MotorcycleParking.Amount);
        Assert.Equal(60m, draft.Bill.CableTv.Amount);
        Assert.Equal(3510m, draft.Bill.Total.Amount);
        Assert.Equal(MeterReading.Create(110m), draft.Bill.Meters!.CurrentWater);
        Assert.Equal(request.Tenant, draft.Bill.Tenant);
        Assert.Equal(string.Empty, draft.Bill.InvoiceNumber);
        Assert.Empty(repository.GetAll());

        Assert.Equal("N001", service.Create(request).InvoiceNumber);
    }

    [Fact]
    public void Preview_WhenMeterOfPeriodIsMissing_ReturnsReasonInsteadOfDraft()
    {
        var service = new CreateMonthlyBillService(new InMemoryBillRepository());

        var draft = service.Preview(DraftRequest(hasCurrentElectricity: false));

        Assert.Null(draft.Bill);
        Assert.Equal("ยังไม่มีข้อมูลบันทึกมิเตอร์ของรอบบิลนี้ กรุณาบันทึกมิเตอร์น้ำ-ไฟก่อนทำการสร้างบิล", draft.Reason);
    }

    [Fact]
    public void Preview_WhenCurrentMeterIsLowerThanPrevious_ReturnsReasonFromCalculation()
    {
        var service = new CreateMonthlyBillService(new InMemoryBillRepository());

        var draft = service.Preview(DraftRequest(currentWater: MeterReading.Create(90m)));

        Assert.Null(draft.Bill);
        Assert.NotNull(draft.Reason);
        Assert.Contains("ค่าปัจจุบันต้องไม่น้อยกว่าค่าก่อนหน้า", draft.Reason);
    }

    [Fact]
    public void Preview_WhenRoomHasNoTenant_ReturnsSameReasonAsCreate()
    {
        var service = new CreateMonthlyBillService(new InMemoryBillRepository());

        var draft = service.Preview(DraftRequest(hasTenant: false));

        Assert.Null(draft.Bill);
        Assert.Equal("ห้องนี้ไม่มีผู้เช่า จึงออกบิลไม่ได้ กรุณาเพิ่มผู้เช่าให้ห้องนี้ก่อน", draft.Reason);
    }
}
