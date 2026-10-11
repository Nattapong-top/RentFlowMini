using System;
using System.Collections.Generic;
using System.Text;

namespace RentFlowApp.Application.Rooms;

public interface IRoomRepository
{
    void Add(Room room);

    bool ExistsByRoomNumber(string roomNumber);

    IReadOnlyList<Room> GetAll();
}
