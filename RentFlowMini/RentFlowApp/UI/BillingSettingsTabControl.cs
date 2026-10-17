using RentFlowApp.Application.Billing;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Forms;

public class BillingSettingsTabControl : UserControl
{
    private readonly IBillingSettingsRepository _settingsRepository;
    private readonly UpdateBillingSettingsService _updateSettingsService;

    private readonly TextBox _waterRateTextBox = new();
    private readonly TextBox _elecRateTextBox = new();
    private readonly TextBox _motorcycleRateTextBox = new();
    private readonly TextBox _carRateTextBox = new();
    private readonly TextBox _cableTvRateTextBox = new();
    private readonly Button _saveButton = new();
    private readonly Label _messageLabel = new();

    public BillingSettingsTabControl(
        IBillingSettingsRepository settingsRepository,
        UpdateBillingSettingsService updateSettingsService)
    {
        ArgumentNullException.ThrowIfNull(settingsRepository);
        ArgumentNullException.ThrowIfNull(updateSettingsService);

        _settingsRepository = settingsRepository;
        _updateSettingsService = updateSettingsService;

        Name = "BillingSettingsTabControl";
        Dock = DockStyle.Fill;
        BackColor = Color.White;
        Font = new Font("Segoe UI", 11F);

        InitializeComponents();
        LoadSettings();
    }

    private void InitializeComponents()
    {
        var titleLabel = new Label
        {
            Text = "ตั้งค่าราคากลางประจำระบบ",
            Left = 20,
            Top = 20,
            Width = 400,
            Height = 35,
            Font = new Font("Segoe UI", 16, FontStyle.Bold)
        };

        var group = new GroupBox
        {
            Name = "BillingSettingsGroup",
            Text = "อัตราค่าบริการกลาง",
            Left = 20,
            Top = 70,
            Width = 520,
            Height = 320
        };

        var waterLabel = new Label { Text = "ค่าน้ำประปา (บาท/หน่วย):", Left = 20, Top = 38, Width = 230, Height = 25 };
        _waterRateTextBox.Name = "WaterRateTextBox";
        _waterRateTextBox.SetBounds(260, 35, 180, 30);
        ConfigureNumericTextBox(_waterRateTextBox);

        var elecLabel = new Label { Text = "ค่าไฟฟ้า (บาท/หน่วย):", Left = 20, Top = 83, Width = 230, Height = 25 };
        _elecRateTextBox.Name = "ElecRateTextBox";
        _elecRateTextBox.SetBounds(260, 80, 180, 30);
        ConfigureNumericTextBox(_elecRateTextBox);

        var motorcycleLabel = new Label { Text = "ค่าจอดรถมอเตอร์ไซค์ (บาท/เดือน):", Left = 20, Top = 128, Width = 230, Height = 25 };
        _motorcycleRateTextBox.Name = "MotorcycleRateTextBox";
        _motorcycleRateTextBox.SetBounds(260, 125, 180, 30);
        ConfigureNumericTextBox(_motorcycleRateTextBox);

        var carLabel = new Label { Text = "ค่าจอดรถยนต์ (บาท/เดือน):", Left = 20, Top = 173, Width = 230, Height = 25 };
        _carRateTextBox.Name = "CarRateTextBox";
        _carRateTextBox.SetBounds(260, 170, 180, 30);
        ConfigureNumericTextBox(_carRateTextBox);

        var cableTvLabel = new Label { Text = "ค่า Cable TV (บาท/เดือน):", Left = 20, Top = 218, Width = 230, Height = 25 };
        _cableTvRateTextBox.Name = "CableTvRateTextBox";
        _cableTvRateTextBox.SetBounds(260, 215, 180, 30);
        ConfigureNumericTextBox(_cableTvRateTextBox);

        _saveButton.Name = "SaveSettingsButton";
        _saveButton.Text = "บันทึกราคากลาง";
        _saveButton.Font = new Font("Segoe UI", 11, FontStyle.Bold);
        _saveButton.SetBounds(260, 262, 180, 40);
        _saveButton.Click += SaveButton_Click;

        group.Controls.AddRange(
        [
            waterLabel, _waterRateTextBox,
            elecLabel, _elecRateTextBox,
            motorcycleLabel, _motorcycleRateTextBox,
            carLabel, _carRateTextBox,
            cableTvLabel, _cableTvRateTextBox,
            _saveButton
        ]);

        _messageLabel.Name = "SettingsMessageLabel";
        _messageLabel.SetBounds(20, 410, 650, 35);
        _messageLabel.Font = new Font("Segoe UI", 11, FontStyle.Regular);

        Controls.AddRange(
        [
            titleLabel,
            group,
            _messageLabel
        ]);
    }

