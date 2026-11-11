namespace RentFlowApp.Domain.ValueObjects;

public sealed record Money
{
    private Money(decimal amount)
    {
        Amount = amount;
    }

    public decimal Amount { get; }

    public static Money Create(decimal amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "จำนวนเงินต้องไม่ติดลบ");
        }

        return new Money(amount);
    }
}
