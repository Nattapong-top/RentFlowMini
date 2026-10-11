
using RentFlowApp;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Application.Rooms;

public class CreateRoomService
{
    private readonly IRoomRepository _repository;

    public CreateRoomService(IRoomRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public Room Create(string roomNumber)
    {
        var number = new RoomNumber(roomNumber);

        if (_repository.ExistsByRoomNumber(number.Value))
        {
            throw new InvalidOperationException(
                $"Room number '{number.Value}' already exists."
            );
        }

        var room = new Room(RoomId.New(), number);

        _repository.Add(room);

        return room;
    }
}
