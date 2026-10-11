using RentFlowApp.Application.Rooms;
using RentFlowApp.Forms;
using RentFlowApp.Infrastructure.Rooms;

namespace RentFlowApp;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        const string connectionString =
            @"Server=localhost\SQLEXPRESS;Database=RentFlowDb;Integrated Security=True;TrustServerCertificate=True;";

        IRoomRepository roomRepository =
            new SqlServerRoomRepository(connectionString);

        var createRoomService =
            new CreateRoomService(roomRepository);

        System.Windows.Forms.Application.Run(
            new RoomForm(createRoomService, roomRepository));
    }
}