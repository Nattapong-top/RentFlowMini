using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp
{
    public class Room
    {
        public RoomNumber RoomNumber { get; }

        public Room(RoomNumber roomNumber)
        {
            ArgumentNullException.ThrowIfNull(roomNumber);

            RoomNumber = roomNumber;
        }
    }
}
