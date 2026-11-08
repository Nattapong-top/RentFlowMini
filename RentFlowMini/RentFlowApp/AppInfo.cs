namespace RentFlowApp;

// ชื่อที่ผู้ใช้เห็น ใช้ที่เดียวเพื่อให้หัวหน้าต่าง หัวกล่องข้อความ และโฟลเดอร์ข้อมูลตรงกันเสมอ
public static class AppInfo
{
    public const string Name = "RentFlowMiniApp";

    // ไฟล์ไอคอนเดียวกับที่ฝังใน exe ใช้กับหน้าต่างเพื่อให้ taskbar และหัวหน้าต่างตรงกับ shortcut
    public static Icon LoadIcon()
    {
        using var stream = typeof(AppInfo).Assembly.GetManifestResourceStream("RentFlowApp.app.ico")
            ?? throw new InvalidOperationException("ไม่พบไฟล์ไอคอนของโปรแกรม");
        return new Icon(stream);
    }
}
