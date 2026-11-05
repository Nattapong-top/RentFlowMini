using RentFlowApp.Domain.Billing;

namespace RentFlowApp.Tests.Domain.Billing;

public class InvoiceSettingsTests
{
    [Fact]
    public void Create_TrimsValuesAndAllowsEmptyBankAccount()
    {
        var settings = InvoiceSettings.Create("  หอทดสอบ ", " ที่อยู่ทดสอบ ", null);

        Assert.Equal("หอทดสอบ", settings.PropertyName);
        Assert.Equal("ที่อยู่ทดสอบ", settings.Address);
        Assert.Equal(string.Empty, settings.BankAccountDetails);
    }

    [Fact]
    public void IsConfigured_IsFalseForEmptySettingsAndTrueAfterCreate()
    {
        Assert.False(InvoiceSettings.Empty.IsConfigured);
        Assert.True(InvoiceSettings.Create("หอทดสอบ", "ที่อยู่ทดสอบ", null).IsConfigured);
    }

    [Theory]
    [InlineData(" ", "ที่อยู่ทดสอบ", "ชื่อหอพัก")]
    [InlineData("หอทดสอบ", " ", "ที่อยู่หอพัก")]
    public void Create_WhenRequiredValueIsMissing_ThrowsThaiMessage(string name, string address, string expectedMessage)
    {
        var exception = Assert.Throws<ArgumentException>(() => InvoiceSettings.Create(name, address, null));

        Assert.Contains(expectedMessage, exception.Message);
    }
}
