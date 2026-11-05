using RentFlowApp.Application.Billing;
using RentFlowApp.Application.Rooms;
using RentFlowApp.Application.Tenants;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Infrastructure.Billing;
using RentFlowApp.Infrastructure.Tenants;

namespace RentFlowApp.Forms;

public class MonthlyBillTabControl : BaseTabControl
{
    private const string CreateBillButtonText = "คำนวณและสร้างบิล";
    private const string SaveBillEditButtonText = "บันทึกการแก้ไขบิล";

    private readonly CreateMonthlyBillService? _createMonthlyBillService;
    private readonly UpdateBillChargesService _updateBillChargesService;
    private readonly IInvoiceSettingsRepository _invoiceSettingsRepository;
    private readonly MonthlyInvoiceBuilder _invoiceBuilder;
    private readonly Action<MonthlyInvoicePrintDocument> _showInvoicePreview;
    private readonly IRoomRepository _roomRepository;
    private readonly IBillingSettingsRepository _billingSettingsRepository;
    private readonly IMonthlyMeterReadingRepository _monthlyMeterReadingRepository;
    private readonly IBillRepository _billRepository;
    private readonly ITenantRepository _tenantRepository;

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
    private readonly DataGridView _billGrid = new();

    // Result Labels (Right Side Detailed Summary Box)
    private readonly Label _resultRoomLabel = new();
    private readonly Label _resultInvoiceNumberLabel = new();
    private readonly Label _resultPeriodLabel = new();
    private readonly Label _resultCreatedAtLabel = new();
    private readonly Label _resultDueDateLabel = new();
    private readonly Label _resultTenantLabel = new();
    private readonly Label _resultRentLabel = new();
    private readonly Label _resultWaterDetailsLabel = new();
    private readonly Label _resultWaterAmountLabel = new();
    private readonly Label _resultElecDetailsLabel = new();
    private readonly Label _resultElecAmountLabel = new();
    private readonly Label _resultMotorcycleParkingLabel = new();
    private readonly Label _resultCarParkingLabel = new();
    private readonly Label _resultCableTvLabel = new();
    private readonly Label _resultTotalLabel = new();
    private readonly Label _billMessageLabel = new();

    private Bill? _editingBill;

    private sealed record RoomItem(RoomId Id, string RoomNumber, decimal MonthlyRent, bool IsCableTvExempt)
    {
        public override string ToString() => RoomNumber;
    }

    private sealed record BillGridItem(
        BillId BillId,
        int Sequence,
        string InvoiceNumber,
        string RoomNumber,
        string TenantName,
        string BillingPeriodDisplay,
        string CreatedAtDisplay,
        string DueDateDisplay,
        string RentAmount,
        string WaterReading,
        string WaterUsed,
        string WaterAmount,
        string ElecReading,
        string ElecUsed,
        string ElecAmount,
        string MotorcycleAmount,
        string CarAmount,
        string CableAmount,
        string TotalAmount);

    public MonthlyBillTabControl(
        CreateMonthlyBillService? createMonthlyBillService,
        IRoomRepository roomRepository,
        IMonthlyMeterReadingRepository? monthlyMeterReadingRepository = null,
        IBillRepository? billRepository = null,
        ITenantRepository? tenantRepository = null,
        IBillingSettingsRepository? billingSettingsRepository = null,
        IInvoiceSettingsRepository? invoiceSettingsRepository = null,
        Action<MonthlyInvoicePrintDocument>? showInvoicePreview = null)
    {
        ArgumentNullException.ThrowIfNull(roomRepository);

        _createMonthlyBillService = createMonthlyBillService;
        _roomRepository = roomRepository;
        _billingSettingsRepository = billingSettingsRepository ?? new InMemoryBillingSettingsRepository();
        _monthlyMeterReadingRepository = monthlyMeterReadingRepository ?? new InMemoryMonthlyMeterReadingRepository();
        _billRepository = billRepository ?? new InMemoryBillRepository();
        _updateBillChargesService = new UpdateBillChargesService(_billRepository);
        _tenantRepository = tenantRepository ?? new InMemoryTenantRepository();
        _invoiceSettingsRepository = invoiceSettingsRepository ?? new InMemoryInvoiceSettingsRepository();
        _invoiceBuilder = new MonthlyInvoiceBuilder(
            _roomRepository,
            _tenantRepository,
            _invoiceSettingsRepository);
        _showInvoicePreview = showInvoicePreview ?? ShowInvoicePreviewDialog;

        Name = "MonthlyBillTabControl";

        InitializeComponents();
        RefreshRoomList();
        RefreshBillGrid();
    }

    private void InitializeComponents()
    {
        var mainLayout = CreateStandardLayout(3, [50f, 440f, -1f]);
        var titleLabel = CreateTitleLabel("สร้างบิลประจำเดือน");
        var inputAndResultPanel = BuildInputAndResultPanel();
        var listGroup = BuildListGroup();

        mainLayout.Controls.Add(titleLabel, 0, 0);
        mainLayout.Controls.Add(inputAndResultPanel, 0, 1);
        mainLayout.Controls.Add(listGroup, 0, 2);
        Controls.Add(mainLayout);
    }

