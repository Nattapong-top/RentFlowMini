using System.Runtime.InteropServices;

namespace RentFlowApp.Tests.UI;

// เทสต์ UI ต้องเปิดฟอร์มจริง ถ้าไม่ซ่อน หน้าต่างจะบังจอและแย่งโฟกัสคีย์บอร์ดจากงานที่ผู้ใช้ทำอยู่
internal static class HiddenTestWindows
{
    // ฟอร์มยังเปิดและจัด layout ตามปกติ แต่โปร่งใสทั้งหมดและไม่ขึ้นใน taskbar
    // วางไว้นอกจอด้วย เพราะ popup ของฟอร์ม เช่น รายการของ ComboBox ไม่โปร่งใสตามฟอร์ม
    public static void ShowHidden(Form form)
    {
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(-32000, -32000);
        form.Opacity = 0;
        form.ShowInTaskbar = false;
        form.Show();
    }

    // กันไม่ให้หน้าต่างที่ thread นี้สร้างกลายเป็นหน้าต่างที่ active ซึ่งจะดึงโฟกัสคีย์บอร์ดไป
    public static IDisposable BlockActivation()
    {
        return new ActivationBlock();
    }

    private sealed class ActivationBlock : IDisposable
    {
        private const int CbtHook = 5;
        private const int CbtActivate = 5;

        // เก็บ delegate ไว้ใน field กัน GC เก็บไปก่อนที่ Windows จะเรียกใช้
        private readonly HookProc _proc;
        private readonly IntPtr _hook;

        public ActivationBlock()
        {
            _proc = (code, wParam, lParam) =>
                code == CbtActivate ? 1 : CallNextHookEx(IntPtr.Zero, code, wParam, lParam);
            _hook = SetWindowsHookEx(CbtHook, _proc, IntPtr.Zero, GetCurrentThreadId());
        }

        private delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

        public void Dispose()
        {
            if (_hook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hook);
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int hookId, HookProc proc, IntPtr module, uint threadId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hook);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();
    }
}
