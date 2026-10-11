
using RentFlowApp.Application.Rooms;

namespace RentFlowApp.Forms;

public class RoomForm : Form
{
    private readonly CreateRoomService _createRoomService;
    private readonly IRoomRepository _roomRepository;

    private readonly TextBox _roomNumberTextBox = new();
    private readonly Button _addRoomButton = new();
    private readonly DataGridView _roomGrid = new();
    private readonly Label _messageLabel = new();

    public RoomForm(
        CreateRoomService createRoomService,
        IRoomRepository roomRepository)
    {
        ArgumentNullException.ThrowIfNull(createRoomService);
        ArgumentNullException.ThrowIfNull(roomRepository);

        _createRoomService = createRoomService;
        _roomRepository = roomRepository;

        InitializeForm();
        RefreshRoomList();
    }

    private void InitializeForm()
    {
        Text = "RentFlow - Room Management";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 800;
        Height = 500;
        MinimumSize = new Size(600, 400);

        var titleLabel = new Label
        {
            Text = "Room Management",
            Left = 20,
            Top = 20,
            Width = 300,
            Height = 35,
            Font = new Font("Segoe UI", 16, FontStyle.Bold)
        };

        var roomNumberLabel = new Label
        {
            Text = "Room Number",
            Left = 20,
            Top = 80,
            Width = 120,
            Height = 25
        };

        _roomNumberTextBox.SetBounds(145, 77, 180, 30);

        _addRoomButton.Text = "Add Room";
        _addRoomButton.SetBounds(340, 76, 110, 32);
        _addRoomButton.Click += AddRoomButton_Click;

        _messageLabel.SetBounds(20, 120, 740, 25);
        _messageLabel.ForeColor = Color.DarkGreen;

        _roomGrid.SetBounds(20, 160, 740, 270);
        _roomGrid.Anchor =
            AnchorStyles.Top |
            AnchorStyles.Bottom |
            AnchorStyles.Left |
            AnchorStyles.Right;

        _roomGrid.ReadOnly = true;
        _roomGrid.AllowUserToAddRows = false;
        _roomGrid.AllowUserToDeleteRows = false;
        _roomGrid.AutoGenerateColumns = false;
        _roomGrid.SelectionMode =
            DataGridViewSelectionMode.FullRowSelect;
        _roomGrid.MultiSelect = false;
        _roomGrid.RowHeadersVisible = false;
        _roomGrid.AutoSizeColumnsMode =
            DataGridViewAutoSizeColumnsMode.Fill;

        _roomGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "RoomNumber",
            HeaderText = "Room Number",
            DataPropertyName = "RoomNumber"
        });

        _roomGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "RoomId",
            HeaderText = "Room ID",
            DataPropertyName = "RoomId"
        });

        Controls.AddRange(
        [
            titleLabel,
            roomNumberLabel,
            _roomNumberTextBox,
            _addRoomButton,
            _messageLabel,
            _roomGrid
        ]);
    }

    private void AddRoomButton_Click(
        object? sender,
        EventArgs e)
    {
        try
        {
            var room = _createRoomService.Create(
                _roomNumberTextBox.Text);

            RefreshRoomList();

            _roomNumberTextBox.Clear();
            _roomNumberTextBox.Focus();

            _messageLabel.ForeColor = Color.DarkGreen;
            _messageLabel.Text =
                $"Room {room.RoomNumber.Value} created successfully.";
        }
        catch (ArgumentException ex)
        {
            ShowError(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            ShowError(ex.Message);
        }
    }

    private void RefreshRoomList()
    {
        _roomGrid.DataSource = _roomRepository
            .GetAll()
            .Select(room => new
            {
                RoomNumber = room.RoomNumber.Value,
                RoomId = room.Id.Value
            })
            .ToList();
    }

    private void ShowError(string message)
    {
        _messageLabel.ForeColor = Color.DarkRed;
        _messageLabel.Text = message;
    }
}
