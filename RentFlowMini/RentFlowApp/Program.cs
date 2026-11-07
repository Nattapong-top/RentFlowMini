using Microsoft.Data.Sqlite;
using RentFlowApp.Application.Billing;
using RentFlowApp.Application.Rooms;
using RentFlowApp.Application.Tenants;
using RentFlowApp.Forms;
using RentFlowApp.Infrastructure.Sqlite;

namespace RentFlowApp;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // กล่อง error มาตรฐานของ WinForms แสดง stack trace ให้ผู้ใช้เห็น จึงต้องดักไว้ก่อน
        System.Windows.Forms.Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        System.Windows.Forms.Application.ThreadException += (_, e) =>
        {
            System.Diagnostics.Trace.TraceError(e.Exception.ToString());
            MessageBox.Show(
                BaseTabControl.UnexpectedErrorMessage,
                "RentFlow",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        };

        RoomForm mainForm;
        try
        {
            var storage = SqliteAppStorage.Open(GetDataFolder());

            if (storage.SettingsImportFailed)
            {
                MessageBox.Show(
                    "ไม่สามารถอ่านไฟล์ตั้งค่าเดิมได้ ระบบจะใช้ค่าเริ่มต้นชั่วคราว กรุณาตรวจสอบข้อมูลในแท็บตั้งค่าพื้นฐาน ไฟล์เดิมยังเก็บไว้ที่เดิมโดยไม่มีการแก้ไข",
                    "RentFlow",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            mainForm = new RoomForm(
                new CreateRoomService(storage.Rooms),
                storage.Rooms,
                new CreateMonthlyBillService(storage.Bills),
                new CreateTenantService(storage.Tenants),
                storage.Tenants,
                storage.MeterReadings,
                storage.Bills,
                billingSettingsRepository: storage.Settings,
                invoiceSettingsRepository: storage.Settings);
        }
        catch (InvalidOperationException ex)
        {
            ShowStartupError(ex, ex.Message);
            return;
        }
        // แท็บบางแท็บอ่านข้อมูลตั้งแต่ตอนสร้าง ถ้าไม่ดักตรงนี้แอปจะปิดไปโดยไม่มีข้อความบอกผู้ใช้
        catch (SqliteException ex)
        {
            ShowStartupError(ex, SqliteAppStorage.DescribeOpenFailure(ex));
            return;
        }

        System.Windows.Forms.Application.Run(mainForm);
    }

    private static void ShowStartupError(Exception exception, string message)
    {
        System.Diagnostics.Trace.TraceError(exception.ToString());
        MessageBox.Show(message, "RentFlow", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    // เก็บนอกโฟลเดอร์โปรเจ็กต์ เพื่อไม่ให้ข้อมูลหอพัก ผู้เช่า และเลขบัญชีติดไปกับ git
    private static string GetDataFolder() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RentFlow");
}
