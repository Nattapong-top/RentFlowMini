using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Tests.Domain;

public class TenantIdTests
{
    [Fact]
    public void New_CreatesNonEmptyId()
    {
        var tenantId = TenantId.New();

        Assert.NotEqual(Guid.Empty, tenantId.Value);
    }

    [Fact]
    public void FromEmptyGuid_ThrowsException()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => TenantId.From(Guid.Empty));

        Assert.StartsWith("รหัสผู้เช่าต้องไม่เป็นค่าว่าง", exception.Message);
    }
}
