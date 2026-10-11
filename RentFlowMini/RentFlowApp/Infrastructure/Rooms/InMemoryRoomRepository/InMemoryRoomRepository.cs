using RentFlowApp;
using RentFlowApp.Application.Rooms;

namespace RentFlowApp.Infrastructure.Rooms;

public sealed class InMemoryRoomRepository : IRoomRepository
{
    private readonly List<Room> _rooms = [];

    public void Add(Room room)
    {
        ArgumentNullException.ThrowIfNull(room);

        if (ExistsByRoomNumber(room.RoomNumber.Value))
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
        return _rooms.AsReadOnly();
    }
}