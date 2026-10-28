using RentFlowApp.Application.Billing;
using RentFlowApp.Application.Rooms;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Infrastructure.Billing;

namespace RentFlowApp.Forms;

public class MonthlyBillTabControl : BaseTabControl
{
    private readonly CreateMonthlyBillService? _createMonthlyBillService;
    private readonly IRoomRepository _roomRepository;
    private readonly IMonthlyMeterReadingRepository _monthlyMeterReadingRepository;
    private readonly SaveMonthlyMeterReadingService _saveMonthlyMeterReadingService;

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
    private readonly Button _saveMeterReadingButton = new();
    private readonly Label _resultRentLabel = new();
    private readonly Label _resultWaterLabel = new();
    private readonly Label _resultElecLabel = new();
    private readonly Label _resultTotalLabel = new();
    private readonly Label _billMessageLabel = new();

    private sealed record RoomItem(RoomId Id, string RoomNumber)
    {
        public override string ToString() => RoomNumber;
    }

    public MonthlyBillTabControl(
        CreateMonthlyBillService? createMonthlyBillService,
        IRoomRepository roomRepository,
        IMonthlyMeterReadingRepository? monthlyMeterReadingRepository,
        SaveMonthlyMeterReadingService? saveMonthlyMeterReadingService)
    {
        ArgumentNullException.ThrowIfNull(roomRepository);

        _createMonthlyBillService = createMonthlyBillService;
        _roomRepository = roomRepository;
        _monthlyMeterReadingRepository = monthlyMeterReadingRepository ?? new InMemoryMonthlyMeterReadingRepository();
        _saveMonthlyMeterReadingService = saveMonthlyMeterReadingService ?? new SaveMonthlyMeterReadingService(_monthlyMeterReadingRepository);

        Name = "MonthlyBillTabControl";

        InitializeComponents();
        RefreshRoomList();
    }

    private void InitializeComponents()
    {
        var mainLayout = CreateStandardLayout(3, [50f, 360f, -1f]);
        var titleLabel = CreateTitleLabel("สร้างบิลประจำเดือน");
        var inputGroup = BuildInputGroup();
        var resultGroup = BuildResultGroup();

        mainLayout.Controls.Add(titleLabel, 0, 0);
        mainLayout.Controls.Add(inputGroup, 0, 1);
        mainLayout.Controls.Add(resultGroup, 0, 2);
        Controls.Add(mainLayout);
    }

    private GroupBox BuildInputGroup()
    {
        var group = new GroupBox
        {
            Name = "MonthlyBillInputGroup",
            Text = "สร้างบิลประจำเดือน",
            Dock = DockStyle.Fill,
            Padding = new Padding(12)
        };

        var panel = new Panel
        {
            Dock = DockStyle.Fill
        };

        // Room Selection
        var roomLabel = new Label { Text = "เลือกห้อง:", Left = 12, Top = 25, Width = 110, Height = 25 };
        _billRoomComboBox.Name = "BillRoomComboBox";
        _billRoomComboBox.SetBounds(130, 22, 180, 30);
        _billRoomComboBox.DropDownStyle = ComboBoxStyle.DropDownList;

        // Billing Period
        var periodLabel = new Label { Text = "รอบบิล (YYYY-MM):", Left = 330, Top = 25, Width = 130, Height = 25 };
        _billingPeriodTextBox.Name = "BillingPeriodTextBox";
        _billingPeriodTextBox.SetBounds(470, 22, 130, 30);
        _billingPeriodTextBox.Text = DateTime.Now.ToString("yyyy-MM");

        // Monthly Rent
        var rentLabel = new Label { Text = "ค่าเช่าห้อง (บาท):", Left = 12, Top = 68, Width = 110, Height = 25 };
        _monthlyRentTextBox.Name = "MonthlyRentTextBox";
        _monthlyRentTextBox.SetBounds(130, 65, 180, 30);

        // Water GroupBox
        var waterGroup = new GroupBox
        {
            Name = "WaterGroup",
            Text = "ค่าน้ำประปา",
            Left = 12,
            Top = 110,
            Width = 360,
            Height = 135
        };

        var prevWaterLabel = new Label { Text = "มิเตอร์ก่อนหน้า:", Left = 15, Top = 25, Width = 100, Height = 25 };
        _prevWaterTextBox.Name = "PrevWaterTextBox";
        _prevWaterTextBox.SetBounds(135, 22, 130, 30);

        var currWaterLabel = new Label { Text = "มิเตอร์ปัจจุบัน:", Left = 15, Top = 62, Width = 100, Height = 25 };
        _currWaterTextBox.Name = "CurrWaterTextBox";
        _currWaterTextBox.SetBounds(135, 59, 130, 30);

        var waterRateLabel = new Label { Text = "อัตรา (บาท/หน่วย):", Left = 15, Top = 98, Width = 110, Height = 25 };
        _waterRateTextBox.Name = "WaterRateTextBox";
        _waterRateTextBox.SetBounds(135, 95, 130, 30);
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
            Left = 390,
            Top = 110,
            Width = 360,
            Height = 135
        };

