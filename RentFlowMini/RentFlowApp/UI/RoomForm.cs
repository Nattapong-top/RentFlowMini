using System.Data;
using Microsoft.Data.SqlClient;
using RentFlowApp.Application.Billing;
using RentFlowApp.Application.Rooms;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Forms;

public class RoomForm : Form
{
    private readonly CreateRoomService _createRoomService;
    private readonly IRoomRepository _roomRepository;
    private readonly CreateMonthlyBillService? _createMonthlyBillService;

    // Room tab controls
    private readonly TextBox _roomNumberTextBox = new();
    private readonly Button _addRoomButton = new();
    private readonly DataGridView _roomGrid = new();
    private readonly Label _roomMessageLabel = new();

    // Billing tab controls
    private readonly ComboBox _billRoomComboBox = new();
    private readonly TextBox _billingPeriodTextBox = new();
    private readonly TextBox _monthlyRentTextBox = new();
    private readonly TextBox _prevWaterTextBox = new();
    private readonly TextBox _currWaterTextBox = new();
    private readonly TextBox _waterRateTextBox = new();
    private readonly TextBox _prevElecTextBox = new();
    private readonly TextBox _currElecTextBox = new();
    private readonly TextBox _elecRateTextBox = new();
    private readonly Button _createBillButton = new();
    private readonly Label _resultRentLabel = new();
    private readonly Label _resultWaterLabel = new();
    private readonly Label _resultElecLabel = new();
    private readonly Label _resultTotalLabel = new();
    private readonly Label _billMessageLabel = new();

    private sealed record RoomItem(RoomId Id, string RoomNumber)
    {
        public override string ToString() => RoomNumber;
    }

    public RoomForm(
        CreateRoomService createRoomService,
        IRoomRepository roomRepository,
        CreateMonthlyBillService? createMonthlyBillService = null)
    {
        ArgumentNullException.ThrowIfNull(createRoomService);
        ArgumentNullException.ThrowIfNull(roomRepository);

        _createRoomService = createRoomService;
        _roomRepository = roomRepository;
        _createMonthlyBillService = createMonthlyBillService;

        InitializeForm();

        try
        {
            RefreshRoomList();
        }
        catch (SqlException)
        {
            ShowRoomError(
                "ไม่สามารถเชื่อมต่อฐานข้อมูลได้ กรุณาตรวจสอบ SQL Server และลองใหม่อีกครั้ง");
        }
    }

    private void InitializeForm()
    {
        Text = "RentFlow - ระบบจัดการห้องพักและสร้างบิล";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 850;
        Height = 650;
        MinimumSize = new Size(750, 600);

        var tabControl = new TabControl
        {
            Dock = DockStyle.Fill
        };

        var roomTabPage = new TabPage("ห้องพัก (Rooms)");
        var billingTabPage = new TabPage("สร้างบิลประจำเดือน (Monthly Bill)");

        BuildRoomTab(roomTabPage);
        BuildBillingTab(billingTabPage);

        tabControl.TabPages.Add(roomTabPage);
        tabControl.TabPages.Add(billingTabPage);
        tabControl.SelectedIndexChanged += (s, e) =>
        {
            if (tabControl.SelectedTab == billingTabPage)
            {
                PopulateBillingRoomOptions();
            }
        };

        Controls.Add(tabControl);
    }

