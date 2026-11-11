using System;
using System.Collections.Generic;
using System.Text;
using RentFlowApp.Domain;

namespace RentFlowApp.Application.Rooms;

public interface IRoomRepository
{
    void Add(Room room);

    bool ExistsByRoomNumber(string roomNumber);

    IReadOnlyList<Room> GetAll();
}
