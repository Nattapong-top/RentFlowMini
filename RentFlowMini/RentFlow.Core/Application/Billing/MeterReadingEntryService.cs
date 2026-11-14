using RentFlowApp.Application.Rooms;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Application.Billing;

public sealed class MeterReadingEntryService
{
    private readonly IRoomRepository _rooms;
    private readonly IMonthlyMeterReadingRepository _readings;

    public MeterReadingEntryService(IRoomRepository rooms, IMonthlyMeterReadingRepository readings)
    {
        ArgumentNullException.ThrowIfNull(rooms);
        ArgumentNullException.ThrowIfNull(readings);
        _rooms = rooms;
        _readings = readings;
    }

    // ห้องที่บันทึกรอบนี้แล้วไม่ต้องให้เลือกซ้ำ ยกเว้นห้องที่ผู้ใช้เปิดจากตารางเพื่อแก้ไข
    public IReadOnlyList<MeterRoomOption> GetRoomsToRecord(BillingPeriod? billingPeriod, RoomId? forceIncludeRoomId = null)
    {
        // ดึงห้องที่บันทึกรอบนี้แล้วครั้งเดียว แทนการถามที่เก็บข้อมูลทีละห้อง
        var recordedRoomIds = billingPeriod is null
            ? []
            : _readings.GetAll()
                .Where(reading => reading.BillingPeriod == billingPeriod)
                .Select(reading => reading.RoomId)
                .ToHashSet();

        return _rooms.GetAll()
            .Where(room => room.Id == forceIncludeRoomId || !recordedRoomIds.Contains(room.Id))
            .Select(room => new MeterRoomOption(room.Id, room.RoomNumber.Value))
            .ToList();
    }

    public IReadOnlyList<MeterReadingListItem> GetReadings(BillingPeriod? billingPeriod)
    {
        var readings = _readings.GetAll()
            .Where(reading => billingPeriod is null || reading.BillingPeriod == billingPeriod)
            .GroupBy(reading => new { reading.RoomId, reading.BillingPeriod })
            .Select(group => group.Last());
        var roomNumbers = _rooms.GetAll().ToDictionary(room => room.Id, room => room.RoomNumber.Value);

        return readings
            .OrderBy(reading => roomNumbers.GetValueOrDefault(reading.RoomId, ""), RoomNumber.DisplayOrder)
            .Select((reading, index) => new MeterReadingListItem(
                RoomId: reading.RoomId,
                Sequence: index + 1,
                RoomNumber: roomNumbers.GetValueOrDefault(reading.RoomId, "-"),
                BillingPeriod: reading.BillingPeriod,
                PreviousWater: reading.PreviousWaterMeter.Value,
                CurrentWater: reading.CurrentWaterMeter.Value,
                WaterUsed: reading.CurrentWaterMeter.Value - reading.PreviousWaterMeter.Value,
                PreviousElectricity: reading.PreviousElectricityMeter.Value,
                CurrentElectricity: reading.CurrentElectricityMeter.Value,
                ElectricityUsed: reading.CurrentElectricityMeter.Value - reading.PreviousElectricityMeter.Value))
            .ToList();
    }

    public MeterEntryDefaults GetEntryDefaults(RoomId roomId, BillingPeriod billingPeriod)
    {
        ArgumentNullException.ThrowIfNull(roomId);
        ArgumentNullException.ThrowIfNull(billingPeriod);

        var existing = _readings.Get(roomId, billingPeriod);
        if (existing is not null)
        {
            return new MeterEntryDefaults(
                existing.PreviousWaterMeter.Value,
                existing.PreviousElectricityMeter.Value,
                existing.CurrentWaterMeter.Value,
                existing.CurrentElectricityMeter.Value,
                IsPreviousLocked: true);
        }

        // เลขก่อนหน้าของรอบนี้คือเลขปัจจุบันของรอบก่อน จึงล็อกไว้ไม่ให้พิมพ์ทับจนเลขสองเดือนไม่ต่อกัน
        var previous = _readings.Get(roomId, billingPeriod.GetPrevious());
        if (previous is not null)
        {
            return new MeterEntryDefaults(
                previous.CurrentWaterMeter.Value,
                previous.CurrentElectricityMeter.Value,
                null,
                null,
                IsPreviousLocked: true);
        }

        // ไม่มีประวัติเลย เช่น เริ่มใช้งานครั้งแรก ผู้ใช้ต้องพิมพ์เลขก่อนหน้าเอง
        return new MeterEntryDefaults(0m, 0m, null, null, IsPreviousLocked: false);
    }
}
