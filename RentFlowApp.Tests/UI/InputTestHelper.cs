using System.Reflection;

namespace RentFlowApp.Tests.UI;

internal static class InputTestHelper
{
    private const BindingFlags ProtectedInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    public static void RunInSta(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var activationBlock = HiddenTestWindows.BlockActivation();
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception != null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }

    // แท็บที่ไม่ได้เลือกจะซ่อนตัวควบคุมไว้ ทำให้ grid ไม่เลือกแถวและปุ่มไม่เปลี่ยนสถานะเหมือนตอนผู้ใช้เปิดแท็บนั้น
    public static TabPage SelectTab(Form form, string tabPageName)
    {
        var tabs = form.Controls.OfType<TabControl>().First();
        var page = tabs.TabPages[tabPageName]!;
        tabs.SelectedTab = page;
        return page;
    }

    public static void DoubleClickRow(DataGridView grid, int rowIndex)
    {
        typeof(DataGridView).GetMethod("OnCellDoubleClick", ProtectedInstance)!
            .Invoke(grid, [new DataGridViewCellEventArgs(1, rowIndex)]);
    }

    public static void PressEnter(Control control)
    {
        typeof(Control).GetMethod("OnKeyDown", ProtectedInstance)!
            .Invoke(control, [new KeyEventArgs(Keys.Enter)]);
    }

    public static bool IsKeyBlocked(Control control, char key)
    {
        var args = new KeyPressEventArgs(key);
        typeof(Control).GetMethod("OnKeyPress", ProtectedInstance)!.Invoke(control, [args]);
        return args.Handled;
    }
}
