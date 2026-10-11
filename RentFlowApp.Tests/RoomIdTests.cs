using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Tests;

public class RoomIdTests
{
    [Fact]
    public void New_CreatesRoomId()
    {
        // Act
        var roomId = RoomId.New();

        // Assert
        Assert.NotEqual(Guid.Empty, roomId.Value);
    }


    [Fact]
    public void From_WithValidGuid_CreatesRoomId()
    {
        // Arrange
        Guid value = Guid.NewGuid();

        // Act
        RoomId roomId = RoomId.From(value);

        // Assert
        Assert.Equal(value, roomId.Value);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => RoomId.From(Guid.Empty));
    }

    [Fact]
    public void Equals_WithSameValue_ReturnsTrue()
    {
        // Arrange
        Guid value = Guid.NewGuid();
        RoomId first = RoomId.From(value);
        RoomId second = RoomId.From(value);

        // Assert
        Assert.Equal(first, second);
    }

}