        var prevElecLabel = new Label { Text = "มิเตอร์ก่อนหน้า:", Left = 15, Top = 25, Width = 100, Height = 25 };
        _prevElecTextBox.Name = "PrevElecTextBox";
        _prevElecTextBox.SetBounds(135, 22, 130, 30);

        var currElecLabel = new Label { Text = "มิเตอร์ปัจจุบัน:", Left = 15, Top = 62, Width = 100, Height = 25 };
        _currElecTextBox.Name = "CurrElecTextBox";
        _currElecTextBox.SetBounds(135, 59, 130, 30);

        var elecRateLabel = new Label { Text = "อัตรา (บาท/หน่วย):", Left = 15, Top = 98, Width = 110, Height = 25 };
        _elecRateTextBox.Name = "ElecRateTextBox";
        _elecRateTextBox.SetBounds(135, 95, 130, 30);
        _elecRateTextBox.Text = "8";

        elecGroup.Controls.AddRange(
        [
            prevElecLabel, _prevElecTextBox,
            currElecLabel, _currElecTextBox,
            elecRateLabel, _elecRateTextBox
        ]);

        // Create Bill & Save Meter Buttons
        _createBillButton.Name = "CreateBillButton";
        _createBillButton.Text = "คำนวณและสร้างบิล";
        _createBillButton.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        _createBillButton.SetBounds(12, 265, 180, 40);
        _createBillButton.Click += CreateBillButton_Click;

        _saveMeterReadingButton.Name = "SaveMeterReadingButton";
        _saveMeterReadingButton.Text = "บันทึกมิเตอร์ประจำเดือน";
        _saveMeterReadingButton.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        _saveMeterReadingButton.SetBounds(205, 265, 200, 40);
        _saveMeterReadingButton.Click += SaveMeterReadingButton_Click;

        // Bill Message / Error Label
        _billMessageLabel.Name = "BillMessageLabel";
        _billMessageLabel.SetBounds(12, 315, 738, 25);
        _billMessageLabel.Font = new Font("Segoe UI", 10, FontStyle.Regular);

        panel.Controls.AddRange(
        [
            roomLabel, _billRoomComboBox,
            periodLabel, _billingPeriodTextBox,
            rentLabel, _monthlyRentTextBox,
            waterGroup,
            elecGroup,
            _createBillButton,
            _saveMeterReadingButton,
            _billMessageLabel
        ]);

