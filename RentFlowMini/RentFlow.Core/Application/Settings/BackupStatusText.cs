using System.Globalization;
using RentFlowApp.Application.Reports;

namespace RentFlowApp.Application.Settings;

public static class BackupStatusText
{
    public static string Describe(DateTime? lastBackupTime) =>
        lastBackupTime is { } time
            ? $"สำรองล่าสุด: {ThaiCalendar.FormatDate(time)} {time.ToString("HH:mm", CultureInfo.InvariantCulture)}"
            : "ยังไม่มีไฟล์สำรอง";
}
