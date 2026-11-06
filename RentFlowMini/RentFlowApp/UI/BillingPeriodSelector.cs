using RentFlowApp.Application.Reports;
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
        for (int i = 0; i < ThaiCalendar.MonthNames.Count; i++)
        {
            _monthComboBox.Items.Add(new MonthItem(i + 1, ThaiCalendar.MonthNames[i]));
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

        int thaiYear = period.Year + 543;
        AddYearIfMissing(thaiYear);
        _yearComboBox.SelectedItem = thaiYear;

        foreach (MonthItem m in _monthComboBox.Items)
        {
            if (m.MonthNumber == period.Month)
            {
                _monthComboBox.SelectedItem = m;
                break;
            }
        }
    }

    // บิลค้างเก่าอาจอยู่นอกช่วงปีที่แสดง ถ้าไม่เพิ่มปีนั้น เดือนจะเปลี่ยนแต่ปีไม่เปลี่ยน รอบบิลจึงผิดโดยไม่มีการเตือน
    private void AddYearIfMissing(int thaiYear)
    {
        var years = _yearComboBox.Items.Cast<int>().ToList();
        if (years.Contains(thaiYear))
        {
            return;
        }

        int position = years.Count(year => year < thaiYear);
        _yearComboBox.Items.Insert(position, thaiYear);
    }
}
