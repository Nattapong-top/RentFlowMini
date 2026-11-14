using System.Text.Json;
using RentFlowApp.Application.Settings;

namespace RentFlowApp.Infrastructure.Settings;

// โหมดการแสดงผลเป็นค่าของเครื่องที่เปิดโปรแกรม จึงเก็บแยกจากฐานข้อมูล และไม่ติดไปกับไฟล์สำรองหรือการกู้คืน
public sealed class JsonFileAppearanceSettings : IAppearanceSettingsRepository
{
    public const string FileName = "appearance.json";

    private const string SaveFailedMessage =
        "ไม่สามารถบันทึกโหมดการแสดงผลได้ กรุณาตรวจสอบสิทธิ์การเข้าถึงโฟลเดอร์แล้วลองอีกครั้ง";

    private readonly string _filePath;

    public JsonFileAppearanceSettings(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = filePath;
    }

    // อ่านไม่ได้ไม่ควรกันไม่ให้เปิดโปรแกรม จึงกลับไปใช้ค่าเริ่มต้นคือตามระบบ Windows
    public AppearanceMode Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return AppearanceMode.FollowWindows;
            }

            var file = JsonSerializer.Deserialize<AppearanceFile>(File.ReadAllText(_filePath));
            return file is not null
                && Enum.TryParse<AppearanceMode>(file.Mode, ignoreCase: false, out var mode)
                && Enum.IsDefined(mode)
                ? mode
                : AppearanceMode.FollowWindows;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            System.Diagnostics.Trace.TraceWarning(ex.ToString());
            return AppearanceMode.FollowWindows;
        }
    }

    public void Save(AppearanceMode mode)
    {
        if (!Enum.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }

        var tempPath = _filePath + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_filePath))!);
            File.WriteAllText(tempPath, JsonSerializer.Serialize(new AppearanceFile(mode.ToString())));
            // เขียนไฟล์ชั่วคราวก่อนแล้วค่อยแทนที่ เพื่อไม่ให้เหลือไฟล์ที่เขียนไม่จบถ้าเครื่องดับกลางคัน
            File.Move(tempPath, _filePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException(SaveFailedMessage, ex);
        }
    }

    private sealed record AppearanceFile(string Mode);
}
