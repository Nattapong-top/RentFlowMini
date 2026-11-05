using RentFlowApp.Application.Tenants;
using RentFlowApp.Domain;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Infrastructure.Tenants;

namespace RentFlowApp.Tests.Application.Tenants;

public class TenantLifecycleServiceTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Now);

    private readonly InMemoryTenantRepository _repository = new();

    private Tenant CreateTenant(RoomId? roomId = null, DateOnly? moveInDate = null, decimal? deposit = null) =>
        new CreateTenantService(_repository).Create("สมชาย", "ใจดี", "0812345678", roomId, moveInDate, deposit);

    private UpdateTenantRequest RequestFor(Tenant tenant, RoomId? roomId) => new(
        tenant.Id, tenant.FirstName, tenant.LastName, tenant.PhoneNumber, roomId, tenant.MoveInDate, tenant.SecurityDeposit);

    [Fact]
    public void Create_StoresMoveInDateAndDeposit()
    {
        var tenant = CreateTenant(moveInDate: new DateOnly(2026, 11, 1), deposit: 5000m);

        var stored = _repository.GetById(tenant.Id)!;

        Assert.Equal(new DateOnly(2026, 11, 1), stored.MoveInDate);
        Assert.Equal(5000m, stored.SecurityDeposit);
    }

    [Fact]
    public void Create_WhenDepositIsNegative_IsRejected()
    {
        var exception = Assert.Throws<InvalidSecurityDepositException>(() => CreateTenant(deposit: -1m));

        Assert.Equal("เงินประกันต้องไม่ติดลบ", exception.Message);
    }

    [Fact]
    public void Update_ChangesDetailsAndKeepsId()
    {
        var tenant = CreateTenant();

        new UpdateTenantService(_repository).Update(new UpdateTenantRequest(
            tenant.Id, "สมศักดิ์", "มีสุข", null, null, new DateOnly(2026, 10, 1), 3000m));

        var stored = _repository.GetById(tenant.Id)!;
        Assert.Equal("สมศักดิ์ มีสุข", stored.FullName);
        Assert.Null(stored.PhoneNumber);
        Assert.Equal(new DateOnly(2026, 10, 1), stored.MoveInDate);
        Assert.Equal(3000m, stored.SecurityDeposit);
    }

    [Fact]
    public void Update_CanMoveToFreeRoomAndReleasesOldRoom()
    {
        var oldRoom = RoomId.New();
        var newRoom = RoomId.New();
        var tenant = CreateTenant(oldRoom);

        new UpdateTenantService(_repository).Update(RequestFor(tenant, newRoom));

        Assert.False(_repository.ExistsByRoomId(oldRoom));
        Assert.True(_repository.ExistsByRoomId(newRoom));
    }

    [Fact]
    public void Update_KeepingOwnRoom_IsAllowed()
    {
        var room = RoomId.New();
        var tenant = CreateTenant(room);

        new UpdateTenantService(_repository).Update(RequestFor(tenant, room));

        Assert.Equal(room, _repository.GetById(tenant.Id)!.RoomId);
    }

    [Fact]
    public void Update_WhenRoomBelongsToAnotherTenant_IsRejectedAndKeepsRoom()
    {
        var takenRoom = RoomId.New();
        var ownRoom = RoomId.New();
        CreateTenant(takenRoom);
        var tenant = CreateTenant(ownRoom);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new UpdateTenantService(_repository).Update(RequestFor(tenant, takenRoom)));

        Assert.Contains("มีผู้เช่าอยู่แล้ว", exception.Message);
        Assert.Equal(ownRoom, _repository.GetById(tenant.Id)!.RoomId);
    }

    [Fact]
    public void Update_WhenTenantMovedOut_IsRejected()
    {
        var tenant = CreateTenant(RoomId.New());
        new MoveOutTenantService(_repository).MoveOut(tenant.Id, Today);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new UpdateTenantService(_repository).Update(RequestFor(tenant, null)));

        Assert.Contains("ย้ายออกแล้ว", exception.Message);
    }

    [Fact]
    public void MoveOut_ReleasesRoomAndKeepsHistory()
    {
        var room = RoomId.New();
        var tenant = CreateTenant(room, deposit: 5000m);

        new MoveOutTenantService(_repository).MoveOut(tenant.Id, Today);

        var stored = _repository.GetById(tenant.Id)!;
        Assert.True(stored.IsMovedOut);
        Assert.Equal(Today, stored.MovedOutOn);
        Assert.Null(stored.RoomId);
        Assert.Equal(room, stored.MovedOutFromRoomId);
        Assert.Equal(5000m, stored.SecurityDeposit);
        Assert.False(_repository.ExistsByRoomId(room));
    }

    [Fact]
    public void MoveOut_AllowsNewTenantInSameRoom()
    {
        var room = RoomId.New();
        var tenant = CreateTenant(room);

        new MoveOutTenantService(_repository).MoveOut(tenant.Id, Today);
        var newTenant = CreateTenant(room);

        Assert.Equal(room, _repository.GetById(newTenant.Id)!.RoomId);
    }

    [Fact]
    public void MoveOut_WhenDateIsInFuture_IsRejected()
    {
        var tenant = CreateTenant(RoomId.New());

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new MoveOutTenantService(_repository).MoveOut(tenant.Id, Today.AddDays(1)));

        Assert.Contains("ต้องไม่เกินวันนี้", exception.Message);
        Assert.False(_repository.GetById(tenant.Id)!.IsMovedOut);
    }

    [Fact]
    public void MoveOut_WhenDateIsBeforeMoveIn_IsRejected()
    {
        var tenant = CreateTenant(RoomId.New(), moveInDate: Today);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new MoveOutTenantService(_repository).MoveOut(tenant.Id, Today.AddDays(-1)));

        Assert.Contains("ต้องไม่ก่อนวันเข้าอยู่", exception.Message);
    }

    [Fact]
    public void MoveOut_WhenAlreadyMovedOut_KeepsFirstDate()
    {
        var tenant = CreateTenant(RoomId.New());
        var service = new MoveOutTenantService(_repository);
        service.MoveOut(tenant.Id, Today);

        Assert.Throws<InvalidOperationException>(() => service.MoveOut(tenant.Id, Today.AddDays(-1)));

        Assert.Equal(Today, _repository.GetById(tenant.Id)!.MovedOutOn);
    }
}
