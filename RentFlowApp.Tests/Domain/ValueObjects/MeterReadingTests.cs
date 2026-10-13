using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Tests.Domain.ValueObjects;

public class MeterReadingTests
{
    [Fact]
    public void Create_WithNonNegativeValue_ReturnsMeterReading()
    {
        var reading = MeterReading.Create(120m);

        Assert.Equal(120m, reading.Value);
    }

    [Fact]
    public void Create_WhenValueIsNegative_ThrowsInvalidMeterReadingException()
    {
        Assert.Throws<InvalidMeterReadingException>(() =>
            MeterReading.Create(-1m));
    }
}
