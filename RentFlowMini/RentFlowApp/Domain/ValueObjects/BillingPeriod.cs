using System.Globalization;

namespace RentFlowApp.Domain.ValueObjects;

public sealed record BillingPeriod
{
    private BillingPeriod(int year, int month)
    {
        Year = year;
        Month = month;
        Value = $"{year:D4}-{month:D2}";
    }

    public int Year { get; }

    public int Month { get; }

    public string Value { get; }

    public static BillingPeriod Create(string value)
    {
        int year = 0;
        int month = 0;

        bool hasValidShape =
            value.Length == 7 &&
            value[4] == '-' &&
            int.TryParse(
                value[..4],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out year) &&
            int.TryParse(
                value[5..],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out month) &&
            year > 0 &&
            month is >= 1 and <= 12;

        if (!hasValidShape)
        {
            throw new ArgumentException(
                "รอบบิลต้องอยู่ในรูปแบบ YYYY-MM เช่น 2026-09",
                nameof(value));
        }

        return new BillingPeriod(year, month);
    }

    public override string ToString()
    {
        return Value;
    }
}
