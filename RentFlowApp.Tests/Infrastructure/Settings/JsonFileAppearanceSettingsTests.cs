using RentFlowApp.Application.Settings;
using RentFlowApp.Infrastructure.Settings;

namespace RentFlowApp.Tests.Infrastructure.Settings;

public sealed class JsonFileAppearanceSettingsTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"RentFlowTests-{Guid.NewGuid():N}");

    private string FilePath => Path.Combine(_directory, JsonFileAppearanceSettings.FileName);

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void Load_WhenFileDoesNotExist_FollowsWindows()
    {
        var settings = new JsonFileAppearanceSettings(FilePath);

        Assert.Equal(AppearanceMode.FollowWindows, settings.Load());
        Assert.False(File.Exists(FilePath));
    }

    [Theory]
    [InlineData(AppearanceMode.Light)]
    [InlineData(AppearanceMode.Dark)]
    [InlineData(AppearanceMode.FollowWindows)]
    public void Save_KeepsModeAfterReopening(AppearanceMode mode)
    {
        new JsonFileAppearanceSettings(FilePath).Save(mode);

        Assert.Equal(mode, new JsonFileAppearanceSettings(FilePath).Load());
        Assert.False(File.Exists(FilePath + ".tmp"));
    }

    [Fact]
    public void Save_ReplacesPreviousMode()
    {
        var settings = new JsonFileAppearanceSettings(FilePath);

        settings.Save(AppearanceMode.Dark);
        settings.Save(AppearanceMode.Light);

        Assert.Equal(AppearanceMode.Light, settings.Load());
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{\"Mode\":\"Sepia\"}")]
    [InlineData("{\"Mode\":\"99\"}")]
    [InlineData("{}")]
    [InlineData("null")]
    public void Load_WhenFileIsUnreadable_FollowsWindows(string content)
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(FilePath, content);

        Assert.Equal(AppearanceMode.FollowWindows, new JsonFileAppearanceSettings(FilePath).Load());
    }

    [Fact]
    public void Save_WhenFileCannotBeWritten_ThrowsThaiMessage()
    {
        // ใช้โฟลเดอร์ชื่อเดียวกับไฟล์ เพื่อให้เขียนทับไม่ได้
        Directory.CreateDirectory(FilePath);

        var ex = Assert.Throws<InvalidOperationException>(
            () => new JsonFileAppearanceSettings(FilePath).Save(AppearanceMode.Dark));

        Assert.StartsWith("ไม่สามารถบันทึกโหมดการแสดงผลได้", ex.Message);
    }

    [Fact]
    public void Options_StartWithFollowWindowsAndCoverEveryMode()
    {
        Assert.Equal(AppearanceMode.FollowWindows, AppearanceModeOption.All[0].Mode);
        Assert.Equal(Enum.GetValues<AppearanceMode>(), AppearanceModeOption.All.Select(o => o.Mode).Order());
    }
}
