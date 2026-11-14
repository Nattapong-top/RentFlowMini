using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Application.Rooms;

public sealed record RoomListItem(
    RoomId RoomId,
    int Sequence,
    string RoomNumber,
    string OccupancyStatus,
    string TenantName,
    decimal MonthlyRent,
    bool IsCableTvExempt);
