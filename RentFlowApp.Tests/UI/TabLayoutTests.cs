using RentFlowApp.Application.Billing;
using RentFlowApp.Application.Rooms;
using RentFlowApp.Application.Tenants;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Forms;
using RentFlowApp.Infrastructure.Billing;
using RentFlowApp.Infrastructure.Rooms;
using RentFlowApp.Infrastructure.Tenants;
using static RentFlowApp.Tests.UI.InputTestHelper;

namespace RentFlowApp.Tests.UI;

public class TabLayoutTests
{
    private static IEnumerable<Control> Descendants(Control parent) =>
        parent.Controls.Cast<Control>().SelectMany(child => new[] { child }.Concat(Descendants(child)));

    // วัดด้วย TextRenderer เพราะแอปจริงวาดตัวอักษรด้วย GDI ซึ่งกว้างกว่าค่าเริ่มต้นตอนรันเทสต์
    private static bool TextFits(Control control)
    {
        // ช่องเครื่องหมายของ CheckBox กินความกว้างราว 17px
        int available = control.ClientSize.Width - (control is CheckBox ? 17 : 0);
        if (TextRenderer.MeasureText(control.Text, control.Font).Width <= available)
        {
            return true;
        }

        var wrapped = TextRenderer.MeasureText(
            control.Text,
            control.Font,
            new Size(available, int.MaxValue),
            TextFormatFlags.WordBreak);
        return wrapped.Height <= control.ClientSize.Height;
    }

    private sealed class BillTab : IDisposable
    {
        private readonly Form _form = new() { Width = 1366, Height = 768 };

        public BillTab()
        {
            var rooms = new InMemoryRoomRepository();
            var tenants = new InMemoryTenantRepository();
            var bills = new InMemoryBillRepository();
            var room = new CreateRoomService(rooms).Create("101", 3000m, false);
            var tenant = new CreateTenantService(tenants).Create("ผู้เช่าทดสอบ", "นามสกุลสมมติ", null, room.Id);
            var meter = MeterReading.Create(100m);
            var bill = new CreateMonthlyBillService(bills).Create(new CreateMonthlyBillRequest(
                room.Id,
                BillingPeriod.Create(DateTime.Now.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture)),
                3000m,
                meter,
                meter,
                meter,
                meter,
                19m,
                8m,
                Tenant: BillTenantSnapshot.From(tenant)));
            bill.RecordPayment(DateOnly.FromDateTime(DateTime.Now), PaymentMethod.BankTransfer);

            Tab = new MonthlyBillTabControl(
                new CreateMonthlyBillService(bills),
                rooms,
                billRepository: bills,
                tenantRepository: tenants);
            _form.Controls.Add(Tab);
            HiddenTestWindows.ShowHidden(_form);
            Tab.RefreshBillGrid();
            Grid = (DataGridView)Tab.Controls.Find("BillGrid", true)[0];
        }

        public MonthlyBillTabControl Tab { get; }

        public DataGridView Grid { get; }

