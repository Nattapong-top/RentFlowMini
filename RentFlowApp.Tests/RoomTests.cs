
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Tests;

public class RoomTests
{
    [Fact]
    public void Constructor_WithValidRoomNumber_CreatesRoom()
    {
        // Arrange
        var roomNumber = new RoomNumber("101");

        // Act
        var room = new Room(roomNumber);

        // Assert
        Assert.Same(roomNumber, room.RoomNumber);
    }

    [Fact]
    public void Constructor_WithNullRoomNumber_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => new Room(null!));
    }
}
