
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Tests;

public class RoomTests
{
    [Fact]
    public void Constructor_WithValidRoomNumber_CreatesRoom()
    {
        // Arrange
        RoomId roomId = RoomId.New();
        var roomNumber = new RoomNumber("101");

        // Act
        var room = new Room(roomId, roomNumber);

        // Assert
        Assert.Same(roomNumber, room.RoomNumber);
    }

    [Fact]
    public void Constructor_WithNullRoomNumber_ThrowsArgumentNullException()
    {
        // Act & Assert
        RoomId roomId = RoomId.New();

        Assert.Throws<ArgumentNullException>(
            () => new Room(roomId, null!));
    }

    [Fact]
    public void Constructor_WithValidValues_CreatesRoom()
    {
        // Arrange
        RoomId roomId = RoomId.New();
        RoomNumber roomNumber = new RoomNumber("101");

        // Act
        var room = new Room(roomId, roomNumber);

        // Assert
        Assert.Same(roomId, room.Id);
        Assert.Same(roomNumber, room.RoomNumber);
    }


    [Fact]
    public void Constructor_WithNullId_ThrowsArgumentNullException()
    {
        // Arrange
        RoomNumber roomNumber = new RoomNumber("101");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => new Room(null!, roomNumber));
    }

}
