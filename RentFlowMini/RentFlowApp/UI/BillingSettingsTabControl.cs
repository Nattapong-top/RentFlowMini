using System.Globalization;
using RentFlowApp.Application.Billing;
using RentFlowApp.Application.Reports;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Infrastructure.Sqlite;

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

    private readonly SqliteBackupService? _backupService;
    private readonly Label _lastBackupLabel = new();

    public BillingSettingsTabControl(
        IBillingSettingsRepository settingsRepository,
        UpdateBillingSettingsService updateSettingsService,
        IInvoiceSettingsRepository invoiceSettingsRepository,
        SqliteBackupService? backupService = null)
    {
        ArgumentNullException.ThrowIfNull(settingsRepository);
        ArgumentNullException.ThrowIfNull(updateSettingsService);
        ArgumentNullException.ThrowIfNull(invoiceSettingsRepository);

        _settingsRepository = settingsRepository;
        _updateSettingsService = updateSettingsService;
        _invoiceSettingsRepository = invoiceSettingsRepository;
        _backupService = backupService;

        Name = "BillingSettingsTabControl";

        InitializeComponents();
        LoadSettings();
        ShowLastBackupTime();
    }

    private void InitializeComponents()
    {
        var mainLayout = CreateStandardLayout(3, [44f, 32f, -1f]);
        var titleLabel = CreateTitleLabel("ตั้งค่าพื้นฐาน");

        _messageLabel.Name = "SettingsMessageLabel";
        _messageLabel.Dock = DockStyle.Fill;
        _messageLabel.Margin = Padding.Empty;
        _messageLabel.TextAlign = ContentAlignment.MiddleLeft;
        _messageLabel.Font = new Font("Segoe UI", 11, FontStyle.Regular);

        bool hasBackup = _backupService is not null;
        var groupsLayout = hasBackup
            ? CreateStandardLayout(3, [330f, 230f, 150f])
            : CreateStandardLayout(2, [330f, 230f]);
        groupsLayout.Padding = Padding.Empty;
        groupsLayout.Controls.Add(BuildInputGroup(), 0, 0);
        groupsLayout.Controls.Add(BuildInvoiceSettingsGroup(), 0, 1);
        if (hasBackup)
        {
            groupsLayout.Controls.Add(BuildBackupGroup(), 0, 2);
        }

        // เลื่อนเฉพาะฟอร์มกรอกข้อมูล เพื่อให้ผลการบันทึกยังเห็นอยู่บนจอเล็กแม้เลื่อนลงมากดปุ่มล่างสุด
        var scrollPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            AutoScroll = true,
            AutoScrollMinSize = new Size(790, hasBackup ? 710 : 560)
        };
        scrollPanel.Controls.Add(groupsLayout);

        mainLayout.Controls.Add(titleLabel, 0, 0);
        mainLayout.Controls.Add(_messageLabel, 0, 1);
        mainLayout.Controls.Add(scrollPanel, 0, 2);

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

            _messageLabel.ForeColor = ThemePalette.Current.SuccessText;
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

        var waterLabel = new Label { Text = "ค่าน้ำประปา (บาท/หน่วย):", Left = 12, Top = 15, Width = 230, Height = 25 };
        _waterRateTextBox.Name = "WaterRateTextBox";
        _waterRateTextBox.SetBounds(250, 12, 180, 30);
        ConfigureNumericTextBox(_waterRateTextBox);

        var elecLabel = new Label { Text = "ค่าไฟฟ้า (บาท/หน่วย):", Left = 12, Top = 60, Width = 230, Height = 25 };
        _elecRateTextBox.Name = "ElecRateTextBox";
        _elecRateTextBox.SetBounds(250, 57, 180, 30);
        ConfigureNumericTextBox(_elecRateTextBox);

        var motorcycleLabel = new Label { Text = "ค่าจอดรถมอเตอร์ไซค์ (บาท/เดือน):", Left = 12, Top = 105, Width = 230, Height = 25 };
        _motorcycleRateTextBox.Name = "MotorcycleRateTextBox";
        _motorcycleRateTextBox.SetBounds(250, 102, 180, 30);
        ConfigureNumericTextBox(_motorcycleRateTextBox);

        var carLabel = new Label { Text = "ค่าจอดรถยนต์ (บาท/เดือน):", Left = 12, Top = 150, Width = 230, Height = 25 };
        _carRateTextBox.Name = "CarRateTextBox";
        _carRateTextBox.SetBounds(250, 147, 180, 30);
        ConfigureNumericTextBox(_carRateTextBox);

        var cableTvLabel = new Label { Text = "ค่า Cable TV (บาท/เดือน):", Left = 12, Top = 195, Width = 230, Height = 25 };
        _cableTvRateTextBox.Name = "CableTvRateTextBox";
        _cableTvRateTextBox.SetBounds(250, 192, 180, 30);
        ConfigureNumericTextBox(_cableTvRateTextBox);

        _saveButton.Name = "SaveSettingsButton";
        _saveButton.Text = "บันทึกราคากลาง";
        _saveButton.Font = new Font("Segoe UI", 11, FontStyle.Bold);
        _saveButton.SetBounds(250, 235, 180, 40);
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
            ShowError(LogUnexpectedError(ex, "ไม่สามารถโหลดการตั้งค่าได้ กรุณาปิดแล้วเปิดโปรแกรมใหม่อีกครั้ง"));
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

            _messageLabel.ForeColor = ThemePalette.Current.SuccessText;
            _messageLabel.Text = "บันทึกราคากลางสำเร็จเรียบร้อย";
        }
        catch (ArgumentOutOfRangeException)
        {
            ShowError("อัตราค่าบริการต้องไม่ติดลบ");
        }
        catch (InvalidOperationException ex)
        {
            ShowError(ex.Message);
        }
        catch (Exception ex)
        {
            ShowError(LogUnexpectedError(ex));
        }
    }

    private GroupBox BuildBackupGroup()
    {
        var group = new GroupBox
        {
            Name = "BackupGroup",
            Text = "สำรองและกู้คืนข้อมูล",
            Dock = DockStyle.Fill,
            Padding = new Padding(12)
        };

        var panel = new Panel { Dock = DockStyle.Fill };

        _lastBackupLabel.Name = "LastBackupLabel";
        _lastBackupLabel.SetBounds(12, 12, 740, 25);

        var backUpNowButton = CreateBackupButton("BackUpNowButton", "สำรองตอนนี้", 12);
        backUpNowButton.Click += BackUpNowButton_Click;

        var restoreButton = CreateBackupButton("RestoreBackupButton", "กู้คืนจากไฟล์สำรอง", 222);
        restoreButton.Click += RestoreBackupButton_Click;

        var openFolderButton = CreateBackupButton("OpenBackupFolderButton", "เปิดโฟลเดอร์ไฟล์สำรอง", 432);
        openFolderButton.Click += OpenBackupFolderButton_Click;

        panel.Controls.AddRange([_lastBackupLabel, backUpNowButton, restoreButton, openFolderButton]);
        group.Controls.Add(panel);
        return group;
    }

    private static Button CreateBackupButton(string name, string text, int left)
    {
        var button = new Button
        {
            Name = name,
            Text = text,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        button.SetBounds(left, 50, 200, 40);
        return button;
    }

    private void ShowLastBackupTime()
    {
        if (_backupService is null)
        {
            return;
        }

        try
        {
            _lastBackupLabel.Text = _backupService.LastBackupTime is { } time
                ? $"สำรองล่าสุด: {ThaiCalendar.FormatDate(time)} {time.ToString("HH:mm", CultureInfo.InvariantCulture)}"
                : "ยังไม่มีไฟล์สำรอง";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _lastBackupLabel.Text = "ไม่สามารถอ่านรายการไฟล์สำรองได้";
        }
    }

    private void BackUpNowButton_Click(object? sender, EventArgs e)
    {
        try
        {
            _backupService!.BackUpNow();

            _messageLabel.ForeColor = ThemePalette.Current.SuccessText;
            _messageLabel.Text = "สำรองข้อมูลเรียบร้อยแล้ว";
        }
        catch (InvalidOperationException ex)
        {
            ShowError(ex.Message);
        }

        ShowLastBackupTime();
    }

    private void RestoreBackupButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "เลือกไฟล์สำรองที่ต้องการกู้คืน",
            Filter = "ไฟล์สำรอง RentFlow (*.db)|*.db",
            InitialDirectory = Directory.Exists(_backupService!.BackupFolder) ? _backupService.BackupFolder : string.Empty
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var answer = MessageBox.Show(
            $"ต้องการกู้คืนข้อมูลจากไฟล์ {Path.GetFileName(dialog.FileName)} ใช่หรือไม่\n\n" +
            "ข้อมูลที่บันทึกหลังจากวันที่สำรองไฟล์นี้จะหายไป ระบบจะเก็บสำเนาข้อมูลปัจจุบันไว้ในโฟลเดอร์ไฟล์สำรองก่อน แล้วเปิดโปรแกรมใหม่",
            "ยืนยันการกู้คืนข้อมูล",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

        if (answer != DialogResult.Yes)
        {
            return;
        }

        try
        {
            _backupService.RestoreFrom(dialog.FileName);
        }
        catch (InvalidOperationException ex)
        {
            ShowError(ex.Message);
            return;
        }

        MessageBox.Show(
            "กู้คืนข้อมูลเรียบร้อยแล้ว ระบบจะเปิดโปรแกรมใหม่",
            AppInfo.Name,
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
        // เปิดใหม่ เพื่อให้ทุกแท็บโหลดข้อมูลจากไฟล์ที่กู้คืน และอัปเดต schema ถ้าไฟล์สำรองเป็นรุ่นเก่า
        System.Windows.Forms.Application.Restart();
    }

    private void OpenBackupFolderButton_Click(object? sender, EventArgs e)
    {
        try
        {
            Directory.CreateDirectory(_backupService!.BackupFolder);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = _backupService.BackupFolder,
                UseShellExecute = true
            });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            ShowError("ไม่สามารถเปิดโฟลเดอร์ไฟล์สำรองได้ กรุณาลองใหม่อีกครั้ง");
        }
    }

    private void ShowError(string message)
    {
        _messageLabel.ForeColor = ThemePalette.Current.ErrorText;
        _messageLabel.Text = message;
    }
}
