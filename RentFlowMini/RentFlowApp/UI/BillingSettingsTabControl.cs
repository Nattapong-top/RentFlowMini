﻿using RentFlowApp.Application.Billing;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Forms;

public class BillingSettingsTabControl : BaseTabControl
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

        InitializeComponents();
        LoadSettings();
    }

    private void InitializeComponents()
    {
        var mainLayout = CreateStandardLayout(3, [50f, 330f, -1f]);
        var titleLabel = CreateTitleLabel("ตั้งค่าราคากลางประจำระบบ");
        var group = BuildInputGroup();

        var messagePanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(4)
        };
        _messageLabel.Name = "SettingsMessageLabel";
        _messageLabel.Dock = DockStyle.Fill;
        _messageLabel.Font = new Font("Segoe UI", 11, FontStyle.Regular);
        messagePanel.Controls.Add(_messageLabel);

        mainLayout.Controls.Add(titleLabel, 0, 0);
        mainLayout.Controls.Add(group, 0, 1);
        mainLayout.Controls.Add(messagePanel, 0, 2);

        Controls.Add(mainLayout);
    }

    private GroupBox BuildInputGroup()
    {
        var group = new GroupBox
        {
            Name = "BillingSettingsGroup",
            Text = "อัตราค่าบริการกลาง",
            Dock = DockStyle.Fill,
            Padding = new Padding(12)
        };

        var panel = new Panel
        {
            Dock = DockStyle.Fill
        };

        var waterLabel = new Label { Text = "ค่าน้ำประปา (บาท/หน่วย):", Left = 12, Top = 25, Width = 230, Height = 25 };
        _waterRateTextBox.Name = "WaterRateTextBox";
        _waterRateTextBox.SetBounds(250, 22, 180, 30);
        ConfigureNumericTextBox(_waterRateTextBox);

        var elecLabel = new Label { Text = "ค่าไฟฟ้า (บาท/หน่วย):", Left = 12, Top = 70, Width = 230, Height = 25 };
        _elecRateTextBox.Name = "ElecRateTextBox";
        _elecRateTextBox.SetBounds(250, 67, 180, 30);
        ConfigureNumericTextBox(_elecRateTextBox);

        var motorcycleLabel = new Label { Text = "ค่าจอดรถมอเตอร์ไซค์ (บาท/เดือน):", Left = 12, Top = 115, Width = 230, Height = 25 };
        _motorcycleRateTextBox.Name = "MotorcycleRateTextBox";
        _motorcycleRateTextBox.SetBounds(250, 112, 180, 30);
        ConfigureNumericTextBox(_motorcycleRateTextBox);

        var carLabel = new Label { Text = "ค่าจอดรถยนต์ (บาท/เดือน):", Left = 12, Top = 160, Width = 230, Height = 25 };
        _carRateTextBox.Name = "CarRateTextBox";
        _carRateTextBox.SetBounds(250, 157, 180, 30);
        ConfigureNumericTextBox(_carRateTextBox);

        var cableTvLabel = new Label { Text = "ค่า Cable TV (บาท/เดือน):", Left = 12, Top = 205, Width = 230, Height = 25 };
        _cableTvRateTextBox.Name = "CableTvRateTextBox";
        _cableTvRateTextBox.SetBounds(250, 202, 180, 30);
        ConfigureNumericTextBox(_cableTvRateTextBox);

        _saveButton.Name = "SaveSettingsButton";
        _saveButton.Text = "บันทึกราคากลาง";
        _saveButton.Font = new Font("Segoe UI", 11, FontStyle.Bold);
        _saveButton.SetBounds(250, 245, 180, 40);
        _saveButton.Click += SaveButton_Click;

        panel.Controls.AddRange(
        [
            waterLabel, _waterRateTextBox,
            elecLabel, _elecRateTextBox,
            motorcycleLabel, _motorcycleRateTextBox,
            carLabel, _carRateTextBox,
            cableTvLabel, _cableTvRateTextBox,
            _saveButton
        ]);

        group.Controls.Add(panel);
        return group;
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
