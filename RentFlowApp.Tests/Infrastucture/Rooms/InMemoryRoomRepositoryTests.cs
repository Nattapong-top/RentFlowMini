
using RentFlowApp;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Infrastructure.Rooms;

namespace RentFlowApp.Tests.Infrastructure.Rooms;

public class InMemoryRoomRepositoryTests
{
    [Fact]
    public void GetAll_WithNoRooms_ReturnsEmptyList()
    {
        // Arrange
        var repository = new InMemoryRoomRepository();

        // Act
        var rooms = repository.GetAll();

        // Assert
        Assert.Empty(rooms);
    }

    [Fact]
    public void GetAll_WithExistingRooms_ReturnsAllRooms()
    {
        // Arrange
        var repository = new InMemoryRoomRepository();

        var room101 = new Room(
            RoomId.New(),
            new RoomNumber("101")
        );

        var room102 = new Room(
            RoomId.New(),
            new RoomNumber("102")
        );

        repository.Add(room101);
        repository.Add(room102);

        // Act
        var rooms = repository.GetAll();

        // Assert
        Assert.Equal(2, rooms.Count);
        Assert.Contains(room101, rooms);
        Assert.Contains(room102, rooms);
    }
}
