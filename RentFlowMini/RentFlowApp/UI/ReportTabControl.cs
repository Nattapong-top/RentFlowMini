using System.ComponentModel;
using System.Drawing.Printing;
using RentFlowApp.Application.Billing;
using RentFlowApp.Application.Reports;
using RentFlowApp.Application.Rooms;
using RentFlowApp.Application.Tenants;
using RentFlowApp.Infrastructure.Reports;

namespace RentFlowApp.Forms;

public class ReportTabControl : BaseTabControl
{
    private readonly IRoomRepository _roomRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IBillRepository _billRepository;
    private readonly Func<DateOnly> _today;
    private readonly Func<string, string, string?> _chooseSavePath;
    private readonly Action<YearlyReport, string> _exportPdf;
    private readonly Action<MonthlyReport, string> _exportMonthlyPdf;

    private readonly RadioButton _monthlyRadioButton = new();
    private readonly RadioButton _yearlyRadioButton = new();
    private readonly Label _yearLabel = new();
    private readonly ComboBox _yearComboBox = new();
    private readonly Button _exportExcelButton = new();
    private readonly Button _exportPdfButton = new();
    private readonly TableLayoutPanel _monthlyContent;
    private readonly GroupBox _yearlyContent = new();
    private readonly Label _yearlyMessageLabel = new();
    private readonly DataGridView _yearlyGrid = new();

    private readonly BillingPeriodSelector _periodSelector = new();
    private readonly Label _roomValueLabel = CreateCardValueLabel("ReportRoomValueLabel");
    private readonly Label _roomDetailLabel = CreateCardDetailLabel("ReportRoomDetailLabel");
    private readonly Label _billedValueLabel = CreateCardValueLabel("ReportBilledValueLabel");
    private readonly Label _billedDetailLabel = CreateCardDetailLabel("ReportBilledDetailLabel");
    private readonly Label _receivedValueLabel = CreateCardValueLabel("ReportReceivedValueLabel");
    private readonly Label _receivedDetailLabel = CreateCardDetailLabel("ReportReceivedDetailLabel");
    private readonly Label _unpaidValueLabel = CreateCardValueLabel("ReportUnpaidValueLabel");
    private readonly Label _unpaidDetailLabel = CreateCardDetailLabel("ReportUnpaidDetailLabel");
    private readonly Label _rentLabel = CreateAmountLabel("ReportRentLabel");
    private readonly Label _waterLabel = CreateAmountLabel("ReportWaterLabel");
    private readonly Label _electricityLabel = CreateAmountLabel("ReportElectricityLabel");
    private readonly Label _parkingLabel = CreateAmountLabel("ReportParkingLabel");
    private readonly Label _cableTvLabel = CreateAmountLabel("ReportCableTvLabel");
    private readonly Label _cashLabel = CreateAmountLabel("ReportCashLabel");
    private readonly Label _bankTransferLabel = CreateAmountLabel("ReportBankTransferLabel");
    private readonly Label _monthlyMessageLabel = new();
    private readonly Label _outstandingSummaryLabel = new();
    private readonly DataGridView _outstandingGrid = new();

    private sealed record OutstandingGridItem(
        string RoomNumber,
        string TenantName,
        string BillingPeriod,
        string DueDate,
        string OverdueStatus,
        string Amount,
        bool IsOverdue);

    public ReportTabControl(
        IRoomRepository roomRepository,
        ITenantRepository tenantRepository,
        IBillRepository billRepository,
        Func<DateOnly>? today = null,
        Func<string, string, string?>? chooseSavePath = null,
        Action<YearlyReport, string>? exportPdf = null,
        Action<MonthlyReport, string>? exportMonthlyPdf = null)
    {
        ArgumentNullException.ThrowIfNull(roomRepository);
        ArgumentNullException.ThrowIfNull(tenantRepository);
        ArgumentNullException.ThrowIfNull(billRepository);

        _roomRepository = roomRepository;
        _tenantRepository = tenantRepository;
        _billRepository = billRepository;
        _today = today ?? (() => DateOnly.FromDateTime(DateTime.Now));
        _chooseSavePath = chooseSavePath ?? ShowSaveDialog;
        _exportPdf = exportPdf ?? YearlyReportPrintDocument.ExportPdf;
        _exportMonthlyPdf = exportMonthlyPdf ?? MonthlyReportPrintDocument.ExportPdf;
        _monthlyContent = CreateStandardLayout(3, [115f, 195f, -1f]);

        Name = "ReportTabControl";

        InitializeComponents();
    }