        group.Controls.Add(panel);
        return group;
    }

    private GroupBox BuildResultGroup()
    {
        var resultGroup = new GroupBox
        {
            Name = "ResultGroup",
            Text = "สรุปยอดบิลที่คำนวณได้",
            Dock = DockStyle.Fill,
            Padding = new Padding(12)
        };

        var panel = new Panel
        {
            Dock = DockStyle.Fill
        };

        var rentHeader = new Label { Text = "ค่าเช่าห้อง:", Left = 12, Top = 25, Width = 110, Height = 25 };
        _resultRentLabel.Name = "ResultRentLabel";
        _resultRentLabel.SetBounds(130, 25, 300, 25);
        _resultRentLabel.Text = "-";

        var waterHeader = new Label { Text = "ค่าน้ำประปา:", Left = 12, Top = 60, Width = 110, Height = 25 };
        _resultWaterLabel.Name = "ResultWaterLabel";
        _resultWaterLabel.SetBounds(130, 60, 300, 25);
        _resultWaterLabel.Text = "-";

        var elecHeader = new Label { Text = "ค่าไฟฟ้า:", Left = 12, Top = 95, Width = 110, Height = 25 };
        _resultElecLabel.Name = "ResultElecLabel";
        _resultElecLabel.SetBounds(130, 95, 300, 25);
        _resultElecLabel.Text = "-";

        var totalHeader = new Label
        {
            Text = "ยอดรวมทั้งสิ้น:",
            Left = 12,
            Top = 135,
            Width = 110,
            Height = 30,
            Font = new Font("Segoe UI", 11, FontStyle.Bold)
        };
        _resultTotalLabel.Name = "ResultTotalLabel";
        _resultTotalLabel.SetBounds(130, 135, 300, 30);
        _resultTotalLabel.Font = new Font("Segoe UI", 12, FontStyle.Bold);
        _resultTotalLabel.ForeColor = Color.DarkBlue;
        _resultTotalLabel.Text = "-";

        panel.Controls.AddRange(
        [
            rentHeader, _resultRentLabel,
            waterHeader, _resultWaterLabel,
            elecHeader, _resultElecLabel,
            totalHeader, _resultTotalLabel
        ]);

        resultGroup.Controls.Add(panel);
        return resultGroup;
    }

    public void RefreshRoomList()
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

    private void SaveMeterReadingButton_Click(object? sender, EventArgs e)
    {
        if (_billRoomComboBox.SelectedItem is not RoomItem selectedRoom)
        {
            ShowBillError("กรุณาเลือกห้องพัก");
            return;
        }

        try
        {
            var billingPeriod = BillingPeriod.Create(_billingPeriodTextBox.Text.Trim());
            var reading = MonthlyMeterReading.Create(
                selectedRoom.Id,
                billingPeriod,
                ParseRequiredMeterReading(_prevWaterTextBox.Text, "มิเตอร์น้ำก่อนหน้า"),
                ParseRequiredMeterReading(_currWaterTextBox.Text, "มิเตอร์น้ำปัจจุบัน"),
                ParseRequiredMeterReading(_prevElecTextBox.Text, "มิเตอร์ไฟฟ้าก่อนหน้า"),
                ParseRequiredMeterReading(_currElecTextBox.Text, "มิเตอร์ไฟฟ้าปัจจุบัน"));

            _monthlyMeterReadingRepository.Add(reading);
            _billMessageLabel.ForeColor = Color.DarkGreen;
            _billMessageLabel.Text =
                $"บันทึกมิเตอร์ห้อง {selectedRoom.RoomNumber} รอบ {billingPeriod.Value} สำเร็จเรียบร้อย";
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
    }

    private static MeterReading ParseRequiredMeterReading(string text, string meterName)
    {
        if (!decimal.TryParse(text.Trim(), out var value))
        {
            throw new InvalidMeterReadingException(
                meterName,
                "ต้องระบุเป็นตัวเลขที่ถูกต้อง");
        }

        return MeterReading.Create(value);
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

    private void ShowBillError(string message)
    {
        _billMessageLabel.ForeColor = Color.DarkRed;
        _billMessageLabel.Text = message;
    }
}
