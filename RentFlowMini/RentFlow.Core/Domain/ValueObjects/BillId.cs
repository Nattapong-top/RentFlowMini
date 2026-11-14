namespace RentFlowApp.Domain.ValueObjects;

public sealed record BillId
{
    public Guid Value { get; }

    private BillId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Bill ID cannot be empty.",
                nameof(value));
        }

        Value = value;
    }

    public static BillId New()
    {
        return new BillId(Guid.NewGuid());
    }

    public static BillId From(Guid value)
    {
        return new BillId(value);
    }
}
