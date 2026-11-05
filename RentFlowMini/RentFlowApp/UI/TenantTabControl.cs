using System.Globalization;
using RentFlowApp.Application.Billing;
using RentFlowApp.Application.Rooms;
using RentFlowApp.Application.Tenants;
using RentFlowApp.Domain;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Forms;

public class TenantTabControl : BaseTabControl
{
    private const string AddTenantButtonText = "เพิ่มผู้เช่า";
    private const string SaveTenantEditButtonText = "บันทึกการแก้ไข";
    private const string NoValue = "-";

    private readonly ITenantRepository _tenantRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly IBillRepository _billRepository;
    private readonly CreateTenantService _createTenantService;
    private readonly UpdateTenantService _updateTenantService;
    private readonly MoveOutTenantService _moveOutTenantService;
    private readonly Func<string, bool> _confirmMoveOut;

    private readonly TextBox _tenantFirstNameTextBox = new();
    private readonly TextBox _tenantLastNameTextBox = new();
    private readonly TextBox _tenantPhoneTextBox = new();
    private readonly ComboBox _tenantRoomComboBox = new();
    private readonly DateTimePicker _moveInDatePicker = new();
    private readonly TextBox _securityDepositTextBox = new();
    private readonly Button _addTenantButton = new();
    private readonly Button _cancelEditButton = new();
    private readonly Label _tenantMessageLabel = new();
    private readonly TextBox _searchTextBox = new();
    private readonly CheckBox _showMovedOutCheckBox = new();
    private readonly DateTimePicker _moveOutDatePicker = new();
    private readonly DataGridView _tenantGrid = new();

    private TenantId? _editingTenantId;

    public event EventHandler? TenantsChanged;

    private sealed record TenantRoomOption(RoomId? Id, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }

    private sealed record TenantGridItem(
        int Sequence,
        string RoomNumber,
        string FullName,
        string PhoneNumber,
        string MoveInDate,
        string SecurityDeposit,
        string OutstandingAmount,
        string MovedOutDate,
        Guid TenantId,
        bool IsMovedOut,
        bool HasOutstanding);

    public TenantTabControl(
        ITenantRepository tenantRepository,
        IRoomRepository roomRepository,
        CreateTenantService createTenantService,
        IBillRepository billRepository,
        Func<string, bool>? confirmMoveOut = null)
    {
        ArgumentNullException.ThrowIfNull(tenantRepository);
        ArgumentNullException.ThrowIfNull(roomRepository);
        ArgumentNullException.ThrowIfNull(createTenantService);
        ArgumentNullException.ThrowIfNull(billRepository);

        _tenantRepository = tenantRepository;
        _roomRepository = roomRepository;
        _billRepository = billRepository;
        _createTenantService = createTenantService;
        _updateTenantService = new UpdateTenantService(tenantRepository);
        _moveOutTenantService = new MoveOutTenantService(tenantRepository);
        _confirmMoveOut = confirmMoveOut ?? ShowMoveOutConfirmation;

        Name = "TenantTabControl";
        // แถบเครื่องมือจัดตำแหน่งตายตัว จอแคบจึงต้องเลื่อนแนวนอนเพื่อให้ถึงปุ่มย้ายออก
        AutoScroll = true;
        AutoScrollMinSize = new Size(850, 0);

        InitializeComponents();
        RefreshTab();
    }

    private void InitializeComponents()
    {
        var mainLayout = CreateStandardLayout(3, [50f, 190f, -1f]);
        var titleLabel = CreateTitleLabel("จัดการข้อมูลผู้เช่า");
        var inputGroup = BuildInputGroup();
        var listGroup = BuildListGroup();

        mainLayout.Controls.Add(titleLabel, 0, 0);
        mainLayout.Controls.Add(inputGroup, 0, 1);
        mainLayout.Controls.Add(listGroup, 0, 2);
        Controls.Add(mainLayout);
    }

