using RentFlowApp.Application.Rooms;
using RentFlowApp.Application.Tenants;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Forms;

public class TenantTabControl : BaseTabControl
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly CreateTenantService _createTenantService;

    private readonly TextBox _tenantFirstNameTextBox = new();
    private readonly TextBox _tenantLastNameTextBox = new();
    private readonly TextBox _tenantPhoneTextBox = new();
    private readonly ComboBox _tenantRoomComboBox = new();
    private readonly Button _addTenantButton = new();
    private readonly Label _tenantMessageLabel = new();
    private readonly DataGridView _tenantGrid = new();

    public event EventHandler? TenantAdded;

    private sealed record TenantRoomOption(RoomId? Id, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }

    public TenantTabControl(
        ITenantRepository tenantRepository,
        IRoomRepository roomRepository,
        CreateTenantService createTenantService)
    {
        ArgumentNullException.ThrowIfNull(tenantRepository);
        ArgumentNullException.ThrowIfNull(roomRepository);
        ArgumentNullException.ThrowIfNull(createTenantService);

        _tenantRepository = tenantRepository;
        _roomRepository = roomRepository;
        _createTenantService = createTenantService;

        Name = "TenantTabControl";

        InitializeComponents();
        RefreshTab();
    }

    private void InitializeComponents()
    {
        var mainLayout = CreateStandardLayout(3, [50f, 200f, -1f]);
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
            Text = "จัดการข้อมูลผู้เช่า",
            Dock = DockStyle.Fill,
            Padding = new Padding(12)
        };

        var panel = new Panel
        {
            Dock = DockStyle.Fill
        };

        var firstNameLabel = new Label
        {
            Text = "ชื่อผู้เช่า:",
            Left = 12,
            Top = 25,
            Width = 100,
            Height = 25
        };
        _tenantFirstNameTextBox.Name = "TenantFirstNameTextBox";
        _tenantFirstNameTextBox.SetBounds(120, 22, 180, 30);

        var lastNameLabel = new Label
        {
            Text = "นามสกุล:",
            Left = 320,
            Top = 25,
            Width = 80,
            Height = 25
        };
        _tenantLastNameTextBox.Name = "TenantLastNameTextBox";
        _tenantLastNameTextBox.SetBounds(410, 22, 180, 30);

        var phoneLabel = new Label
        {
            Text = "เบอร์โทรศัพท์:",
            Left = 12,
            Top = 70,
            Width = 100,
            Height = 25
        };
        _tenantPhoneTextBox.Name = "TenantPhoneTextBox";
        _tenantPhoneTextBox.SetBounds(120, 67, 180, 30);
        _tenantPhoneTextBox.MaxLength = 10;
        _tenantPhoneTextBox.KeyPress += TenantPhoneTextBox_KeyPress;

        var roomLabel = new Label
        {
            Text = "ห้องพัก:",
            Left = 320,
            Top = 70,
            Width = 80,
            Height = 25
        };
        _tenantRoomComboBox.Name = "TenantRoomComboBox";
        _tenantRoomComboBox.SetBounds(410, 67, 180, 30);
        _tenantRoomComboBox.DropDownStyle = ComboBoxStyle.DropDownList;

        _addTenantButton.Name = "AddTenantButton";
        _addTenantButton.Text = "เพิ่มผู้เช่า";
        _addTenantButton.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        _addTenantButton.SetBounds(610, 65, 120, 35);
        _addTenantButton.Click += AddTenantButton_Click;

        _tenantMessageLabel.Name = "TenantMessageLabel";
        _tenantMessageLabel.SetBounds(12, 115, 720, 25);
        _tenantMessageLabel.ForeColor = Color.DarkGreen;

        panel.Controls.AddRange(
        [
            firstNameLabel, _tenantFirstNameTextBox,
            lastNameLabel, _tenantLastNameTextBox,
            phoneLabel, _tenantPhoneTextBox,
            roomLabel, _tenantRoomComboBox,
            _addTenantButton,
            _tenantMessageLabel
        ]);

        group.Controls.Add(panel);
        return group;
    }

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

        _tenantGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "FullName",
            HeaderText = "ชื่อ - นามสกุล",
            DataPropertyName = "FullName"
        });

        _tenantGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "PhoneNumber",
            HeaderText = "เบอร์โทรศัพท์",
            DataPropertyName = "PhoneNumber"
        });

        _tenantGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "RoomNumber",
            HeaderText = "ห้องพัก",
            DataPropertyName = "RoomNumber"
        });

        _tenantGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "TenantId",
            HeaderText = "รหัสผู้เช่า (Tenant ID)",
            DataPropertyName = "TenantId"
        });

        group.Controls.Add(_tenantGrid);
        return group;
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

        var rooms = _roomRepository.GetAll();
        foreach (var room in rooms)
        {
            if (!_tenantRepository.ExistsByRoomId(room.Id))
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
        var tenants = _tenantRepository.GetAll();
        var rooms = _roomRepository.GetAll().ToDictionary(r => r.Id, r => r.RoomNumber.Value);

        _tenantGrid.DataSource = tenants
            .Select(t => new
            {
                FullName = $"{t.FirstName} {t.LastName}",
                PhoneNumber = t.PhoneNumber ?? "-",
                RoomNumber = t.RoomId != null && rooms.TryGetValue(t.RoomId, out var rn) ? rn : "ไม่มีห้องพัก",
                TenantId = t.Id.Value
            })
            .ToList();
    }

    private void AddTenantButton_Click(object? sender, EventArgs e)
    {
        string firstName = _tenantFirstNameTextBox.Text;
        string lastName = _tenantLastNameTextBox.Text;
        string? phone = string.IsNullOrWhiteSpace(_tenantPhoneTextBox.Text) ? null : _tenantPhoneTextBox.Text.Trim();
        RoomId? selectedRoomId = (_tenantRoomComboBox.SelectedItem as TenantRoomOption)?.Id;

        try
        {
            var tenant = _createTenantService.Create(firstName, lastName, phone, selectedRoomId);
            RefreshTab();
            TenantAdded?.Invoke(this, EventArgs.Empty);

            _tenantFirstNameTextBox.Clear();
            _tenantLastNameTextBox.Clear();
            _tenantPhoneTextBox.Clear();
            _tenantFirstNameTextBox.Focus();

            _tenantMessageLabel.ForeColor = Color.DarkGreen;
            _tenantMessageLabel.Text = $"เพิ่มผู้เช่า {tenant.FirstName} {tenant.LastName} สำเร็จแล้ว";
        }
        catch (ArgumentException ex)
        {
            ShowTenantError(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            ShowTenantError(ex.Message);
        }
        catch (Exception ex)
        {
            ShowTenantError($"เกิดข้อผิดพลาด: {ex.Message}");
        }
    }

    private void ShowTenantError(string message)
    {
        _tenantMessageLabel.ForeColor = Color.DarkRed;
        _tenantMessageLabel.Text = message;
    }

    private static void TenantPhoneTextBox_KeyPress(object? sender, KeyPressEventArgs e)
    {
        if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
        {
            e.Handled = true;
        }
    }
}
