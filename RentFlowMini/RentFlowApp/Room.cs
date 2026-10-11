using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp
{
    public class Room
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
    }
}
