
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp
{
    public class Room : IEquatable<Room>
    {
        public RoomId Id { get; }
        public RoomNumber RoomNumber { get; }
        public Money MonthlyRent { get; private set; }
        public bool IsCableTvExempt { get; }

        public Room(RoomId id, RoomNumber roomNumber)
            : this(id, roomNumber, Money.Create(0), false)
        {
        }

        public Room(
            RoomId id,
            RoomNumber roomNumber,
            Money monthlyRent,
            bool isCableTvExempt)
        {
            ArgumentNullException.ThrowIfNull(id);
            ArgumentNullException.ThrowIfNull(roomNumber);
            ArgumentNullException.ThrowIfNull(monthlyRent);

            Id = id;
            RoomNumber = roomNumber;
            MonthlyRent = monthlyRent;
            IsCableTvExempt = isCableTvExempt;
        }

        public void ChangeMonthlyRent(Money monthlyRent)
        {
            ArgumentNullException.ThrowIfNull(monthlyRent);
            MonthlyRent = monthlyRent;
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
