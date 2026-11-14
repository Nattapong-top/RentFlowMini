using RentFlowApp.Application.Tenants;

namespace RentFlowApp.Application.Rooms;

public sealed class RoomListService
{
    private readonly IRoomRepository _rooms;
    private readonly ITenantRepository _tenants;

    public RoomListService(IRoomRepository rooms, ITenantRepository tenants)
    {
        ArgumentNullException.ThrowIfNull(rooms);
        ArgumentNullException.ThrowIfNull(tenants);
        _rooms = rooms;
        _tenants = tenants;
    }

    public IReadOnlyList<RoomListItem> GetRooms()
    {
        var tenantNamesByRoomId = _tenants.GetAll()
            .Where(tenant => tenant.RoomId is not null)
            .ToDictionary(tenant => tenant.RoomId!, tenant => tenant.FullName);

        return _rooms.GetAll()
            .Select((room, index) =>
            {
                var hasTenant = tenantNamesByRoomId.TryGetValue(room.Id, out var tenantName);
                return new RoomListItem(
                    RoomId: room.Id,
                    Sequence: index + 1,
                    RoomNumber: room.RoomNumber.Value,
                    OccupancyStatus: hasTenant ? "ไม่ว่าง" : "ว่าง",
                    TenantName: hasTenant ? tenantName! : "-",
                    MonthlyRent: room.MonthlyRent.Amount,
                    IsCableTvExempt: room.IsCableTvExempt);
            })
            .ToList();
    }
}
