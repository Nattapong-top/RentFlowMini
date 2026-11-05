using RentFlowApp.Application.Billing;
using RentFlowApp.Application.Rooms;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Forms;
using RentFlowApp.Infrastructure.Billing;
using RentFlowApp.Infrastructure.Rooms;
using static RentFlowApp.Tests.UI.InputTestHelper;

namespace RentFlowApp.Tests.UI;

public class ErrorMessageTests
{
    private const string InternalDetail = "รายละเอียดภายในระบบ";

    private static BillingPeriod CurrentPeriod => BillingPeriod.Create(
        DateTime.Now.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture));

    private static T Find<T>(Control parent, string name) where T : Control =>
        (T)parent.Controls.Find(name, true)[0];

    private sealed class FailingMeterRepository : IMonthlyMeterReadingRepository
    {
        public void Add(MonthlyMeterReading reading) => throw new IOException(InternalDetail);

        public void Update(MonthlyMeterReading reading) => throw new IOException(InternalDetail);

        public MonthlyMeterReading? Get(RoomId roomId, BillingPeriod billingPeriod) => null;

        public bool Exists(RoomId roomId, BillingPeriod billingPeriod) => false;

        public IEnumerable<MonthlyMeterReading> GetAll() => [];
    }

    private sealed class FailingBillRepository : IBillRepository
    {
        public void Add(Bill bill) => throw new IOException(InternalDetail);

        public void Update(Bill bill) => throw new IOException(InternalDetail);

        public bool Exists(RoomId roomId, BillingPeriod billingPeriod) => false;

        public IReadOnlyCollection<Bill> GetAll() => [];

        public Bill? Get(BillId id) => null;
    }

    private sealed class FailingSettingsRepository(Exception? onGet, Exception? onSave) : IBillingSettingsRepository
    {
        private readonly InMemoryBillingSettingsRepository _inner = new();

        public BillingSettings Get() => onGet is null ? _inner.Get() : throw onGet;

        public void Save(BillingSettings settings) => throw onSave!;
    }

    private static string AddRoomAndReadMessage(InMemoryRoomRepository rooms, string roomNumber)
    {
        using var form = new RoomForm(new CreateRoomService(rooms), rooms);
        form.Show();
        Find<TextBox>(form, "RoomNumberTextBox").Text = roomNumber;
        Find<TextBox>(form, "RoomMonthlyRentTextBox").Text = "3000";
        Find<Button>(form, "AddRoomButton").PerformClick();
        return Find<Label>(form, "RoomMessageLabel").Text;
    }

    [Theory]
    [InlineData("", "กรุณาระบุหมายเลขห้อง")]
    [InlineData("10 1", "หมายเลขห้องต้องไม่มีช่องว่าง กรุณาระบุใหม่อีกครั้ง")]
    public void RoomTab_InvalidRoomNumber_ShowsThaiMessageOnly(string roomNumber, string expected)
    {
        RunInSta(() => Assert.Equal(expected, AddRoomAndReadMessage(new InMemoryRoomRepository(), roomNumber)));
    }

    [Fact]
    public void RoomTab_DuplicateRoomNumber_ShowsThaiMessage()
    {
        RunInSta(() =>
        {
            var rooms = new InMemoryRoomRepository();
            new CreateRoomService(rooms).Create("101", 3000m, false);

            Assert.Equal(
                "หมายเลขห้อง 101 มีอยู่แล้ว กรุณาระบุหมายเลขอื่น",
                AddRoomAndReadMessage(rooms, "101"));
        });
    }

    [Fact]
    public void MeterTab_UnexpectedError_DoesNotShowInternalDetail()
    {
        RunInSta(() =>
        {
            var rooms = new InMemoryRoomRepository();
            new CreateRoomService(rooms).Create("101", 3000m, false);
            var meters = new FailingMeterRepository();
            using var form = new Form();
            var tab = new MeterReadingTabControl(rooms, meters, new SaveMonthlyMeterReadingService(meters));
            form.Controls.Add(tab);
            form.Show();
            Find<TextBox>(tab, "CurrWaterTextBox").Text = "10";
            Find<TextBox>(tab, "CurrElecTextBox").Text = "10";

            Find<Button>(tab, "SaveMeterButton").PerformClick();

            string message = Find<Label>(tab, "MeterMessageLabel").Text;
            Assert.StartsWith("เกิดข้อผิดพลาด", message);
            Assert.DoesNotContain(InternalDetail, message);
        });
    }

    [Fact]
    public void BillTab_UnexpectedError_DoesNotShowInternalDetail()
    {
        RunInSta(() =>
        {
            var rooms = new InMemoryRoomRepository();
            var room = new CreateRoomService(rooms).Create("101", 3000m, false);
            var meters = new InMemoryMonthlyMeterReadingRepository();
            new SaveMonthlyMeterReadingService(meters).Save(
                room.Id,
                CurrentPeriod,
                MeterReading.Create(0m),
                MeterReading.Create(10m),
                MeterReading.Create(0m),
                MeterReading.Create(10m));
            var bills = new FailingBillRepository();
            using var form = new Form();
            var tab = new MonthlyBillTabControl(
                new CreateMonthlyBillService(bills),
                rooms,
                monthlyMeterReadingRepository: meters,
                billRepository: bills);
            form.Controls.Add(tab);
            form.Show();

            Find<Button>(tab, "CreateBillButton").PerformClick();

            string message = Find<Label>(tab, "BillMessageLabel").Text;
            Assert.StartsWith("เกิดข้อผิดพลาด", message);
            Assert.DoesNotContain(InternalDetail, message);
        });
    }

    [Fact]
    public void BillTab_SettingsCannotBeLoaded_DoesNotCreateBillFromBuiltInRates()
    {
        RunInSta(() =>
        {
            var rooms = new InMemoryRoomRepository();
            var room = new CreateRoomService(rooms).Create("101", 3000m, false);
            var meters = new InMemoryMonthlyMeterReadingRepository();
            new SaveMonthlyMeterReadingService(meters).Save(
                room.Id,
                CurrentPeriod,
                MeterReading.Create(0m),
                MeterReading.Create(10m),
                MeterReading.Create(0m),
                MeterReading.Create(10m));
            var bills = new InMemoryBillRepository();
            using var form = new Form();
            var tab = new MonthlyBillTabControl(
                new CreateMonthlyBillService(bills),
                rooms,
                monthlyMeterReadingRepository: meters,
                billRepository: bills,
                billingSettingsRepository: new FailingSettingsRepository(new IOException(InternalDetail), null));
            form.Controls.Add(tab);
            form.Show();

            Find<Button>(tab, "CreateBillButton").PerformClick();

            Assert.Empty(bills.GetAll());
            string message = Find<Label>(tab, "BillMessageLabel").Text;
            Assert.StartsWith("ไม่สามารถโหลดราคากลาง", message);
            Assert.DoesNotContain(InternalDetail, message);
        });
    }

    private static BillingSettingsTabControl CreateSettingsTab(Form form, IBillingSettingsRepository repository)
    {
        var tab = new BillingSettingsTabControl(
            repository,
            new UpdateBillingSettingsService(repository),
            new InMemoryInvoiceSettingsRepository());
        form.Controls.Add(tab);
        form.Show();
        return tab;
    }

    [Fact]
    public void SettingsTab_UnexpectedSaveError_DoesNotShowInternalDetail()
    {
        RunInSta(() =>
        {
            using var form = new Form();
            var tab = CreateSettingsTab(form, new FailingSettingsRepository(null, new IOException(InternalDetail)));

            Find<Button>(tab, "SaveSettingsButton").PerformClick();

            string message = Find<Label>(tab, "SettingsMessageLabel").Text;
            Assert.StartsWith("เกิดข้อผิดพลาด", message);
            Assert.DoesNotContain(InternalDetail, message);
        });
    }

    [Fact]
    public void SettingsTab_SaveFailureWrittenForUsers_IsShownAsIs()
    {
        RunInSta(() =>
        {
            const string userMessage = "ไม่สามารถบันทึกการตั้งค่าได้ กรุณาลองใหม่อีกครั้ง";
            using var form = new Form();
            var tab = CreateSettingsTab(form, new FailingSettingsRepository(null, new InvalidOperationException(userMessage)));

            Find<Button>(tab, "SaveSettingsButton").PerformClick();

            Assert.Equal(userMessage, Find<Label>(tab, "SettingsMessageLabel").Text);
        });
    }

    [Fact]
    public void SettingsTab_LoadError_DoesNotShowInternalDetail()
    {
        RunInSta(() =>
        {
            using var form = new Form();
            var tab = CreateSettingsTab(form, new FailingSettingsRepository(new IOException(InternalDetail), null));

            string message = Find<Label>(tab, "SettingsMessageLabel").Text;
            Assert.StartsWith("ไม่สามารถโหลด", message);
            Assert.DoesNotContain(InternalDetail, message);
        });
    }
}