    private void BuildRoomTab(TabPage page)
    {
        var titleLabel = new Label
        {
            Text = "จัดการห้องพัก",
            Left = 20,
            Top = 20,
            Width = 300,
            Height = 35,
            Font = new Font("Segoe UI", 16, FontStyle.Bold)
        };

        var roomNumberLabel = new Label
        {
            Text = "หมายเลขห้อง:",
            Left = 20,
            Top = 75,
            Width = 110,
            Height = 25
        };

        _roomNumberTextBox.SetBounds(135, 72, 180, 27);

        _addRoomButton.Text = "เพิ่มห้อง";
        _addRoomButton.SetBounds(325, 70, 110, 30);
        _addRoomButton.Click += AddRoomButton_Click;

        _roomMessageLabel.SetBounds(20, 110, 780, 25);
        _roomMessageLabel.ForeColor = Color.DarkGreen;

        _roomGrid.SetBounds(20, 145, 780, 420);
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
            HeaderText = "หมายเลขห้อง",
            DataPropertyName = "RoomNumber"
        });

        _roomGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "RoomId",
            HeaderText = "รหัสห้อง (Room ID)",
            DataPropertyName = "RoomId"
        });

        page.Controls.AddRange(
        [
            titleLabel,
            roomNumberLabel,
            _roomNumberTextBox,
            _addRoomButton,
            _roomMessageLabel,
            _roomGrid
        ]);
    }

    private void BuildBillingTab(TabPage page)
    {
        var titleLabel = new Label
        {
            Text = "สร้างบิลประจำเดือน (UC-01)",
            Left = 20,
            Top = 20,
            Width = 400,
            Height = 35,
            Font = new Font("Segoe UI", 16, FontStyle.Bold)
        };

        // Room Selection
        var roomLabel = new Label { Text = "เลือกห้อง:", Left = 20, Top = 70, Width = 110, Height = 25 };
        _billRoomComboBox.Name = "BillRoomComboBox";
        _billRoomComboBox.SetBounds(135, 67, 180, 28);
        _billRoomComboBox.DropDownStyle = ComboBoxStyle.DropDownList;

        // Billing Period
        var periodLabel = new Label { Text = "รอบบิล (YYYY-MM):", Left = 340, Top = 70, Width = 130, Height = 25 };
        _billingPeriodTextBox.Name = "BillingPeriodTextBox";
        _billingPeriodTextBox.SetBounds(480, 67, 120, 27);
        _billingPeriodTextBox.Text = DateTime.Now.ToString("yyyy-MM");

        // Monthly Rent
        var rentLabel = new Label { Text = "ค่าเช่าห้อง (บาท):", Left = 20, Top = 110, Width = 110, Height = 25 };
        _monthlyRentTextBox.Name = "MonthlyRentTextBox";
        _monthlyRentTextBox.SetBounds(135, 107, 180, 27);

        // Water GroupBox
        var waterGroup = new GroupBox
        {
            Name = "WaterGroup",
            Text = "ค่าน้ำประปา",
            Left = 20,
            Top = 150,
            Width = 360,
            Height = 135
        };

        var prevWaterLabel = new Label { Text = "มิเตอร์ก่อนหน้า:", Left = 15, Top = 25, Width = 100, Height = 25 };
        _prevWaterTextBox.Name = "PrevWaterTextBox";
        _prevWaterTextBox.SetBounds(125, 22, 100, 25);

        var currWaterLabel = new Label { Text = "มิเตอร์ปัจจุบัน:", Left = 15, Top = 60, Width = 100, Height = 25 };
        _currWaterTextBox.Name = "CurrWaterTextBox";
        _currWaterTextBox.SetBounds(125, 57, 100, 25);

        var waterRateLabel = new Label { Text = "อัตรา (บาท/หน่วย):", Left = 15, Top = 95, Width = 110, Height = 25 };
        _waterRateTextBox.Name = "WaterRateTextBox";
        _waterRateTextBox.SetBounds(125, 92, 100, 25);
        _waterRateTextBox.Text = "19";

        waterGroup.Controls.AddRange(
        [
            prevWaterLabel, _prevWaterTextBox,
            currWaterLabel, _currWaterTextBox,
            waterRateLabel, _waterRateTextBox
        ]);

        // Electricity GroupBox
        var elecGroup = new GroupBox
        {
            Name = "ElectricityGroup",
            Text = "ค่าไฟฟ้า",
            Left = 400,
            Top = 150,
            Width = 360,
            Height = 135
        };

        var prevElecLabel = new Label { Text = "มิเตอร์ก่อนหน้า:", Left = 15, Top = 25, Width = 100, Height = 25 };
        _prevElecTextBox.Name = "PrevElecTextBox";
        _prevElecTextBox.SetBounds(125, 22, 100, 25);

        var currElecLabel = new Label { Text = "มิเตอร์ปัจจุบัน:", Left = 15, Top = 60, Width = 100, Height = 25 };
        _currElecTextBox.Name = "CurrElecTextBox";
        _currElecTextBox.SetBounds(125, 57, 100, 25);

        var elecRateLabel = new Label { Text = "อัตรา (บาท/หน่วย):", Left = 15, Top = 95, Width = 110, Height = 25 };
        _elecRateTextBox.Name = "ElecRateTextBox";
        _elecRateTextBox.SetBounds(125, 92, 100, 25);
        _elecRateTextBox.Text = "8";

        elecGroup.Controls.AddRange(
        [
            prevElecLabel, _prevElecTextBox,
            currElecLabel, _currElecTextBox,
            elecRateLabel, _elecRateTextBox
        ]);

        // Create Bill Button
        _createBillButton.Name = "CreateBillButton";
        _createBillButton.Text = "คำนวณและสร้างบิล";
        _createBillButton.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        _createBillButton.SetBounds(20, 300, 180, 38);
        _createBillButton.Click += CreateBillButton_Click;

        // Bill Message / Error Label
        _billMessageLabel.Name = "BillMessageLabel";
        _billMessageLabel.SetBounds(215, 308, 545, 30);
        _billMessageLabel.Font = new Font("Segoe UI", 9, FontStyle.Regular);

        // Result Summary GroupBox
        var resultGroup = new GroupBox
        {
            Name = "ResultGroup",
            Text = "สรุปยอดบิลที่คำนวณได้",
            Left = 20,
            Top = 350,
            Width = 740,
            Height = 190
        };

        var rentHeader = new Label { Text = "ค่าเช่าห้อง:", Left = 20, Top = 35, Width = 100, Height = 25 };
        _resultRentLabel.Name = "ResultRentLabel";
        _resultRentLabel.SetBounds(130, 35, 200, 25);
        _resultRentLabel.Text = "-";

        var waterHeader = new Label { Text = "ค่าน้ำประปา:", Left = 20, Top = 70, Width = 100, Height = 25 };
        _resultWaterLabel.Name = "ResultWaterLabel";
        _resultWaterLabel.SetBounds(130, 70, 200, 25);
        _resultWaterLabel.Text = "-";

        var elecHeader = new Label { Text = "ค่าไฟฟ้า:", Left = 20, Top = 105, Width = 100, Height = 25 };
        _resultElecLabel.Name = "ResultElecLabel";
        _resultElecLabel.SetBounds(130, 105, 200, 25);
        _resultElecLabel.Text = "-";

        var totalHeader = new Label
        {
            Text = "ยอดรวมทั้งสิ้น:",
            Left = 20,
            Top = 145,
            Width = 110,
            Height = 30,
            Font = new Font("Segoe UI", 11, FontStyle.Bold)
        };
        _resultTotalLabel.Name = "ResultTotalLabel";
        _resultTotalLabel.SetBounds(130, 145, 300, 30);
        _resultTotalLabel.Font = new Font("Segoe UI", 12, FontStyle.Bold);
        _resultTotalLabel.ForeColor = Color.DarkBlue;
        _resultTotalLabel.Text = "-";

        resultGroup.Controls.AddRange(
        [
            rentHeader, _resultRentLabel,
            waterHeader, _resultWaterLabel,
            elecHeader, _resultElecLabel,
            totalHeader, _resultTotalLabel
        ]);

        page.Controls.AddRange(
        [
            titleLabel,
            roomLabel, _billRoomComboBox,
            periodLabel, _billingPeriodTextBox,
            rentLabel, _monthlyRentTextBox,
            waterGroup,
            elecGroup,
            _createBillButton,
            _billMessageLabel,
            resultGroup
        ]);
    }

    private void AddRoomButton_Click(object? sender, EventArgs e)
    {
        try
        {
            var room = _createRoomService.Create(_roomNumberTextBox.Text);
            RefreshRoomList();

            _roomNumberTextBox.Clear();
            _roomNumberTextBox.Focus();

            _roomMessageLabel.ForeColor = Color.DarkGreen;
            _roomMessageLabel.Text = $"สร้างห้อง {room.RoomNumber.Value} สำเร็จแล้ว";
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

    private void CreateBillButton_Click(object? sender, EventArgs e)
    {
        if (_createMonthlyBillService == null)
        {
            ShowBillError("ระบบสร้างบิลยังไม่ได้ติดตั้งในแอปพลิเคชัน");
            return;
        }

        if (_billRoomComboBox.SelectedItem is not RoomItem selectedRoom)
        {
            ShowBillError("กรุณาเลือกห้องพัก");
            return;
        }

        string billingPeriodText = _billingPeriodTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(billingPeriodText))
        {
            ShowBillError("กรุณาระบุรอบบิล เช่น 2026-10");
            return;
        }

        if (!decimal.TryParse(_monthlyRentTextBox.Text.Trim(), out var monthlyRent) || monthlyRent < 0)
        {
            ShowBillError("กรุณาระบุค่าเช่าห้องเป็นจำนวนเงินที่ถูกต้อง");
            return;
        }

        if (!decimal.TryParse(_waterRateTextBox.Text.Trim(), out var waterRate) || waterRate < 0)
        {
            ShowBillError("กรุณาระบุอัตราค่าน้ำเป็นตัวเลขที่ถูกต้อง");
            return;
        }

        if (!decimal.TryParse(_elecRateTextBox.Text.Trim(), out var elecRate) || elecRate < 0)
        {
            ShowBillError("กรุณาระบุอัตราค่าไฟเป็นตัวเลขที่ถูกต้อง");
            return;
        }

        MeterReading? prevWater = TryParseMeterReading(_prevWaterTextBox.Text.Trim());
        MeterReading? currWater = TryParseMeterReading(_currWaterTextBox.Text.Trim());
        MeterReading? prevElec = TryParseMeterReading(_prevElecTextBox.Text.Trim());
        MeterReading? currElec = TryParseMeterReading(_currElecTextBox.Text.Trim());

        try
        {
            var billingPeriod = BillingPeriod.Create(billingPeriodText);

            var request = new CreateMonthlyBillRequest(
                selectedRoom.Id,
                billingPeriod,
                monthlyRent,
                prevWater,
                currWater,
                prevElec,
                currElec,
                waterRate,
                elecRate);

            var bill = _createMonthlyBillService.Create(request);

            _resultRentLabel.Text = $"{bill.Rent.Amount:N2} บาท";
            _resultWaterLabel.Text = $"{bill.Water.Amount:N2} บาท";
            _resultElecLabel.Text = $"{bill.Electricity.Amount:N2} บาท";
            _resultTotalLabel.Text = $"{bill.Total.Amount:N2} บาท";

            _billMessageLabel.ForeColor = Color.DarkGreen;
            _billMessageLabel.Text =
                $"สร้างบิลสำหรับห้อง {selectedRoom.RoomNumber} รอบ {bill.BillingPeriod.Value} สำเร็จเรียบร้อย";
        }
        catch (InvalidMeterReadingException ex)
        {
            ShowBillError(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            ShowBillError(ex.Message);
        }
        catch (ArgumentException ex)
        {
            ShowBillError(ex.Message);
        }
        catch (Exception ex)
        {
            ShowBillError($"เกิดข้อผิดพลาด: {ex.Message}");
        }
    }

    private static MeterReading? TryParseMeterReading(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (!decimal.TryParse(text, out var value))
        {
            return null;
        }

        return MeterReading.Create(value);
    }

    private void RefreshRoomList()
    {
        var rooms = _roomRepository.GetAll();

        _roomGrid.DataSource = rooms
            .Select(room => new
            {
                RoomNumber = room.RoomNumber.Value,
                RoomId = room.Id.Value
            })
            .ToList();

        PopulateBillingRoomOptions();
    }

    private void PopulateBillingRoomOptions()
    {
        var selectedItem = _billRoomComboBox.SelectedItem as RoomItem;
        _billRoomComboBox.Items.Clear();

        var rooms = _roomRepository.GetAll();
        foreach (var room in rooms)
        {
            var item = new RoomItem(room.Id, room.RoomNumber.Value);
            _billRoomComboBox.Items.Add(item);

            if (selectedItem != null && item.Id == selectedItem.Id)
            {
                _billRoomComboBox.SelectedItem = item;
            }
        }

        if (_billRoomComboBox.SelectedItem == null && _billRoomComboBox.Items.Count > 0)
        {
            _billRoomComboBox.SelectedIndex = 0;
        }
    }

    private void ShowRoomError(string message)
    {
        _roomMessageLabel.ForeColor = Color.DarkRed;
        _roomMessageLabel.Text = message;
    }

    private void ShowBillError(string message)
    {
        _billMessageLabel.ForeColor = Color.DarkRed;
        _billMessageLabel.Text = message;
    }
}
