
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
        return Create(roomNumber, 0, false);
    }

    public Room Create(
        string roomNumber,
        decimal monthlyRent,
        bool isCableTvExempt)
    {
        var number = new RoomNumber(roomNumber);
        var rent = Money.Create(monthlyRent);

        if (_repository.ExistsByRoomNumber(number.Value))
        {
            throw new InvalidOperationException(
                $"Room number '{number.Value}' already exists."
            );
        }

        var room = new Room(
            RoomId.New(),
            number,
            rent,
            isCableTvExempt);

        _repository.Add(room);

        return room;
    }
}
