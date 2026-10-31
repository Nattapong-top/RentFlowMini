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
    private readonly IBillingSettingsRepository _billingSettingsRepository;
    private readonly IMonthlyMeterReadingRepository _monthlyMeterReadingRepository;
    private readonly SaveMonthlyMeterReadingService _saveMonthlyMeterReadingService;

    private readonly ComboBox _billRoomComboBox = new();
    private readonly BillingPeriodSelector _periodSelector = new();
    private readonly TextBox _monthlyRentTextBox = new();
    private readonly TextBox _prevWaterTextBox = new();
    private readonly TextBox _currWaterTextBox = new();
    private readonly TextBox _waterRateTextBox = new();
    private readonly TextBox _prevElecTextBox = new();
    private readonly TextBox _currElecTextBox = new();
    private readonly TextBox _elecRateTextBox = new();

    // Parking & Cable TV Controls
    private readonly CheckBox _chargeMotorcycleCheckBox = new();
    private readonly TextBox _motorcycleRateTextBox = new();
    private readonly CheckBox _chargeCarCheckBox = new();
    private readonly TextBox _carRateTextBox = new();
    private readonly CheckBox _isCableTvExemptCheckBox = new();
    private readonly TextBox _cableTvRateTextBox = new();

    private readonly Button _createBillButton = new();
    private readonly Button _saveMeterReadingButton = new();

    // Result Labels
    private readonly Label _resultRentLabel = new();
    private readonly Label _resultWaterLabel = new();
    private readonly Label _resultElecLabel = new();
    private readonly Label _resultMotorcycleParkingLabel = new();
    private readonly Label _resultCarParkingLabel = new();
    private readonly Label _resultCableTvLabel = new();
    private readonly Label _resultTotalLabel = new();
    private readonly Label _billMessageLabel = new();

    private sealed record RoomItem(RoomId Id, string RoomNumber, decimal MonthlyRent, bool IsCableTvExempt)
    {
        public override string ToString() => RoomNumber;
    }

    public MonthlyBillTabControl(
        CreateMonthlyBillService? createMonthlyBillService,
        IRoomRepository roomRepository,
        IMonthlyMeterReadingRepository? monthlyMeterReadingRepository,
        SaveMonthlyMeterReadingService? saveMonthlyMeterReadingService,
        IBillingSettingsRepository? billingSettingsRepository = null)
    {
        ArgumentNullException.ThrowIfNull(roomRepository);

        _createMonthlyBillService = createMonthlyBillService;
        _roomRepository = roomRepository;
        _billingSettingsRepository = billingSettingsRepository ?? new InMemoryBillingSettingsRepository();
        _monthlyMeterReadingRepository = monthlyMeterReadingRepository ?? new InMemoryMonthlyMeterReadingRepository();
        _saveMonthlyMeterReadingService = saveMonthlyMeterReadingService ?? new SaveMonthlyMeterReadingService(_monthlyMeterReadingRepository);

        Name = "MonthlyBillTabControl";

        InitializeComponents();
        RefreshRoomList();
        LoadDefaultSettings();
    }

    private void InitializeComponents()
    {
        var mainLayout = CreateStandardLayout(3, [50f, 440f, -1f]);
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
            Text = "สร้างบิลประจำเดือน (ครอบคลุม 6 รายการ)",
            Dock = DockStyle.Fill,
            Padding = new Padding(12)
        };

        var panel = new Panel
        {
            Dock = DockStyle.Fill
        };

        // Room Selection
        var roomLabel = new Label { Text = "เลือกห้อง:", Left = 12, Top = 22, Width = 110, Height = 25 };
        _billRoomComboBox.Name = "BillRoomComboBox";
        _billRoomComboBox.SetBounds(130, 19, 180, 30);
        _billRoomComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _billRoomComboBox.SelectedIndexChanged += BillRoomComboBox_SelectedIndexChanged;

        // Billing Period Selector (Month & Year ComboBoxes)
        _periodSelector.Name = "BillingPeriodSelector";
        _periodSelector.SetBounds(320, 19, 360, 32);
        _periodSelector.PeriodChanged += (s, e) =>
        {
            RefreshRoomList();
            TryAutoLoadReadings();
        };

        // Monthly Rent
        var rentLabel = new Label { Text = "ค่าเช่าห้อง (บาท):", Left = 12, Top = 62, Width = 110, Height = 25 };
        _monthlyRentTextBox.Name = "MonthlyRentTextBox";
        _monthlyRentTextBox.SetBounds(130, 59, 180, 30);

        // Water GroupBox
        var waterGroup = new GroupBox
        {
            Name = "WaterGroup",
            Text = "ค่าน้ำประปา",
            Left = 12,
            Top = 100,
            Width = 360,
            Height = 130
        };

        var prevWaterLabel = new Label { Text = "มิเตอร์ก่อนหน้า:", Left = 15, Top = 25, Width = 100, Height = 25 };
        _prevWaterTextBox.Name = "PrevWaterTextBox";
        _prevWaterTextBox.SetBounds(135, 22, 130, 30);

        var currWaterLabel = new Label { Text = "มิเตอร์ปัจจุบัน:", Left = 15, Top = 60, Width = 100, Height = 25 };
        _currWaterTextBox.Name = "CurrWaterTextBox";
        _currWaterTextBox.SetBounds(135, 57, 130, 30);

        var waterRateLabel = new Label { Text = "อัตรา (บาท/หน่วย):", Left = 15, Top = 95, Width = 110, Height = 25 };
        _waterRateTextBox.Name = "WaterRateTextBox";
        _waterRateTextBox.SetBounds(135, 92, 130, 30);
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
            Top = 100,
            Width = 360,
            Height = 130
        };

        var prevElecLabel = new Label { Text = "มิเตอร์ก่อนหน้า:", Left = 15, Top = 25, Width = 100, Height = 25 };
        _prevElecTextBox.Name = "PrevElecTextBox";
        _prevElecTextBox.SetBounds(135, 22, 130, 30);

        var currElecLabel = new Label { Text = "มิเตอร์ปัจจุบัน:", Left = 15, Top = 60, Width = 100, Height = 25 };
        _currElecTextBox.Name = "CurrElecTextBox";
        _currElecTextBox.SetBounds(135, 57, 130, 30);

        var elecRateLabel = new Label { Text = "อัตรา (บาท/หน่วย):", Left = 15, Top = 95, Width = 110, Height = 25 };
        _elecRateTextBox.Name = "ElecRateTextBox";
        _elecRateTextBox.SetBounds(135, 92, 130, 30);
        _elecRateTextBox.Text = "8";

        elecGroup.Controls.AddRange(
        [
            prevElecLabel, _prevElecTextBox,
            currElecLabel, _currElecTextBox,
            elecRateLabel, _elecRateTextBox
        ]);

        // Additional Charges GroupBox (Parking & Cable TV)
        var addGroup = new GroupBox
        {
            Name = "AdditionalChargesGroup",
            Text = "ค่าบริการอื่นๆ (ที่จอดรถ & Cable TV)",
            Left = 12,
            Top = 238,
            Width = 738,
            Height = 100
        };

        _chargeMotorcycleCheckBox.Name = "ChargeMotorcycleCheckBox";
        _chargeMotorcycleCheckBox.Text = "มอเตอร์ไซค์";
        _chargeMotorcycleCheckBox.SetBounds(15, 25, 110, 25);
        _motorcycleRateTextBox.Name = "MotorcycleRateTextBox";
        _motorcycleRateTextBox.SetBounds(130, 23, 80, 27);
        _motorcycleRateTextBox.Text = "300";

        _chargeCarCheckBox.Name = "ChargeCarCheckBox";
        _chargeCarCheckBox.Text = "รถยนต์";
        _chargeCarCheckBox.SetBounds(230, 25, 80, 25);
        _carRateTextBox.Name = "CarRateTextBox";
        _carRateTextBox.SetBounds(310, 23, 80, 27);
        _carRateTextBox.Text = "500";

        _isCableTvExemptCheckBox.Name = "IsCableTvExemptCheckBox";
        _isCableTvExemptCheckBox.Text = "ยกเว้น Cable TV";
        _isCableTvExemptCheckBox.SetBounds(410, 25, 130, 25);
        
        var cableTvRateLbl = new Label { Text = "อัตรา Cable:", Left = 550, Top = 27, Width = 80, Height = 22 };
        _cableTvRateTextBox.Name = "CableTvRateTextBox";
        _cableTvRateTextBox.SetBounds(635, 23, 85, 27);
        _cableTvRateTextBox.Text = "60";

        addGroup.Controls.AddRange(
        [
            _chargeMotorcycleCheckBox, _motorcycleRateTextBox,
            _chargeCarCheckBox, _carRateTextBox,
            _isCableTvExemptCheckBox,
            cableTvRateLbl, _cableTvRateTextBox
        ]);

        // Create Bill & Save Meter Buttons
        _createBillButton.Name = "CreateBillButton";
        _createBillButton.Text = "คำนวณและสร้างบิล";
        _createBillButton.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        _createBillButton.SetBounds(12, 348, 180, 38);
        _createBillButton.Click += CreateBillButton_Click;

        _saveMeterReadingButton.Name = "SaveMeterReadingButton";
        _saveMeterReadingButton.Text = "บันทึกมิเตอร์ประจำเดือน";
        _saveMeterReadingButton.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        _saveMeterReadingButton.SetBounds(205, 348, 200, 38);
        _saveMeterReadingButton.Click += SaveMeterReadingButton_Click;

        // Bill Message / Error Label
        _billMessageLabel.Name = "BillMessageLabel";
        _billMessageLabel.SetBounds(12, 392, 738, 25);
        _billMessageLabel.Font = new Font("Segoe UI", 10, FontStyle.Regular);

        panel.Controls.AddRange(
        [
            roomLabel, _billRoomComboBox,
            _periodSelector,
            rentLabel, _monthlyRentTextBox,
            waterGroup,
            elecGroup,
            addGroup,
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
            Text = "สรุปยอดบิลที่คำนวณได้ (แยกตาม 6 รายการ)",
            Dock = DockStyle.Fill,
            Padding = new Padding(12)
        };

        var panel = new Panel
        {
            Dock = DockStyle.Fill
        };

        var rentHeader = new Label { Text = "ค่าเช่าห้อง:", Left = 12, Top = 22, Width = 140, Height = 22 };
        _resultRentLabel.Name = "ResultRentLabel";
        _resultRentLabel.SetBounds(160, 22, 200, 22);
        _resultRentLabel.Text = "-";

        var waterHeader = new Label { Text = "ค่าน้ำประปา:", Left = 12, Top = 48, Width = 140, Height = 22 };
        _resultWaterLabel.Name = "ResultWaterLabel";
        _resultWaterLabel.SetBounds(160, 48, 200, 22);
        _resultWaterLabel.Text = "-";

        var elecHeader = new Label { Text = "ค่าไฟฟ้า:", Left = 12, Top = 74, Width = 140, Height = 22 };
        _resultElecLabel.Name = "ResultElecLabel";
        _resultElecLabel.SetBounds(160, 74, 200, 22);
        _resultElecLabel.Text = "-";

        var motorcycleHeader = new Label { Text = "ค่าจอดรถมอเตอร์ไซค์:", Left = 380, Top = 22, Width = 140, Height = 22 };
        _resultMotorcycleParkingLabel.Name = "ResultMotorcycleParkingLabel";
        _resultMotorcycleParkingLabel.SetBounds(530, 22, 200, 22);
        _resultMotorcycleParkingLabel.Text = "-";

        var carHeader = new Label { Text = "ค่าจอดรถยนต์:", Left = 380, Top = 48, Width = 140, Height = 22 };
        _resultCarParkingLabel.Name = "ResultCarParkingLabel";
        _resultCarParkingLabel.SetBounds(530, 48, 200, 22);
        _resultCarParkingLabel.Text = "-";

        var cableTvHeader = new Label { Text = "ค่า Cable TV:", Left = 380, Top = 74, Width = 140, Height = 22 };
        _resultCableTvLabel.Name = "ResultCableTvLabel";
        _resultCableTvLabel.SetBounds(530, 74, 200, 22);
        _resultCableTvLabel.Text = "-";

        var totalHeader = new Label
        {
            Text = "ยอดรวมทั้งสิ้น:",
            Left = 12,
            Top = 108,
            Width = 140,
            Height = 28,
            Font = new Font("Segoe UI", 11, FontStyle.Bold)
        };
        _resultTotalLabel.Name = "ResultTotalLabel";
        _resultTotalLabel.SetBounds(160, 108, 300, 28);
        _resultTotalLabel.Font = new Font("Segoe UI", 12, FontStyle.Bold);
        _resultTotalLabel.ForeColor = Color.DarkBlue;
        _resultTotalLabel.Text = "-";

        panel.Controls.AddRange(
        [
            rentHeader, _resultRentLabel,
            waterHeader, _resultWaterLabel,
            elecHeader, _resultElecLabel,
            motorcycleHeader, _resultMotorcycleParkingLabel,
            carHeader, _resultCarParkingLabel,
            cableTvHeader, _resultCableTvLabel,
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
            var item = new RoomItem(room.Id, room.RoomNumber.Value, room.MonthlyRent.Amount, room.IsCableTvExempt);
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
        else if (_billRoomComboBox.SelectedItem is RoomItem currentRoom)
        {
            ApplyRoomDefaults(currentRoom);
        }

        TryAutoLoadReadings();
    }

    private void BillRoomComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_billRoomComboBox.SelectedItem is RoomItem room)
        {
            ApplyRoomDefaults(room);
        }
        TryAutoLoadReadings();
    }

    private void ApplyRoomDefaults(RoomItem room)
    {
        _monthlyRentTextBox.Text = room.MonthlyRent.ToString("0.##");
        _isCableTvExemptCheckBox.Checked = room.IsCableTvExempt;
    }

    private void TryAutoLoadReadings()
    {
        if (_billRoomComboBox.SelectedItem is not RoomItem selectedRoom)
        {
            _prevWaterTextBox.Clear();
            _currWaterTextBox.Clear();
            _prevElecTextBox.Clear();
            _currElecTextBox.Clear();
            return;
        }

        var currentPeriod = _periodSelector.GetBillingPeriod();
        if (currentPeriod is null)
        {
            return;
        }

        // 1. ตรวจสอบว่ารอบบิลนี้เคยบันทึกมิเตอร์ไปแล้วหรือยัง
        var existingReading = _monthlyMeterReadingRepository.Get(selectedRoom.Id, currentPeriod);
        if (existingReading != null)
        {
            _prevWaterTextBox.Text = existingReading.PreviousWaterMeter.Value.ToString("0.##");
            _currWaterTextBox.Text = existingReading.CurrentWaterMeter.Value.ToString("0.##");
            _prevElecTextBox.Text = existingReading.PreviousElectricityMeter.Value.ToString("0.##");
            _currElecTextBox.Text = existingReading.CurrentElectricityMeter.Value.ToString("0.##");
            _billMessageLabel.ForeColor = Color.DarkGreen;
            _billMessageLabel.Text = $"โหลดข้อมูลมิเตอร์รอบบิล {currentPeriod.Value} เรียบร้อย";
            return;
        }

        // 2. ถ้ายังไม่เคยบันทึกรอบนี้ ให้ดึงค่าปัจจุบันจากรอบบิลก่อนหน้ามาเป็นค่าก่อนหน้า
        var previousPeriod = currentPeriod.GetPrevious();
        var prevReading = _monthlyMeterReadingRepository.Get(selectedRoom.Id, previousPeriod);
        if (prevReading != null)
        {
            _prevWaterTextBox.Text = prevReading.CurrentWaterMeter.Value.ToString("0.##");
            _prevElecTextBox.Text = prevReading.CurrentElectricityMeter.Value.ToString("0.##");
        }
        else
        {
            _prevWaterTextBox.Text = "0";
            _prevElecTextBox.Text = "0";
        }

        _currWaterTextBox.Clear();
        _currElecTextBox.Clear();

        _billMessageLabel.ForeColor = Color.DarkRed;
        _billMessageLabel.Text = $"ยังไม่มีข้อมูลบันทึกมิเตอร์ของรอบบิลนี้ กรุณาบันทึกมิเตอร์น้ำ-ไฟก่อนทำการสร้างบิล";
    }

    private void LoadDefaultSettings()
    {
        try
        {
            var settings = _billingSettingsRepository.Get();
            _waterRateTextBox.Text = settings.WaterRate.Amount.ToString("0.##");
            _elecRateTextBox.Text = settings.ElectricityRate.Amount.ToString("0.##");
            _motorcycleRateTextBox.Text = settings.MotorcycleParkingRate.Amount.ToString("0.##");
            _carRateTextBox.Text = settings.CarParkingRate.Amount.ToString("0.##");
            _cableTvRateTextBox.Text = settings.CableTvRate.Amount.ToString("0.##");
        }
        catch
        {
            // Fallback defaults
        }
    }

    private void SaveMeterReadingButton_Click(object? sender, EventArgs e)
    {
        if (_billRoomComboBox.SelectedItem is not RoomItem selectedRoom)
        {
            ShowBillError("กรุณาเลือกห้องพัก");
            return;
        }

        var billingPeriod = _periodSelector.GetBillingPeriod();
        if (billingPeriod is null)
        {
            ShowBillError("รอบบิลไม่ถูกต้อง");
            return;
        }

        try
        {
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

        var billingPeriod = _periodSelector.GetBillingPeriod();
        if (billingPeriod is null)
        {
            ShowBillError("รอบบิลไม่ถูกต้อง");
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

        if (!decimal.TryParse(_motorcycleRateTextBox.Text.Trim(), out var motorcycleRate) || motorcycleRate < 0)
        {
            ShowBillError("กรุณาระบุอัตราค่าจอดรถมอเตอร์ไซค์เป็นตัวเลขที่ถูกต้อง");
            return;
        }

        if (!decimal.TryParse(_carRateTextBox.Text.Trim(), out var carRate) || carRate < 0)
        {
            ShowBillError("กรุณาระบุอัตราค่าจอดรถยนต์เป็นตัวเลขที่ถูกต้อง");
            return;
        }

        if (!decimal.TryParse(_cableTvRateTextBox.Text.Trim(), out var cableTvRate) || cableTvRate < 0)
        {
            ShowBillError("กรุณาระบุอัตรา Cable TV เป็นตัวเลขที่ถูกต้อง");
            return;
        }

        MeterReading? prevWater = TryParseMeterReading(_prevWaterTextBox.Text.Trim());
        MeterReading? currWater = TryParseMeterReading(_currWaterTextBox.Text.Trim());
        MeterReading? prevElec = TryParseMeterReading(_prevElecTextBox.Text.Trim());
        MeterReading? currElec = TryParseMeterReading(_currElecTextBox.Text.Trim());

        bool chargeMotorcycle = _chargeMotorcycleCheckBox.Checked;
        bool chargeCar = _chargeCarCheckBox.Checked;
        bool isCableTvExempt = _isCableTvExemptCheckBox.Checked;

        try
        {
            var request = new CreateMonthlyBillRequest(
                selectedRoom.Id,
                billingPeriod,
                monthlyRent,
                prevWater,
                currWater,
                prevElec,
                currElec,
                waterRate,
                elecRate,
                chargeMotorcycle,
                motorcycleRate,
                chargeCar,
                carRate,
                isCableTvExempt,
                cableTvRate);

            var bill = _createMonthlyBillService.Create(request);

            _resultRentLabel.Text = $"{bill.Rent.Amount:N2} บาท";
            _resultWaterLabel.Text = $"{bill.Water.Amount:N2} บาท";
            _resultElecLabel.Text = $"{bill.Electricity.Amount:N2} บาท";
            _resultMotorcycleParkingLabel.Text = $"{bill.MotorcycleParking.Amount:N2} บาท";
            _resultCarParkingLabel.Text = $"{bill.CarParking.Amount:N2} บาท";
            _resultCableTvLabel.Text = $"{bill.CableTv.Amount:N2} บาท";
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
