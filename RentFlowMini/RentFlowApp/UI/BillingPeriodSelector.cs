using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Forms;

public sealed class BillingPeriodSelector : UserControl
{
    private readonly ComboBox _monthComboBox = new();
    private readonly ComboBox _yearComboBox = new();

    public event EventHandler? PeriodChanged;

    private sealed record MonthItem(int MonthNumber, string MonthName)
    {
        public override string ToString() => MonthName;
    }

    public BillingPeriodSelector()
    {
        Height = 32;
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        var monthLabel = new Label
        {
            Text = "เดือน:",
            Left = 0,
            Top = 5,
            Width = 45,
            Height = 25,
            TextAlign = ContentAlignment.MiddleRight
        };

        _monthComboBox.Name = "BillingMonthComboBox";
        _monthComboBox.SetBounds(50, 2, 130, 30);
        _monthComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        PopulateMonths();
        _monthComboBox.SelectedIndexChanged += (s, e) => PeriodChanged?.Invoke(this, EventArgs.Empty);

        var yearLabel = new Label
        {
            Text = "ปี (พ.ศ.):",
            Left = 190,
            Top = 5,
            Width = 60,
            Height = 25,
            TextAlign = ContentAlignment.MiddleRight
        };

        _yearComboBox.Name = "BillingYearComboBox";
        _yearComboBox.SetBounds(255, 2, 90, 30);
        _yearComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        PopulateYears();
        _yearComboBox.SelectedIndexChanged += (s, e) => PeriodChanged?.Invoke(this, EventArgs.Empty);

        Controls.AddRange([monthLabel, _monthComboBox, yearLabel, _yearComboBox]);
    }

    private void PopulateMonths()
    {
        _monthComboBox.Items.Clear();
        string[] months = ["มกราคม", "กุมภาพันธ์", "มีนาคม", "เมษายน", "พฤษภาคม", "มิถุนายน", "กรกฎาคม", "สิงหาคม", "กันยายน", "ตุลาคม", "พฤศจิกายน", "ธันวาคม"];
        for (int i = 0; i < months.Length; i++)
        {
            _monthComboBox.Items.Add(new MonthItem(i + 1, months[i]));
        }

        int currentMonth = DateTime.Now.Month;
        _monthComboBox.SelectedIndex = currentMonth - 1;
    }

    private void PopulateYears()
    {
        _yearComboBox.Items.Clear();
        int currentThaiYear = DateTime.Now.Year + 543;
        for (int y = currentThaiYear - 2; y <= currentThaiYear + 2; y++)
        {
            _yearComboBox.Items.Add(y);
        }
        _yearComboBox.SelectedItem = currentThaiYear;
    }

    public BillingPeriod? GetBillingPeriod()
    {
        if (_monthComboBox.SelectedItem is not MonthItem monthItem ||
            _yearComboBox.SelectedItem is not int thaiYear)
        {
            return null;
        }

        int gregorianYear = thaiYear - 543;
        string periodStr = $"{gregorianYear}-{monthItem.MonthNumber:D2}";
        return BillingPeriod.TryParse(periodStr, out var period) ? period : null;
    }

    public void SetBillingPeriod(BillingPeriod period)
    {
        if (period == null) return;
        var parts = period.Value.Split('-');
        if (parts.Length == 2 && int.TryParse(parts[0], out var gYear) && int.TryParse(parts[1], out var mNum))
        {
            int thaiYear = gYear + 543;
            foreach (var y in _yearComboBox.Items)
            {
                if (y is int ty && ty == thaiYear)
                {
                    _yearComboBox.SelectedItem = ty;
                    break;
                }
            }

            foreach (MonthItem m in _monthComboBox.Items)
            {
                if (m.MonthNumber == mNum)
                {
                    _monthComboBox.SelectedItem = m;
                    break;
                }
            }
        }
    }
}
