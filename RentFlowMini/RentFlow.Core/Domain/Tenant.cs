using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Domain;

public sealed class Tenant : IEquatable<Tenant>
{
    public TenantId Id { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public string? PhoneNumber { get; }
    public RoomId? RoomId { get; }
    public DateOnly? MoveInDate { get; }
    public decimal? SecurityDeposit { get; }
    public DateOnly? MovedOutOn { get; }
    public RoomId? MovedOutFromRoomId { get; }

    public bool IsMovedOut => MovedOutOn is not null;

    public string FullName => $"{FirstName} {LastName}";

    public Tenant(
        TenantId id,
        string firstName,
        string lastName,
        string? phoneNumber = null,
        RoomId? roomId = null,
        DateOnly? moveInDate = null,
        decimal? securityDeposit = null,
        DateOnly? movedOutOn = null,
        RoomId? movedOutFromRoomId = null)
    {
        ArgumentNullException.ThrowIfNull(id);

        if (securityDeposit < 0m)
        {
            throw new InvalidSecurityDepositException();
        }

        Id = id;
        FirstName = NormalizeFirstName(firstName);
        LastName = NormalizeLastName(lastName);
        PhoneNumber = NormalizePhoneNumber(phoneNumber);
        RoomId = roomId;
        MoveInDate = moveInDate;
        SecurityDeposit = securityDeposit;
        MovedOutOn = movedOutOn;
        MovedOutFromRoomId = movedOutFromRoomId;
    }

    public Tenant WithDetails(
        string firstName,
        string lastName,
        string? phoneNumber,
        RoomId? roomId,
        DateOnly? moveInDate,
        decimal? securityDeposit)
    {
        if (IsMovedOut)
        {
            throw new InvalidOperationException("ผู้เช่าที่ย้ายออกแล้วแก้ไขข้อมูลไม่ได้");
        }

        return new Tenant(Id, firstName, lastName, phoneNumber, roomId, moveInDate, securityDeposit);
    }

    public Tenant MoveOut(DateOnly movedOutOn)
    {
        if (IsMovedOut)
        {
            throw new InvalidOperationException("ผู้เช่ารายนี้ย้ายออกแล้ว");
        }

        if (movedOutOn < MoveInDate)
        {
            throw new InvalidOperationException("วันที่ย้ายออกต้องไม่ก่อนวันเข้าอยู่");
        }

        // คืนห้องทันที โดยจำห้องเดิมไว้แสดงเป็นประวัติ
        return new Tenant(
            Id,
            FirstName,
            LastName,
            PhoneNumber,
            roomId: null,
            MoveInDate,
            SecurityDeposit,
            movedOutOn,
            movedOutFromRoomId: RoomId);
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

public sealed class InvalidSecurityDepositException : ArgumentException
{
    public InvalidSecurityDepositException()
        : base("เงินประกันต้องไม่ติดลบ")
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