    private GroupBox BuildInputGroup()
    {
        var group = new GroupBox
        {
            Name = "TenantInputGroup",
            Text = "เพิ่ม / แก้ไขผู้เช่า",
            Dock = DockStyle.Fill,
            Padding = new Padding(12)
        };

        var panel = new Panel
        {
            Dock = DockStyle.Fill
        };

        var firstNameLabel = CreateFieldLabel("ชื่อผู้เช่า:", 12, 15, 80);
        _tenantFirstNameTextBox.Name = "TenantFirstNameTextBox";
        _tenantFirstNameTextBox.SetBounds(95, 12, 150, 30);

        var lastNameLabel = CreateFieldLabel("นามสกุล:", 260, 15, 70);
        _tenantLastNameTextBox.Name = "TenantLastNameTextBox";
        _tenantLastNameTextBox.SetBounds(335, 12, 150, 30);

        var phoneLabel = CreateFieldLabel("เบอร์โทรศัพท์:", 500, 15, 100);
        _tenantPhoneTextBox.Name = "TenantPhoneTextBox";
        _tenantPhoneTextBox.SetBounds(605, 12, 130, 30);
        _tenantPhoneTextBox.MaxLength = 10;
        ConfigureNumericTextBox(_tenantPhoneTextBox, allowDecimal: false);

        var roomLabel = CreateFieldLabel("ห้องพัก:", 12, 53, 80);
        _tenantRoomComboBox.Name = "TenantRoomComboBox";
        _tenantRoomComboBox.SetBounds(95, 50, 150, 30);
        _tenantRoomComboBox.DropDownStyle = ComboBoxStyle.DropDownList;

        var moveInLabel = CreateFieldLabel("เข้าอยู่:", 260, 53, 70);
        _moveInDatePicker.Name = "TenantMoveInDatePicker";
        _moveInDatePicker.Format = DateTimePickerFormat.Short;
        _moveInDatePicker.ShowCheckBox = true;
        _moveInDatePicker.SetBounds(335, 50, 150, 30);

        var depositLabel = CreateFieldLabel("เงินประกัน:", 500, 53, 100);
        _securityDepositTextBox.Name = "TenantSecurityDepositTextBox";
        _securityDepositTextBox.SetBounds(605, 50, 130, 30);
        _securityDepositTextBox.TextAlign = HorizontalAlignment.Right;
        ConfigureNumericTextBox(_securityDepositTextBox, allowThousandsSeparator: true);

        HandleEnterKey(
            () =>
            {
                if (_editingTenantId is not null)
                {
                    _addTenantButton.PerformClick();
                }
            },
            _tenantFirstNameTextBox,
            _tenantLastNameTextBox,
            _tenantPhoneTextBox,
            _tenantRoomComboBox,
            _moveInDatePicker,
            _securityDepositTextBox);

        _addTenantButton.Name = "AddTenantButton";
        _addTenantButton.Text = AddTenantButtonText;
        _addTenantButton.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        _addTenantButton.SetBounds(95, 88, 140, 34);
        _addTenantButton.Click += AddTenantButton_Click;

        _cancelEditButton.Name = "CancelTenantEditButton";
        _cancelEditButton.Text = "ยกเลิก";
        _cancelEditButton.Enabled = false;
        _cancelEditButton.SetBounds(245, 88, 90, 34);
        _cancelEditButton.Click += (s, e) =>
        {
            ClearInputForm();
            _tenantMessageLabel.Text = string.Empty;
        };

        _tenantMessageLabel.Name = "TenantMessageLabel";
        _tenantMessageLabel.SetBounds(345, 93, 800, 25);
        _tenantMessageLabel.ForeColor = Color.DarkGreen;

        panel.Controls.AddRange(
        [
            firstNameLabel, _tenantFirstNameTextBox,
            lastNameLabel, _tenantLastNameTextBox,
            phoneLabel, _tenantPhoneTextBox,
            roomLabel, _tenantRoomComboBox,
            moveInLabel, _moveInDatePicker,
            depositLabel, _securityDepositTextBox,
            _addTenantButton,
            _cancelEditButton,
            _tenantMessageLabel
        ]);

        group.Controls.Add(panel);
        return group;
    }

    private static Label CreateFieldLabel(string text, int left, int top, int width) => new()
    {
        Text = text,
        Left = left,
        Top = top,
        Width = width,
        Height = 25
    };