    private Panel BuildInputAndResultPanel()
    {
        var outerPanel = new Panel
        {
            Dock = DockStyle.Fill
        };

        // Left Group: Input Controls (Width: 760, Height: 430)
        var inputGroup = new GroupBox
        {
            Name = "MonthlyBillInputGroup",
            Text = "สร้างบิลประจำเดือน (ครอบคลุม 6 รายการ)",
            Left = 0,
            Top = 0,
            Width = 760,
            Height = 430,
            Padding = new Padding(8)
        };

        var inputPanel = new Panel { Dock = DockStyle.Fill };

        // Room Selection
        var roomLabel = new Label { Text = "เลือกห้อง:", Left = 12, Top = 18, Width = 90, Height = 25 };
        _billRoomComboBox.Name = "BillRoomComboBox";
        _billRoomComboBox.SetBounds(110, 15, 170, 30);
        _billRoomComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _billRoomComboBox.SelectedIndexChanged += BillRoomComboBox_SelectedIndexChanged;

        // Billing Period Selector (Month & Year ComboBoxes)
        _periodSelector.Name = "BillingPeriodSelector";
        _periodSelector.SetBounds(295, 14, 350, 32);
        _periodSelector.PeriodChanged += (s, e) =>
        {
            RefreshRoomList();
            RefreshBillGrid();
            TryAutoLoadReadings();
        };

        // Monthly Rent (ReadOnly by default as loaded from DB)
        var rentLabel = new Label { Text = "ค่าเช่าห้อง (บาท):", Left = 12, Top = 58, Width = 110, Height = 25 };
        _monthlyRentTextBox.Name = "MonthlyRentTextBox";
        _monthlyRentTextBox.SetBounds(130, 55, 150, 30);
        _monthlyRentTextBox.ReadOnly = true;
        _monthlyRentTextBox.TabStop = false;
        _monthlyRentTextBox.BackColor = SystemColors.ControlLight;

        // Water GroupBox (Readonly for previous, current & rate loaded from DB/Settings)
        var waterGroup = new GroupBox
        {
            Name = "WaterGroup",
            Text = "ค่าน้ำประปา",
            Left = 12,
            Top = 95,
            Width = 355,
            Height = 135
        };

        var prevWaterLabel = new Label { Text = "มิเตอร์ก่อนหน้า:", Left = 12, Top = 25, Width = 100, Height = 25 };
        _prevWaterTextBox.Name = "PrevWaterTextBox";
        _prevWaterTextBox.SetBounds(125, 22, 120, 30);
        _prevWaterTextBox.ReadOnly = true;
        _prevWaterTextBox.TabStop = false;
        _prevWaterTextBox.BackColor = SystemColors.ControlLight;

        var currWaterLabel = new Label { Text = "มิเตอร์ปัจจุบัน:", Left = 12, Top = 62, Width = 100, Height = 25 };
        _currWaterTextBox.Name = "CurrWaterTextBox";
        _currWaterTextBox.SetBounds(125, 59, 120, 30);
        _currWaterTextBox.ReadOnly = true;
        _currWaterTextBox.TabStop = false;
        _currWaterTextBox.BackColor = SystemColors.ControlLight;

        var waterRateLabel = new Label { Text = "อัตรา (บาท/หน่วย):", Left = 12, Top = 98, Width = 110, Height = 25 };
        _waterRateTextBox.Name = "WaterRateTextBox";
        _waterRateTextBox.SetBounds(125, 95, 120, 30);
        _waterRateTextBox.Text = "19";
        _waterRateTextBox.ReadOnly = true;
        _waterRateTextBox.TabStop = false;
        _waterRateTextBox.BackColor = SystemColors.ControlLight;

        waterGroup.Controls.AddRange(
        [
            prevWaterLabel, _prevWaterTextBox,
            currWaterLabel, _currWaterTextBox,
            waterRateLabel, _waterRateTextBox
        ]);

        // Electricity GroupBox (Readonly)
        var elecGroup = new GroupBox
        {
            Name = "ElectricityGroup",
            Text = "ค่าไฟฟ้า",
            Left = 380,
            Top = 95,
            Width = 355,
            Height = 135
        };

        var prevElecLabel = new Label { Text = "มิเตอร์ก่อนหน้า:", Left = 12, Top = 25, Width = 100, Height = 25 };
        _prevElecTextBox.Name = "PrevElecTextBox";
        _prevElecTextBox.SetBounds(125, 22, 120, 30);
        _prevElecTextBox.ReadOnly = true;
        _prevElecTextBox.TabStop = false;
        _prevElecTextBox.BackColor = SystemColors.ControlLight;

        var currElecLabel = new Label { Text = "มิเตอร์ปัจจุบัน:", Left = 12, Top = 62, Width = 100, Height = 25 };
        _currElecTextBox.Name = "CurrElecTextBox";
        _currElecTextBox.SetBounds(125, 59, 120, 30);
        _currElecTextBox.ReadOnly = true;
        _currElecTextBox.TabStop = false;
        _currElecTextBox.BackColor = SystemColors.ControlLight;

        var elecRateLabel = new Label { Text = "อัตรา (บาท/หน่วย):", Left = 12, Top = 98, Width = 110, Height = 25 };
        _elecRateTextBox.Name = "ElecRateTextBox";
        _elecRateTextBox.SetBounds(125, 95, 120, 30);
        _elecRateTextBox.Text = "8";
        _elecRateTextBox.ReadOnly = true;
        _elecRateTextBox.TabStop = false;
        _elecRateTextBox.BackColor = SystemColors.ControlLight;

        elecGroup.Controls.AddRange(
        [
            prevElecLabel, _prevElecTextBox,
            currElecLabel, _currElecTextBox,
            elecRateLabel, _elecRateTextBox
        ]);

        // Additional Charges GroupBox (Parking & Cable TV Rates ReadOnly)
        var addGroup = new GroupBox
        {
            Name = "AdditionalChargesGroup",
            Text = "ค่าบริการอื่นๆ (ที่จอดรถ & Cable TV)",
            Left = 12,
            Top = 238,
            Width = 723,
            Height = 90
        };

        _chargeMotorcycleCheckBox.Name = "ChargeMotorcycleCheckBox";
        _chargeMotorcycleCheckBox.Text = "มอเตอร์ไซค์";
        _chargeMotorcycleCheckBox.SetBounds(12, 25, 95, 25);
        _motorcycleRateTextBox.Name = "MotorcycleRateTextBox";
        _motorcycleRateTextBox.SetBounds(110, 23, 70, 27);
        _motorcycleRateTextBox.Text = "300";
        _motorcycleRateTextBox.ReadOnly = true;
        _motorcycleRateTextBox.TabStop = false;
        _motorcycleRateTextBox.BackColor = SystemColors.ControlLight;

        _chargeCarCheckBox.Name = "ChargeCarCheckBox";
        _chargeCarCheckBox.Text = "รถยนต์";
        _chargeCarCheckBox.SetBounds(190, 25, 75, 25);
        _carRateTextBox.Name = "CarRateTextBox";
        _carRateTextBox.SetBounds(265, 23, 70, 27);
        _carRateTextBox.Text = "500";
        _carRateTextBox.ReadOnly = true;
        _carRateTextBox.TabStop = false;
        _carRateTextBox.BackColor = SystemColors.ControlLight;

        _isCableTvExemptCheckBox.Name = "IsCableTvExemptCheckBox";
        _isCableTvExemptCheckBox.Text = "ยกเว้น Cable";
        _isCableTvExemptCheckBox.SetBounds(345, 25, 110, 25);

        
        var cableTvRateLbl = new Label { Text = "อัตรา Cable:", Left = 465, Top = 27, Width = 75, Height = 22 };
        _cableTvRateTextBox.Name = "CableTvRateTextBox";
        _cableTvRateTextBox.SetBounds(545, 23, 75, 27);
        _cableTvRateTextBox.Text = "60";
        _cableTvRateTextBox.ReadOnly = true;
        _cableTvRateTextBox.TabStop = false;
        _cableTvRateTextBox.BackColor = SystemColors.ControlLight;

        addGroup.Controls.AddRange(
        [
            _chargeMotorcycleCheckBox, _motorcycleRateTextBox,
            _chargeCarCheckBox, _carRateTextBox,
            _isCableTvExemptCheckBox,
            cableTvRateLbl, _cableTvRateTextBox
        ]);

        // Create Bill Button & Message Label
        _createBillButton.Name = "CreateBillButton";
        _createBillButton.Text = CreateBillButtonText;
        _createBillButton.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        _createBillButton.SetBounds(12, 345, 180, 42);
        _createBillButton.Click += CreateBillButton_Click;

        _billMessageLabel.Name = "BillMessageLabel";
        _billMessageLabel.SetBounds(205, 353, 530, 28);
        _billMessageLabel.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

        inputPanel.Controls.AddRange(
        [
            roomLabel, _billRoomComboBox,
            _periodSelector,
            rentLabel, _monthlyRentTextBox,
            waterGroup,
            elecGroup,
            addGroup,
            _createBillButton,
            _billMessageLabel
        ]);
        inputGroup.Controls.Add(inputPanel);

        // Right Group: Detailed Result Summary Box (Width: 360, Height: 430)
        var resultGroup = BuildResultGroup();
        resultGroup.Left = 770;
        resultGroup.Top = 0;
        resultGroup.Width = 380;
        resultGroup.Height = 430;

        outerPanel.Controls.AddRange([inputGroup, resultGroup]);
        return outerPanel;
    }

