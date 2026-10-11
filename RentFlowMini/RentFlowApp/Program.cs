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

        IRoomRepository roomRepository =
            new InMemoryRoomRepository();

        var createRoomService =
            new CreateRoomService(roomRepository);

        System.Windows.Forms.Application.Run(
            new RoomForm(createRoomService, roomRepository));
    }
}
