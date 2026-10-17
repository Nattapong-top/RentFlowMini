using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Application.Rooms;

public sealed class UpdateRoomRentService
{
    private readonly IRoomEditor _repository;

    public UpdateRoomRentService(IRoomEditor repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public Room Update(RoomId roomId, decimal monthlyRent)
    {
        ArgumentNullException.ThrowIfNull(roomId);

        var room = _repository.GetById(roomId)
            ?? throw new InvalidOperationException("ไม่พบห้องพักที่ต้องการแก้ไข");

        room.ChangeMonthlyRent(Money.Create(monthlyRent));
        _repository.Update(room);

        return room;
    }
}