    private GroupBox BuildListGroup()
    {
        var group = new GroupBox
        {
            Name = "TenantListGroup",
            Text = "รายการผู้เช่าทั้งหมด",
            Dock = DockStyle.Fill,
            Padding = new Padding(12)
        };

        _tenantGrid.Name = "TenantGrid";
        _tenantGrid.Dock = DockStyle.Fill;
        _tenantGrid.ReadOnly = true;
        _tenantGrid.AllowUserToAddRows = false;
        _tenantGrid.AllowUserToDeleteRows = false;
        _tenantGrid.AutoGenerateColumns = false;
        _tenantGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _tenantGrid.MultiSelect = false;
        _tenantGrid.RowHeadersVisible = false;
        _tenantGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _tenantGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 248, 252);
        _tenantGrid.RowTemplate.Height = 30;
        _tenantGrid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _tenantGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        _tenantGrid.ColumnHeadersDefaultCellStyle.Font = new Font(Font, FontStyle.Bold);
        _tenantGrid.CellDoubleClick += TenantGrid_CellDoubleClick;
        _tenantGrid.CellFormatting += TenantGrid_CellFormatting;
        // ไม่เลือกแถวแรกให้เอง เพื่อกันการกดย้ายออกผิดคน
        _tenantGrid.DataBindingComplete += (s, e) => _tenantGrid.ClearSelection();

        AddColumn("Sequence", "ลำดับ", 50, DataGridViewContentAlignment.MiddleCenter);
        AddColumn("RoomNumber", "ห้องพัก", 70, DataGridViewContentAlignment.MiddleCenter);
        AddColumn("FullName", "ชื่อ - นามสกุล", 200, DataGridViewContentAlignment.MiddleLeft);
        AddColumn("PhoneNumber", "เบอร์โทรศัพท์", 110, DataGridViewContentAlignment.MiddleCenter);
        AddColumn("MoveInDate", "เข้าอยู่", 100, DataGridViewContentAlignment.MiddleCenter);
        AddColumn("SecurityDeposit", "เงินประกัน", 90, DataGridViewContentAlignment.MiddleRight);
        AddColumn("OutstandingAmount", "ยอดค้างชำระ", 100, DataGridViewContentAlignment.MiddleRight);
        AddColumn("MovedOutDate", "ย้ายออก", 100, DataGridViewContentAlignment.MiddleCenter).Visible = false;
        // ซ่อนรหัสไว้ใช้ภายใน ผู้ใช้ไม่จำเป็นต้องเห็น
        AddColumn("TenantId", "รหัสผู้เช่า", 100, DataGridViewContentAlignment.MiddleLeft).Visible = false;

        var toolbar = new Panel { Dock = DockStyle.Top, Height = 40 };

        var searchLabel = CreateFieldLabel("ค้นหา:", 0, 9, 50);
        _searchTextBox.Name = "TenantSearchTextBox";
        _searchTextBox.PlaceholderText = "ชื่อ ห้อง หรือเบอร์โทร";
        _searchTextBox.SetBounds(55, 6, 180, 30);
        _searchTextBox.TextChanged += (s, e) => RefreshTenantList();

        _showMovedOutCheckBox.Name = "ShowMovedOutTenantsCheckBox";
        _showMovedOutCheckBox.Text = "แสดงผู้เช่าที่ย้ายออกแล้ว";
        _showMovedOutCheckBox.SetBounds(250, 7, 200, 26);
        _showMovedOutCheckBox.CheckedChanged += (s, e) =>
        {
            _tenantGrid.Columns["MovedOutDate"]!.Visible = _showMovedOutCheckBox.Checked;
            RefreshTenantList();
        };

        var moveOutDateLabel = CreateFieldLabel("วันที่ย้ายออก:", 460, 9, 90);
        _moveOutDatePicker.Name = "TenantMoveOutDatePicker";
        _moveOutDatePicker.Format = DateTimePickerFormat.Short;
        _moveOutDatePicker.SetBounds(555, 6, 125, 28);

        var moveOutButton = new Button
        {
            Name = "MoveOutTenantButton",
            Text = "ย้ายออก",
            Left = 690,
            Top = 3,
            Width = 100,
            Height = 34
        };
        moveOutButton.Click += MoveOutButton_Click;

        toolbar.Controls.AddRange(
        [
            searchLabel, _searchTextBox,
            _showMovedOutCheckBox,
            moveOutDateLabel, _moveOutDatePicker, moveOutButton
        ]);

