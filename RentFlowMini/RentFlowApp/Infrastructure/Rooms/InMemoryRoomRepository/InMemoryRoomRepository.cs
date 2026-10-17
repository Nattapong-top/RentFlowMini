using RentFlowApp;
using RentFlowApp.Application.Rooms;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Infrastructure.Rooms;

public sealed class InMemoryRoomRepository : IRoomEditor
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
                new RoomNumber(room.RoomNumber.Value),
                Money.Create(room.MonthlyRent.Amount),
                room.IsCableTvExempt))
            .ToList()
            .AsReadOnly();
    }

    public Room? GetById(RoomId roomId)
    {
        ArgumentNullException.ThrowIfNull(roomId);

        var room = _rooms.FirstOrDefault(existingRoom => existingRoom.Id == roomId);
        return room is null
            ? null
            : new Room(
                RoomId.From(room.Id.Value),
                new RoomNumber(room.RoomNumber.Value),
                Money.Create(room.MonthlyRent.Amount),
                room.IsCableTvExempt);
    }

    public void Update(Room room)
    {
        ArgumentNullException.ThrowIfNull(room);

        var index = _rooms.FindIndex(existingRoom => existingRoom.Id == room.Id);
        if (index < 0)
        {
            throw new InvalidOperationException("ไม่พบห้องพักที่ต้องการแก้ไข");
        }

        _rooms[index] = room;
    }
}
