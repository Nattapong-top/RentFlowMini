
namespace RentFlowApp.Domain.ValueObjects;

public sealed class RoomNumber
{
    // เรียงตามความยาวก่อน เพื่อให้ห้อง 99 มาก่อน 101 ซึ่งการเรียงแบบตัวอักษรทำไม่ได้
    public static IComparer<string> DisplayOrder { get; } = Comparer<string>.Create((left, right) =>
        left.Length != right.Length
            ? left.Length.CompareTo(right.Length)
            : string.CompareOrdinal(left, right));

    public string Value { get; }

    public RoomNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            // ไม่ส่งชื่อ parameter เพราะ .NET จะต่อท้ายข้อความด้วย "(Parameter ...)" ซึ่งผู้ใช้จะเห็นด้วย
            throw new ArgumentException("กรุณาระบุหมายเลขห้อง");
        }

        string normalizedValue = value.Trim();

        if (normalizedValue.Contains(' '))
        {
            throw new ArgumentException("หมายเลขห้องต้องไม่มีช่องว่าง กรุณาระบุใหม่อีกครั้ง");
        }

        Value = normalizedValue;
    }

    public override string ToString()
    {
        return Value;
    }
}