    private static void ConfigureNumericTextBox(TextBox textBox)
    {
        textBox.KeyPress += (s, e) =>
        {
            // อนุญาตเฉพาะตัวเลข (0-9), จุดทศนิยม (.), และปุ่มควบคุม (Backspace, etc.)
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
            {
                e.Handled = true;
            }

            // อนุญาตจุดทศนิยมได้เพียง 1 จุดเท่านั้น
            if (e.KeyChar == '.' && textBox.Text.Contains('.'))
            {
                e.Handled = true;
            }
        };
    }

    public void LoadSettings()
    {
        try
        {
            var settings = _settingsRepository.Get();
            _waterRateTextBox.Text = settings.WaterRate.Amount.ToString("0.##");
            _elecRateTextBox.Text = settings.ElectricityRate.Amount.ToString("0.##");
            _motorcycleRateTextBox.Text = settings.MotorcycleParkingRate.Amount.ToString("0.##");
            _carRateTextBox.Text = settings.CarParkingRate.Amount.ToString("0.##");
            _cableTvRateTextBox.Text = settings.CableTvRate.Amount.ToString("0.##");
        }
        catch (Exception ex)
        {
            ShowError($"ไม่สามารถโหลดราคากลางได้: {ex.Message}");
        }
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        if (!decimal.TryParse(_waterRateTextBox.Text.Trim(), out var water) || water < 0)
        {
            ShowError("กรุณาระบุค่าน้ำประปาเป็นตัวเลขที่ไม่ติดลบ");
            return;
        }

        if (!decimal.TryParse(_elecRateTextBox.Text.Trim(), out var elec) || elec < 0)
        {
            ShowError("กรุณาระบุค่าไฟฟ้าเป็นตัวเลขที่ไม่ติดลบ");
            return;
        }

        if (!decimal.TryParse(_motorcycleRateTextBox.Text.Trim(), out var motorcycle) || motorcycle < 0)
        {
            ShowError("กรุณาระบุค่าจอดรถมอเตอร์ไซค์เป็นตัวเลขที่ไม่ติดลบ");
            return;
        }

        if (!decimal.TryParse(_carRateTextBox.Text.Trim(), out var car) || car < 0)
        {
            ShowError("กรุณาระบุค่าจอดรถยนต์เป็นตัวเลขที่ไม่ติดลบ");
            return;
        }

        if (!decimal.TryParse(_cableTvRateTextBox.Text.Trim(), out var cableTv) || cableTv < 0)
        {
            ShowError("กรุณาระบุค่า Cable TV เป็นตัวเลขที่ไม่ติดลบ");
            return;
        }

        try
        {
            _updateSettingsService.Update(
                Money.Create(water),
                Money.Create(elec),
                Money.Create(motorcycle),
                Money.Create(car),
                Money.Create(cableTv));

            _messageLabel.ForeColor = Color.DarkGreen;
            _messageLabel.Text = "บันทึกราคากลางสำเร็จเรียบร้อย";
        }
        catch (ArgumentOutOfRangeException)
        {
            ShowError("อัตราค่าบริการต้องไม่ติดลบ");
        }
        catch (Exception ex)
        {
            ShowError($"เกิดข้อผิดพลาด: {ex.Message}");
        }
    }

    private void ShowError(string message)
    {
        _messageLabel.ForeColor = Color.DarkRed;
        _messageLabel.Text = message;
    }
}
