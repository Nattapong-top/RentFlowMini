namespace RentFlowApp.Domain.Billing;

public sealed class InvalidMeterReadingException : ArgumentException
{
    public InvalidMeterReadingException(string meterType, string reason)
        : base($"ข้อมูล{meterType}ไม่ถูกต้อง: {reason}")
    {
    }
}
