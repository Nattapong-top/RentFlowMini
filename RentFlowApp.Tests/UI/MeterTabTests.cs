using RentFlowApp.Application.Billing;
using RentFlowApp.Application.Reports;
using RentFlowApp.Application.Rooms;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Forms;
using RentFlowApp.Infrastructure.Billing;
using RentFlowApp.Infrastructure.Rooms;
using static RentFlowApp.Tests.UI.InputTestHelper;

namespace RentFlowApp.Tests.UI;

public class MeterTabTests
{
    private static readonly BillingPeriod CurrentPeriod = BillingPeriod.Create(
        DateTime.Now.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture));

    private sealed class MeterTab : IDisposable
    {
        private readonly Form _form = new();

        public MeterTab()
        {
            Rooms = new InMemoryRoomRepository();
            Readings = new InMemoryMonthlyMeterReadingRepository();
        }

        public InMemoryRoomRepository Rooms { get; }

        public InMemoryMonthlyMeterReadingRepository Readings { get; }

        public MeterReadingTabControl Control { get; private set; } = null!;

        public RoomId AddRoom(string number) => new CreateRoomService(Rooms).Create(number, 3000m, false).Id;

        public void AddReading(RoomId roomId)
        {
            new SaveMonthlyMeterReadingService(Readings).Save(
                roomId,
                CurrentPeriod,
                MeterReading.Create(0m),
                MeterReading.Create(10m),
                MeterReading.Create(0m),
                MeterReading.Create(10m));
        }

        public void Show()
        {
            Control = new MeterReadingTabControl(Rooms, Readings, new SaveMonthlyMeterReadingService(Readings));
            _form.Controls.Add(Control);
            _form.Show();
        }

        public T Find<T>(string name) where T : Control => (T)Control.Controls.Find(name, true)[0];

        public void FillCurrentReadings()
        {
            Find<TextBox>("CurrWaterTextBox").Text = "10";
            Find<TextBox>("CurrElecTextBox").Text = "10";
        }

        public void Dispose() => _form.Dispose();
    }

    [Fact]
    public void Enter_WhileRoomListIsOpen_SelectsWithoutSaving()
    {
        RunInSta(() =>
        {
            using var tab = new MeterTab();
            tab.AddRoom("101");
            tab.Show();
            tab.FillCurrentReadings();
            var roomBox = tab.Find<ComboBox>("MeterRoomComboBox");
            roomBox.DroppedDown = true;

            PressEnter(roomBox);

            Assert.Empty(tab.Readings.GetAll());
        });
    }

    [Fact]
    public void Enter_WhileRoomListIsClosed_Saves()
    {
        RunInSta(() =>
        {
            using var tab = new MeterTab();
            tab.AddRoom("101");
            tab.Show();
            tab.FillCurrentReadings();

            PressEnter(tab.Find<ComboBox>("MeterRoomComboBox"));

            Assert.Single(tab.Readings.GetAll());
        });
    }

    [Fact]
    public void SaveMessage_ShowsPeriodInBuddhistYear()
    {
        RunInSta(() =>
        {
            using var tab = new MeterTab();
            tab.AddRoom("101");
            tab.Show();
            tab.FillCurrentReadings();

            tab.Find<Button>("SaveMeterButton").PerformClick();

            Assert.Equal(
                $"บันทึกมิเตอร์ห้อง 101 รอบบิล {ThaiCalendar.FormatPeriod(CurrentPeriod)} สำเร็จ",
                tab.Find<Label>("MeterMessageLabel").Text);
        });
    }

    [Fact]
    public void ReadingList_SortsRoomsByNumber()
    {
        RunInSta(() =>
        {
            using var tab = new MeterTab();
            tab.AddReading(tab.AddRoom("101"));
            tab.AddReading(tab.AddRoom("99"));
            tab.Show();

            var rooms = tab.Find<DataGridView>("MeterGrid").Rows.Cast<DataGridViewRow>()
                .Select(row => row.Cells["RoomNumber"].Value?.ToString());

            Assert.Equal(["99", "101"], rooms);
        });
    }
}
