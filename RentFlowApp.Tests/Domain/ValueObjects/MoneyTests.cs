using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Tests.Domain.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Create_WithNonNegativeAmount_ReturnsMoney()
    {
        var money = Money.Create(2800m);

        Assert.Equal(2800m, money.Amount);
    }

    [Fact]
    public void Create_WhenAmountIsNegative_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Money.Create(-1m));
    }
}
