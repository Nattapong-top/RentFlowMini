using RentFlowApp.Application.Settings;

namespace RentFlowApp.Tests.Application.Settings;

public class AppearanceModeServiceTests
{
    private sealed class FakeAppearanceSettings(AppearanceMode stored) : IAppearanceSettingsRepository
    {
        public AppearanceMode Stored { get; private set; } = stored;
        public int SaveCount { get; private set; }
        public bool FailOnSave { get; init; }

        public AppearanceMode Load() => Stored;

        public void Save(AppearanceMode mode)
        {
            if (FailOnSave)
            {
                throw new InvalidOperationException("บันทึกไม่ได้");
            }

            Stored = mode;
            SaveCount++;
        }
    }

    [Fact]
    public void Constructor_UsesStoredModeAsAppliedAndSavedMode()
    {
        var service = new AppearanceModeService(new FakeAppearanceSettings(AppearanceMode.Dark));

        Assert.Equal(AppearanceMode.Dark, service.AppliedMode);
        Assert.Equal(AppearanceMode.Dark, service.SavedMode);
    }

    [Fact]
    public void Save_DifferentFromAppliedMode_StoresModeAndNeedsRestart()
    {
        var repository = new FakeAppearanceSettings(AppearanceMode.FollowWindows);
        var service = new AppearanceModeService(repository);

        var result = service.Save(AppearanceMode.Dark);

        Assert.True(result.NeedsRestart);
        Assert.Contains("เปิดโปรแกรมครั้งถัดไป", result.Message);
        Assert.Equal(AppearanceMode.Dark, repository.Stored);
        Assert.Equal(AppearanceMode.Dark, service.SavedMode);
        Assert.Equal(AppearanceMode.FollowWindows, service.AppliedMode);
    }

    [Fact]
    public void Save_BackToAppliedMode_StoresModeWithoutRestart()
    {
        var repository = new FakeAppearanceSettings(AppearanceMode.FollowWindows);
        var service = new AppearanceModeService(repository);
        service.Save(AppearanceMode.Dark);

        var result = service.Save(AppearanceMode.FollowWindows);

        Assert.False(result.NeedsRestart);
        Assert.Equal("บันทึกโหมดสีเรียบร้อยแล้ว", result.Message);
        Assert.Equal(AppearanceMode.FollowWindows, repository.Stored);
    }

    [Fact]
    public void Save_SameAsSavedMode_DoesNotWriteAgain()
    {
        var repository = new FakeAppearanceSettings(AppearanceMode.Light);
        var service = new AppearanceModeService(repository);

        var result = service.Save(AppearanceMode.Light);

        Assert.False(result.NeedsRestart);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public void Save_WhenStoreFails_KeepsSavedMode()
    {
        var service = new AppearanceModeService(
            new FakeAppearanceSettings(AppearanceMode.Light) { FailOnSave = true });

        Assert.Throws<InvalidOperationException>(() => service.Save(AppearanceMode.Dark));
        Assert.Equal(AppearanceMode.Light, service.SavedMode);
    }
}
