using RentFlowApp.Application.Billing;
using RentFlowApp.Application.Rooms;
using RentFlowApp.Forms;
using RentFlowApp.Infrastructure.Billing;
using RentFlowApp.Infrastructure.Rooms;

namespace RentFlowApp;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        const string defaultConnectionString =
            @"Server=localhost\SQLEXPRESS;Database=RentFlowDb;Integrated Security=True;TrustServerCertificate=True;";

        string connectionString =
            Environment.GetEnvironmentVariable("RENTFLOW_CONNECTION_STRING")
            ?? defaultConnectionString;

        IRoomRepository roomRepository =
            new SqlServerRoomRepository(connectionString);
        IBillRepository billRepository =
            new SqlServerBillRepository(connectionString);

        var createRoomService =
            new CreateRoomService(roomRepository);
        var createMonthlyBillService =
            new CreateMonthlyBillService(billRepository);

        System.Windows.Forms.Application.Run(
            new RoomForm(createRoomService, roomRepository, createMonthlyBillService));
    }
}
