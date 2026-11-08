using RentFlowApp.Application.Rooms;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Application.Billing;

public sealed record LatestRoomDataForBill(
    decimal MonthlyRent,
    bool IsCableTvExempt,
    BillMeterReadings? Meters,
    string? Notice);

public sealed class LatestRoomDataForBillService
{
    private readonly IBillRepository _bills;
    private readonly IRoomRepository _rooms;
    private readonly IMonthlyMeterReadingRepository _readings;

    public LatestRoomDataForBillService(
        IBillRepository bills,
        IRoomRepository rooms,
        IMonthlyMeterReadingRepository readings)
    {
        ArgumentNullException.ThrowIfNull(bills);
        ArgumentNullException.ThrowIfNull(rooms);
        ArgumentNullException.ThrowIfNull(readings);
        _bills = bills;
        _rooms = rooms;
        _readings = readings;
    }

    public LatestRoomDataForBill Get(BillId billId)
    {
        ArgumentNullException.ThrowIfNull(billId);

        var bill = _bills.Get(billId)
            ?? throw new InvalidOperationException("ไม่พบบิลที่ต้องการแก้ไข กรุณาเลือกบิลจากตารางอีกครั้ง");
        if (bill.IsPaid)
        {
            throw new InvalidOperationException(Bill.PaidBillCannotBeChangedMessage);
        }

        var room = _rooms.GetAll().FirstOrDefault(r => r.Id == bill.RoomId)
            ?? throw new InvalidOperationException("ไม่พบห้องพักของบิลนี้ กรุณาตรวจสอบข้อมูลห้องพัก");
        decimal rent = room.MonthlyRent.Amount;

        if (bill.Meters is null)
        {
            return new LatestRoomDataForBill(rent, room.IsCableTvExempt, null,
                "บิลนี้ไม่ได้เก็บอัตราค่าน้ำและค่าไฟไว้ จึงดึงได้เฉพาะค่าเช่าและสถานะยกเว้น Cable TV");
        }

        var reading = _readings.Get(bill.RoomId, bill.BillingPeriod);
        if (reading is null)
        {
            return new LatestRoomDataForBill(rent, room.IsCableTvExempt, null,
                "ไม่พบเลขมิเตอร์ของรอบบิลนี้ จึงใช้เลขมิเตอร์เดิมในบิล");
        }

        return new LatestRoomDataForBill(
            rent,
            room.IsCableTvExempt,
            new BillMeterReadings(
                reading.PreviousWaterMeter,
                reading.CurrentWaterMeter,
                reading.PreviousElectricityMeter,
                reading.CurrentElectricityMeter),
            Notice: null);
    }
}
