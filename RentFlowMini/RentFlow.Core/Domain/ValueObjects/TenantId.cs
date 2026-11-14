namespace RentFlowApp.Domain.ValueObjects;

public sealed record TenantId
{
    public Guid Value { get; }

    private TenantId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "รหัสผู้เช่าต้องไม่เป็นค่าว่าง",
                nameof(value));
        }

        Value = value;
    }

    public static TenantId New()
    {
        return new TenantId(Guid.NewGuid());
    }

    public static TenantId From(Guid value)
    {
        return new TenantId(value);
    }
}
