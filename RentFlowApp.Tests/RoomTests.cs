
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


    [Fact]
    public void Equals_WithSameId_ReturnsTrue()
    {
        // Arrange
        RoomId roomId = RoomId.New();

        var first = new Room(
            roomId,
            new RoomNumber("101"));

        var second = new Room(
            roomId,
            new RoomNumber("102"));

        // Act & Assert
        Assert.True(first.Equals(second));
    }


    [Fact]
    public void Equals_WithSameIdAsObject_ReturnsTrue()
    {
        // Arrange
        RoomId roomId = RoomId.New();

        var first = new Room(
            roomId,
            new RoomNumber("101"));

        object second = new Room(
            roomId,
            new RoomNumber("102"));

        // Act & Assert
        Assert.True(first.Equals(second));
    }

    [Fact]
    public void Equals_WithDifferentIdAsObject_ReturnsFalse()
    {
        // Arrange
        var first = new Room(
            RoomId.New(),
            new RoomNumber("101"));

        object second = new Room(
            RoomId.New(),
            new RoomNumber("101"));

        // Act & Assert
        Assert.False(first.Equals(second));
    }

    [Fact]
    public void GetHashCode_WithSameId_ReturnsSameHashCode()
    {
        // Arrange
        RoomId roomId = RoomId.New();

        var first = new Room(
            roomId,
            new RoomNumber("101"));

        var second = new Room(
            roomId,
            new RoomNumber("102"));

        // Act
        int firstHashCode = first.GetHashCode();
        int secondHashCode = second.GetHashCode();

        // Assert
        Assert.Equal(firstHashCode, secondHashCode);
    }


    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        // Arrange
        var room = new Room(
            RoomId.New(),
            new RoomNumber("101"));

        // Act
        bool result = room.Equals((Room?)null);

        // Assert
        Assert.False(result);
    }


    [Fact]
    public void Equals_WithSameId_HasConsistentHashCode()
    {
        // Arrange
        RoomId roomId = RoomId.New();

        var first = new Room(
            roomId,
            new RoomNumber("101"));

        var second = new Room(
            RoomId.From(roomId.Value),
            new RoomNumber("102"));

        // Act
        bool areEqual = first.Equals(second);
        int firstHashCode = first.GetHashCode();
        int secondHashCode = second.GetHashCode();

        // Assert
        Assert.True(areEqual);
        Assert.Equal(firstHashCode, secondHashCode);
    }




}