        public void Dispose() => _form.Dispose();
    }

    [Fact]
    public void BillTab_LabelsAndCheckBoxes_ShowAllText()
    {
        RunInSta(() =>
        {
            using var tab = new BillTab();
            // เปิดบิลเพื่อให้กล่องสรุปแสดงข้อความจริง ไม่ใช่ขีดว่าง
            DoubleClickRow(tab.Grid, 0);

            var clipped = Descendants(tab.Tab)
                .Where(control => control is Label or CheckBox && control.Text.Length > 0 && !TextFits(control))
                .Select(control => control.Text)
                .ToList();

            Assert.Empty(clipped);
        });
    }

    [Theory]
    [InlineData("TenantName")]
    [InlineData("PaymentStatus")]
    public void BillGrid_ColumnFitsItsText(string columnName)
    {
        RunInSta(() =>
        {
            using var tab = new BillTab();
            var column = tab.Grid.Columns[columnName]!;
            var cell = tab.Grid.Rows[0].Cells[columnName];

            int textWidth = TextRenderer.MeasureText(cell.FormattedValue!.ToString(), cell.InheritedStyle.Font).Width;

            Assert.True(column.Width >= textWidth, $"\"{cell.Value}\" ต้องการ {textWidth} แต่คอลัมน์กว้าง {column.Width}");
        });
    }

    private static RoomForm ShowForm(
        InMemoryRoomRepository rooms,
        int tabIndex,
        int width,
        int height,
        out TabPage page,
        InMemoryBillRepository? bills = null)
    {
        var form = new RoomForm(new CreateRoomService(rooms), rooms, billRepository: bills);
        HiddenTestWindows.ShowHidden(form);
        // ฟอร์มเปิดแบบขยายเต็มจอ จึงต้องคืนสถานะก่อนกำหนดขนาด
        form.WindowState = FormWindowState.Normal;
        form.Size = new Size(width, height);
        var tabs = form.Controls.OfType<TabControl>().Single();
        page = tabs.TabPages[tabIndex];
        tabs.SelectedTab = page;
        return form;
    }

    private static RoomForm ShowFormOnSettingsTab(InMemoryRoomRepository rooms, int width, int height, out TabPage page) =>
        ShowForm(rooms, 5, width, height, out page);

    private static List<string> FindOverflowing(Control root) =>
        // พื้นที่เลื่อนได้ตั้งใจให้เนื้อหาใหญ่กว่ากรอบ จึงตรวจเฉพาะกรอบที่เลื่อนไม่ได้
        Descendants(root)
            .Where(control => control.Parent is not (ScrollableControl { AutoScroll: true } or DataGridView))
            .Where(control =>
                control.Right > control.Parent!.ClientSize.Width ||
                control.Bottom > control.Parent.ClientSize.Height)
            .Select(control => string.IsNullOrEmpty(control.Name) ? control.GetType().Name : control.Name)
            .ToList();

    private static InMemoryBillRepository CreateBillsForCurrentPeriod(InMemoryRoomRepository rooms, int count)
    {
        var bills = new InMemoryBillRepository();
        var meter = MeterReading.Create(100m);
        var period = BillingPeriod.Create(DateTime.Now.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture));
        for (int i = 0; i < count; i++)
        {
            var room = new CreateRoomService(rooms).Create($"{101 + i}", 3000m, false);
            new CreateMonthlyBillService(bills).Create(new CreateMonthlyBillRequest(
                room.Id, period, 3000m, meter, meter, meter, meter, 19m, 8m, Tenant: TestTenants.Snapshot()));
        }

        return bills;
    }

    [Theory]
    [InlineData(1366, 728)]
    [InlineData(850, 650)]
    [InlineData(750, 600)]
    public void BillTab_ShowsAtLeastThreeBillRows_OnSmallWindows(int width, int height)
    {
        RunInSta(() =>
        {
            var rooms = new InMemoryRoomRepository();
            var bills = CreateBillsForCurrentPeriod(rooms, 6);
            using var form = ShowForm(rooms, 2, width, height, out var page, bills);
            var grid = (DataGridView)page.Controls.Find("BillGrid", true)[0];

            Assert.True(grid.DisplayedRowCount(false) >= 3, $"เห็นแค่ {grid.DisplayedRowCount(false)} แถว");
        });
    }

    [Fact]
    public void BillTab_KeepsFullInputArea_OnLargeWindows()
    {
        RunInSta(() =>
        {
            var rooms = new InMemoryRoomRepository();
            var bills = new InMemoryBillRepository();
            using var form = new Form();
            // Windows บีบหน้าต่างไม่ให้เกินจอ แต่ไม่บีบ control ข้างใน เทสต์จึงให้ผลเท่ากันทุกเครื่อง
            var host = new Panel { Size = new Size(1900, 900) };
            var tab = new MonthlyBillTabControl(
                new CreateMonthlyBillService(bills),
                rooms,
                billRepository: bills,
                tenantRepository: new InMemoryTenantRepository());
            host.Controls.Add(tab);
            form.Controls.Add(host);
            HiddenTestWindows.ShowHidden(form);
            var split = (SplitContainer)tab.Controls.Find("BillSplitContainer", true)[0];

            Assert.Equal(440, split.SplitterDistance);
        });
    }

    [Fact]
    public void BillTab_KeepsUserSplitterPosition_WhenWindowIsResized()
    {
        RunInSta(() =>
        {
            using var form = ShowForm(new InMemoryRoomRepository(), 2, 1920, 1040, out var page);
            var split = (SplitContainer)page.Controls.Find("BillSplitContainer", true)[0];

            typeof(SplitContainer).GetMethod(
                    "OnSplitterMoving",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)!
                .Invoke(split, [new SplitterCancelEventArgs(0, 300, 0, 300)]);
            split.SplitterDistance = 300;
            form.Size = new Size(1800, 1000);

            Assert.Equal(300, split.SplitterDistance);
        });
    }

    [Theory]
    [InlineData(1366, 728)]
    [InlineData(750, 600)]
    public void BillTab_ControlsStayInsideTheirParents(int width, int height)
    {
        RunInSta(() =>
        {
            using var form = ShowForm(new InMemoryRoomRepository(), 2, width, height, out var page);

            Assert.Empty(FindOverflowing(page));
        });
    }

    [Theory]
    [InlineData(850, 650)]
    [InlineData(750, 600)]
    public void BillTab_InputArea_DoesNotAddSecondHorizontalScrollBar(int width, int height)
    {
        RunInSta(() =>
        {
            using var form = ShowForm(new InMemoryRoomRepository(), 2, width, height, out var page);
            var inputArea = (ScrollableControl)page.Controls.Find("MonthlyBillInputGroup", true)[0].Parent!;

            Assert.False(inputArea.HorizontalScroll.Visible);
        });
    }

    [Theory]
    [InlineData(850, 650)]
    [InlineData(750, 600)]
    public void TenantTab_MoveOutButtonStaysReachable_OnSmallWindows(int width, int height)
    {
        RunInSta(() =>
        {
            using var form = ShowForm(new InMemoryRoomRepository(), 3, width, height, out var page);
            var button = page.Controls.Find("MoveOutTenantButton", true)[0];

            Assert.True(button.Right <= button.Parent!.ClientSize.Width, $"ปุ่มล้นขวา {button.Right} > {button.Parent.ClientSize.Width}");
        });
    }

    [Theory]
    [InlineData(1366, 728)]
    [InlineData(850, 650)]
    [InlineData(750, 600)]
    public void SettingsTab_MessageStaysReadable_OnEveryWindowSize(int width, int height)
    {
        RunInSta(() =>
        {
            using var form = ShowFormOnSettingsTab(new InMemoryRoomRepository(), width, height, out var page);
            var label = (Label)page.Controls.Find("SettingsMessageLabel", true)[0];

            var labelOnPage = page.RectangleToClient(label.RectangleToScreen(label.ClientRectangle));

            Assert.True(page.ClientRectangle.Contains(labelOnPage), $"ป้ายข้อความ {labelOnPage} อยู่นอกหน้าจอ {page.ClientRectangle}");
            Assert.True(
                label.ClientSize.Height >= TextRenderer.MeasureText("ทดสอบ", label.Font).Height,
                $"ป้ายข้อความสูงแค่ {label.ClientSize.Height}");
        });
    }

    [Theory]
    [InlineData(1366, 728)]
    [InlineData(750, 600)]
    public void SettingsTab_ControlsStayInsideTheirParents(int width, int height)
    {
        RunInSta(() =>
        {
            using var form = ShowFormOnSettingsTab(new InMemoryRoomRepository(), width, height, out var page);

            Assert.Empty(FindOverflowing(page));
        });
    }
}
