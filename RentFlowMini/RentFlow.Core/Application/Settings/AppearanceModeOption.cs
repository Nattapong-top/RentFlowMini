namespace RentFlowApp.Application.Settings;

public sealed record AppearanceModeOption(AppearanceMode Mode, string Label)
{
    // ตามระบบ Windows อยู่บนสุด เพราะเป็นค่าเริ่มต้น
    public static IReadOnlyList<AppearanceModeOption> All { get; } =
    [
        new(AppearanceMode.FollowWindows, "ตามระบบ Windows"),
        new(AppearanceMode.Light, "โหมดสว่าง"),
        new(AppearanceMode.Dark, "โหมดมืด")
    ];

    public override string ToString() => Label;
}
