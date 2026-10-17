using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Domain;

public sealed class Tenant : IEquatable<Tenant>
{
    public TenantId Id { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public string? PhoneNumber { get; }
    public RoomId? RoomId { get; }

    public Tenant(
        TenantId id,
        string firstName,
        string lastName,
        string? phoneNumber = null,
        RoomId? roomId = null)
    {
        ArgumentNullException.ThrowIfNull(id);

        Id = id;
        FirstName = NormalizeFirstName(firstName);
        LastName = NormalizeLastName(lastName);
        PhoneNumber = NormalizePhoneNumber(phoneNumber);
        RoomId = roomId;
    }

    public bool Equals(Tenant? other)
    {
        return other is not null && Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is Tenant other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    private static string NormalizeFirstName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new TenantFirstNameRequiredException();
        }

        return value.Trim();
    }

    private static string NormalizeLastName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new TenantLastNameRequiredException();
        }

        return value.Trim();
    }

    private static string? NormalizePhoneNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string trimmed = value.Trim();
        if (!trimmed.All(char.IsDigit) || trimmed.Length < 9 || trimmed.Length > 10)
        {
            throw new InvalidPhoneNumberException();
        }

        return trimmed;
    }
}

public sealed class TenantFirstNameRequiredException : ArgumentException
{
    public TenantFirstNameRequiredException()
        : base("ต้องระบุชื่อและนามสกุลผู้เช่า")
    {
    }
}

public sealed class TenantLastNameRequiredException : ArgumentException
{
    public TenantLastNameRequiredException()
        : base("ต้องระบุนามสกุลผู้เช่า")
    {
    }
}

public sealed class InvalidPhoneNumberException : ArgumentException
{
    public InvalidPhoneNumberException()
        : base("เบอร์โทรศัพท์ต้องเป็นตัวเลข 9-10 หลัก")
    {
    }
}

