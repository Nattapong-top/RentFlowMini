using RentFlowApp.Application.Rooms;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Tests.Application.Rooms;

public class UpdateRoomRentServiceTests
{
    [Fact]
    public void Update_WithZeroRent_UpdatesExistingRoom()
    {
        var repository = new InMemoryRoomEditor();
        var room = new Room(
            RoomId.New(),
            new RoomNumber("101"),
            Money.Create(5000),
            true);
        repository.Add(room);
        var service = new UpdateRoomRentService(repository);

        var updatedRoom = service.Update(room.Id, 0);

        Assert.Equal(0, updatedRoom.MonthlyRent.Amount);
        Assert.Equal("101", updatedRoom.RoomNumber.Value);
        Assert.True(updatedRoom.IsCableTvExempt);
    }

    [Fact]
    public void Update_WithUnknownRoom_ThrowsInvalidOperationException()
    {
        var service = new UpdateRoomRentService(new InMemoryRoomEditor());

        Assert.Throws<InvalidOperationException>(
            () => service.Update(RoomId.New(), 1000));
    }

    private sealed class InMemoryRoomEditor : IRoomEditor
    {
        private readonly List<Room> _rooms = [];

        public void Add(Room room) => _rooms.Add(room);

        public bool ExistsByRoomNumber(string roomNumber) =>
            _rooms.Any(room => room.RoomNumber.Value == roomNumber);

        public IReadOnlyList<Room> GetAll() => _rooms.AsReadOnly();

        public Room? GetById(RoomId roomId) =>
            _rooms.FirstOrDefault(room => room.Id == roomId);

        public void Update(Room room)
        {
            var index = _rooms.FindIndex(existingRoom => existingRoom.Id == room.Id);
            _rooms[index] = room;
        }
    }
}
