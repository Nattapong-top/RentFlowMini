
using RentFlowApp.Domain.ValueObjects;
using Xunit;

namespace RentFlowApp
{
    public class Room : IEquatable<Room>
    {
        public RoomId Id { get; }
        public RoomNumber RoomNumber { get; }

        public Room(RoomId id, RoomNumber roomNumber)
        {
            ArgumentNullException.ThrowIfNull(id);
            ArgumentNullException.ThrowIfNull(roomNumber);

            Id = id;
            RoomNumber = roomNumber;
        }

        public bool Equals(Room? other)
        {
            if (other is null)
            {
                return false;
            }

            return Id == other.Id;
        }

        public override bool Equals(object? obj)
        {
            return obj is Room other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
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

    }
}
