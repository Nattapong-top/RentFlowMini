using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Application.Rooms;

public interface IRoomEditor : IRoomRepository
{
    Room? GetById(RoomId roomId);

    void Update(Room room);
}
