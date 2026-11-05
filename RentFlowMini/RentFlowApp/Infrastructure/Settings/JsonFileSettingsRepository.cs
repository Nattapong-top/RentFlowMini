using System.Text.Encodings.Web;
using System.Text.Json;
using RentFlowApp.Application.Billing;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Infrastructure.Billing;

namespace RentFlowApp.Infrastructure.Settings;

public sealed class JsonFileSettingsRepository : IBillingSettingsRepository, IInvoiceSettingsRepository
{
    private const string SaveFailedMessage =
        "ไม่สามารถบันทึกการตั้งค่าลงไฟล์ได้ กรุณาตรวจสอบสิทธิ์การเข้าถึงโฟลเดอร์แล้วลองอีกครั้ง";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        // ให้เปิดอ่านไฟล์แล้วเห็นภาษาไทยตรง ๆ ไฟล์นี้ไม่ได้ถูกนำไปแสดงเป็น HTML
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly string _filePath;
    private BillingSettings _billingSettings = new InMemoryBillingSettingsRepository().Get();
    private InvoiceSettings _invoiceSettings = InvoiceSettings.Empty;

    public JsonFileSettingsRepository(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        _filePath = filePath;
        Load();
    }

    public bool LoadFailed { get; private set; }

    BillingSettings IBillingSettingsRepository.Get() => _billingSettings;

    void IBillingSettingsRepository.Save(BillingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        WriteFile(settings, _invoiceSettings);
        _billingSettings = settings;
    }

    InvoiceSettings IInvoiceSettingsRepository.Get() => _invoiceSettings;

    void IInvoiceSettingsRepository.Save(InvoiceSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        WriteFile(_billingSettings, settings);
        _invoiceSettings = settings;
    }

    private void Load()
    {
        if (!File.Exists(_filePath))
        {
            return;
        }

        try
        {
            var file = JsonSerializer.Deserialize<SettingsFile>(File.ReadAllText(_filePath))
                ?? throw new JsonException("Settings file is empty.");

            var billingSettings = BillingSettings.Create(
                file.WaterRate,
                file.ElectricityRate,
                file.MotorcycleParkingRate,
                file.CarParkingRate,
                file.CableTvRate);
            var invoiceSettings =
                string.IsNullOrWhiteSpace(file.PropertyName) && string.IsNullOrWhiteSpace(file.Address)
                    ? InvoiceSettings.Empty
                    : InvoiceSettings.Create(file.PropertyName, file.Address, file.BankAccountDetails);

            _billingSettings = billingSettings;
            _invoiceSettings = invoiceSettings;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or ArgumentException)
        {
            LoadFailed = true;
        }
    }

    private void WriteFile(BillingSettings billingSettings, InvoiceSettings invoiceSettings)
    {
        var file = new SettingsFile
        {
            WaterRate = billingSettings.WaterRate.Amount,
            ElectricityRate = billingSettings.ElectricityRate.Amount,
            MotorcycleParkingRate = billingSettings.MotorcycleParkingRate.Amount,
            CarParkingRate = billingSettings.CarParkingRate.Amount,
            CableTvRate = billingSettings.CableTvRate.Amount,
            PropertyName = invoiceSettings.PropertyName,
            Address = invoiceSettings.Address,
            BankAccountDetails = invoiceSettings.BankAccountDetails
        };

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_filePath))!);

            // เก็บไฟล์ที่อ่านไม่ได้ไว้ เพื่อให้ยังกู้ข้อมูลเดิมได้หลังบันทึกทับ
            if (LoadFailed && File.Exists(_filePath))
            {
                File.Copy(_filePath, $"{_filePath}.bad", overwrite: true);
            }

            // เขียนไฟล์ชั่วคราวก่อน เพื่อไม่ให้ไฟล์ตั้งค่าเสียถ้าเครื่องดับระหว่างบันทึก
            string temporaryPath = $"{_filePath}.tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(file, SerializerOptions));
            File.Move(temporaryPath, _filePath, overwrite: true);

            LoadFailed = false;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException(SaveFailedMessage, ex);
        }
    }

    private sealed class SettingsFile
    {
        public required decimal WaterRate { get; init; }

        public required decimal ElectricityRate { get; init; }

        public required decimal MotorcycleParkingRate { get; init; }

        public required decimal CarParkingRate { get; init; }

        public required decimal CableTvRate { get; init; }

        public string? PropertyName { get; init; }

        public string? Address { get; init; }

        public string? BankAccountDetails { get; init; }
    }
}
