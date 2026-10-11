
using RentFlowApp.Application.Rooms;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Tests.Application.Rooms;

public class CreateRoomServiceTests
{
    [Fact]
    public void Create_WithValidRoomNumber_ReturnsRoom()
    {
        // Arrange
        var repository = new FakeRoomRepository();
        var service = new CreateRoomService(repository);

        // Act
        var room = service.Create("101");

        // Assert
        Assert.NotNull(room);
        Assert.Equal("101", room.RoomNumber.Value);
    }

    [Fact]
    public void Create_WithValidRoomNumber_SavesRoom()
    {
        // Arrange
        var repository = new FakeRoomRepository();
        var service = new CreateRoomService(repository);

        // Act
        var room = service.Create("101");

        // Assert
        Assert.Contains(room, repository.Rooms);
    }


    [Fact]
    public void Create_WithDuplicateRoomNumber_ThrowsException()
    {
        // Arrange
        var repository = new FakeRoomRepository();
        var service = new CreateRoomService(repository);

        service.Create("101");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => service.Create("101")
        );
    }

    private sealed class FakeRoomRepository : IRoomRepository
    {
        public List<Room> Rooms { get; } = [];

        public void Add(Room room)
        {
            Rooms.Add(room);
        }

        public bool ExistsByRoomNumber(string roomNumber)
        {
            return Rooms.Exists(
                room => room.RoomNumber.Value == roomNumber
            );
        }

        public IReadOnlyList<Room> GetAll()
        {
            return Rooms.AsReadOnly();
        }
    }

}
