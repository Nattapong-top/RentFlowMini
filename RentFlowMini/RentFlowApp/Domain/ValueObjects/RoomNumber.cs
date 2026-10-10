
namespace RentFlowApp.Domain.ValueObjects;

public sealed class RoomNumber
{
    public string Value { get; }

    public RoomNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Room number cannot be empty or whitespace.",
                nameof(value));
        }

        string normalizedValue = value.Trim();

        if (normalizedValue.Contains(' '))
        {
            throw new ArgumentException(
                "Room number cannot contain spaces.",
                nameof(value));
        }

        Value = normalizedValue;
    }

    public override string ToString()
    {
        return Value;
    }
}
