using System.Reflection;

namespace RentFlowApp.Tests.UI;

internal static class InputTestHelper
{
    private const BindingFlags ProtectedInstance = BindingFlags.Instance | BindingFlags.NonPublic;

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