    private void InitializeComponents()
    {
        var mainLayout = CreateStandardLayout(2, [50f, -1f]);

        _monthlyContent.Padding = Padding.Empty;
        _monthlyContent.Controls.Add(BuildSummaryCards(), 0, 0);
        _monthlyContent.Controls.Add(BuildBreakdowns(), 0, 1);
        _monthlyContent.Controls.Add(BuildOutstandingGroup(), 0, 2);
        BuildYearlyContent();

        var content = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
        content.Controls.Add(_monthlyContent);
        content.Controls.Add(_yearlyContent);

        mainLayout.Controls.Add(BuildHeader(), 0, 0);
        mainLayout.Controls.Add(content, 0, 1);
        Controls.Add(mainLayout);

        ShowSelectedView();
    }

    private FlowLayoutPanel BuildHeader()
    {
        // เรียงตามความกว้างจริงของตัวอักษร เพราะแต่ละเครื่องวาดตัวอักษรไทยกว้างไม่เท่ากัน
        var header = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty
        };

        var titleLabel = CreateTitleLabel("รายงาน");
        titleLabel.Dock = DockStyle.None;
        titleLabel.AutoSize = true;
        titleLabel.UseCompatibleTextRendering = false;
        titleLabel.Margin = new Padding(0, 5, 6, 0);

        _monthlyRadioButton.Name = "ReportMonthlyRadioButton";
        _monthlyRadioButton.Text = "รายเดือน";
        _monthlyRadioButton.Checked = true;
        _monthlyRadioButton.AutoSize = true;
        _monthlyRadioButton.Margin = new Padding(0, 11, 4, 0);
        _monthlyRadioButton.CheckedChanged += (s, e) => ShowSelectedView();

        _yearlyRadioButton.Name = "ReportYearlyRadioButton";
        _yearlyRadioButton.Text = "รายปี";
        _yearlyRadioButton.AutoSize = true;
        _yearlyRadioButton.Margin = new Padding(0, 11, 4, 0);

        _periodSelector.Name = "ReportPeriodSelector";
        _periodSelector.Size = new Size(350, 32);
        _periodSelector.Margin = new Padding(0, 8, 2, 0);
        _periodSelector.PeriodChanged += (s, e) =>
        {
            ShowMonthlyMessage(string.Empty, isError: false);
            RefreshMonthlyReport();
        };

        _yearLabel.Text = "ปี (พ.ศ.):";
        _yearLabel.AutoSize = true;
        _yearLabel.UseCompatibleTextRendering = false;
        _yearLabel.Margin = new Padding(6, 13, 2, 0);

        _yearComboBox.Name = "ReportYearComboBox";
        _yearComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _yearComboBox.Width = 85;
        _yearComboBox.Margin = new Padding(0, 10, 8, 0);
        int currentThaiYear = _today().Year + 543;
        for (int year = currentThaiYear - 2; year <= currentThaiYear + 2; year++)
        {
            _yearComboBox.Items.Add(year);
        }

        _yearComboBox.SelectedItem = currentThaiYear;
        _yearComboBox.SelectedIndexChanged += (s, e) =>
        {
            _yearlyMessageLabel.Text = string.Empty;
            RefreshYearlyReport();
        };

        _exportExcelButton.Name = "ExportExcelButton";
        _exportExcelButton.Text = "Excel";
        _exportExcelButton.Size = new Size(58, 34);
        _exportExcelButton.Margin = new Padding(0, 6, 3, 0);
        _exportExcelButton.Click += (s, e) => ExportSelectedView(
            "xlsx",
            "ไฟล์ Excel (*.xlsx)|*.xlsx",
            YearlyReportExcelExporter.Export,
            MonthlyReportExcelExporter.Export);

        _exportPdfButton.Name = "ExportPdfButton";
        _exportPdfButton.Text = "PDF";
        _exportPdfButton.Size = new Size(58, 34);
        _exportPdfButton.Margin = new Padding(0, 6, 0, 0);
        _exportPdfButton.Click += (s, e) => ExportSelectedView(
            "pdf",
            "ไฟล์ PDF (*.pdf)|*.pdf",
            _exportPdf,
            _exportMonthlyPdf);

