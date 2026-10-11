
using RentFlowApp.Domain.ValueObjects;

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
    }
}
