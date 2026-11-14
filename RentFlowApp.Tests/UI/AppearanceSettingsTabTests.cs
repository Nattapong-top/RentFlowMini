using RentFlowApp.Application.Billing;
using RentFlowApp.Application.Settings;
using RentFlowApp.Forms;
using RentFlowApp.Infrastructure.Billing;
using static RentFlowApp.Tests.UI.InputTestHelper;

namespace RentFlowApp.Tests.UI;

public class AppearanceSettingsTabTests
{
    private sealed class FakeAppearanceSettings(AppearanceMode stored) : IAppearanceSettingsRepository
    {
        public AppearanceMode Stored { get; private set; } = stored;
        public Exception? SaveError { get; init; }

        public AppearanceMode Load() => Stored;

        public void Save(AppearanceMode mode)
        {
            if (SaveError is not null)
            {
                throw SaveError;
            }

            Stored = mode;
        }
    }

    private sealed class RestartSpy(bool answer)
    {
        public int ConfirmCount { get; private set; }
        public int RestartCount { get; private set; }

        public bool Confirm()
        {
            ConfirmCount++;
            return answer;
        }

        public void Restart() => RestartCount++;
    }

    private static T Find<T>(Control parent, string name) where T : Control =>
        (T)parent.Controls.Find(name, true)[0];

    private static BillingSettingsTabControl CreateTab(
        Form form,
        IAppearanceSettingsRepository? appearance,
        RestartSpy? restart = null)
    {
        var billing = new InMemoryBillingSettingsRepository();
        restart ??= new RestartSpy(false);
        var tab = new BillingSettingsTabControl(
            billing,
            new UpdateBillingSettingsService(billing),
            new InMemoryInvoiceSettingsRepository(),
            appearanceModeService: appearance is null ? null : new AppearanceModeService(appearance),
            confirmRestart: restart.Confirm,
            restartApplication: restart.Restart);
        form.Controls.Add(tab);
        HiddenTestWindows.ShowHidden(form);
        return tab;
    }

    private static void Select(BillingSettingsTabControl tab, AppearanceMode mode)
    {
        var comboBox = Find<ComboBox>(tab, "AppearanceModeComboBox");
        comboBox.SelectedItem = comboBox.Items.Cast<AppearanceModeOption>().Single(o => o.Mode == mode);
    }

    [Fact]
    public void SettingsTab_WithoutAppearanceService_HidesAppearanceGroup()
    {
        RunInSta(() =>
        {
            using var form = new Form();
            var tab = CreateTab(form, appearance: null);

            Assert.Empty(tab.Controls.Find("AppearanceGroup", true));
        });
    }

    [Fact]
    public void SettingsTab_ShowsSavedModeWithFollowWindowsFirst()
    {
        RunInSta(() =>
        {
            using var form = new Form();
            var tab = CreateTab(form, new FakeAppearanceSettings(AppearanceMode.Dark));

            var comboBox = Find<ComboBox>(tab, "AppearanceModeComboBox");
            Assert.Equal("ตามระบบ Windows", comboBox.Items[0]!.ToString());
            Assert.Equal("โหมดมืด", comboBox.Text);
        });
    }

    [Fact]
    public void SaveAppearanceMode_NewMode_SavesAndRestartsWhenConfirmed()
    {
        RunInSta(() =>
        {
            using var form = new Form();
            var appearance = new FakeAppearanceSettings(AppearanceMode.FollowWindows);
            var restart = new RestartSpy(answer: true);
            var tab = CreateTab(form, appearance, restart);

            Select(tab, AppearanceMode.Dark);
            Find<Button>(tab, "SaveAppearanceModeButton").PerformClick();

            Assert.Equal(AppearanceMode.Dark, appearance.Stored);
            Assert.Equal(1, restart.ConfirmCount);
            Assert.Equal(1, restart.RestartCount);
        });
    }

    [Fact]
    public void SaveAppearanceMode_RestartDeclined_SavesAndSaysItAppliesNextTime()
    {
        RunInSta(() =>
        {
            using var form = new Form();
            var appearance = new FakeAppearanceSettings(AppearanceMode.FollowWindows);
            var restart = new RestartSpy(answer: false);
            var tab = CreateTab(form, appearance, restart);

            Select(tab, AppearanceMode.Light);
            Find<Button>(tab, "SaveAppearanceModeButton").PerformClick();

            Assert.Equal(AppearanceMode.Light, appearance.Stored);
            Assert.Equal(0, restart.RestartCount);
            Assert.Contains("เปิดโปรแกรมครั้งถัดไป", Find<Label>(tab, "SettingsMessageLabel").Text);
        });
    }

    [Fact]
    public void SaveAppearanceMode_ModeAlreadyInUse_DoesNotAskToRestart()
    {
        RunInSta(() =>
        {
            using var form = new Form();
            var restart = new RestartSpy(answer: true);
            var tab = CreateTab(form, new FakeAppearanceSettings(AppearanceMode.Dark), restart);

            Find<Button>(tab, "SaveAppearanceModeButton").PerformClick();

            Assert.Equal(0, restart.ConfirmCount);
            Assert.Equal("บันทึกโหมดสีเรียบร้อยแล้ว", Find<Label>(tab, "SettingsMessageLabel").Text);
        });
    }

    [Fact]
    public void SaveAppearanceMode_WhenSaveFails_ShowsMessageWithoutRestart()
    {
        RunInSta(() =>
        {
            const string userMessage = "ไม่สามารถบันทึกโหมดการแสดงผลได้ กรุณาลองใหม่";
            using var form = new Form();
            var restart = new RestartSpy(answer: true);
            var appearance = new FakeAppearanceSettings(AppearanceMode.FollowWindows)
            {
                SaveError = new InvalidOperationException(userMessage)
            };
            var tab = CreateTab(form, appearance, restart);

            Select(tab, AppearanceMode.Dark);
            Find<Button>(tab, "SaveAppearanceModeButton").PerformClick();

            Assert.Equal(userMessage, Find<Label>(tab, "SettingsMessageLabel").Text);
            Assert.Equal(0, restart.ConfirmCount);
        });
    }
}
