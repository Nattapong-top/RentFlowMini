using RentFlowApp.Application.Settings;

namespace RentFlowApp.Tests.Application.Settings;

public class BackupStatusTextTests
{
    [Fact]
    public void Describe_WhenNoBackupYet_SaysThereIsNoBackupFile()
    {
        Assert.Equal("ยังไม่มีไฟล์สำรอง", BackupStatusText.Describe(null));
    }

    [Fact]
    public void Describe_WithBackupTime_ShowsThaiDateAndTime()
    {
        Assert.Equal(
            "สำรองล่าสุด: 05/11/2569 09:07",
            BackupStatusText.Describe(new DateTime(2026, 11, 5, 9, 7, 45)));
    }
}
