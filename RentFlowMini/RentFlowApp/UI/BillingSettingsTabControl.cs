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

    private readonly IInvoiceSettingsRepository _invoiceSettingsRepository;
    private readonly TextBox _propertyNameTextBox = new();
    private readonly TextBox _addressTextBox = new();
    private readonly TextBox _bankAccountTextBox = new();
    private readonly Button _saveInvoiceSettingsButton = new();

    public BillingSettingsTabControl(
        IBillingSettingsRepository settingsRepository,
        UpdateBillingSettingsService updateSettingsService,
        IInvoiceSettingsRepository invoiceSettingsRepository)
    {
        ArgumentNullException.ThrowIfNull(settingsRepository);
        ArgumentNullException.ThrowIfNull(updateSettingsService);
        ArgumentNullException.ThrowIfNull(invoiceSettingsRepository);

        _settingsRepository = settingsRepository;
        _updateSettingsService = updateSettingsService;
        _invoiceSettingsRepository = invoiceSettingsRepository;

        Name = "BillingSettingsTabControl";

        InitializeComponents();
        LoadSettings();
    }

    private void InitializeComponents()
    {
        var mainLayout = CreateStandardLayout(4, [50f, 330f, 230f, -1f]);
        var titleLabel = CreateTitleLabel("ตั้งค่าพื้นฐาน");
        var group = BuildInputGroup();
        var invoiceGroup = BuildInvoiceSettingsGroup();

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
        mainLayout.Controls.Add(invoiceGroup, 0, 2);
        mainLayout.Controls.Add(messagePanel, 0, 3);

        Controls.Add(mainLayout);
    }

    private GroupBox BuildInvoiceSettingsGroup()
    {
        var group = new GroupBox
        {
            Name = "InvoiceSettingsGroup",
            Text = "ข้อมูลหอพักบนใบแจ้งหนี้",
            Dock = DockStyle.Fill,
            Padding = new Padding(12)
        };

        var panel = new Panel { Dock = DockStyle.Fill };

        var nameLabel = new Label { Text = "ชื่อหอพัก:", Left = 12, Top = 15, Width = 230, Height = 25 };
        _propertyNameTextBox.Name = "InvoicePropertyNameTextBox";
        _propertyNameTextBox.SetBounds(250, 12, 500, 30);

        var addressLabel = new Label { Text = "ที่อยู่:", Left = 12, Top = 55, Width = 230, Height = 25 };
        _addressTextBox.Name = "InvoiceAddressTextBox";
        _addressTextBox.SetBounds(250, 52, 500, 30);

        var bankLabel = new Label { Text = "บัญชีสำหรับโอนเงิน (ธนาคาร เลขบัญชี ชื่อบัญชี):", Left = 12, Top = 95, Width = 235, Height = 40 };
        _bankAccountTextBox.Name = "InvoiceBankAccountTextBox";
        _bankAccountTextBox.SetBounds(250, 92, 500, 30);

        _saveInvoiceSettingsButton.Name = "SaveInvoiceSettingsButton";
        _saveInvoiceSettingsButton.Text = "บันทึกข้อมูลหอพัก";
        _saveInvoiceSettingsButton.Font = new Font("Segoe UI", 11, FontStyle.Bold);
        _saveInvoiceSettingsButton.SetBounds(250, 135, 180, 40);
        _saveInvoiceSettingsButton.Click += SaveInvoiceSettingsButton_Click;

        panel.Controls.AddRange(
        [
            nameLabel, _propertyNameTextBox,
            addressLabel, _addressTextBox,
            bankLabel, _bankAccountTextBox,
            _saveInvoiceSettingsButton
        ]);

        group.Controls.Add(panel);
        return group;
    }

    private void SaveInvoiceSettingsButton_Click(object? sender, EventArgs e)
    {
        try
        {
            _invoiceSettingsRepository.Save(InvoiceSettings.Create(
                _propertyNameTextBox.Text,
                _addressTextBox.Text,
                _bankAccountTextBox.Text));

            _messageLabel.ForeColor = Color.DarkGreen;
            _messageLabel.Text = "บันทึกข้อมูลหอพักสำเร็จเรียบร้อย";
        }
        catch (ArgumentException ex)
        {
            ShowError(ex.ParamName is null ? ex.Message : ex.Message.Split(" (Parameter")[0]);
        }
        catch (InvalidOperationException ex)
        {
            ShowError(ex.Message);
        }
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

            var invoiceSettings = _invoiceSettingsRepository.Get();
            _propertyNameTextBox.Text = invoiceSettings.PropertyName;
            _addressTextBox.Text = invoiceSettings.Address;
            _bankAccountTextBox.Text = invoiceSettings.BankAccountDetails;
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