        header.Controls.AddRange(
        [
            titleLabel,
            _monthlyRadioButton,
            _yearlyRadioButton,
            _periodSelector,
            _yearLabel,
            _yearComboBox,
            _exportExcelButton,
            _exportPdfButton
        ]);
        return header;
    }

    private void ShowSelectedView()
    {
        bool isYearly = _yearlyRadioButton.Checked;

        _monthlyContent.Visible = !isYearly;
        _periodSelector.Visible = !isYearly;
        _yearlyContent.Visible = isYearly;
        _yearLabel.Visible = isYearly;
        _yearComboBox.Visible = isYearly;
    }

    private void BuildYearlyContent()
    {
        _yearlyContent.Name = "ReportYearlyGroup";
        _yearlyContent.Text = "สรุปรายปี (นับตามรอบบิล)";
        _yearlyContent.Dock = DockStyle.Fill;
        _yearlyContent.Padding = new Padding(10, 4, 10, 8);

        _yearlyMessageLabel.Name = "ReportYearlyMessageLabel";
        _yearlyMessageLabel.Dock = DockStyle.Top;
        _yearlyMessageLabel.Height = 28;
        _yearlyMessageLabel.TextAlign = ContentAlignment.MiddleLeft;
        _yearlyMessageLabel.AutoEllipsis = true;

        _yearlyGrid.Name = "ReportYearlyGrid";
        _yearlyGrid.Dock = DockStyle.Fill;
        _yearlyGrid.ReadOnly = true;
        _yearlyGrid.AllowUserToAddRows = false;
        _yearlyGrid.AllowUserToDeleteRows = false;
        _yearlyGrid.AllowUserToResizeRows = false;
        _yearlyGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _yearlyGrid.MultiSelect = false;
        _yearlyGrid.RowHeadersVisible = false;
        _yearlyGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _yearlyGrid.RowTemplate.Height = 30;
        _yearlyGrid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _yearlyGrid.ColumnHeadersDefaultCellStyle.Font = new Font(Font, FontStyle.Bold);
        _yearlyGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;

        AddYearlyColumn(YearlyReport.MonthHeader, 100, 90, DataGridViewContentAlignment.MiddleLeft);
        AddYearlyColumn(YearlyReport.BillCountHeader, 50, 45, DataGridViewContentAlignment.MiddleCenter);
        foreach (var header in YearlyReport.AmountHeaders)
        {
            AddYearlyColumn(header, 100, 90, DataGridViewContentAlignment.MiddleRight);
        }

        // Dock.Top ต้องเพิ่มหลัง Dock.Fill เพื่อให้ WinForms จัดแถวบนก่อน
        _yearlyContent.Controls.Add(_yearlyGrid);
        _yearlyContent.Controls.Add(_yearlyMessageLabel);
    }

    private void AddYearlyColumn(string header, int fillWeight, int minimumWidth, DataGridViewContentAlignment alignment)
    {
        _yearlyGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = header,
            FillWeight = fillWeight,
            // ตัวเลขทั้งปีหลักเยอะ จอเล็กจึงให้เลื่อนแนวนอนแทนการตัดตัวเลข
            MinimumWidth = minimumWidth,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = alignment }
        });
    }

    private YearlyReport? BuildSelectedYearlyReport() =>
        _yearComboBox.SelectedItem is int thaiYear
            ? YearlyReportBuilder.Build(_billRepository.GetAll(), thaiYear - 543)
            : null;

    private void RefreshYearlyReport()
    {
        var report = BuildSelectedYearlyReport();
        if (report is null)
        {
            return;
        }

        _yearlyGrid.Rows.Clear();
        foreach (var row in report.AllRows)
        {
            object[] cells =
            [
                row.Label,
                row.BillCount.ToString("N0"),
                .. row.Amounts.Select(amount => (object)amount.ToString("N2"))
            ];
            _yearlyGrid.Rows.Add(cells);
        }

        var totalRow = _yearlyGrid.Rows[_yearlyGrid.Rows.Count - 1];
        // ใช้ Font เดียวกับหัวตาราง เพราะเมธอดนี้ถูกเรียกซ้ำ ถ้าสร้างใหม่ทุกครั้งจะไม่ได้คืนทรัพยากร
        totalRow.DefaultCellStyle.Font = _yearlyGrid.ColumnHeadersDefaultCellStyle.Font;
        totalRow.DefaultCellStyle.BackColor = ThemePalette.Current.TotalRowBack;
        _yearlyGrid.ClearSelection();
    }

    private void ExportSelectedView(
        string extension,
        string filter,
        Action<YearlyReport, string> exportYearly,
        Action<MonthlyReport, string> exportMonthly)
    {
        if (_yearlyRadioButton.Checked)
        {
            var report = BuildSelectedYearlyReport();
            if (report is null)
            {
                return;
            }

            if (!report.HasBills)
            {
                ShowYearlyMessage($"ไม่มีบิลในปี {report.BuddhistYear} จึงไม่มีข้อมูลให้ส่งออก", isError: true);
                return;
            }

            SaveToChosenFile(
                $"รายงานประจำปี-{report.BuddhistYear}.{extension}",
                filter,
                filePath => exportYearly(report, filePath),
                ShowYearlyMessage);
        }
        else
        {
            var report = BuildSelectedMonthlyReport();
            if (report is null)
            {
                return;
            }

            if (!report.HasBills)
            {
                ShowMonthlyMessage(
                    $"ไม่มีบิลรอบ {ThaiCalendar.FormatPeriod(report.Period)} และไม่มีบิลค้างชำระ จึงไม่มีข้อมูลให้ส่งออก",
                    isError: true);
                return;
            }

            SaveToChosenFile(
                $"รายงานประจำเดือน-{report.Period.ToBuddhistValue()}.{extension}",
                filter,
                filePath => exportMonthly(report, filePath),
                ShowMonthlyMessage);
        }
    }

    private void SaveToChosenFile(
        string suggestedFileName,
        string filter,
        Action<string> export,
        Action<string, bool> showMessage)
    {
        string? filePath = _chooseSavePath(suggestedFileName, filter);
        if (filePath is null)
        {
            return;
        }

        try
        {
            export(filePath);
            showMessage($"ส่งออกรายงานแล้ว: {filePath}", false);
        }
        catch (InvalidPrinterException)
        {
            showMessage(
                "ไม่พบเครื่องพิมพ์ Microsoft Print to PDF กรุณาเปิดใช้งานใน Windows แล้วลองใหม่อีกครั้ง",
                true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or Win32Exception)
        {
            showMessage(
                "บันทึกไฟล์ไม่สำเร็จ กรุณาปิดไฟล์เดิมหากเปิดอยู่ หรือเลือกที่บันทึกอื่น แล้วลองใหม่อีกครั้ง",
                true);
        }
    }

    private void ShowYearlyMessage(string message, bool isError)
    {
        _yearlyMessageLabel.ForeColor = isError ? ThemePalette.Current.ErrorText : ThemePalette.Current.SuccessText;
        _yearlyMessageLabel.Text = message;
    }

    private void ShowMonthlyMessage(string message, bool isError)
    {
        _monthlyMessageLabel.ForeColor = isError ? ThemePalette.Current.ErrorText : ThemePalette.Current.SuccessText;
        _monthlyMessageLabel.Text = message;
        // ซ่อนตอนไม่มีข้อความ เพื่อคืนพื้นที่ให้ตารางค้างชำระ
        _monthlyMessageLabel.Visible = message.Length > 0;
    }

    private string? ShowSaveDialog(string suggestedFileName, string filter)
    {
        using var dialog = new SaveFileDialog
        {
            FileName = suggestedFileName,
            Filter = filter,
            OverwritePrompt = true
        };

        return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileName : null;
    }

    private TableLayoutPanel BuildSummaryCards()
    {
        var cards = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            Margin = Padding.Empty
        };
        for (int i = 0; i < cards.ColumnCount; i++)
        {
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        }

        // จำนวนห้องเป็นสถานะตอนนี้ ไม่ได้ย้อนตามรอบบิลที่เลือก จึงต้องบอกไว้ที่หัวการ์ด
        cards.Controls.Add(BuildCard("ห้องพัก (ปัจจุบัน)", _roomValueLabel, _roomDetailLabel), 0, 0);
        cards.Controls.Add(BuildCard("ยอดเรียกเก็บ", _billedValueLabel, _billedDetailLabel), 1, 0);
        cards.Controls.Add(BuildCard("รับแล้ว", _receivedValueLabel, _receivedDetailLabel), 2, 0);
        cards.Controls.Add(BuildCard("ค้างชำระ", _unpaidValueLabel, _unpaidDetailLabel), 3, 0);
        return cards;
    }

    private static GroupBox BuildCard(string title, Label valueLabel, Label detailLabel)
    {
        var card = new GroupBox
        {
            Text = title,
            Dock = DockStyle.Fill,
            Padding = new Padding(10, 4, 10, 4)
        };

        // Dock.Top ต้องเพิ่มหลัง Dock.Fill เพื่อให้ WinForms จัดแถวบนก่อน
        card.Controls.Add(detailLabel);
        card.Controls.Add(valueLabel);
        return card;
    }

    private static Label CreateCardValueLabel(string name) => new()
    {
        Name = name,
        Dock = DockStyle.Top,
        Height = 40,
        Font = new Font("Segoe UI", 16, FontStyle.Bold),
        TextAlign = ContentAlignment.MiddleLeft
    };

    private static Label CreateCardDetailLabel(string name) => new()
    {
        Name = name,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft
    };

    private static Label CreateAmountLabel(string name) => new()
    {
        Name = name,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleRight
    };

    private TableLayoutPanel BuildBreakdowns()
    {
        var breakdowns = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty
        };
        breakdowns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        breakdowns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        breakdowns.Controls.Add(
            BuildBreakdownGroup(
                "ยอดเรียกเก็บแยกประเภท",
                ("ค่าเช่า", _rentLabel),
                ("ค่าน้ำ", _waterLabel),
                ("ค่าไฟ", _electricityLabel),
                ("ค่าจอดรถ", _parkingLabel),
                ("Cable TV", _cableTvLabel)),
            0,
            0);
        breakdowns.Controls.Add(
            BuildBreakdownGroup(
                "รับชำระแยกช่องทาง",
                ("เงินสด", _cashLabel),
                ("โอนเข้าบัญชี", _bankTransferLabel)),
            1,
            0);
        return breakdowns;
    }

    private static GroupBox BuildBreakdownGroup(string title, params (string Name, Label Amount)[] rows)
    {
        var group = new GroupBox
        {
            Text = title,
            Dock = DockStyle.Fill,
            Padding = new Padding(10, 4, 10, 4)
        };

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = rows.Length + 1
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        // คอลัมน์ว่างรับพื้นที่เหลือ ไม่ให้ยอดเงินถูกยืดไปไกลจากชื่อรายการบนจอกว้าง
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        for (int i = 0; i < rows.Length; i++)
        {
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            table.Controls.Add(
                new Label { Text = rows[i].Name, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft },
                0,
                i);
            table.Controls.Add(rows[i].Amount, 1, i);
        }

        group.Controls.Add(table);
        return group;
    }

    private GroupBox BuildOutstandingGroup()
    {
        var group = new GroupBox
        {
            Text = "รายการค้างชำระทุกเดือน",
            Dock = DockStyle.Fill,
            Padding = new Padding(10, 4, 10, 8)
        };

        _outstandingSummaryLabel.Name = "ReportOutstandingSummaryLabel";
        _outstandingSummaryLabel.Dock = DockStyle.Top;
        _outstandingSummaryLabel.Height = 28;
        _outstandingSummaryLabel.TextAlign = ContentAlignment.MiddleLeft;

        _monthlyMessageLabel.Name = "ReportMonthlyMessageLabel";
        _monthlyMessageLabel.Dock = DockStyle.Bottom;
        _monthlyMessageLabel.Height = 26;
        _monthlyMessageLabel.TextAlign = ContentAlignment.MiddleLeft;
        _monthlyMessageLabel.AutoEllipsis = true;
        _monthlyMessageLabel.Visible = false;

        _outstandingGrid.Name = "ReportOutstandingGrid";
        _outstandingGrid.Dock = DockStyle.Fill;
        _outstandingGrid.ReadOnly = true;
        _outstandingGrid.AllowUserToAddRows = false;
        _outstandingGrid.AllowUserToDeleteRows = false;
        _outstandingGrid.AutoGenerateColumns = false;
        _outstandingGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _outstandingGrid.MultiSelect = false;
        _outstandingGrid.RowHeadersVisible = false;
        _outstandingGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _outstandingGrid.AlternatingRowsDefaultCellStyle.BackColor = ThemePalette.Current.AlternatingRowBack;
        _outstandingGrid.RowTemplate.Height = 30;
        _outstandingGrid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _outstandingGrid.ColumnHeadersDefaultCellStyle.Font = new Font(Font, FontStyle.Bold);
        _outstandingGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        _outstandingGrid.CellFormatting += OutstandingGrid_CellFormatting;
        _outstandingGrid.DataBindingComplete += (s, e) => _outstandingGrid.ClearSelection();

        AddColumn("RoomNumber", "ห้อง", 60, DataGridViewContentAlignment.MiddleCenter);
        AddColumn("TenantName", "ผู้เช่า", 200, DataGridViewContentAlignment.MiddleLeft);
        AddColumn("BillingPeriod", "รอบบิล", 80, DataGridViewContentAlignment.MiddleCenter);
        AddColumn("DueDate", "ครบกำหนด", 100, DataGridViewContentAlignment.MiddleCenter);
        AddColumn("OverdueStatus", "เกินกำหนด", 110, DataGridViewContentAlignment.MiddleCenter);
        AddColumn("Amount", "ยอดค้าง", 100, DataGridViewContentAlignment.MiddleRight);

        group.Controls.Add(_outstandingGrid);
        group.Controls.Add(_outstandingSummaryLabel);
        group.Controls.Add(_monthlyMessageLabel);
        return group;
    }

    private void AddColumn(string name, string header, int fillWeight, DataGridViewContentAlignment alignment)
    {
        _outstandingGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = name,
            HeaderText = header,
            DataPropertyName = name,
            FillWeight = fillWeight,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = alignment }
        });
    }

    private void OutstandingGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex >= 0 &&
            _outstandingGrid.Rows[e.RowIndex].DataBoundItem is OutstandingGridItem { IsOverdue: true } &&
            _outstandingGrid.Columns[e.ColumnIndex].Name == "OverdueStatus")
        {
            e.CellStyle.ForeColor = ThemePalette.Current.OverdueText;
            e.CellStyle.SelectionForeColor = Color.MistyRose;
        }
    }

    public void RefreshReport()
    {
        RefreshMonthlyReport();
        RefreshYearlyReport();
    }

    private MonthlyReport? BuildSelectedMonthlyReport()
    {
        var period = _periodSelector.GetBillingPeriod();
        if (period is null)
        {
            return null;
        }

        return MonthlyReportBuilder.Build(
            _roomRepository.GetAll(),
            _tenantRepository.GetAll(),
            _billRepository.GetAll(),
            period,
            _today());
    }

    private void RefreshMonthlyReport()
    {
        var report = BuildSelectedMonthlyReport();
        if (report is null)
        {
            return;
        }

        _roomValueLabel.Text = $"มีผู้เช่า {report.OccupiedRooms}/{report.TotalRooms}";
        _roomDetailLabel.Text = $"ว่าง {report.VacantRooms} ห้อง";

        _billedValueLabel.Text = report.BilledTotal.ToString("N2");
        _billedDetailLabel.Text = report.BilledCount == 0 ? "ยังไม่มีบิลในรอบนี้" : $"{report.BilledCount} บิล";

        _receivedValueLabel.Text = report.ReceivedTotal.ToString("N2");
        _receivedDetailLabel.Text = $"{report.ReceivedCount} บิล";

        _unpaidValueLabel.Text = report.UnpaidTotal.ToString("N2");
        _unpaidValueLabel.ForeColor = report.UnpaidCount > 0 ? ThemePalette.Current.OverdueText : SystemColors.ControlText;
        _unpaidDetailLabel.Text = report.OverdueCount > 0
            ? $"{report.UnpaidCount} บิล (เกินกำหนด {report.OverdueCount})"
            : $"{report.UnpaidCount} บิล";

        _rentLabel.Text = report.Rent.ToString("N2");
        _waterLabel.Text = report.Water.ToString("N2");
        _electricityLabel.Text = report.Electricity.ToString("N2");
        _parkingLabel.Text = report.Parking.ToString("N2");
        _cableTvLabel.Text = report.CableTv.ToString("N2");
        _cashLabel.Text = report.ReceivedByCash.ToString("N2");
        _bankTransferLabel.Text = report.ReceivedByBankTransfer.ToString("N2");

        _outstandingSummaryLabel.Text = report.OutstandingBills.Count == 0
            ? "ไม่มีบิลค้างชำระ"
            : $"ค้างชำระทั้งหมด {report.OutstandingBills.Count} บิล รวม {report.OutstandingBillsTotal:N2} บาท";

        _outstandingGrid.DataSource = report.OutstandingBills
            .Select(row => new OutstandingGridItem(
                row.RoomNumber,
                row.TenantName,
                row.BillingPeriod.ToBuddhistValue(),
                ThaiCalendar.FormatDate(row.DueDate),
                row.OverdueStatus,
                row.Amount.ToString("N2"),
                row.OverdueDays > 0))
            .ToList();
    }
}
