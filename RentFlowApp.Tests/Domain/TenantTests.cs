using RentFlowApp.Domain;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Tests.Domain;

public class TenantTests
{
    [Fact]
    public void Create_WithValidData_StoresTenantData()
    {
        var tenantId = TenantId.New();
        var roomId = RoomId.New();

        var tenant = new Tenant(
            tenantId,
            "สมชาย",
            "ใจดี",
            "0812345678",
            roomId);

        Assert.Same(tenantId, tenant.Id);
        Assert.Equal("สมชาย", tenant.FirstName);
        Assert.Equal("ใจดี", tenant.LastName);
        Assert.Equal("0812345678", tenant.PhoneNumber);
        Assert.Same(roomId, tenant.RoomId);
    }

    [Fact]
    public void Create_WithEmptyFirstName_ThrowsTenantFirstNameRequiredException()
    {
        Assert.Throws<TenantFirstNameRequiredException>(() => new Tenant(
            TenantId.New(),
            " ",
            "ใจดี"));
    }

    [Fact]
    public void Create_WithEmptyLastName_ThrowsTenantLastNameRequiredException()
    {
        Assert.Throws<TenantLastNameRequiredException>(() => new Tenant(
            TenantId.New(),
            "สมชาย",
            " "));
    }

    [Fact]
    public void Create_WithoutPhoneNumber_IsAllowed()
    {
        var tenant = new Tenant(
            TenantId.New(),
            "สมชาย",
            "ใจดี");

        Assert.Null(tenant.PhoneNumber);
    }

    [Fact]
    public void Create_WithNineDigitPhoneNumber_IsAllowed()
    {
        var tenant = new Tenant(
            TenantId.New(),
            "สมชาย",
            "ใจดี",
            "021234567");

        Assert.Equal("021234567", tenant.PhoneNumber);
    }

    [Theory]
    [InlineData("12345678")]     // 8 digits (too short)
    [InlineData("081234567890")] // 12 digits (too long)
    [InlineData("081-2345678")]  // Has hyphen
    [InlineData("abcdefghij")]   // Letters
    public void Create_WithInvalidPhoneNumber_ThrowsInvalidPhoneNumberException(string invalidPhone)
    {
        Assert.Throws<InvalidPhoneNumberException>(() => new Tenant(
            TenantId.New(),
            "สมชาย",
            "ใจดี",
            invalidPhone));
    }

    [Fact]
    public void Tenants_WithSameId_AreEqual()
    {
        var tenantId = TenantId.New();
        var first = new Tenant(tenantId, "สมชาย", "ใจดี");
        var second = new Tenant(tenantId, "สมชาย", "คนใหม่");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }
}
