using RentFlowApp.Application.Billing;
using RentFlowApp.Application.Rooms;
using RentFlowApp.Forms;
using RentFlowApp.Infrastructure.Billing;
using RentFlowApp.Infrastructure.Rooms;

namespace RentFlowApp.Tests.UI;

public class RoomFormTests
{
    private static void RunInSta(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception != null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }

    [Fact]
    public void RoomForm_InitializesWithTwoTabsAndDefaultRates()
    {
        RunInSta(() =>
        {
            // Arrange
            var roomRepo = new InMemoryRoomRepository();
            var billRepo = new InMemoryBillRepository();
            var createRoomService = new CreateRoomService(roomRepo);
            var createBillService = new CreateMonthlyBillService(billRepo);

            // Act
            using var form = new RoomForm(createRoomService, roomRepo, createBillService);

            // Assert
            var tabControl = form.Controls.OfType<TabControl>().FirstOrDefault();
            Assert.NotNull(tabControl);
            Assert.Equal(2, tabControl.TabPages.Count);
            Assert.Equal("ห้องพัก (Rooms)", tabControl.TabPages[0].Text);
            Assert.Equal("สร้างบิลประจำเดือน (Monthly Bill)", tabControl.TabPages[1].Text);

            var billingTab = tabControl.TabPages[1];
            var waterRateTextBox = billingTab.Controls.Find("WaterRateTextBox", true).FirstOrDefault() as TextBox;
            Assert.NotNull(waterRateTextBox);
            Assert.Equal("19", waterRateTextBox.Text);

            var elecRateTextBox = billingTab.Controls.Find("ElecRateTextBox", true).FirstOrDefault() as TextBox;
            Assert.NotNull(elecRateTextBox);
            Assert.Equal("8", elecRateTextBox.Text);
        });
    }

    [Fact]
    public void RoomForm_WhenRoomsExist_PopulatesRoomSelectionInBillingTab()
    {
        RunInSta(() =>
        {
            // Arrange
            var roomRepo = new InMemoryRoomRepository();
            var billRepo = new InMemoryBillRepository();
            var createRoomService = new CreateRoomService(roomRepo);
            var createBillService = new CreateMonthlyBillService(billRepo);

            createRoomService.Create("101");
            createRoomService.Create("102");

            // Act
            using var form = new RoomForm(createRoomService, roomRepo, createBillService);
            var billingTab = form.Controls.OfType<TabControl>().First().TabPages[1];
            var comboBox = billingTab.Controls.Find("BillRoomComboBox", true).First() as ComboBox;
            Assert.NotNull(comboBox);

            // Assert
            Assert.Equal(2, comboBox.Items.Count);
            Assert.Equal("101", comboBox.Items[0]?.ToString());
            Assert.Equal("102", comboBox.Items[1]?.ToString());
        });
    }

    [Fact]
    public void RoomForm_CreateBill_WhenInputValid_CalculatesAndSavesBill()
    {
        RunInSta(() =>
        {
            // Arrange
            var roomRepo = new InMemoryRoomRepository();
            var billRepo = new InMemoryBillRepository();
            var createRoomService = new CreateRoomService(roomRepo);
            var createBillService = new CreateMonthlyBillService(billRepo);

            createRoomService.Create("101");

            using var form = new RoomForm(createRoomService, roomRepo, createBillService);
            form.Show();
            var tabControl = form.Controls.OfType<TabControl>().First();
            var billingTab = tabControl.TabPages[1];
            tabControl.SelectedTab = billingTab;
            var comboBox = (ComboBox)billingTab.Controls.Find("BillRoomComboBox", true).First();
            comboBox.SelectedIndex = 0;

            var periodBox = (TextBox)billingTab.Controls.Find("BillingPeriodTextBox", true).First();
            var rentBox = (TextBox)billingTab.Controls.Find("MonthlyRentTextBox", true).First();
            var prevWaterBox = (TextBox)billingTab.Controls.Find("PrevWaterTextBox", true).First();
            var currWaterBox = (TextBox)billingTab.Controls.Find("CurrWaterTextBox", true).First();
            var prevElecBox = (TextBox)billingTab.Controls.Find("PrevElecTextBox", true).First();
            var currElecBox = (TextBox)billingTab.Controls.Find("CurrElecTextBox", true).First();
            var createButton = (Button)billingTab.Controls.Find("CreateBillButton", true).First();
            var totalLabel = (Label)billingTab.Controls.Find("ResultTotalLabel", true).First();
            var statusLabel = (Label)billingTab.Controls.Find("BillMessageLabel", true).First();

            periodBox.Text = "2026-10";
            rentBox.Text = "3000";
            prevWaterBox.Text = "100";
            currWaterBox.Text = "110"; // 10 units * 19 = 190
            prevElecBox.Text = "500";
            currElecBox.Text = "550"; // 50 units * 8 = 400

            // Act
            createButton.PerformClick();

            // Assert: Total should be 3000 + 190 + 400 = 3590.00
            Assert.Contains("3,590.00", totalLabel.Text);
            Assert.Equal(System.Drawing.Color.DarkGreen, statusLabel.ForeColor);
            Assert.Contains("สำเร็จเรียบร้อย", statusLabel.Text);
        });
    }

    [Fact]
    public void RoomForm_CreateBill_WhenCurrentMeterLowerThanPrevious_DisplaysErrorMessage()
    {
        RunInSta(() =>
        {
            // Arrange
            var roomRepo = new InMemoryRoomRepository();
            var billRepo = new InMemoryBillRepository();
            var createRoomService = new CreateRoomService(roomRepo);
            var createBillService = new CreateMonthlyBillService(billRepo);

            createRoomService.Create("101");

            using var form = new RoomForm(createRoomService, roomRepo, createBillService);
            form.Show();
            var tabControl = form.Controls.OfType<TabControl>().First();
            var billingTab = tabControl.TabPages[1];
            tabControl.SelectedTab = billingTab;
            var comboBox = (ComboBox)billingTab.Controls.Find("BillRoomComboBox", true).First();
            comboBox.SelectedIndex = 0;

            var periodBox = (TextBox)billingTab.Controls.Find("BillingPeriodTextBox", true).First();
            var rentBox = (TextBox)billingTab.Controls.Find("MonthlyRentTextBox", true).First();
            var prevWaterBox = (TextBox)billingTab.Controls.Find("PrevWaterTextBox", true).First();
            var currWaterBox = (TextBox)billingTab.Controls.Find("CurrWaterTextBox", true).First();
            var prevElecBox = (TextBox)billingTab.Controls.Find("PrevElecTextBox", true).First();
            var currElecBox = (TextBox)billingTab.Controls.Find("CurrElecTextBox", true).First();
            var createButton = (Button)billingTab.Controls.Find("CreateBillButton", true).First();
            var statusLabel = (Label)billingTab.Controls.Find("BillMessageLabel", true).First();

            periodBox.Text = "2026-10";
            rentBox.Text = "3000";
            prevWaterBox.Text = "100";
            currWaterBox.Text = "90"; // Invalid: current < previous
            prevElecBox.Text = "500";
            currElecBox.Text = "550";

            // Act
            createButton.PerformClick();

            // Assert
            Assert.Equal(System.Drawing.Color.DarkRed, statusLabel.ForeColor);
            Assert.Contains("มิเตอร์", statusLabel.Text);
        });
    }
}
