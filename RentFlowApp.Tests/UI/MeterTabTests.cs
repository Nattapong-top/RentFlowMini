using RentFlowApp.Application.Billing;
using RentFlowApp.Application.Reports;
using RentFlowApp.Application.Rooms;
using RentFlowApp.Domain.Billing;
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
            Bills = new InMemoryBillRepository();
        }

        public InMemoryBillRepository Bills { get; }

        public void AddBill(RoomId roomId, bool isPaid) => AddBill(roomId, CurrentPeriod, isPaid);

        public void AddBill(RoomId roomId, BillingPeriod period, bool isPaid)
        {
            var bill = Bill.Create(
                roomId,
                period,
                new BillingCalculationResult(
                    Money.Create(3000m),
                    Money.Create(0m),
                    Money.Create(0m),
                    Money.Create(0m),
                    Money.Create(0m),
                    Money.Create(0m),
                    Money.Create(3000m)));
            if (isPaid)
            {
                bill.RecordPayment(DateOnly.FromDateTime(DateTime.Today), PaymentMethod.Cash);
            }

            Bills.Add(bill);
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

        public void Show(Size? clientSize = null)
        {
            Control = new MeterReadingTabControl(Rooms, Readings, new SaveMonthlyMeterReadingService(Readings, Bills));
            if (clientSize is null)
            {
                _form.Controls.Add(Control);
            }
            else
            {
                // Windows ย่อหน้าต่างไม่ให้เกินจอของเครื่อง เช่นเครื่อง CI ที่จอกว้าง 1024px
                // จึงวางแท็บในแผงที่กำหนดขนาดเอง เพราะ control ภายในหน้าต่างไม่ถูกย่อตามจอ
                var host = new Panel { Size = clientSize.Value };
                host.Controls.Add(Control);
                _form.Controls.Add(host);
            }

            HiddenTestWindows.ShowHidden(_form);
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
    public void Save_WhenBillForPeriodIsPaid_ShowsErrorWithoutSaving()
    {
        RunInSta(() =>
        {
            using var tab = new MeterTab();
            var roomId = tab.AddRoom("101");
            tab.AddBill(roomId, isPaid: true);
            tab.Show();
            tab.FillCurrentReadings();

            tab.Find<Button>("SaveMeterButton").PerformClick();

            var message = tab.Find<Label>("MeterMessageLabel");
            Assert.Contains("ยกเลิกการรับชำระ", message.Text);
            Assert.Equal(ThemePalette.Current.ErrorText, message.ForeColor);
            Assert.Empty(tab.Readings.GetAll());
        });
    }

    [Fact]
    public void Save_WhenBillForPeriodIsUnpaid_SavesAndShowsRefreshWarning()
    {
        RunInSta(() =>
        {
            using var tab = new MeterTab();
            var roomId = tab.AddRoom("101");
            tab.AddBill(roomId, isPaid: false);
            tab.Show();
            tab.FillCurrentReadings();

            tab.Find<Button>("SaveMeterButton").PerformClick();

            var message = tab.Find<Label>("MeterMessageLabel");
            Assert.StartsWith("บันทึกมิเตอร์ห้อง 101", message.Text);
            Assert.Contains("ดึงข้อมูลห้องใหม่", message.Text);
            Assert.Equal(ThemePalette.Current.InfoText, message.ForeColor);
            Assert.Single(tab.Readings.GetAll());
        });
    }

    [Fact]
    public void MessageLabel_OnCommonLaptopWidth_ShowsLongestSaveMessageWithoutClipping()
    {
        RunInSta(() =>
        {
            using var tab = new MeterTab();
            var roomId = tab.AddRoom("101");
            var previousPeriod = CurrentPeriod.GetPrevious();
            var service = new SaveMonthlyMeterReadingService(tab.Readings, tab.Bills);
            service.Save(roomId, previousPeriod, Meter(0m), Meter(60m), Meter(0m), Meter(520m));
            service.Save(roomId, CurrentPeriod, Meter(60m), Meter(75m), Meter(520m), Meter(580m));
            tab.AddBill(roomId, CurrentPeriod, isPaid: false);
            tab.AddBill(roomId, previousPeriod, isPaid: false);
            // ข้อความยาวที่สุดที่การบันทึกสร้างได้: เตือนบิลของรอบที่แก้ แจ้งการปรับรอบถัดไป และเตือนบิลของรอบถัดไป
            var warning = service.Save(roomId, previousPeriod, Meter(0m), Meter(50m), Meter(0m), Meter(500m)).Warning;
            tab.Show(new Size(1350, 700));
            var label = tab.Find<Label>("MeterMessageLabel");

            label.Text = $"อัปเดตมิเตอร์ห้อง 101 รอบบิล {ThaiCalendar.FormatPeriod(previousPeriod)} สำเร็จ {warning}";

            var needed = TextRenderer.MeasureText(
                label.Text,
                label.Font,
                new Size(label.ClientSize.Width, int.MaxValue),
                TextFormatFlags.WordBreak);
            Assert.True(
                needed.Height <= label.ClientSize.Height,
                $"ข้อความต้องสูง {needed.Height}px แต่ช่องสูง {label.ClientSize.Height}px กว้าง {label.ClientSize.Width}px");
            Assert.True(label.Right <= label.Parent!.ClientSize.Width, "ช่องข้อความต้องไม่ล้นขอบขวา");
            Assert.True(
                label.Bottom <= label.Parent.ClientSize.Height,
                $"ช่องข้อความต้องไม่ล้นขอบล่าง (ล่างสุด {label.Bottom}px, กรอบสูง {label.Parent.ClientSize.Height}px)");
        });
    }

    private static MeterReading Meter(decimal value) => MeterReading.Create(value);

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