        // แถบเครื่องมือต้องเพิ่มหลังตาราง เพื่อให้ WinForms จัด Dock.Top ก่อน Dock.Fill
        group.Controls.Add(_tenantGrid);
        group.Controls.Add(toolbar);
        return group;
    }

    private DataGridViewTextBoxColumn AddColumn(
        string name,
        string header,
        int fillWeight,
        DataGridViewContentAlignment alignment)
    {
        var column = new DataGridViewTextBoxColumn
        {
            Name = name,
            HeaderText = header,
            DataPropertyName = name,
            FillWeight = fillWeight,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = alignment }
        };
        _tenantGrid.Columns.Add(column);
        return column;
    }

    public void RefreshTab()
    {
        PopulateTenantRoomOptions();
        RefreshTenantList();
    }

    private void PopulateTenantRoomOptions()
    {
        var selectedItem = _tenantRoomComboBox.SelectedItem as TenantRoomOption;
        _tenantRoomComboBox.Items.Clear();

        var defaultOption = new TenantRoomOption(null, "-- ไม่ระบุห้องพัก --");
        _tenantRoomComboBox.Items.Add(defaultOption);

        // ตอนแก้ไขต้องเลือกห้องเดิมของผู้เช่าคนนี้ได้ ไม่เช่นนั้นห้องจะหายจากรายการ
        var editingRoomId = _editingTenantId is null ? null : _tenantRepository.GetById(_editingTenantId)?.RoomId;

        var rooms = _roomRepository.GetAll();
        foreach (var room in rooms)
        {
            if (room.Id == editingRoomId || !_tenantRepository.ExistsByRoomId(room.Id))
            {
                var option = new TenantRoomOption(room.Id, $"ห้อง {room.RoomNumber.Value}");
                _tenantRoomComboBox.Items.Add(option);

                if (selectedItem != null && selectedItem.Id == room.Id)
                {
                    _tenantRoomComboBox.SelectedItem = option;
                }
            }
        }

        if (_tenantRoomComboBox.SelectedItem == null && _tenantRoomComboBox.Items.Count > 0)
        {
            _tenantRoomComboBox.SelectedIndex = 0;
        }
    }

    private void RefreshTenantList()
    {
        var rooms = _roomRepository.GetAll().ToDictionary(r => r.Id, r => r.RoomNumber.Value);
        // ยอดค้างผูกกับผู้เช่าในบิล ไม่ใช่ห้อง เพื่อไม่ให้หนี้ของคนเก่าไปตกกับคนใหม่ในห้องเดิม
        var outstandingByTenant = _billRepository.GetAll()
            .Where(bill => !bill.IsPaid && bill.Tenant is not null)
            .GroupBy(bill => bill.Tenant!.TenantId)
            .ToDictionary(group => group.Key, group => group.Sum(bill => bill.Total.Amount));
        string search = _searchTextBox.Text.Trim();

        _tenantGrid.DataSource = _tenantRepository.GetAll()
            .Where(tenant => _showMovedOutCheckBox.Checked || !tenant.IsMovedOut)
            .Select(tenant => new
            {
                Tenant = tenant,
                Room = GetRoomDisplay(tenant, rooms),
                Outstanding = outstandingByTenant.GetValueOrDefault(tenant.Id)
            })
            .Where(row => search.Length == 0 ||
                row.Tenant.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                row.Room.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (row.Tenant.PhoneNumber?.Contains(search, StringComparison.Ordinal) ?? false))
            // คนที่พักอยู่มาก่อน และเรียงเลขห้องให้ 99 มาก่อน 101
            .OrderBy(row => row.Tenant.RoomId is null)
            .ThenBy(row => row.Room.Length)
            .ThenBy(row => row.Room, StringComparer.Ordinal)
            .ThenBy(row => row.Tenant.FullName, StringComparer.Ordinal)
            .Select((row, index) => new TenantGridItem(
                index + 1,
                row.Room,
                row.Tenant.FullName,
                row.Tenant.PhoneNumber ?? NoValue,
                row.Tenant.MoveInDate is { } moveIn ? FormatDateThai(moveIn) : NoValue,
                row.Tenant.SecurityDeposit is { } deposit ? deposit.ToString("N2") : NoValue,
                row.Outstanding > 0m ? row.Outstanding.ToString("N2") : NoValue,
                row.Tenant.MovedOutOn is { } movedOut ? FormatDateThai(movedOut) : NoValue,
                row.Tenant.Id.Value,
                row.Tenant.IsMovedOut,
                row.Outstanding > 0m))
            .ToList();
    }

    private static string GetRoomDisplay(Tenant tenant, Dictionary<RoomId, string> rooms)
    {
        if (tenant.IsMovedOut)
        {
            return tenant.MovedOutFromRoomId is not null && rooms.TryGetValue(tenant.MovedOutFromRoomId, out var previousRoom)
                ? $"{previousRoom} (เดิม)"
                : NoValue;
        }

        return tenant.RoomId is not null && rooms.TryGetValue(tenant.RoomId, out var room)
            ? room
            : "ไม่มีห้องพัก";
    }

    private static string FormatDateThai(DateOnly date) =>
        $"{date.Day:D2}/{date.Month:D2}/{date.Year + 543}";

    private void TenantGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || _tenantGrid.Rows[e.RowIndex].DataBoundItem is not TenantGridItem item)
        {
            return;
        }

        if (item.HasOutstanding && _tenantGrid.Columns[e.ColumnIndex].Name == "OutstandingAmount")
        {
            e.CellStyle.ForeColor = Color.Firebrick;
            e.CellStyle.SelectionForeColor = Color.MistyRose;
        }
        else if (item.IsMovedOut)
        {
            e.CellStyle.ForeColor = Color.Gray;
        }
    }

    private void AddTenantButton_Click(object? sender, EventArgs e)
    {
        if (!TryReadSecurityDeposit(out decimal? securityDeposit))
        {
            ShowTenantError("เงินประกันต้องเป็นตัวเลข");
            return;
        }

        string firstName = _tenantFirstNameTextBox.Text;
        string lastName = _tenantLastNameTextBox.Text;
        string? phone = string.IsNullOrWhiteSpace(_tenantPhoneTextBox.Text) ? null : _tenantPhoneTextBox.Text.Trim();
        RoomId? selectedRoomId = (_tenantRoomComboBox.SelectedItem as TenantRoomOption)?.Id;
        DateOnly? moveInDate = _moveInDatePicker.Checked ? DateOnly.FromDateTime(_moveInDatePicker.Value) : null;

        try
        {
            string successMessage;
            if (_editingTenantId is null)
            {
                var tenant = _createTenantService.Create(firstName, lastName, phone, selectedRoomId, moveInDate, securityDeposit);
                successMessage = $"เพิ่มผู้เช่า {tenant.FullName} สำเร็จแล้ว";
            }
            else
            {
                var tenant = _updateTenantService.Update(new UpdateTenantRequest(
                    _editingTenantId, firstName, lastName, phone, selectedRoomId, moveInDate, securityDeposit));
                successMessage = $"แก้ไขข้อมูล {tenant.FullName} สำเร็จแล้ว";
            }

            ClearInputForm();
            TenantsChanged?.Invoke(this, EventArgs.Empty);
            _tenantFirstNameTextBox.Focus();

            _tenantMessageLabel.ForeColor = Color.DarkGreen;
            _tenantMessageLabel.Text = successMessage;
        }
        catch (ArgumentException ex)
        {
            ShowTenantError(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            ShowTenantError(ex.Message);
        }
    }

    private bool TryReadSecurityDeposit(out decimal? securityDeposit)
    {
        securityDeposit = null;
        string text = _securityDepositTextBox.Text.Trim();
        if (text.Length == 0)
        {
            return true;
        }

        if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value))
        {
            return false;
        }

        securityDeposit = value;
        return true;
    }

    private void ClearInputForm()
    {
        _editingTenantId = null;
        _addTenantButton.Text = AddTenantButtonText;
        _cancelEditButton.Enabled = false;

        _tenantFirstNameTextBox.Clear();
        _tenantLastNameTextBox.Clear();
        _tenantPhoneTextBox.Clear();
        _securityDepositTextBox.Clear();
        _moveInDatePicker.Value = DateTime.Now;
        _moveInDatePicker.Checked = true;

        RefreshTab();
        _tenantRoomComboBox.SelectedIndex = 0;
    }

    private void TenantGrid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 ||
            _tenantGrid.Rows[e.RowIndex].DataBoundItem is not TenantGridItem item ||
            _tenantRepository.GetById(TenantId.From(item.TenantId)) is not { } tenant)
        {
            return;
        }

        if (tenant.IsMovedOut)
        {
            _tenantMessageLabel.ForeColor = Color.DarkBlue;
            _tenantMessageLabel.Text = $"{tenant.FullName} ย้ายออกแล้ว จึงแก้ไขข้อมูลไม่ได้";
            return;
        }

        _editingTenantId = tenant.Id;
        _addTenantButton.Text = SaveTenantEditButtonText;
        _cancelEditButton.Enabled = true;

        _tenantFirstNameTextBox.Text = tenant.FirstName;
        _tenantLastNameTextBox.Text = tenant.LastName;
        _tenantPhoneTextBox.Text = tenant.PhoneNumber ?? string.Empty;
        _securityDepositTextBox.Text = tenant.SecurityDeposit?.ToString("0.##", CultureInfo.InvariantCulture) ?? string.Empty;

        // ถ้ายังไม่มีวันเข้าอยู่ ให้เอาติ๊กออก เพื่อไม่ให้บันทึกวันนี้แทนโดยไม่ตั้งใจ
        _moveInDatePicker.Value = tenant.MoveInDate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.Now;
        _moveInDatePicker.Checked = tenant.MoveInDate is not null;

        PopulateTenantRoomOptions();
        _tenantRoomComboBox.SelectedItem = _tenantRoomComboBox.Items
            .Cast<TenantRoomOption>()
            .First(option => option.Id == tenant.RoomId);

        _tenantMessageLabel.ForeColor = Color.DarkBlue;
        _tenantMessageLabel.Text = $"กำลังแก้ไขข้อมูล {tenant.FullName}";

        // ย้ายออกจากตาราง เพราะ Enter ในตารางจะเลื่อนแถวแทนการบันทึก
        _tenantFirstNameTextBox.Focus();
        _tenantFirstNameTextBox.SelectAll();
    }

    private void MoveOutButton_Click(object? sender, EventArgs e)
    {
        if (_tenantGrid.SelectedRows.Count != 1 ||
            _tenantGrid.SelectedRows[0].DataBoundItem is not TenantGridItem item)
        {
            ShowTenantError("กรุณาเลือกผู้เช่าในตาราง 1 รายการ");
            return;
        }

        var tenantId = TenantId.From(item.TenantId);
        var movedOutOn = DateOnly.FromDateTime(_moveOutDatePicker.Value);

        // รายการที่ย้ายออกไปแล้วไม่ต้องถามยืนยัน ให้ service แจ้งข้อความแทน
        if (!item.IsMovedOut && !_confirmMoveOut(BuildMoveOutConfirmation(item, tenantId, movedOutOn)))
        {
            return;
        }

        try
        {
            var tenant = _moveOutTenantService.MoveOut(tenantId, movedOutOn);

            if (_editingTenantId == tenant.Id)
            {
                ClearInputForm();
            }
            else
            {
                RefreshTab();
            }

            TenantsChanged?.Invoke(this, EventArgs.Empty);

            _tenantMessageLabel.ForeColor = Color.DarkGreen;
            _tenantMessageLabel.Text = $"บันทึกการย้ายออกของ {tenant.FullName} สำเร็จแล้ว";
        }
        catch (InvalidOperationException ex)
        {
            ShowTenantError(ex.Message);
        }
    }

    private string BuildMoveOutConfirmation(TenantGridItem item, TenantId tenantId, DateOnly movedOutOn)
    {
        string message =
            $"ยืนยันการย้ายออกของ {item.FullName} (ห้อง {item.RoomNumber}) เมื่อวันที่ {FormatDateThai(movedOutOn)} ใช่หรือไม่?\n\n" +
            "ห้องจะว่างทันที และยกเลิกการย้ายออกภายหลังไม่ได้";

        var unpaidBills = _billRepository.GetAll()
            .Where(bill => !bill.IsPaid && bill.Tenant?.TenantId == tenantId)
            .ToList();

        return unpaidBills.Count == 0
            ? message
            : $"{message}\n\nผู้เช่ารายนี้ยังค้างชำระ {unpaidBills.Count} บิล รวม {unpaidBills.Sum(bill => bill.Total.Amount):N2} บาท " +
              "และยังติดตามรับชำระได้ในแท็บสร้างบิล";
    }

    private bool ShowMoveOutConfirmation(string message) =>
        MessageBox.Show(
            FindForm(),
            message,
            "ยืนยันการย้ายออก",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2) == DialogResult.Yes;

    private void ShowTenantError(string message)
    {
        _tenantMessageLabel.ForeColor = Color.DarkRed;
        _tenantMessageLabel.Text = message;
    }
}