    private static string FormatDateThai(DateTime date) =>
        $"{date.Day:D2}/{date.Month:D2}/{date.Year + 543}";

    private static string FormatDateThai(DateOnly date) =>
        FormatDateThai(date.ToDateTime(TimeOnly.MinValue));

    private GroupBox BuildResultGroup()
    {
        var resultGroup = new GroupBox
        {
            Name = "ResultGroup",
            Text = "สรุปรายละเอียดบิล",
            Padding = new Padding(10),
            BackColor = Color.White
        };

        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = false
        };

        // Header Card (Room, Invoice No, Tenant, Billing Month, Date)
        var headerPanel = new Panel
        {
            Left = 4,
            Top = 6,
            Width = 350,
            Height = 68,
            BackColor = Color.FromArgb(246, 248, 251)
        };

        _resultRoomLabel.Name = "ResultRoomLabel";
        _resultRoomLabel.SetBounds(8, 6, 150, 26);
        _resultRoomLabel.Font = new Font("Segoe UI", 13.5f, FontStyle.Bold);
        _resultRoomLabel.ForeColor = Color.FromArgb(24, 76, 120);
        _resultRoomLabel.Text = "ห้อง -";

        _resultInvoiceNumberLabel.Name = "ResultInvoiceNumberLabel";
        _resultInvoiceNumberLabel.SetBounds(160, 6, 182, 24);
        _resultInvoiceNumberLabel.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
        _resultInvoiceNumberLabel.ForeColor = Color.FromArgb(24, 76, 120);
        _resultInvoiceNumberLabel.TextAlign = ContentAlignment.MiddleRight;
        _resultInvoiceNumberLabel.Text = "ใบแจ้งหนี้ เลขที่ : -";

        _resultTenantLabel.Name = "ResultTenantLabel";
        _resultTenantLabel.SetBounds(8, 32, 175, 21);
        _resultTenantLabel.Font = new Font("Segoe UI", 10.5f, FontStyle.Regular);
        _resultTenantLabel.ForeColor = Color.FromArgb(50, 50, 50);
        _resultTenantLabel.Text = "ผู้เช่า: -";

        _resultPeriodLabel.Name = "ResultPeriodLabel";
        _resultPeriodLabel.SetBounds(185, 32, 157, 21);
        _resultPeriodLabel.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        _resultPeriodLabel.ForeColor = Color.FromArgb(70, 95, 125);
        _resultPeriodLabel.TextAlign = ContentAlignment.MiddleRight;
        _resultPeriodLabel.Text = "(รอบเดือนที่ -)";

        _resultCreatedAtLabel.Name = "ResultCreatedAtLabel";
        _resultCreatedAtLabel.SetBounds(150, 51, 192, 18);
        _resultCreatedAtLabel.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
        _resultCreatedAtLabel.ForeColor = Color.DimGray;
        _resultCreatedAtLabel.TextAlign = ContentAlignment.MiddleRight;
        _resultCreatedAtLabel.Text = "วันที่ออกบิล: -";

