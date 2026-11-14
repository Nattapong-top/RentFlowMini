using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Application.Billing;

public sealed record MeterRoomOption(RoomId RoomId, string RoomNumber);
