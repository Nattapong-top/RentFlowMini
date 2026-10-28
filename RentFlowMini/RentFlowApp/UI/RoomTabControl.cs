using RentFlowApp.Application.Rooms;
using RentFlowApp.Application.Tenants;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Forms;

public sealed class RoomTabControl : BaseTabControl
{
    private readonly CreateRoomService _createRoomService;
    private readonly IRoomEditor _roomRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly UpdateRoomRentService _updateRoomRentService;

    private readonly TextBox _roomNumberTextBox = new();
    private readonly TextBox _roomMonthlyRentTextBox = new();
    private readonly CheckBox _roomCableTvExemptCheckBox = new();
    private readonly Button _addRoomButton = new();
    private readonly Button _editRoomRentButton = new();
    private readonly DataGridView _roomGrid = new();
    private readonly Label _roomMessageLabel = new();
    private RoomId? _editingRoomId;

    private sealed record RoomGridItem(
        RoomId RoomId,
        int Sequence,
        string RoomNumber,
        string OccupancyStatus,
        string TenantName,
        string MonthlyRent,
        string CableTvExempt);

    public event EventHandler? RoomsChanged;

    public RoomTabControl(
        CreateRoomService createRoomService,
        IRoomEditor roomRepository,
        ITenantRepository tenantRepository)
    {
        ArgumentNullException.ThrowIfNull(createRoomService);
        ArgumentNullException.ThrowIfNull(roomRepository);
        ArgumentNullException.ThrowIfNull(tenantRepository);

        _createRoomService = createRoomService;
        _roomRepository = roomRepository;
        _tenantRepository = tenantRepository;
        _updateRoomRentService = new UpdateRoomRentService(roomRepository);

        Name = "RoomTabControl";

        BuildLayout();
    }

    public void RefreshRoomList()
    {
        var rooms = _roomRepository.GetAll().ToList();
        var tenantsByRoomId = _tenantRepository.GetAll()
            .Where(tenant => tenant.RoomId is not null)
            .ToDictionary(tenant => tenant.RoomId!, tenant => $"{tenant.FirstName} {tenant.LastName}");

        _roomGrid.DataSource = rooms
            .Select((room, index) =>
            {
                var hasTenant = tenantsByRoomId.TryGetValue(room.Id, out var tenantName);
                return new RoomGridItem(
                    RoomId: room.Id,
                    Sequence: index + 1,
                    RoomNumber: room.RoomNumber.Value,
                    OccupancyStatus: hasTenant ? "ไม่ว่าง" : "ว่าง",
                    TenantName: hasTenant ? tenantName! : "-",
                    MonthlyRent: room.MonthlyRent.Amount.ToString("N2"),
                    CableTvExempt: room.IsCableTvExempt ? "ใช่" : "ไม่");
            })
            .ToList();

        _editRoomRentButton.Enabled = false;
    }

    public void ShowError(string message)
    {
        _roomMessageLabel.ForeColor = Color.DarkRed;
        _roomMessageLabel.Text = message;
    }

    private void BuildLayout()
    {
        var layout = CreateStandardLayout(3, [50f, 190f, -1f]);
        var titleLabel = CreateTitleLabel("จัดการข้อมูลห้องพัก");
        var inputGroup = BuildInputGroup();
        var listGroup = BuildListGroup();

        layout.Controls.Add(titleLabel, 0, 0);
        layout.Controls.Add(inputGroup, 0, 1);
        layout.Controls.Add(listGroup, 0, 2);
        Controls.Add(layout);
    }

