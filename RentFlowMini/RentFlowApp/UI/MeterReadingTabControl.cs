using RentFlowApp.Application.Billing;
using RentFlowApp.Application.Rooms;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Forms;

public class MeterReadingTabControl : UserControl
{
    private readonly IRoomRepository _roomRepository;
    private readonly IMonthlyMeterReadingRepository _meterReadingRepository;
    private readonly SaveMonthlyMeterReadingService _saveReadingService;

    private readonly ComboBox _roomComboBox = new();
    private readonly ComboBox _monthComboBox = new();
    private readonly ComboBox _yearComboBox = new();
    private readonly TextBox _prevWaterTextBox = new();
    private readonly TextBox _currWaterTextBox = new();
    private readonly TextBox _prevElecTextBox = new();
    private readonly TextBox _currElecTextBox = new();
    private readonly Button _saveButton = new();
    private readonly DataGridView _meterGrid = new();
    private readonly Label _messageLabel = new();

    private sealed record RoomItem(RoomId Id, string RoomNumber)
    {
        public override string ToString() => RoomNumber;
    }

    private sealed record MonthItem(int MonthNumber, string MonthName)
    {
        public override string ToString() => MonthName;
    }

    private sealed record MeterReadingGridItem(
        RoomId RoomId,
        int Sequence,
        string RoomNumber,
        string BillingPeriodDisplay,
        string BillingPeriodRaw,
        string WaterReading,
        string WaterUsed,
        string ElecReading,
        string ElecUsed);

    public MeterReadingTabControl(
        IRoomRepository roomRepository,
        IMonthlyMeterReadingRepository meterReadingRepository,
        SaveMonthlyMeterReadingService saveReadingService)
    {
        ArgumentNullException.ThrowIfNull(roomRepository);
        ArgumentNullException.ThrowIfNull(meterReadingRepository);
        ArgumentNullException.ThrowIfNull(saveReadingService);

        _roomRepository = roomRepository;
        _meterReadingRepository = meterReadingRepository;
        _saveReadingService = saveReadingService;

        Name = "MeterReadingTabControl";
        Dock = DockStyle.Fill;
        BackColor = SystemColors.Control;
        Font = new Font("Segoe UI", 11F);

        InitializeComponents();
        RefreshRoomList();
    }

