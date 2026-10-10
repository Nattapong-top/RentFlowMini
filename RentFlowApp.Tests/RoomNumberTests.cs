
using RentFlowApp.Domain.ValueObjects;
using Xunit;

namespace RentFlowApp.Tests;

public class RoomNumberTests
{
    [Fact]
    public void Constructor_WithWhitespaceAroundRoomNumber_TrimsWhitespace()
    {
        // Arrange
        string input = " 101 ";

        // Act
        RoomNumber roomNumber = new RoomNumber(input);

        // Assert
        Assert.Equal("101", roomNumber.Value);
        Assert.Equal("101", roomNumber.ToString());
    }

    [Fact]
    public void Constructor_WithWhitespaceOnly_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new RoomNumber("   "));
    }

    [Fact]
    public void Constructor_WithInternalSpace_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new RoomNumber("A 101"));
    }

    [Fact]
    public void Constructor_WithNull_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new RoomNumber(null!));
    }
}
