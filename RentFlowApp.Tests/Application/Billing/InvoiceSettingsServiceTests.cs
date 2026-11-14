using RentFlowApp.Application.Billing;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Infrastructure.Billing;

namespace RentFlowApp.Tests.Application.Billing;

public class InvoiceSettingsServiceTests
{
    [Fact]
    public void GetCurrent_WhenNothingSaved_ReturnsEmptySettings()
    {
        var service = new InvoiceSettingsService(new InMemoryInvoiceSettingsRepository());

        Assert.Equal(InvoiceSettings.Empty, service.GetCurrent());
    }

    [Fact]
    public void Save_WithValidInput_SavesTrimmedSettingsAndReturnsThem()
    {
        var repository = new InMemoryInvoiceSettingsRepository();
        var service = new InvoiceSettingsService(repository);

        var saved = service.Save(" หอพักสุขใจ ", " 99 ถนนสุขุมวิท ", " กสิกรไทย 123-4-56789-0 ");

        Assert.Equal("หอพักสุขใจ", saved.PropertyName);
        Assert.Equal("99 ถนนสุขุมวิท", saved.Address);
        Assert.Equal("กสิกรไทย 123-4-56789-0", saved.BankAccountDetails);
        Assert.Equal(saved, repository.Get());
        Assert.Equal(saved, service.GetCurrent());
    }

    // ข้อความ error ต้องแสดงให้ผู้ใช้ได้ทันที จึงต้องไม่มีท้ายข้อความ "(Parameter '...')" ที่ .NET เติมให้
    [Theory]
    [InlineData("  ", "99 ถนนสุขุมวิท", "กรุณาระบุชื่อหอพัก")]
    [InlineData("หอพักสุขใจ", "", "กรุณาระบุที่อยู่หอพัก")]
    public void Save_WithMissingRequiredField_RejectsWithPlainMessageAndKeepsOldSettings(
        string propertyName, string address, string expected)
    {
        var repository = new InMemoryInvoiceSettingsRepository();
        var service = new InvoiceSettingsService(repository);

        var exception = Assert.Throws<ArgumentException>(() => service.Save(propertyName, address, null));

        Assert.Equal(expected, exception.Message);
        Assert.Equal(InvoiceSettings.Empty, repository.Get());
    }
}
