using System.Globalization;
using Microsoft.Data.Sqlite;
using RentFlowApp.Domain;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Infrastructure.Sqlite;

namespace RentFlowApp.Tests.Infrastructure.Sqlite;

public sealed class SqliteBillRepositoryTests : IDisposable
{
    private static readonly BillingPeriod September = BillingPeriod.Create("2026-09");

    private readonly string _folder =
        Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "RentFlowTests", Guid.NewGuid().ToString())).FullName;

    private readonly SqliteDatabase _database;

    public SqliteBillRepositoryTests()
    {
        _database = new SqliteDatabase(Path.Combine(_folder, "rentflow.db"));
        _database.Initialize();
    }

    public void Dispose()
    {
        // pool ยังถือไฟล์ไว้แม้ปิด connection แล้ว ถ้าไม่ล้างจะลบไฟล์เทสต์ไม่ได้
        SqliteConnection.ClearAllPools();
        Directory.Delete(_folder, recursive: true);
    }

    [Fact]
    public void GetAll_FromNewRepository_ReturnsEverySavedValue()
    {
        var roomId = AddRoom("101");
        var tenantId = AddTenant("Anna", "NewName");
        var billId = BillId.New();
        var createdAt = new DateTime(2026, 9, 30, 14, 15, 16, 789);
        var savedDueDate = new DateOnly(2026, 12, 31);
        var meters = new BillMeterSnapshot(
            MeterReading.Create(100.25m),
            MeterReading.Create(125.5m),
            MeterReading.Create(1000.75m),
            MeterReading.Create(1010.125m),
            6.5m,
            4.25m);
        // ชื่อตอนออกบิลต่างจากชื่อปัจจุบัน เพื่อยืนยันว่าบิลเก่าเก็บชื่อของตัวเอง
        var tenant = new BillTenantSnapshot(tenantId, "Anna OldName");
        var payment = new BillPayment(new DateOnly(2026, 10, 3), PaymentMethod.BankTransfer);
        new SqliteBillRepository(_database).Add(Bill.Restore(
            billId,
            roomId,
            September,
            new BillingCalculationResult(
                Rent: Money.Create(2800.25m),
                Water: Money.Create(150.50m),
                Electricity: Money.Create(40.75m),
                MotorcycleParking: Money.Create(300.10m),
                CarParking: Money.Create(500.20m),
                CableTv: Money.Create(0m),
                Total: Money.Create(3791.80m)),
            "N007",
            createdAt,
            savedDueDate,
            meters,
            tenant,
            isCableTvExempt: true,
            payment));

        var saved = Assert.Single(new SqliteBillRepository(_database).GetAll());

        Assert.Equal(billId, saved.Id);
        Assert.Equal(roomId, saved.RoomId);
        Assert.Equal(September, saved.BillingPeriod);
        Assert.Equal(2800.25m, saved.Rent.Amount);
        Assert.Equal(150.50m, saved.Water.Amount);
        Assert.Equal(40.75m, saved.Electricity.Amount);
        Assert.Equal(300.10m, saved.MotorcycleParking.Amount);
        Assert.Equal(500.20m, saved.CarParking.Amount);
        Assert.Equal(0m, saved.CableTv.Amount);
        Assert.Equal(3791.80m, saved.Total.Amount);
        Assert.Equal("N007", saved.InvoiceNumber);
        Assert.Equal(createdAt, saved.CreatedAt);
        Assert.Equal(savedDueDate, saved.DueDate);
        Assert.Equal(meters, saved.Meters);
        Assert.Equal(tenant, saved.Tenant);
        Assert.True(saved.IsCableTvExempt);
        Assert.Equal(payment, saved.Payment);
    }

    [Fact]
    public void Add_OnThaiCultureMachine_StoresGregorianDates()
    {
        var bill = CreateCompleteBill(new DateTime(2026, 9, 30, 14, 15, 16));

        var originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("th-TH");
        try
        {
            new SqliteBillRepository(_database).Add(bill);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }

        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT CreatedAt, DueDate, PaidOn FROM Bills;";
        using var reader = command.ExecuteReader();
        Assert.True(reader.Read());

        Assert.StartsWith("2026-09-30T14:15:16", reader.GetString(0));
        Assert.Equal("2026-12-31", reader.GetString(1));
        Assert.Equal("2026-10-03", reader.GetString(2));
    }

    [Fact]
    public void GetAll_ReturnsLocalCreationTimeExactly()
    {
        // Bill.Create ใช้ DateTime.Now จึงต้องอ่านกลับได้ตรงทั้งเวลาและเขตเวลา
        var createdAt = DateTime.Now;
        new SqliteBillRepository(_database).Add(CreateCompleteBill(createdAt));

        var saved = Assert.Single(new SqliteBillRepository(_database).GetAll());

        Assert.Equal(createdAt, saved.CreatedAt);
        Assert.Equal(DateTimeKind.Local, saved.CreatedAt.Kind);
    }

    [Fact]
    public void GetAll_WithoutMetersTenantOrPayment_ReturnsEmptyValues()
    {
        // ตาราง Bills ยอมให้คอลัมน์กลุ่มนี้ว่าง จึงต้องอ่านแถวแบบนี้ได้โดยไม่พัง
        var bill = Bill.Create(
            AddRoom("101"),
            September,
            new BillingCalculationResult(
                Rent: Money.Create(2800m),
                Water: Money.Create(0m),
                Electricity: Money.Create(0m),
                MotorcycleParking: Money.Create(0m),
                CarParking: Money.Create(0m),
                CableTv: Money.Create(0m),
                Total: Money.Create(2800m)));
        new SqliteBillRepository(_database).Add(bill);

        var saved = Assert.Single(new SqliteBillRepository(_database).GetAll());

        Assert.Null(saved.Meters);
        Assert.Null(saved.Tenant);
        Assert.Null(saved.Payment);
        Assert.False(saved.IsPaid);
        Assert.False(saved.IsCableTvExempt);
        Assert.Equal(2800m, saved.Total.Amount);
    }

    [Fact]
    public void GetAll_ForUnpaidBillWithMetersAndTenant_ReturnsNoPayment()
    {
        var meters = new BillMeterSnapshot(
            MeterReading.Create(100m),
            MeterReading.Create(125m),
            MeterReading.Create(1000m),
            MeterReading.Create(1010m),
            6.5m,
            4.25m);
        var tenant = new BillTenantSnapshot(AddTenant("Anna", "Example"), "Anna Example");
        new SqliteBillRepository(_database).Add(
            Bill.Create(AddRoom("101"), September, RentOnly(), meters: meters, tenant: tenant));

        var saved = Assert.Single(new SqliteBillRepository(_database).GetAll());

        Assert.Equal(meters, saved.Meters);
        Assert.Equal(tenant, saved.Tenant);
        Assert.Null(saved.Payment);
        Assert.False(saved.IsPaid);
    }

    [Fact]
    public void Add_WithSameRoomAndPeriod_ThrowsThaiMessage()
    {
        var repository = new SqliteBillRepository(_database);
        var roomId = AddRoom("101");
        repository.Add(Bill.Create(roomId, September, RentOnly(), "N001"));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            repository.Add(Bill.Create(roomId, September, RentOnly(), "N002")));

        Assert.Equal("มีบิลของห้องและรอบบิลนี้อยู่แล้ว", exception.Message);
    }

    [Fact]
    public void Exists_MatchesBothRoomAndPeriod()
    {
        var repository = new SqliteBillRepository(_database);
        var billedRoomId = AddRoom("101");
        var otherRoomId = AddRoom("102");
        repository.Add(Bill.Create(billedRoomId, September, RentOnly()));

        Assert.True(repository.Exists(billedRoomId, September));
        Assert.False(repository.Exists(billedRoomId, BillingPeriod.Create("2026-10")));
        Assert.False(repository.Exists(otherRoomId, September));
    }

    [Fact]
    public void Get_ReturnsRequestedBillOrNull()
    {
        var repository = new SqliteBillRepository(_database);
        var firstBill = Bill.Create(AddRoom("101"), September, RentOnly(), "N001");
        var secondBill = Bill.Create(AddRoom("102"), September, RentOnly(), "N002");
        repository.Add(firstBill);
        repository.Add(secondBill);

        Assert.Equal(secondBill.Id, repository.Get(secondBill.Id)?.Id);
        Assert.Null(repository.Get(BillId.New()));
    }

    [Fact]
    public void Update_AfterRecordingPayment_KeepsPaymentForNewRepository()
    {
        var repository = new SqliteBillRepository(_database);
        var bill = Bill.Create(AddRoom("101"), September, RentOnly());
        repository.Add(bill);
        var paidOn = new DateOnly(2026, 10, 3);

        bill.RecordPayment(paidOn, PaymentMethod.BankTransfer);
        repository.Update(bill);

        var saved = new SqliteBillRepository(_database).Get(bill.Id);
        Assert.NotNull(saved);
        Assert.True(saved.IsPaid);
        Assert.Equal(new BillPayment(paidOn, PaymentMethod.BankTransfer), saved.Payment);
    }

    [Fact]
    public void Update_AfterCancellingPayment_ClearsPaymentForNewRepository()
    {
        var repository = new SqliteBillRepository(_database);
        var bill = Bill.Create(AddRoom("101"), September, RentOnly());
        bill.RecordPayment(new DateOnly(2026, 10, 3), PaymentMethod.Cash);
        repository.Add(bill);

        bill.CancelPayment();
        repository.Update(bill);

        var saved = new SqliteBillRepository(_database).Get(bill.Id);
        Assert.NotNull(saved);
        Assert.False(saved.IsPaid);
        Assert.Null(saved.Payment);
    }

    [Fact]
    public void Update_AfterChangingCharges_KeepsNewAmountsForNewRepository()
    {
        var repository = new SqliteBillRepository(_database);
        var bill = Bill.Create(
            AddRoom("101"),
            September,
            new BillingCalculationResult(
                Rent: Money.Create(2800m),
                Water: Money.Create(150m),
                Electricity: Money.Create(40m),
                MotorcycleParking: Money.Create(0m),
                CarParking: Money.Create(0m),
                CableTv: Money.Create(100m),
                Total: Money.Create(3090m)));
        repository.Add(bill);

        bill.UpdateAdditionalCharges(
            Money.Create(300m),
            Money.Create(500m),
            Money.Create(0m),
            isCableTvExempt: true);
        repository.Update(bill);

        var saved = new SqliteBillRepository(_database).Get(bill.Id);
        Assert.NotNull(saved);
        Assert.Equal(300m, saved.MotorcycleParking.Amount);
        Assert.Equal(500m, saved.CarParking.Amount);
        Assert.Equal(0m, saved.CableTv.Amount);
        Assert.True(saved.IsCableTvExempt);
        Assert.Equal(3790m, saved.Total.Amount);
        Assert.Equal(2800m, saved.Rent.Amount);
        Assert.Equal(150m, saved.Water.Amount);
        Assert.Equal(40m, saved.Electricity.Amount);
    }

    [Fact]
    public void Update_AfterRevisingRoomCharges_KeepsRentMetersAndAmountsForNewRepository()
    {
        var repository = new SqliteBillRepository(_database);
        var meter = MeterReading.Create(100m);
        var bill = Bill.Create(
            AddRoom("101"),
            September,
            BillingCalculator.Calculate(2800m, meter, meter, meter, meter, 19m, 8m),
            meters: new BillMeterSnapshot(meter, meter, meter, meter, 19m, 8m));
        repository.Add(bill);

        // น้ำ 100 → 120.5 (20.5 หน่วย x 19 = 389.5), ไฟ 100 → 115 (15 หน่วย x 8 = 120)
        bill.ReviseRoomCharges(
            Money.Create(3000m),
            new BillMeterReadings(meter, MeterReading.Create(120.5m), meter, MeterReading.Create(115m)));
        repository.Update(bill);

        var saved = new SqliteBillRepository(_database).Get(bill.Id);
        Assert.NotNull(saved);
        Assert.Equal(3000m, saved.Rent.Amount);
        Assert.Equal(389.5m, saved.Water.Amount);
        Assert.Equal(120m, saved.Electricity.Amount);
        Assert.Equal(3509.5m, saved.Total.Amount);
        Assert.Equal(
            new BillMeterSnapshot(meter, MeterReading.Create(120.5m), meter, MeterReading.Create(115m), 19m, 8m),
            saved.Meters);
    }

    [Fact]
    public void Update_WithMissingBill_ThrowsThaiMessage()
    {
        var repository = new SqliteBillRepository(_database);
        var missingBill = Bill.Create(AddRoom("101"), September, RentOnly());

        var exception = Assert.Throws<InvalidOperationException>(() => repository.Update(missingBill));

        Assert.Equal("ไม่พบบิลที่ต้องการแก้ไข กรุณาเลือกบิลจากตารางอีกครั้ง", exception.Message);
    }

    [Fact]
    public void Add_WithSameInvoiceNumberInPeriod_ThrowsThaiMessage()
    {
        var repository = new SqliteBillRepository(_database);
        repository.Add(Bill.Create(AddRoom("101"), September, RentOnly(), "N001"));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            repository.Add(Bill.Create(AddRoom("102"), September, RentOnly(), "N001")));

        Assert.Equal("เลขที่ใบแจ้งหนี้นี้ถูกใช้แล้ว กรุณาออกบิลใหม่อีกครั้ง", exception.Message);
    }

    [Fact]
    public void Add_WithMissingRoom_ThrowsThaiMessage()
    {
        var repository = new SqliteBillRepository(_database);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            repository.Add(Bill.Create(RoomId.New(), September, RentOnly())));

        Assert.Equal("ไม่พบห้องพักหรือผู้เช่าของบิลนี้ กรุณาตรวจสอบข้อมูลแล้วลองอีกครั้ง", exception.Message);
    }

    [Fact]
    public void GetAll_ReturnsBillsInOrderAdded()
    {
        var repository = new SqliteBillRepository(_database);
        var added = new[] { "101", "102", "103", "104", "105" }
            .Select((roomNumber, index) => Bill.Create(AddRoom(roomNumber), September, RentOnly(), $"N00{index + 1}"))
            .ToList();
        added.ForEach(repository.Add);

        Assert.Equal(
            added.Select(bill => bill.Id),
            repository.GetAll().Select(bill => bill.Id));
    }

    private static BillingCalculationResult RentOnly()
    {
        return new BillingCalculationResult(
            Rent: Money.Create(2800m),
            Water: Money.Create(0m),
            Electricity: Money.Create(0m),
            MotorcycleParking: Money.Create(0m),
            CarParking: Money.Create(0m),
            CableTv: Money.Create(0m),
            Total: Money.Create(2800m));
    }

    private Bill CreateCompleteBill(DateTime createdAt)
    {
        return Bill.Restore(
            BillId.New(),
            AddRoom("101"),
            September,
            new BillingCalculationResult(
                Rent: Money.Create(2800m),
                Water: Money.Create(0m),
                Electricity: Money.Create(0m),
                MotorcycleParking: Money.Create(0m),
                CarParking: Money.Create(0m),
                CableTv: Money.Create(0m),
                Total: Money.Create(2800m)),
            "N001",
            createdAt,
            new DateOnly(2026, 12, 31),
            new BillMeterSnapshot(
                MeterReading.Create(0m),
                MeterReading.Create(0m),
                MeterReading.Create(0m),
                MeterReading.Create(0m),
                0m,
                0m),
            new BillTenantSnapshot(AddTenant("Anna", "Example"), "Anna Example"),
            isCableTvExempt: false,
            new BillPayment(new DateOnly(2026, 10, 3), PaymentMethod.Cash));
    }

    private RoomId AddRoom(string roomNumber)
    {
        var room = new Room(RoomId.New(), new RoomNumber(roomNumber));
        new SqliteRoomRepository(_database).Add(room);

        return room.Id;
    }

    private TenantId AddTenant(string firstName, string lastName)
    {
        var tenant = new Tenant(TenantId.New(), firstName, lastName);
        new SqliteTenantRepository(_database).Add(tenant);

        return tenant.Id;
    }
}
