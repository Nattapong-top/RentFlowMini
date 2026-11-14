using RentFlowApp.Application.Rooms;
using RentFlowApp.Application.Tenants;
using RentFlowApp.Infrastructure.Rooms;
using RentFlowApp.Infrastructure.Tenants;

namespace RentFlowApp.Tests.Application.Rooms;

public class RoomListServiceTests
{
    [Fact]
    public void GetRooms_WhenNoRooms_ReturnsEmptyList()
    {
        var service = new RoomListService(new InMemoryRoomRepository(), new InMemoryTenantRepository());

        Assert.Empty(service.GetRooms());
    }

    [Fact]
    public void GetRooms_ReturnsRoomsInStoredOrderWithSequenceRentAndCableTvExemption()
    {
        var rooms = new InMemoryRoomRepository();
        var createRoom = new CreateRoomService(rooms);
        var first = createRoom.Create("101", 3000m, false);
        var second = createRoom.Create("102", 3250.50m, true);
        var service = new RoomListService(rooms, new InMemoryTenantRepository());

        var items = service.GetRooms();

        Assert.Equal(2, items.Count);
        Assert.Equal(first.Id, items[0].RoomId);
        Assert.Equal(1, items[0].Sequence);
        Assert.Equal("101", items[0].RoomNumber);
        Assert.Equal(3000m, items[0].MonthlyRent);
        Assert.False(items[0].IsCableTvExempt);
        Assert.Equal(second.Id, items[1].RoomId);
        Assert.Equal(2, items[1].Sequence);
        Assert.Equal("102", items[1].RoomNumber);
        Assert.Equal(3250.50m, items[1].MonthlyRent);
        Assert.True(items[1].IsCableTvExempt);
    }

    [Fact]
    public void GetRooms_RoomWithTenant_IsOccupiedAndShowsTenantFullName()
    {
        var rooms = new InMemoryRoomRepository();
        var tenants = new InMemoryTenantRepository();
        var room = new CreateRoomService(rooms).Create("101", 3000m, false);
        new CreateTenantService(tenants).Create("สมชาย", "ใจดี", roomId: room.Id);
        var service = new RoomListService(rooms, tenants);

        var item = Assert.Single(service.GetRooms());

        Assert.Equal("ไม่ว่าง", item.OccupancyStatus);
        Assert.Equal("สมชาย ใจดี", item.TenantName);
    }

    [Fact]
    public void GetRooms_RoomWithoutTenant_IsVacantAndShowsDash()
    {
        var rooms = new InMemoryRoomRepository();
        var tenants = new InMemoryTenantRepository();
        new CreateRoomService(rooms).Create("101", 3000m, false);
        // ผู้เช่าที่ไม่ได้ผูกห้อง ต้องไม่ทำให้ห้องใดกลายเป็นไม่ว่าง
        new CreateTenantService(tenants).Create("สมหญิง", "รักดี");
        var service = new RoomListService(rooms, tenants);

        var item = Assert.Single(service.GetRooms());

        Assert.Equal("ว่าง", item.OccupancyStatus);
        Assert.Equal("-", item.TenantName);
    }
}
