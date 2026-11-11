namespace RentFlowApp.Domain.ValueObjects;

public sealed record RoomId
{
    public Guid Value { get; }

    private RoomId(Guid value)
        {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Room ID cannot be empty.",
                nameof(value));
        }

        Value = value;
    }

    public static RoomId New()
    {
        return new RoomId(Guid.NewGuid());
    }

    public static RoomId From(Guid value)
    {
        return new RoomId(value);
    }
}
