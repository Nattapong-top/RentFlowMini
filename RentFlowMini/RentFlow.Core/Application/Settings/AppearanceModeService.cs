namespace RentFlowApp.Application.Settings;

public sealed record AppearanceModeSaveResult(bool NeedsRestart, string Message);

// โหมดสีของ WinForms ตั้งได้ตอนเปิดโปรแกรมเท่านั้น จึงแยกโหมดที่ใช้อยู่ออกจากโหมดที่บันทึกล่าสุด
public sealed class AppearanceModeService
{
    private readonly IAppearanceSettingsRepository _repository;

    public AppearanceModeService(IAppearanceSettingsRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);

        _repository = repository;
        AppliedMode = repository.Load();
        SavedMode = AppliedMode;
    }

    public AppearanceMode AppliedMode { get; }

    public AppearanceMode SavedMode { get; private set; }

    public AppearanceModeSaveResult Save(AppearanceMode mode)
    {
        if (mode != SavedMode)
        {
            _repository.Save(mode);
            SavedMode = mode;
        }

        return mode == AppliedMode
            ? new AppearanceModeSaveResult(false, "บันทึกโหมดสีเรียบร้อยแล้ว")
            : new AppearanceModeSaveResult(true, "บันทึกโหมดสีเรียบร้อยแล้ว โหมดใหม่จะมีผลเมื่อเปิดโปรแกรมครั้งถัดไป");
    }
}
