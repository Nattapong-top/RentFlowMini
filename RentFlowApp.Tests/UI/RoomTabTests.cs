using RentFlowApp.Application.Rooms;
using RentFlowApp.Forms;
using RentFlowApp.Infrastructure.Rooms;
using static RentFlowApp.Tests.UI.InputTestHelper;

namespace RentFlowApp.Tests.UI;

public class RoomTabTests
{
    private static T Find<T>(Control parent, string name) where T : Control =>
        (T)parent.Controls.Find(name, true)[0];

    [Fact]
    public void AddRoom_AfterEditingExemptRoom_IsChargedCableTvByDefault()
    {
        RunInSta(() =>
        {
            var rooms = new InMemoryRoomRepository();
            new CreateRoomService(rooms).Create("101", 3000m, true);
            using var form = new RoomForm(new CreateRoomService(rooms), rooms);
            form.Show();

            DoubleClickRow(Find<DataGridView>(form, "RoomGrid"), 0);
            Find<TextBox>(form, "RoomMonthlyRentTextBox").Text = "3500";
            Find<Button>(form, "EditRoomRentButton").PerformClick();

            // ไม่แตะช่องยกเว้น เพราะห้องใหม่ต้องเก็บค่า Cable TV เว้นแต่ผู้ใช้จะติ๊กเอง
            Find<TextBox>(form, "RoomNumberTextBox").Text = "102";
            Find<TextBox>(form, "RoomMonthlyRentTextBox").Text = "3000";
            Find<Button>(form, "AddRoomButton").PerformClick();

            Assert.True(rooms.GetAll().Single(room => room.RoomNumber.Value == "101").IsCableTvExempt);
            Assert.False(rooms.GetAll().Single(room => room.RoomNumber.Value == "102").IsCableTvExempt);
        });
    }

    private sealed class RoomTab : IDisposable
    {
        private readonly RoomForm _form;

        public RoomTab()
        {
            Rooms = new InMemoryRoomRepository();
            var createRoom = new CreateRoomService(Rooms);
            createRoom.Create("101", 3000m, false);
            createRoom.Create("102", 3200m, false);
            _form = new RoomForm(createRoom, Rooms);
            _form.Show();
        }

        public InMemoryRoomRepository Rooms { get; }

        public DataGridView Grid => Find<DataGridView>(_form, "RoomGrid");

        public Button Add => Find<Button>(_form, "AddRoomButton");

        public Button Edit => Find<Button>(_form, "EditRoomRentButton");

        public Button Cancel => Find<Button>(_form, "CancelRoomEditButton");

        public TextBox Number => Find<TextBox>(_form, "RoomNumberTextBox");

        public TextBox Rent => Find<TextBox>(_form, "RoomMonthlyRentTextBox");

        public CheckBox Exempt => Find<CheckBox>(_form, "RoomCableTvExemptCheckBox");

        public Label Message => Find<Label>(_form, "RoomMessageLabel");

        public RoomTabControl Tab => Find<RoomTabControl>(_form, "RoomTabControl");

        public decimal RentOf(string roomNumber) =>
            Rooms.GetAll().Single(room => room.RoomNumber.Value == roomNumber).MonthlyRent.Amount;

        public void SelectRow(int index)
        {
            Grid.ClearSelection();
            Grid.Rows[index].Selected = true;
        }

        public void Dispose() => _form.Dispose();
    }

    [Fact]
    public void EditButton_IsAvailable_ForTheRowAlreadySelectedAfterAddingARoom()
    {
        RunInSta(() =>
        {
            using var tab = new RoomTab();
            tab.Number.Text = "103";
            tab.Rent.Text = "3000";

            tab.Add.PerformClick();

            Assert.True(tab.Grid.SelectedRows.Count > 0);
            Assert.True(tab.Edit.Enabled);
        });
    }

    [Fact]
    public void SelectingAnotherRow_WhileEditing_StillSavesTheRoomBeingEdited()
    {
        RunInSta(() =>
        {
            using var tab = new RoomTab();
            DoubleClickRow(tab.Grid, 0);
            tab.Rent.Text = "3500";

            tab.SelectRow(1);
            tab.Edit.PerformClick();

            Assert.Equal(3500m, tab.RentOf("101"));
            Assert.Equal(3200m, tab.RentOf("102"));
            Assert.Equal("แก้ไขค่าเช่า", tab.Edit.Text);
        });
    }

    [Fact]
    public void RefreshingTheList_WhileEditing_KeepsSaveAvailable()
    {
        RunInSta(() =>
        {
            using var tab = new RoomTab();
            DoubleClickRow(tab.Grid, 0);
            tab.Rent.Text = "3500";

            // เกิดเมื่อบันทึกผู้เช่าระหว่างที่ค้างโหมดแก้ค่าเช่าไว้
            tab.Tab.RefreshRoomList();
            tab.Edit.PerformClick();

            Assert.Equal(3500m, tab.RentOf("101"));
        });
    }

    [Fact]
    public void Editing_SavesCableTvExemptionWithRent()
    {
        RunInSta(() =>
        {
            using var tab = new RoomTab();
            DoubleClickRow(tab.Grid, 0);

            Assert.True(tab.Exempt.Enabled);
            tab.Exempt.Checked = true;
            tab.Rent.Text = "3500";
            tab.Edit.PerformClick();

            var room = tab.Rooms.GetAll().Single(item => item.RoomNumber.Value == "101");
            Assert.True(room.IsCableTvExempt);
            Assert.Equal(3500m, room.MonthlyRent.Amount);
            Assert.False(tab.Rooms.GetAll().Single(item => item.RoomNumber.Value == "102").IsCableTvExempt);
        });
    }

    [Fact]
    public void CancelButton_IsAvailableOnlyWhileEditing()
    {
        RunInSta(() =>
        {
            using var tab = new RoomTab();
            Assert.False(tab.Cancel.Enabled);

            DoubleClickRow(tab.Grid, 0);
            Assert.True(tab.Cancel.Enabled);

            tab.Edit.PerformClick();
            Assert.False(tab.Cancel.Enabled);
        });
    }

    [Fact]
    public void CancelButton_ExitsEditingWithoutSavingAndClearsTheForm()
    {
        RunInSta(() =>
        {
            using var tab = new RoomTab();
            DoubleClickRow(tab.Grid, 0);
            tab.Rent.Text = "9999";
            tab.Exempt.Checked = true;

            tab.Cancel.PerformClick();

            Assert.Equal(3000m, tab.RentOf("101"));
            Assert.Equal("แก้ไขค่าเช่า", tab.Edit.Text);
            Assert.True(tab.Add.Enabled);
            Assert.False(tab.Number.ReadOnly);
            Assert.Empty(tab.Number.Text);
            Assert.Empty(tab.Rent.Text);
            Assert.False(tab.Exempt.Checked);
            Assert.Empty(tab.Message.Text);
        });
    }
}