    private GroupBox BuildInputGroup()
    {
        var group = new GroupBox
        {
            Name = "RoomInputGroup",
            Text = "ข้อมูลห้องพัก",
            Dock = DockStyle.Fill,
            Padding = new Padding(16)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 3
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var roomNumberLabel = new Label
        {
            Text = "หมายเลขห้อง:",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _roomNumberTextBox.Name = "RoomNumberTextBox";
        _roomNumberTextBox.Dock = DockStyle.Fill;

        var roomMonthlyRentLabel = new Label
        {
            Text = "ค่าเช่ารายเดือน:",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _roomMonthlyRentTextBox.Name = "RoomMonthlyRentTextBox";
        _roomMonthlyRentTextBox.Dock = DockStyle.Fill;

        _roomCableTvExemptCheckBox.Name = "RoomCableTvExemptCheckBox";
        _roomCableTvExemptCheckBox.Text = "ยกเว้นค่า Cable TV";
        _roomCableTvExemptCheckBox.Dock = DockStyle.Fill;
        _roomCableTvExemptCheckBox.Padding = new Padding(12, 0, 0, 0);

        _addRoomButton.Name = "AddRoomButton";
        _addRoomButton.Text = "เพิ่มห้อง";
        _addRoomButton.AutoSize = true;
        _addRoomButton.Click += AddRoomButton_Click;

        _editRoomRentButton.Name = "EditRoomRentButton";
        _editRoomRentButton.Text = "แก้ไขค่าเช่า";
        _editRoomRentButton.AutoSize = true;
        _editRoomRentButton.Enabled = false;
        _editRoomRentButton.Click += EditRoomRentButton_Click;

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 2, 0, 0)
        };
        buttonPanel.Controls.Add(_editRoomRentButton);
        buttonPanel.Controls.Add(_addRoomButton);

        _roomMessageLabel.Name = "RoomMessageLabel";
        _roomMessageLabel.Dock = DockStyle.Fill;
        _roomMessageLabel.TextAlign = ContentAlignment.MiddleLeft;
        _roomMessageLabel.ForeColor = Color.DarkGreen;

        layout.Controls.Add(roomNumberLabel, 0, 0);
        layout.Controls.Add(_roomNumberTextBox, 1, 0);
        layout.Controls.Add(roomMonthlyRentLabel, 0, 1);
        layout.Controls.Add(_roomMonthlyRentTextBox, 1, 1);
        layout.Controls.Add(_roomCableTvExemptCheckBox, 2, 1);
        layout.Controls.Add(buttonPanel, 2, 2);
        layout.Controls.Add(_roomMessageLabel, 0, 2);
        layout.SetColumnSpan(_roomMessageLabel, 2);

        group.Controls.Add(layout);
        return group;
    }

    private GroupBox BuildListGroup()
    {
        var group = new GroupBox
        {
            Name = "RoomListGroup",
            Text = "รายการห้องพัก",
            Dock = DockStyle.Fill,
            Padding = new Padding(8)
        };

        _roomGrid.Name = "RoomGrid";
        _roomGrid.Dock = DockStyle.Fill;
        _roomGrid.ReadOnly = true;
        _roomGrid.AllowUserToAddRows = false;
        _roomGrid.AllowUserToDeleteRows = false;
        _roomGrid.AutoGenerateColumns = false;
        _roomGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _roomGrid.MultiSelect = false;
        _roomGrid.RowHeadersVisible = false;
        _roomGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _roomGrid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _roomGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 248, 252);
        _roomGrid.SelectionChanged += RoomGrid_SelectionChanged;
        _roomGrid.CellDoubleClick += RoomGrid_CellDoubleClick;
        _roomGrid.RowTemplate.Height = 32;
        _roomGrid.ColumnHeadersHeight = 36;
        _roomGrid.ColumnHeadersDefaultCellStyle.Font = new Font(Font, FontStyle.Bold);

        var sequenceColumn = new DataGridViewTextBoxColumn
        {
            Name = "Sequence",
            HeaderText = "ลำดับ",
            DataPropertyName = "Sequence",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            Width = 70,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleCenter
            }
        };
        _roomGrid.Columns.Add(sequenceColumn);
        _roomGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "RoomNumber",
            HeaderText = "หมายเลขห้อง",
            DataPropertyName = "RoomNumber",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            Width = 150
        });
        _roomGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "OccupancyStatus",
            HeaderText = "สถานะห้อง",
            DataPropertyName = "OccupancyStatus",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            Width = 120,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleCenter
            }
        });
        _roomGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "TenantName",
            HeaderText = "ผู้เช่า",
            DataPropertyName = "TenantName",
            MinimumWidth = 180
        });
        _roomGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "MonthlyRent",
            HeaderText = "ค่าเช่ารายเดือน",
            DataPropertyName = "MonthlyRent",
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleRight,
                Format = "N2"
            }
        });
        _roomGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "CableTvExempt",
            HeaderText = "ยกเว้น Cable TV",
            DataPropertyName = "CableTvExempt",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            Width = 180,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleCenter
            }
        });

        group.Controls.Add(_roomGrid);
        return group;
    }

    private void AddRoomButton_Click(object? sender, EventArgs e)
    {
        if (!decimal.TryParse(
                _roomMonthlyRentTextBox.Text.Trim(),
                out var monthlyRent) || monthlyRent < 0)
        {
            ShowRoomError("กรุณาระบุค่าเช่ารายเดือนเป็นจำนวนเงินที่ถูกต้อง");
            return;
        }

        try
        {
            var room = _createRoomService.Create(
                _roomNumberTextBox.Text,
                monthlyRent,
                _roomCableTvExemptCheckBox.Checked);

            RefreshRoomList();
            ClearRoomInput();
            ShowRoomSuccess($"สร้างห้อง {room.RoomNumber.Value} สำเร็จแล้ว");
            RoomsChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (ArgumentException ex)
        {
            ShowRoomError(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            ShowRoomError(ex.Message);
        }
    }

    private void RoomGrid_SelectionChanged(object? sender, EventArgs e)
    {
        _editRoomRentButton.Enabled = _editingRoomId is null && _roomGrid.SelectedRows.Count > 0;
    }

    private void RoomGrid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || _roomGrid.Rows[e.RowIndex].DataBoundItem is not RoomGridItem selectedRoom)
        {
            return;
        }

        BeginRoomRentEditing(selectedRoom);
    }

    private void EditRoomRentButton_Click(object? sender, EventArgs e)
    {
        if (_editingRoomId is null)
        {
            if (_roomGrid.CurrentRow?.DataBoundItem is not RoomGridItem selectedRoom)
            {
                ShowRoomError("กรุณาเลือกห้องพักที่ต้องการแก้ไข");
                return;
            }

            BeginRoomRentEditing(selectedRoom);
            return;
        }

        if (!decimal.TryParse(
                _roomMonthlyRentTextBox.Text.Trim(),
                out var monthlyRent) || monthlyRent < 0)
        {
            ShowRoomError("กรุณาระบุค่าเช่ารายเดือนเป็นจำนวนเงินที่ถูกต้อง");
            return;
        }

        try
        {
            var room = _updateRoomRentService.Update(_editingRoomId, monthlyRent);
            RefreshRoomList();
            ResetRoomRentEditing();
            ShowRoomSuccess($"แก้ไขค่าเช่าห้อง {room.RoomNumber.Value} สำเร็จแล้ว");
            RoomsChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (ArgumentException ex)
        {
            ShowRoomError(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            ShowRoomError(ex.Message);
        }
    }

    private void ClearRoomInput()
    {
        _roomNumberTextBox.Clear();
        _roomMonthlyRentTextBox.Clear();
        _roomCableTvExemptCheckBox.Checked = false;
        _roomNumberTextBox.Focus();
    }

    private void ResetRoomRentEditing()
    {
        _editingRoomId = null;
        _editRoomRentButton.Text = "แก้ไขค่าเช่า";
        _addRoomButton.Enabled = true;
        _roomNumberTextBox.ReadOnly = false;
        _roomCableTvExemptCheckBox.Enabled = true;
        _roomNumberTextBox.Clear();
        _roomMonthlyRentTextBox.Clear();
    }

    private void BeginRoomRentEditing(RoomGridItem selectedRoom)
    {
        _editingRoomId = selectedRoom.RoomId;
        _roomNumberTextBox.Text = selectedRoom.RoomNumber;
        _roomMonthlyRentTextBox.Text = selectedRoom.MonthlyRent;
        _roomCableTvExemptCheckBox.Checked = selectedRoom.CableTvExempt == "ใช่";
        _roomNumberTextBox.ReadOnly = true;
        _roomCableTvExemptCheckBox.Enabled = false;
        _editRoomRentButton.Text = "บันทึกค่าเช่า";
        _editRoomRentButton.Enabled = true;
        _addRoomButton.Enabled = false;
        _roomMonthlyRentTextBox.Focus();
        _roomMonthlyRentTextBox.SelectAll();
    }

    private void ShowRoomSuccess(string message)
    {
        _roomMessageLabel.ForeColor = Color.DarkGreen;
        _roomMessageLabel.Text = message;
    }

    private void ShowRoomError(string message)
    {
        _roomMessageLabel.ForeColor = Color.DarkRed;
        _roomMessageLabel.Text = message;
    }
}
