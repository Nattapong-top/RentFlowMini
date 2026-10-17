using RentFlowApp;
using RentFlowApp.Application.Rooms;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Infrastructure.Rooms;

public sealed class InMemoryRoomRepository : IRoomRepository
{
    private readonly List<Room> _rooms = [];

    public void Add(Room room)
    {
        ArgumentNullException.ThrowIfNull(room);

        if (_rooms.Exists(existingRoom =>
                existingRoom.Id == room.Id ||
                existingRoom.RoomNumber.Value == room.RoomNumber.Value))
        {
            throw new InvalidOperationException(
                $"Room number '{room.RoomNumber.Value}' already exists."
            );
        }

        _rooms.Add(room);
    }

    public bool ExistsByRoomNumber(string roomNumber)
    {
        return _rooms.Exists(
            room => room.RoomNumber.Value == roomNumber
        );
    }

    public IReadOnlyList<Room> GetAll()
    {
        return _rooms
            .OrderBy(room => room.RoomNumber.Value)
            .Select(room => new Room(
                RoomId.From(room.Id.Value),
                new RoomNumber(room.RoomNumber.Value)))
            .ToList()
            .AsReadOnly();
    }
}
