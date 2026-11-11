namespace RentFlowApp;

// แยกจาก AppInfo เพราะ Icon เป็นของ WinForms และไฟล์ไอคอนฝังอยู่ใน exe ไม่ใช่ใน RentFlow.Core
public static class AppIcon
{
    // ไฟล์ไอคอนเดียวกับที่ฝังใน exe ใช้กับหน้าต่างเพื่อให้ taskbar และหัวหน้าต่างตรงกับ shortcut
    public static Icon Load()
    {
        using var stream = typeof(AppIcon).Assembly.GetManifestResourceStream("RentFlowApp.app.ico")
            ?? throw new InvalidOperationException("ไม่พบไฟล์ไอคอนของโปรแกรม");
        return new Icon(stream);
    }
}
