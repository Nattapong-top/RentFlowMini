using RentFlowApp.Application.Billing;
using RentFlowApp.Application.Rooms;
using RentFlowApp.Application.Tenants;
using RentFlowApp.Forms;
using RentFlowApp.Infrastructure.Billing;
using RentFlowApp.Infrastructure.Rooms;
using RentFlowApp.Infrastructure.Tenants;

namespace RentFlowApp;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        const string defaultConnectionString =
            @"Server=.\SQLEXPRESS;Database=RentFlowDb;Integrated Security=True;Encrypt=False;TrustServerCertificate=True;";

        string connectionString =
            Environment.GetEnvironmentVariable("RENTFLOW_CONNECTION_STRING")
            ?? defaultConnectionString;

        IRoomRepository roomRepository =
            new SqlServerRoomRepository(connectionString);
        IBillRepository billRepository =
            new SqlServerBillRepository(connectionString);
        ITenantRepository tenantRepository =
            new SqlServerTenantRepository(connectionString);

        var createRoomService =
            new CreateRoomService(roomRepository);
        var createMonthlyBillService =
            new CreateMonthlyBillService(billRepository);
        var createTenantService =
            new CreateTenantService(tenantRepository);

        System.Windows.Forms.Application.Run(
            new RoomForm(
                createRoomService,
                roomRepository,
                createMonthlyBillService,
                createTenantService,
                tenantRepository));
    }
}