        _resultDueDateLabel.Name = "ResultDueDateLabel";
        _resultDueDateLabel.SetBounds(8, 51, 142, 18);
        _resultDueDateLabel.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
        _resultDueDateLabel.ForeColor = Color.DimGray;
        _resultDueDateLabel.Text = "ครบกำหนด: -";

        headerPanel.Controls.AddRange([
            _resultRoomLabel, _resultInvoiceNumberLabel,
            _resultTenantLabel, _resultPeriodLabel,
            _resultDueDateLabel, _resultCreatedAtLabel
        ]);

        var topDivider = new Panel
        {
            Left = 4,
            Top = 76,
            Width = 350,
            Height = 1,
            BackColor = Color.FromArgb(226, 232, 240)
        };

        // Item Lines
        var rentHeader = new Label { Text = "ค่าเช่าห้อง", Left = 10, Top = 82, Width = 150, Height = 22, Font = new Font("Segoe UI", 10.5f) };
        _resultRentLabel.Name = "ResultRentLabel";
        _resultRentLabel.SetBounds(160, 82, 190, 22);
        _resultRentLabel.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
        _resultRentLabel.TextAlign = ContentAlignment.MiddleRight;
        _resultRentLabel.ForeColor = Color.FromArgb(30, 41, 59);
        _resultRentLabel.Text = "-";

        var waterHeader = new Label { Text = "ค่าน้ำประปา", Left = 10, Top = 107, Width = 150, Height = 22, Font = new Font("Segoe UI", 10.5f) };
        _resultWaterAmountLabel.Name = "ResultWaterAmountLabel";
        _resultWaterAmountLabel.SetBounds(160, 107, 190, 22);
        _resultWaterAmountLabel.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
        _resultWaterAmountLabel.TextAlign = ContentAlignment.MiddleRight;
        _resultWaterAmountLabel.ForeColor = Color.FromArgb(30, 41, 59);
        _resultWaterAmountLabel.Text = "-";

        _resultWaterDetailsLabel.Name = "ResultWaterDetailsLabel";
        _resultWaterDetailsLabel.SetBounds(22, 131, 328, 19);
        _resultWaterDetailsLabel.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        _resultWaterDetailsLabel.ForeColor = Color.FromArgb(100, 116, 139);
        _resultWaterDetailsLabel.Text = "-";

        var elecHeader = new Label { Text = "ค่าไฟฟ้า", Left = 10, Top = 153, Width = 150, Height = 22, Font = new Font("Segoe UI", 10.5f) };
        _resultElecAmountLabel.Name = "ResultElecAmountLabel";
        _resultElecAmountLabel.SetBounds(160, 153, 190, 22);
        _resultElecAmountLabel.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
        _resultElecAmountLabel.TextAlign = ContentAlignment.MiddleRight;
        _resultElecAmountLabel.ForeColor = Color.FromArgb(30, 41, 59);
        _resultElecAmountLabel.Text = "-";

        _resultElecDetailsLabel.Name = "ResultElecDetailsLabel";
        _resultElecDetailsLabel.SetBounds(22, 177, 328, 19);
        _resultElecDetailsLabel.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        _resultElecDetailsLabel.ForeColor = Color.FromArgb(100, 116, 139);
        _resultElecDetailsLabel.Text = "-";

        var motorcycleHeader = new Label { Text = "ค่าจอดมอเตอร์ไซค์", Left = 10, Top = 199, Width = 150, Height = 22, Font = new Font("Segoe UI", 10.5f) };
        _resultMotorcycleParkingLabel.Name = "ResultMotorcycleParkingLabel";
        _resultMotorcycleParkingLabel.SetBounds(160, 199, 190, 22);
        _resultMotorcycleParkingLabel.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
        _resultMotorcycleParkingLabel.TextAlign = ContentAlignment.MiddleRight;
        _resultMotorcycleParkingLabel.ForeColor = Color.FromArgb(30, 41, 59);
        _resultMotorcycleParkingLabel.Text = "-";

        var carHeader = new Label { Text = "ค่าจอดรถยนต์", Left = 10, Top = 224, Width = 150, Height = 22, Font = new Font("Segoe UI", 10.5f) };
        _resultCarParkingLabel.Name = "ResultCarParkingLabel";
        _resultCarParkingLabel.SetBounds(160, 224, 190, 22);
        _resultCarParkingLabel.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
        _resultCarParkingLabel.TextAlign = ContentAlignment.MiddleRight;
        _resultCarParkingLabel.ForeColor = Color.FromArgb(30, 41, 59);
        _resultCarParkingLabel.Text = "-";

        var cableTvHeader = new Label { Text = "ค่าเคเบิลทีวี (Cable TV)", Left = 10, Top = 249, Width = 150, Height = 22, Font = new Font("Segoe UI", 10.5f) };
        _resultCableTvLabel.Name = "ResultCableTvLabel";
        _resultCableTvLabel.SetBounds(160, 249, 190, 22);
        _resultCableTvLabel.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
        _resultCableTvLabel.TextAlign = ContentAlignment.MiddleRight;
        _resultCableTvLabel.ForeColor = Color.FromArgb(30, 41, 59);
        _resultCableTvLabel.Text = "-";

        // Total Highlight Box
        var totalPanel = new Panel
        {
            Left = 4,
            Top = 275,
            Width = 350,
            Height = 65,
            BackColor = Color.FromArgb(238, 246, 255),
            BorderStyle = BorderStyle.FixedSingle
        };

