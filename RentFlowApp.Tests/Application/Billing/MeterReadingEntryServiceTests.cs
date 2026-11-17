using RentFlowApp.Application.Billing;
using RentFlowApp.Application.Rooms;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Infrastructure.Billing;
using RentFlowApp.Infrastructure.Rooms;

namespace RentFlowApp.Tests.Application.Billing;

public class MeterReadingEntryServiceTests
{
    private static readonly BillingPeriod October = BillingPeriod.Create("2026-10");
    private static readonly BillingPeriod September = BillingPeriod.Create("2026-09");

    private readonly InMemoryRoomRepository _rooms = new();
    private readonly InMemoryMonthlyMeterReadingRepository _readings = new();

    private MeterReadingEntryService CreateService() => new(_rooms, _readings);

    private RoomId AddRoom(string roomNumber) =>
        new CreateRoomService(_rooms).Create(roomNumber, 3000m, false).Id;

    private void SaveReading(
        RoomId roomId,
        BillingPeriod billingPeriod,
        decimal previousWater,
        decimal currentWater,
        decimal previousElectricity,
        decimal currentElectricity) =>
        _readings.Add(MonthlyMeterReading.Create(
            roomId,
            billingPeriod,
            MeterReading.Create(previousWater),
            MeterReading.Create(currentWater),
            MeterReading.Create(previousElectricity),
            MeterReading.Create(currentElectricity)));

    [Fact]
    public void GetRoomsToRecord_HidesRoomsAlreadyRecordedForPeriodOnly()
    {
        var recorded = AddRoom("101");
        var recordedLastMonth = AddRoom("102");
        var notRecorded = AddRoom("103");
        SaveReading(recorded, October, 0, 10, 0, 10);
        SaveReading(recordedLastMonth, September, 0, 10, 0, 10);

        var options = CreateService().GetRoomsToRecord(October);

        Assert.Equal(
            [new MeterRoomOption(recordedLastMonth, "102"), new MeterRoomOption(notRecorded, "103")],
            options);
    }

    [Fact]
    public void GetRoomsToRecord_KeepsForcedRoomEvenWhenRecorded()
    {
        var recorded = AddRoom("101");
        var notRecorded = AddRoom("102");
        SaveReading(recorded, October, 0, 10, 0, 10);

        var options = CreateService().GetRoomsToRecord(October, forceIncludeRoomId: recorded);

        Assert.Equal(
            [new MeterRoomOption(recorded, "101"), new MeterRoomOption(notRecorded, "102")],
            options);
    }

    [Fact]
    public void GetRoomsToRecord_WhenPeriodIsMissing_ReturnsAllRooms()
    {
        var room = AddRoom("101");
        SaveReading(room, October, 0, 10, 0, 10);

        var options = CreateService().GetRoomsToRecord(null);

        Assert.Equal([new MeterRoomOption(room, "101")], options);
    }

    [Fact]
    public void GetReadings_ReturnsReadingsOfPeriodSortedByRoomNumberWithUsage()
    {
        var room102 = AddRoom("102");
        var room101 = AddRoom("101");
        SaveReading(room102, October, 100, 112.5m, 500, 530);
        SaveReading(room101, October, 50, 55, 200, 260);
        SaveReading(room101, September, 40, 50, 150, 200);

        var items = CreateService().GetReadings(October);

        Assert.Equal(2, items.Count);
        Assert.Equal(new MeterReadingListItem(room101, 1, "101", October, 50, 55, 5, 200, 260, 60), items[0]);
        Assert.Equal(new MeterReadingListItem(room102, 2, "102", October, 100, 112.5m, 12.5m, 500, 530, 30), items[1]);
    }

    [Fact]
    public void GetReadings_WhenRoomNoLongerExists_ShowsDashAsRoomNumber()
    {
        var missingRoom = RoomId.New();
        SaveReading(missingRoom, October, 0, 10, 0, 20);

        var item = Assert.Single(CreateService().GetReadings(October));

        Assert.Equal("-", item.RoomNumber);
        Assert.Equal(missingRoom, item.RoomId);
    }

    [Fact]
    public void GetReadings_WhenPeriodIsMissing_ReturnsReadingsOfAllPeriods()
    {
        var room = AddRoom("101");
        SaveReading(room, September, 40, 50, 150, 200);
        SaveReading(room, October, 50, 55, 200, 260);

        var items = CreateService().GetReadings(null);

        Assert.Equal(2, items.Count);
        Assert.Contains(items, item => item.BillingPeriod == September);
        Assert.Contains(items, item => item.BillingPeriod == October);
    }

    [Fact]
    public void GetEntryDefaults_WhenPeriodAndPreviousPeriodAreRecorded_ReturnsSavedValuesWithPreviousLocked()
    {
        var room = AddRoom("101");
        SaveReading(room, September, 40, 50, 150, 200);
        SaveReading(room, October, 50, 55, 200, 260);

        var defaults = CreateService().GetEntryDefaults(room, October);

        Assert.Equal(new MeterEntryDefaults(50, 200, 55, 260, IsPreviousLocked: true), defaults);
    }

    [Fact]
    public void GetEntryDefaults_WhenPeriodIsRecordedButPreviousPeriodIsNot_LetsUserCorrectPrevious()
    {
        var room = AddRoom("101");
        SaveReading(room, October, 50, 55, 200, 260);

        var defaults = CreateService().GetEntryDefaults(room, October);

        Assert.Equal(new MeterEntryDefaults(50, 200, 55, 260, IsPreviousLocked: false), defaults);
    }

    [Fact]
    public void GetEntryDefaults_WhenOnlyPreviousPeriodIsRecorded_UsesItsCurrentValuesAsLockedPrevious()
    {
        var room = AddRoom("101");
        SaveReading(room, September, 40, 50, 150, 200);

        var defaults = CreateService().GetEntryDefaults(room, October);

        Assert.Equal(new MeterEntryDefaults(50, 200, null, null, IsPreviousLocked: true), defaults);
    }

    [Fact]
    public void GetEntryDefaults_WhenRoomHasNoHistory_StartsAtZeroAndLetsUserTypePrevious()
    {
        var room = AddRoom("101");

        var defaults = CreateService().GetEntryDefaults(room, October);

        Assert.Equal(new MeterEntryDefaults(0, 0, null, null, IsPreviousLocked: false), defaults);
    }
}
