
using Xunit;

namespace RentFlowApp.Tests;

public class RoomTests
{
    [Fact]
    public void Constructor_WithValidRoomNumber_SetsRoomNumber()
    {
        // Arrange
        string roomNumber = "101";

        // Act
        Room room = new Room(roomNumber);

        // Assert
        Assert.Equal("101", room.RoomNumber);
    }
}
