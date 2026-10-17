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

        IRoomEditor roomRepository =
            new InMemoryRoomRepository();
        IBillRepository billRepository =
            new InMemoryBillRepository();
        ITenantRepository tenantRepository =
            new InMemoryTenantRepository();

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