        var totalTitle = new Label
        {
            Text = "ยอดรวมทั้งสิ้น:",
            Left = 12,
            Top = 17,
            Width = 110,
            Height = 30,
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 58, 138)
        };

        _resultTotalLabel.Name = "ResultTotalLabel";
        _resultTotalLabel.SetBounds(120, 12, 215, 38);
        _resultTotalLabel.Font = new Font("Segoe UI", 15f, FontStyle.Bold);
        _resultTotalLabel.ForeColor = Color.FromArgb(29, 78, 216);
        _resultTotalLabel.TextAlign = ContentAlignment.MiddleRight;
        _resultTotalLabel.Text = "-";

        totalPanel.Controls.AddRange([totalTitle, _resultTotalLabel]);

        panel.Controls.AddRange(
        [
            headerPanel,
            topDivider,
            rentHeader, _resultRentLabel,
            waterHeader, _resultWaterAmountLabel,
            _resultWaterDetailsLabel,
            elecHeader, _resultElecAmountLabel,
            _resultElecDetailsLabel,
            motorcycleHeader, _resultMotorcycleParkingLabel,
            carHeader, _resultCarParkingLabel,
            cableTvHeader, _resultCableTvLabel,
            totalPanel
        ]);

        resultGroup.Controls.Add(panel);
        return resultGroup;
    }

    private GroupBox BuildListGroup()
    {
        var group = new GroupBox
        {
            Name = "BillListGroup",
            Text = "ประวัติการสร้างบิลประจำเดือน",
            Dock = DockStyle.Fill,
            Padding = new Padding(8)
        };

        _billGrid.Name = "BillGrid";
        _billGrid.Dock = DockStyle.Fill;
        _billGrid.ReadOnly = true;
        _billGrid.AllowUserToAddRows = false;
        _billGrid.AllowUserToDeleteRows = false;
        _billGrid.AutoGenerateColumns = false;
        _billGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _billGrid.MultiSelect = true;
        _billGrid.RowHeadersVisible = false;
        _billGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _billGrid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _billGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 248, 252);
        _billGrid.RowTemplate.Height = 30;
        _billGrid.ColumnHeadersHeight = 35;
        _billGrid.ColumnHeadersDefaultCellStyle.Font = new Font(Font, FontStyle.Bold);
        _billGrid.CellDoubleClick += BillGrid_CellDoubleClick;

        _billGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Sequence",
            HeaderText = "ลำดับ",
            DataPropertyName = "Sequence",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            Width = 50,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
        });
        _billGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "InvoiceNumber",
            HeaderText = "เลขที่ใบแจ้งหนี้",
            DataPropertyName = "InvoiceNumber",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            Width = 100,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                Font = new Font(Font, FontStyle.Bold)
            }
        });
        _billGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "RoomNumber",
            HeaderText = "ห้อง",
            DataPropertyName = "RoomNumber",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            Width = 70
        });
        _billGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "TenantName",
            HeaderText = "ผู้เช่า",
            DataPropertyName = "TenantName",
            Width = 120
        });
        _billGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "BillingPeriodDisplay",
            HeaderText = "รอบบิล",
            DataPropertyName = "BillingPeriodDisplay",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            Width = 90,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
        });
        _billGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "CreatedAtDisplay",
            HeaderText = "วันที่ออกบิล",
            DataPropertyName = "CreatedAtDisplay",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            Width = 115,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
        });
        _billGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "DueDateDisplay",
            HeaderText = "ครบกำหนด",
            DataPropertyName = "DueDateDisplay",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            Width = 100,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
        });
        _billGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "RentAmount",
            HeaderText = "ค่าเช่า",
            DataPropertyName = "RentAmount",
            Width = 85,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
        });
        _billGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "WaterReading",
            HeaderText = "มิเตอร์น้ำ (ก่อน->หลัง)",
            DataPropertyName = "WaterReading",
            Width = 130,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
        });
        _billGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "WaterUsed",
            HeaderText = "ใช้น้ำ",
            DataPropertyName = "WaterUsed",
            Width = 65,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
        });
        _billGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "WaterAmount",
            HeaderText = "ค่าน้ำ",
            DataPropertyName = "WaterAmount",
            Width = 80,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
        });
        _billGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "ElecReading",
            HeaderText = "มิเตอร์ไฟ (ก่อน->หลัง)",
            DataPropertyName = "ElecReading",
            Width = 130,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
        });
        _billGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "ElecUsed",
            HeaderText = "ใช้ไฟ",
            DataPropertyName = "ElecUsed",
            Width = 65,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
        });
        _billGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "ElecAmount",
            HeaderText = "ค่าไฟ",
            DataPropertyName = "ElecAmount",
            Width = 80,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
        });
        _billGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "MotorcycleAmount",
            HeaderText = "จอดมอเตอร์ไซค์",
            DataPropertyName = "MotorcycleAmount",
            Width = 90,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
        });
        _billGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "CarAmount",
            HeaderText = "จอดรถยนต์",
            DataPropertyName = "CarAmount",
            Width = 85,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
        });
        _billGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "CableAmount",
            HeaderText = "Cable TV",
            DataPropertyName = "CableAmount",
            Width = 80,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
        });
        _billGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "TotalAmount",
            HeaderText = "ยอดรวมทั้งสิ้น",
            DataPropertyName = "TotalAmount",
            Width = 105,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font(Font, FontStyle.Bold) }
        });

        var printPanel = new Panel { Dock = DockStyle.Top, Height = 42 };

        var printAllButton = new Button
        {
            Name = "PrintAllInvoicesButton",
            Text = "พิมพ์ใบแจ้งหนี้ทั้งรอบ",
            Left = 0,
            Top = 3,
            Width = 190,
            Height = 34
        };
        printAllButton.Click += (s, e) => PrintInvoices(_billGrid.Rows.Cast<DataGridViewRow>());

        var printSelectedButton = new Button
        {
            Name = "PrintSelectedInvoicesButton",
            Text = "พิมพ์เฉพาะห้องที่เลือก",
            Left = 200,
            Top = 3,
            Width = 190,
            Height = 34
        };
        printSelectedButton.Click += (s, e) => PrintInvoices(_billGrid.SelectedRows.Cast<DataGridViewRow>());

        printPanel.Controls.AddRange([printAllButton, printSelectedButton]);

        // แถบปุ่มต้องเพิ่มหลังตาราง เพื่อให้ WinForms จัด Dock.Top ก่อน Dock.Fill
        group.Controls.Add(_billGrid);
        group.Controls.Add(printPanel);
        return group;
    }

    private void PrintInvoices(IEnumerable<DataGridViewRow> rows)
    {
        // SelectedRows เรียงตามลำดับการเลือก จึงต้องเรียงใหม่ให้ตรงกับตาราง
        var bills = rows
            .OrderBy(row => row.Index)
            .Select(row => row.DataBoundItem)
            .OfType<BillGridItem>()
            .Select(item => _billRepository.Get(item.BillId))
            .OfType<Bill>()
            .ToList();

        if (bills.Count == 0)
        {
            ShowBillError("ไม่มีบิลสำหรับพิมพ์ กรุณาสร้างบิลหรือเลือกห้องจากตารางก่อน");
            return;
        }

        if (!_invoiceSettingsRepository.Get().IsConfigured)
        {
            ShowBillError("กรุณากรอกชื่อและที่อยู่หอพักในแท็บตั้งค่าก่อนพิมพ์ใบแจ้งหนี้");
            return;
        }

        try
        {
            using var document = new MonthlyInvoicePrintDocument(_invoiceBuilder.Build(bills));
            _showInvoicePreview(document);
        }
        catch (System.Drawing.Printing.InvalidPrinterException)
        {
            ShowBillError("ไม่พบเครื่องพิมพ์ในระบบ กรุณาติดตั้งเครื่องพิมพ์แล้วลองอีกครั้ง");
        }
    }

    private void ShowInvoicePreviewDialog(MonthlyInvoicePrintDocument document)
    {
        // เลือกเครื่องพิมพ์ก่อน เพราะปุ่มพิมพ์ใน PrintPreviewDialog จะสั่งพิมพ์ทันทีโดยไม่ถาม
        using var printDialog = new PrintDialog { Document = document, UseEXDialog = true };
        if (printDialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        using var previewDialog = new PrintPreviewDialog
        {
            Document = document,
            WindowState = FormWindowState.Maximized
        };
        previewDialog.ShowDialog(FindForm());
    }

    public void RefreshRoomList(RoomId? forceIncludeRoomId = null)
    {
        LoadDefaultSettings();

        var selectedItem = _billRoomComboBox.SelectedItem as RoomItem;
        _billRoomComboBox.Items.Clear();

        var currentPeriod = _periodSelector.GetBillingPeriod();
        var rooms = _roomRepository.GetAll();
        var createdBills = _billRepository.GetAll()
            .Where(b => currentPeriod != null && b.BillingPeriod == currentPeriod)
            .Select(b => b.RoomId)
            .ToHashSet();

        foreach (var room in rooms)
        {
            bool isBilled = createdBills.Contains(room.Id);
            if (isBilled && room.Id != forceIncludeRoomId)
            {
                continue; // กรองห้องที่สร้างบิลรอบนี้แล้วออก (Auto-advance)
            }

            var item = new RoomItem(room.Id, room.RoomNumber.Value, room.MonthlyRent.Amount, room.IsCableTvExempt);
            _billRoomComboBox.Items.Add(item);

            if (selectedItem != null && item.Id == selectedItem.Id)
            {
                _billRoomComboBox.SelectedItem = item;
            }
            else if (forceIncludeRoomId != null && room.Id == forceIncludeRoomId)
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

        ExitBillEditIfSelectionChanged();
        TryAutoLoadReadings();
    }

    public void RefreshBillGrid()
    {
        var currentPeriod = _periodSelector.GetBillingPeriod();
        var bills = _billRepository.GetAll()
            .Where(b => currentPeriod == null || b.BillingPeriod == currentPeriod)
            .ToList();

        var rooms = _roomRepository.GetAll().ToDictionary(r => r.Id, r => r.RoomNumber.Value);
        var tenants = _tenantRepository.GetAll().Where(t => t.RoomId != null).GroupBy(t => t.RoomId!).ToDictionary(g => g.Key, g => g.First());

        _billGrid.DataSource = bills
            .OrderBy(b => rooms.TryGetValue(b.RoomId, out var num) ? num : "")
            .Select((bill, index) =>
            {
                var roomNum = rooms.TryGetValue(bill.RoomId, out var num) ? num : "-";
                string tenantName = tenants.TryGetValue(bill.RoomId, out var tenant) ? $"{tenant.FirstName} {tenant.LastName}" : "-";
                
                string waterReading = "-";
                string waterUsed = "-";
                string elecReading = "-";
                string elecUsed = "-";

                if (bill.Meters is { } meters)
                {
                    waterReading = $"{meters.PreviousWater.Value:0.##} -> {meters.CurrentWater.Value:0.##}";
                    waterUsed = meters.WaterUsage.ToString("0.##");
                    elecReading = $"{meters.PreviousElectricity.Value:0.##} -> {meters.CurrentElectricity.Value:0.##}";
                    elecUsed = meters.ElectricityUsage.ToString("0.##");
                }

                return new BillGridItem(
                    BillId: bill.Id,
                    Sequence: index + 1,
                    InvoiceNumber: bill.InvoiceNumber,
                    RoomNumber: roomNum,
                    TenantName: tenantName,
                    BillingPeriodDisplay: bill.BillingPeriod.ToBuddhistValue(),
                    CreatedAtDisplay: FormatDateThai(bill.CreatedAt),
                    DueDateDisplay: FormatDateThai(bill.DueDate),
                    RentAmount: bill.Rent.Amount.ToString("N2"),
                    WaterReading: waterReading,
                    WaterUsed: waterUsed,
                    WaterAmount: bill.Water.Amount.ToString("N2"),
                    ElecReading: elecReading,
                    ElecUsed: elecUsed,
                    ElecAmount: bill.Electricity.Amount.ToString("N2"),
                    MotorcycleAmount: bill.MotorcycleParking.Amount.ToString("N2"),
                    CarAmount: bill.CarParking.Amount.ToString("N2"),
                    CableAmount: bill.CableTv.Amount.ToString("N2"),
                    TotalAmount: bill.Total.Amount.ToString("N2"));
            })
            .ToList();
    }

    private void BillRoomComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_billRoomComboBox.SelectedItem is RoomItem room)
        {
            ApplyRoomDefaults(room);
        }

        ExitBillEditIfSelectionChanged();
        TryAutoLoadReadings();
    }

    private void SetEditingBill(Bill? bill)
    {
        _editingBill = bill;
        _createBillButton.Text = bill is null ? CreateBillButtonText : SaveBillEditButtonText;
    }

    private void ExitBillEditIfSelectionChanged()
    {
        if (_editingBill is null)
        {
            return;
        }

        bool isSameBillSelected =
            _billRoomComboBox.SelectedItem is RoomItem room &&
            room.Id == _editingBill.RoomId &&
            _periodSelector.GetBillingPeriod() == _editingBill.BillingPeriod;

        if (!isSameBillSelected)
        {
            SetEditingBill(null);
        }
    }

    private void ApplyRoomDefaults(RoomItem room)
    {
        _monthlyRentTextBox.Text = room.MonthlyRent.ToString("0.##");
        _isCableTvExemptCheckBox.Checked = room.IsCableTvExempt;
        _chargeMotorcycleCheckBox.Checked = false;
        _chargeCarCheckBox.Checked = false;
    }

    private void TryAutoLoadReadings()
    {
        if (_billRoomComboBox.SelectedItem is not RoomItem selectedRoom)
        {
            _prevWaterTextBox.Clear();
            _currWaterTextBox.Clear();
            _prevElecTextBox.Clear();
            _currElecTextBox.Clear();
            _chargeMotorcycleCheckBox.Checked = false;
            _chargeCarCheckBox.Checked = false;
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

        if (_editingBill is not null)
        {
            SaveBillEdit(_editingBill, selectedRoom, motorcycleRate, carRate, cableTvRate);
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

            // อัปเดตกล่องสรุปผลด้านขวาให้ละเอียด
            var tenants = _tenantRepository.GetAll().Where(t => t.RoomId != null).GroupBy(t => t.RoomId!).ToDictionary(g => g.Key, g => g.First());
            string tenantName = tenants.TryGetValue(selectedRoom.Id, out var tenant) ? $"{tenant.FirstName} {tenant.LastName}" : "-";
            
            decimal waterUsed = 0;
            if (prevWater != null && currWater != null) waterUsed = currWater.Value - prevWater.Value;
            decimal elecUsed = 0;
            if (prevElec != null && currElec != null) elecUsed = currElec.Value - prevElec.Value;

            _resultRoomLabel.Text = $"ห้อง {selectedRoom.RoomNumber}";
            _resultInvoiceNumberLabel.Text = $"ใบแจ้งหนี้ เลขที่ : {bill.InvoiceNumber}";
            _resultPeriodLabel.Text = $"(รอบเดือนที่ {bill.BillingPeriod.Month}/{bill.BillingPeriod.Year + 543})";
            _resultCreatedAtLabel.Text = $"วันที่ออกบิล: {FormatDateThai(bill.CreatedAt)}";
            _resultDueDateLabel.Text = $"ครบกำหนด: {FormatDateThai(bill.DueDate)}";
            _resultTenantLabel.Text = $"ผู้เช่า: {(string.IsNullOrWhiteSpace(tenantName) || tenantName == "-" ? "ไม่มีข้อมูลผู้เช่า" : tenantName)}";
            _resultRentLabel.Text = $"{bill.Rent.Amount:N2} บาท";
            _resultWaterDetailsLabel.Text = prevWater != null && currWater != null ? $"└ มิเตอร์ {prevWater.Value:0.##} → {currWater.Value:0.##} ({waterUsed:0.##} หน่วย)" : "-";
            _resultWaterAmountLabel.Text = $"{bill.Water.Amount:N2} บาท";
            _resultElecDetailsLabel.Text = prevElec != null && currElec != null ? $"└ มิเตอร์ {prevElec.Value:0.##} → {currElec.Value:0.##} ({elecUsed:0.##} หน่วย)" : "-";
            _resultElecAmountLabel.Text = $"{bill.Electricity.Amount:N2} บาท";
            _resultMotorcycleParkingLabel.Text = bill.MotorcycleParking.Amount > 0 ? $"{bill.MotorcycleParking.Amount:N2} บาท" : "-";
            _resultCarParkingLabel.Text = bill.CarParking.Amount > 0 ? $"{bill.CarParking.Amount:N2} บาท" : "-";
            _resultCableTvLabel.Text = bill.CableTv.Amount > 0 ? $"{bill.CableTv.Amount:N2} บาท" : (isCableTvExempt ? "ยกเว้น (0.00 บาท)" : "0.00 บาท");
            _resultTotalLabel.Text = $"{bill.Total.Amount:N2} บาท";

            // รีเฟรชตารางและเลื่อนไปยังห้องถัดไป (Auto-advance)
            RefreshBillGrid();
            RefreshRoomList(forceIncludeRoomId: null);

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

    private void SaveBillEdit(
        Bill editingBill,
        RoomItem selectedRoom,
        decimal motorcycleRate,
        decimal carRate,
        decimal cableTvRate)
    {
        bool isCableTvExempt = _isCableTvExemptCheckBox.Checked;

        try
        {
            var bill = _updateBillChargesService.Update(new UpdateBillChargesRequest(
                editingBill.Id,
                _chargeMotorcycleCheckBox.Checked,
                motorcycleRate,
                _chargeCarCheckBox.Checked,
                carRate,
                isCableTvExempt,
                cableTvRate));

            _resultMotorcycleParkingLabel.Text = bill.MotorcycleParking.Amount > 0 ? $"{bill.MotorcycleParking.Amount:N2} บาท" : "-";
            _resultCarParkingLabel.Text = bill.CarParking.Amount > 0 ? $"{bill.CarParking.Amount:N2} บาท" : "-";
            _resultCableTvLabel.Text = bill.CableTv.Amount > 0 ? $"{bill.CableTv.Amount:N2} บาท" : (isCableTvExempt ? "ยกเว้น (0.00 บาท)" : "0.00 บาท");
            _resultTotalLabel.Text = $"{bill.Total.Amount:N2} บาท";

            SetEditingBill(null);
            RefreshBillGrid();
            RefreshRoomList(forceIncludeRoomId: null);

            _billMessageLabel.ForeColor = Color.DarkGreen;
            _billMessageLabel.Text =
                $"บันทึกการแก้ไขบิลห้อง {selectedRoom.RoomNumber} รอบ {bill.BillingPeriod.Value} สำเร็จเรียบร้อย";
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

    private void BillGrid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || _billGrid.Rows[e.RowIndex].DataBoundItem is not BillGridItem gridItem)
        {
            return;
        }

        var bill = _billRepository.Get(gridItem.BillId);
        if (bill == null)
        {
            return;
        }

        // เซ็ตค่ารอบบิล
        _periodSelector.SetBillingPeriod(bill.BillingPeriod);

        // รีเฟรชรายชื่อห้องโดยบังคับรวมห้องนี้เข้ามาใน ComboBox แม้ว่าจะสร้างบิลไปแล้ว เพื่อให้สามารถเลือกดูได้
        RefreshRoomList(forceIncludeRoomId: bill.RoomId);

        // เลือกห้องใน ComboBox
        foreach (RoomItem roomItem in _billRoomComboBox.Items)
        {
            if (roomItem.Id == bill.RoomId)
            {
                _billRoomComboBox.SelectedItem = roomItem;
                _monthlyRentTextBox.Text = bill.Rent.Amount.ToString("0.##");
                _chargeMotorcycleCheckBox.Checked = bill.MotorcycleParking.Amount > 0;
                _chargeCarCheckBox.Checked = bill.CarParking.Amount > 0;
                _isCableTvExemptCheckBox.Checked = bill.CableTv.Amount == 0;

                if (bill.Meters is { } billMeters)
                {
                    _prevWaterTextBox.Text = billMeters.PreviousWater.Value.ToString("0.##");
                    _currWaterTextBox.Text = billMeters.CurrentWater.Value.ToString("0.##");
                    _prevElecTextBox.Text = billMeters.PreviousElectricity.Value.ToString("0.##");
                    _currElecTextBox.Text = billMeters.CurrentElectricity.Value.ToString("0.##");
                }

                break;
            }
        }

        // ดึงข้อมูลผู้เช่าและมิเตอร์มาแสดงในกล่องสรุปด้านขวา
        var tenants = _tenantRepository.GetAll().Where(t => t.RoomId != null).GroupBy(t => t.RoomId!).ToDictionary(g => g.Key, g => g.First());
        string tenantName = tenants.TryGetValue(bill.RoomId, out var tenant) ? $"{tenant.FirstName} {tenant.LastName}" : "-";
        
        string waterDetails = "-";
        string elecDetails = "-";
        if (bill.Meters is { } meters)
        {
            waterDetails = $"└ มิเตอร์ {meters.PreviousWater.Value:0.##} → {meters.CurrentWater.Value:0.##} ({meters.WaterUsage:0.##} หน่วย)";
            elecDetails = $"└ มิเตอร์ {meters.PreviousElectricity.Value:0.##} → {meters.CurrentElectricity.Value:0.##} ({meters.ElectricityUsage:0.##} หน่วย)";
        }

        var roomObj = _roomRepository.GetAll().FirstOrDefault(r => r.Id == bill.RoomId);
        string roomNum = roomObj != null ? roomObj.RoomNumber.Value : "-";

        _resultRoomLabel.Text = $"ห้อง {roomNum}";
        _resultInvoiceNumberLabel.Text = $"ใบแจ้งหนี้ เลขที่ : {bill.InvoiceNumber}";
        _resultPeriodLabel.Text = $"(รอบเดือนที่ {bill.BillingPeriod.Month}/{bill.BillingPeriod.Year + 543})";
        _resultCreatedAtLabel.Text = $"วันที่ออกบิล: {FormatDateThai(bill.CreatedAt)}";
        _resultDueDateLabel.Text = $"ครบกำหนด: {FormatDateThai(bill.DueDate)}";
        _resultTenantLabel.Text = $"ผู้เช่า: {(string.IsNullOrWhiteSpace(tenantName) || tenantName == "-" ? "ไม่มีข้อมูลผู้เช่า" : tenantName)}";
        _resultRentLabel.Text = $"{bill.Rent.Amount:N2} บาท";
        _resultWaterDetailsLabel.Text = waterDetails;
        _resultWaterAmountLabel.Text = $"{bill.Water.Amount:N2} บาท";
        _resultElecDetailsLabel.Text = elecDetails;
        _resultElecAmountLabel.Text = $"{bill.Electricity.Amount:N2} บาท";
        _resultMotorcycleParkingLabel.Text = bill.MotorcycleParking.Amount > 0 ? $"{bill.MotorcycleParking.Amount:N2} บาท" : "-";
        _resultCarParkingLabel.Text = bill.CarParking.Amount > 0 ? $"{bill.CarParking.Amount:N2} บาท" : "-";
        _resultCableTvLabel.Text = bill.CableTv.Amount > 0 ? $"{bill.CableTv.Amount:N2} บาท" : (bill.CableTv.Amount == 0 ? "ยกเว้น (0.00 บาท)" : "0.00 บาท");
        _resultTotalLabel.Text = $"{bill.Total.Amount:N2} บาท";

        SetEditingBill(bill);

        _billMessageLabel.ForeColor = Color.DarkBlue;
        _billMessageLabel.Text = $"แก้ไขบิลห้อง {roomNum} รอบ {bill.BillingPeriod.Value}: แก้ได้เฉพาะค่าจอดรถและ Cable TV";
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