    private void InitializeComponents()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 320));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var inputGroup = BuildInputGroup();
        var listGroup = BuildListGroup();

        layout.Controls.Add(inputGroup, 0, 0);
        layout.Controls.Add(listGroup, 0, 1);
        Controls.Add(layout);
    }

    private GroupBox BuildInputGroup()
    {
        var group = new GroupBox
        {
            Name = "MeterInputGroup",
            Text = "บันทึกมิเตอร์น้ำประปา-ไฟฟ้าประจำเดือน",
            Dock = DockStyle.Fill,
            Padding = new Padding(12)
        };

        var panel = new Panel
        {
            Dock = DockStyle.Fill
        };

        // Room Selection
        var roomLabel = new Label { Text = "เลือกห้อง:", Left = 12, Top = 25, Width = 90, Height = 25 };
        _roomComboBox.Name = "MeterRoomComboBox";
        _roomComboBox.SetBounds(110, 22, 180, 30);
        _roomComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _roomComboBox.SelectedIndexChanged += (s, e) => TryAutoLoadPreviousReadings();
        _roomComboBox.KeyDown += Control_EnterKeyDown;

        // Month Selection
        var monthLabel = new Label { Text = "เดือน:", Left = 310, Top = 25, Width = 60, Height = 25 };
        _monthComboBox.Name = "MeterMonthComboBox";
        _monthComboBox.SetBounds(370, 22, 140, 30);
        _monthComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        PopulateMonths();
        _monthComboBox.SelectedIndexChanged += (s, e) => RefreshRoomList();
        _monthComboBox.KeyDown += Control_EnterKeyDown;

        // Year Selection (Buddhist Era)
        var yearLabel = new Label { Text = "ปี (พ.ศ.):", Left = 530, Top = 25, Width = 70, Height = 25 };
        _yearComboBox.Name = "MeterYearComboBox";
        _yearComboBox.SetBounds(605, 22, 110, 30);
        _yearComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        PopulateYears();
        _yearComboBox.SelectedIndexChanged += (s, e) => RefreshRoomList();
        _yearComboBox.KeyDown += Control_EnterKeyDown;

        // Water GroupBox
        var waterGroup = new GroupBox
        {
            Name = "WaterReadingGroup",
            Text = "มิเตอร์น้ำประปา",
            Left = 12,
            Top = 70,
            Width = 350,
            Height = 140
        };

        var prevWaterLabel = new Label { Text = "มิเตอร์ก่อนหน้า:", Left = 15, Top = 35, Width = 130, Height = 25 };
        _prevWaterTextBox.Name = "PrevWaterTextBox";
        _prevWaterTextBox.SetBounds(150, 32, 170, 30);
        ConfigureNumericTextBox(_prevWaterTextBox);
        _prevWaterTextBox.ReadOnly = true;
        _prevWaterTextBox.BackColor = SystemColors.ControlLight;

        var currWaterLabel = new Label { Text = "มิเตอร์ปัจจุบัน:", Left = 15, Top = 80, Width = 130, Height = 25 };
        _currWaterTextBox.Name = "CurrWaterTextBox";
        _currWaterTextBox.SetBounds(150, 77, 170, 30);
        ConfigureNumericTextBox(_currWaterTextBox);

        waterGroup.Controls.AddRange(
        [
            prevWaterLabel, _prevWaterTextBox,
            currWaterLabel, _currWaterTextBox
        ]);

        // Electricity GroupBox
        var elecGroup = new GroupBox
        {
            Name = "ElectricityReadingGroup",
            Text = "มิเตอร์ไฟฟ้า",
            Left = 380,
            Top = 70,
            Width = 350,
            Height = 140
        };

        var prevElecLabel = new Label { Text = "มิเตอร์ก่อนหน้า:", Left = 15, Top = 35, Width = 130, Height = 25 };
        _prevElecTextBox.Name = "PrevElecTextBox";
        _prevElecTextBox.SetBounds(150, 32, 170, 30);
        ConfigureNumericTextBox(_prevElecTextBox);
        _prevElecTextBox.ReadOnly = true;
        _prevElecTextBox.BackColor = SystemColors.ControlLight;

        var currElecLabel = new Label { Text = "มิเตอร์ปัจจุบัน:", Left = 15, Top = 80, Width = 130, Height = 25 };
        _currElecTextBox.Name = "CurrElecTextBox";
        _currElecTextBox.SetBounds(150, 77, 170, 30);
        ConfigureNumericTextBox(_currElecTextBox);

        elecGroup.Controls.AddRange(
        [
            prevElecLabel, _prevElecTextBox,
            currElecLabel, _currElecTextBox
        ]);

        // Save Button
        _saveButton.Name = "SaveMeterButton";
        _saveButton.Text = "บันทึกมิเตอร์ประจำเดือน";
        _saveButton.Font = new Font("Segoe UI", 11, FontStyle.Bold);
        _saveButton.SetBounds(12, 225, 220, 38);
        _saveButton.Click += SaveButton_Click;

        // Message Label
        _messageLabel.Name = "MeterMessageLabel";
        _messageLabel.SetBounds(245, 230, 500, 30);
        _messageLabel.TextAlign = ContentAlignment.MiddleLeft;

        panel.Controls.AddRange(
        [
            roomLabel, _roomComboBox,
            monthLabel, _monthComboBox,
            yearLabel, _yearComboBox,
            waterGroup,
            elecGroup,
            _saveButton,
            _messageLabel
        ]);

        group.Controls.Add(panel);
        return group;
    }

    private GroupBox BuildListGroup()
    {
        var group = new GroupBox
        {
            Name = "MeterListGroup",
            Text = "ประวัติการบันทึกมิเตอร์น้ำประปา-ไฟฟ้า",
            Dock = DockStyle.Fill,
            Padding = new Padding(8)
        };

        _meterGrid.Name = "MeterGrid";
        _meterGrid.Dock = DockStyle.Fill;
        _meterGrid.ReadOnly = true;
        _meterGrid.AllowUserToAddRows = false;
        _meterGrid.AllowUserToDeleteRows = false;
        _meterGrid.AutoGenerateColumns = false;
        _meterGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _meterGrid.MultiSelect = false;
        _meterGrid.RowHeadersVisible = false;
        _meterGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _meterGrid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _meterGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 248, 252);
        _meterGrid.RowTemplate.Height = 30;
        _meterGrid.ColumnHeadersHeight = 35;
        _meterGrid.ColumnHeadersDefaultCellStyle.Font = new Font(Font, FontStyle.Bold);
        _meterGrid.CellDoubleClick += MeterGrid_CellDoubleClick;

        _meterGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Sequence",
            HeaderText = "ลำดับ",
            DataPropertyName = "Sequence",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            Width = 60,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
        });
        _meterGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "RoomNumber",
            HeaderText = "เลขที่ห้อง",
            DataPropertyName = "RoomNumber",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            Width = 110
        });
        _meterGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "BillingPeriodDisplay",
            HeaderText = "รอบบิล",
            DataPropertyName = "BillingPeriodDisplay",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            Width = 140,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
        });
        _meterGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "WaterReading",
            HeaderText = "มิเตอร์น้ำประปา (ก่อน -> ปัจจุบัน)",
            DataPropertyName = "WaterReading",
            Width = 180,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
        });
        _meterGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "WaterUsed",
            HeaderText = "น้ำประปาใช้ไป (หน่วย)",
            DataPropertyName = "WaterUsed",
            Width = 120,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2" }
        });
        _meterGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "ElecReading",
            HeaderText = "มิเตอร์ไฟฟ้า (ก่อน -> ปัจจุบัน)",
            DataPropertyName = "ElecReading",
            Width = 180,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
        });
        _meterGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "ElecUsed",
            HeaderText = "ไฟฟ้าใช้ไป (หน่วย)",
            DataPropertyName = "ElecUsed",
            Width = 120,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2" }
        });

        group.Controls.Add(_meterGrid);
        return group;
    }

    private void PopulateMonths()
    {
        _monthComboBox.Items.Clear();
        string[] months = ["มกราคม", "กุมภาพันธ์", "มีนาคม", "เมษายน", "พฤษภาคม", "มิถุนายน", "กรกฎาคม", "สิงหาคม", "กันยายน", "ตุลาคม", "พฤศจิกายน", "ธันวาคม"];
        for (int i = 0; i < months.Length; i++)
        {
            _monthComboBox.Items.Add(new MonthItem(i + 1, months[i]));
        }
        
        int currentMonth = DateTime.Now.Month;
        _monthComboBox.SelectedIndex = currentMonth - 1;
    }

    private void PopulateYears()
    {
        _yearComboBox.Items.Clear();
        int currentThaiYear = DateTime.Now.Year + 543;
        for (int y = currentThaiYear - 2; y <= currentThaiYear + 2; y++)
        {
            _yearComboBox.Items.Add(y);
        }
        _yearComboBox.SelectedItem = currentThaiYear;
    }

    private BillingPeriod? GetSelectedBillingPeriod()
    {
        if (_monthComboBox.SelectedItem is not MonthItem monthItem ||
            _yearComboBox.SelectedItem is not int thaiYear)
        {
            return null;
        }

        int gregorianYear = thaiYear - 543;
        string periodStr = $"{gregorianYear}-{monthItem.MonthNumber:D2}";
        return BillingPeriod.TryParse(periodStr, out var period) ? period : null;
    }

    private void ConfigureNumericTextBox(TextBox textBox)
    {
        textBox.KeyPress += (s, e) =>
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
            {
                e.Handled = true;
            }

            if (e.KeyChar == '.' && textBox.Text.Contains('.'))
            {
                e.Handled = true;
            }
        };

        textBox.KeyDown += Control_EnterKeyDown;
    }

    private void Control_EnterKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            _saveButton.PerformClick();
        }
    }

    public void RefreshRoomList(RoomId? forceIncludeRoomId = null)
    {
        var selectedItem = _roomComboBox.SelectedItem as RoomItem;
        _roomComboBox.Items.Clear();

        var currentPeriod = GetSelectedBillingPeriod();
        var rooms = _roomRepository.GetAll();

        foreach (var room in rooms)
        {
            bool isRecorded = currentPeriod != null && _meterReadingRepository.Exists(room.Id, currentPeriod);
            if (isRecorded && room.Id != forceIncludeRoomId)
            {
                continue;
            }

            var item = new RoomItem(room.Id, room.RoomNumber.Value);
            _roomComboBox.Items.Add(item);

            if (selectedItem != null && item.Id == selectedItem.Id)
            {
                _roomComboBox.SelectedItem = item;
            }
            else if (forceIncludeRoomId != null && room.Id == forceIncludeRoomId)
            {
                _roomComboBox.SelectedItem = item;
            }
        }

        if (_roomComboBox.SelectedItem == null && _roomComboBox.Items.Count > 0)
        {
            _roomComboBox.SelectedIndex = 0;
        }

        RefreshReadingList();
        TryAutoLoadPreviousReadings();
    }

    public void RefreshReadingList()
    {
        var currentPeriod = GetSelectedBillingPeriod();
        var readings = _meterReadingRepository.GetAll()
            .Where(r => currentPeriod == null || r.BillingPeriod == currentPeriod)
            .GroupBy(r => new { r.RoomId, r.BillingPeriod })
            .Select(g => g.Last())
            .ToList();
        var rooms = _roomRepository.GetAll().ToDictionary(r => r.Id, r => r.RoomNumber.Value);

        _meterGrid.DataSource = readings
            .OrderBy(r => rooms.TryGetValue(r.RoomId, out var num) ? num : "")
            .Select((reading, index) =>
            {
                var roomNum = rooms.TryGetValue(reading.RoomId, out var num) ? num : "-";
                var parts = reading.BillingPeriod.Value.Split('-');
                string periodDisplay = reading.BillingPeriod.Value;
                if (parts.Length == 2 && int.TryParse(parts[0], out var gYear) && int.TryParse(parts[1], out var mNum))
                {
                    int thaiYear = gYear + 543;
                    string monthName = GetThaiMonthName(mNum);
                    periodDisplay = $"{monthName} {thaiYear}";
                }

                decimal waterUsed = reading.CurrentWaterMeter.Value - reading.PreviousWaterMeter.Value;
                decimal elecUsed = reading.CurrentElectricityMeter.Value - reading.PreviousElectricityMeter.Value;

                return new MeterReadingGridItem(
                    RoomId: reading.RoomId,
                    Sequence: index + 1,
                    RoomNumber: roomNum,
                    BillingPeriodDisplay: periodDisplay,
                    BillingPeriodRaw: reading.BillingPeriod.Value,
                    WaterReading: $"{reading.PreviousWaterMeter.Value:0.##} -> {reading.CurrentWaterMeter.Value:0.##}",
                    WaterUsed: waterUsed.ToString("N2"),
                    ElecReading: $"{reading.PreviousElectricityMeter.Value:0.##} -> {reading.CurrentElectricityMeter.Value:0.##}",
                    ElecUsed: elecUsed.ToString("N2"));
            })
            .ToList();
    }

    private static string GetThaiMonthName(int month)
    {
        return month switch
        {
            1 => "มกราคม",
            2 => "กุมภาพันธ์",
            3 => "มีนาคม",
            4 => "เมษายน",
            5 => "พฤษภาคม",
            6 => "มิถุนายน",
            7 => "กรกฎาคม",
            8 => "สิงหาคม",
            9 => "กันยายน",
            10 => "ตุลาคม",
            11 => "พฤศจิกายน",
            12 => "ธันวาคม",
            _ => month.ToString()
        };
    }

    private void TryAutoLoadPreviousReadings()
    {
        if (_roomComboBox.SelectedItem is not RoomItem selectedRoom)
        {
            _prevWaterTextBox.Text = "0";
            _prevElecTextBox.Text = "0";
            _currWaterTextBox.Clear();
            _currElecTextBox.Clear();
            return;
        }

        var currentPeriod = GetSelectedBillingPeriod();
        if (currentPeriod is null)
        {
            return;
        }

        // 1. ตรวจสอบว่ารอบบิลนี้เคยบันทึกไปแล้วหรือยัง
        var existingReading = _meterReadingRepository.Get(selectedRoom.Id, currentPeriod);
        if (existingReading != null)
        {
            _prevWaterTextBox.Text = existingReading.PreviousWaterMeter.Value.ToString("0.##");
            _currWaterTextBox.Text = existingReading.CurrentWaterMeter.Value.ToString("0.##");
            _prevElecTextBox.Text = existingReading.PreviousElectricityMeter.Value.ToString("0.##");
            _currElecTextBox.Text = existingReading.CurrentElectricityMeter.Value.ToString("0.##");
            
            SetPreviousWaterReadOnly(true);
            SetPreviousElecReadOnly(true);
            return;
        }

        // 2. ถ้ายังไม่เคยบันทึกรอบนี้ ให้ดึงค่าปัจจุบันจากรอบบิลก่อนหน้ามาเป็นค่าก่อนหน้า
        var previousPeriod = currentPeriod.GetPrevious();
        var prevReading = _meterReadingRepository.Get(selectedRoom.Id, previousPeriod);
        if (prevReading != null)
        {
            _prevWaterTextBox.Text = prevReading.CurrentWaterMeter.Value.ToString("0.##");
            _prevElecTextBox.Text = prevReading.CurrentElectricityMeter.Value.ToString("0.##");
            SetPreviousWaterReadOnly(true);
            SetPreviousElecReadOnly(true);
        }
        else
        {
            // ถ้าไม่มีประวัติรอบก่อนหน้าเลย ให้พิมพ์ค่าเริ่มต้นได้ (กรณีเริ่มต้นใช้งานครั้งแรก)
            _prevWaterTextBox.Text = "0";
            _prevElecTextBox.Text = "0";
            SetPreviousWaterReadOnly(false);
            SetPreviousElecReadOnly(false);
        }

        _currWaterTextBox.Clear();
        _currElecTextBox.Clear();
    }

    private void SetPreviousWaterReadOnly(bool readOnly)
    {
        _prevWaterTextBox.ReadOnly = readOnly;
        _prevWaterTextBox.BackColor = readOnly ? SystemColors.ControlLight : SystemColors.Window;
    }

    private void SetPreviousElecReadOnly(bool readOnly)
    {
        _prevElecTextBox.ReadOnly = readOnly;
        _prevElecTextBox.BackColor = readOnly ? SystemColors.ControlLight : SystemColors.Window;
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        if (_roomComboBox.SelectedItem is not RoomItem selectedRoom)
        {
            ShowError("กรุณาเลือกห้องพัก");
            return;
        }

        var billingPeriod = GetSelectedBillingPeriod();
        if (billingPeriod is null)
        {
            ShowError("รอบบิลไม่ถูกต้อง");
            return;
        }

        try
        {
            MeterReading? prevWater = ParseMeterReading(_prevWaterTextBox.Text, "มิเตอร์น้ำก่อนหน้า");
            MeterReading? currWater = ParseMeterReading(_currWaterTextBox.Text, "มิเตอร์น้ำปัจจุบัน");
            MeterReading? prevElec = ParseMeterReading(_prevElecTextBox.Text, "มิเตอร์ไฟฟ้าก่อนหน้า");
            MeterReading? currElec = ParseMeterReading(_currElecTextBox.Text, "มิเตอร์ไฟฟ้าปัจจุบัน");

            bool isExisting = _meterReadingRepository.Exists(selectedRoom.Id, billingPeriod);

            _saveReadingService.Save(
                selectedRoom.Id,
                billingPeriod,
                prevWater,
                currWater,
                prevElec,
                currElec);

            _messageLabel.ForeColor = Color.DarkGreen;
            _messageLabel.Text = isExisting
                ? $"อัปเดตมิเตอร์ห้อง {selectedRoom.RoomNumber} รอบบิล {billingPeriod.Value} สำเร็จ"
                : $"บันทึกมิเตอร์ห้อง {selectedRoom.RoomNumber} รอบบิล {billingPeriod.Value} สำเร็จ";
            
            RefreshRoomList();
            TryAutoLoadPreviousReadings();
        }
        catch (InvalidMeterReadingException ex)
        {
            ShowError(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            ShowError(ex.Message);
        }
        catch (ArgumentException ex)
        {
            ShowError(ex.Message);
        }
        catch (Exception ex)
        {
            ShowError($"เกิดข้อผิดพลาด: {ex.Message}");
        }
    }

    private void MeterGrid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || _meterGrid.Rows[e.RowIndex].DataBoundItem is not MeterReadingGridItem gridItem)
        {
            return;
        }

        // แยกรอบบิลเพื่อเลือกเดือนและปี
        var parts = gridItem.BillingPeriodRaw.Split('-');
        if (parts.Length == 2 && int.TryParse(parts[0], out var gYear) && int.TryParse(parts[1], out var mNum))
        {
            int thaiYear = gYear + 543;
            foreach (var y in _yearComboBox.Items)
            {
                if (y is int ty && ty == thaiYear)
                {
                    _yearComboBox.SelectedItem = ty;
                    break;
                }
            }

            foreach (MonthItem m in _monthComboBox.Items)
            {
                if (m.MonthNumber == mNum)
                {
                    _monthComboBox.SelectedItem = m;
                    break;
                }
            }
        }

        // รีเฟรชรายชื่อห้องโดยบังคับรวมห้องนี้เข้ามาใน ComboBox แม้ว่าจะบันทึกไปแล้ว เพื่อให้สามารถเลือกแก้ไขได้
        RefreshRoomList(gridItem.RoomId);

        // เลือกห้องใน ComboBox ตาม RoomId
        foreach (RoomItem roomItem in _roomComboBox.Items)
        {
            if (roomItem.Id == gridItem.RoomId)
            {
                _roomComboBox.SelectedItem = roomItem;
                break;
            }
        }

        // โหลดข้อมูลค่ามิเตอร์ลง TextBox
        var reading = _meterReadingRepository.Get(gridItem.RoomId, BillingPeriod.Create(gridItem.BillingPeriodRaw));
        if (reading != null)
        {
            _prevWaterTextBox.Text = reading.PreviousWaterMeter.Value.ToString("0.##");
            _currWaterTextBox.Text = reading.CurrentWaterMeter.Value.ToString("0.##");
            _prevElecTextBox.Text = reading.PreviousElectricityMeter.Value.ToString("0.##");
            _currElecTextBox.Text = reading.CurrentElectricityMeter.Value.ToString("0.##");
            
            SetPreviousWaterReadOnly(true);
            SetPreviousElecReadOnly(true);
        }

        _messageLabel.ForeColor = Color.DarkBlue;
        _messageLabel.Text = $"โหมดแก้ไข: รอบบิล {gridItem.BillingPeriodDisplay} ห้อง {gridItem.RoomNumber}";
    }

    private static MeterReading? ParseMeterReading(string text, string meterName)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidMeterReadingException(meterName, "ต้องระบุค่ามิเตอร์");
        }

        if (!decimal.TryParse(text.Trim(), out var value))
        {
            throw new InvalidMeterReadingException(meterName, "ต้องระบุเป็นตัวเลขที่ถูกต้อง");
        }

        return MeterReading.Create(value);
    }

    private void ShowError(string message)
    {
        _messageLabel.ForeColor = Color.DarkRed;
        _messageLabel.Text = message;
    }
}
