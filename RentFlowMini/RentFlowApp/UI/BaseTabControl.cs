namespace RentFlowApp.Forms;

public abstract class BaseTabControl : UserControl
{
    internal const string UnexpectedErrorMessage =
        "เกิดข้อผิดพลาดที่ไม่คาดคิด กรุณาลองใหม่อีกครั้ง หากยังพบปัญหา กรุณาปิดแล้วเปิดโปรแกรมใหม่";

    // ข้อความของ exception ที่ไม่ได้ตั้งใจเขียนให้ผู้ใช้อาจมีรายละเอียดภายในระบบ จึงเก็บไว้เฉพาะใน log
    protected static string LogUnexpectedError(Exception exception, string userMessage = UnexpectedErrorMessage)
    {
        System.Diagnostics.Trace.TraceError(exception.ToString());
        return userMessage;
    }

    protected BaseTabControl()
    {
        Dock = DockStyle.Fill;
        BackColor = SystemColors.Control;
        Font = new Font("Segoe UI", 11F);
    }

    protected static Label CreateTitleLabel(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            Height = 35,
            Font = new Font("Segoe UI", 16, FontStyle.Bold)
        };
    }

    protected static void ConfigureNumericTextBox(
        TextBox textBox,
        bool allowDecimal = true,
        bool allowThousandsSeparator = false)
    {
        textBox.KeyPress += (s, e) =>
        {
            if (char.IsControl(e.KeyChar))
            {
                return;
            }

            string textOutsideSelection = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength);
            bool isAllowedDot = allowDecimal && e.KeyChar == '.' && !textOutsideSelection.Contains('.');
            bool isAllowedSeparator = allowThousandsSeparator && e.KeyChar == ',';
            e.Handled = !char.IsAsciiDigit(e.KeyChar) && !isAllowedDot && !isAllowedSeparator;
        };

        // KeyPress กันได้แค่การพิมพ์ จึงต้องกรองซ้ำเพื่อกันการวางข้อความจาก clipboard
        textBox.TextChanged += (s, e) =>
        {
            string cleaned = KeepNumberCharacters(textBox.Text, allowDecimal, allowThousandsSeparator);
            if (cleaned != textBox.Text)
            {
                int caret = textBox.SelectionStart - (textBox.Text.Length - cleaned.Length);
                textBox.Text = cleaned;
                textBox.SelectionStart = Math.Max(caret, 0);
            }
        };
    }

    private static string KeepNumberCharacters(string text, bool allowDecimal, bool allowThousandsSeparator)
    {
        var cleaned = new System.Text.StringBuilder(text.Length);
        bool hasDot = false;

        foreach (char character in text)
        {
            if (char.IsAsciiDigit(character) || (allowThousandsSeparator && character == ','))
            {
                cleaned.Append(character);
            }
            else if (allowDecimal && character == '.' && !hasDot)
            {
                cleaned.Append(character);
                hasDot = true;
            }
        }

        return cleaned.ToString();
    }

    protected static void HandleEnterKey(Action onEnter, params Control[] controls)
    {
        foreach (var control in controls)
        {
            control.KeyDown += (s, e) =>
            {
                // Enter ตอนเปิดรายการของ ComboBox ค้างไว้คือการเลือกรายการ ไม่ใช่การบันทึก
                if (e.KeyCode == Keys.Enter && control is not ComboBox { DroppedDown: true })
                {
                    e.SuppressKeyPress = true;
                    onEnter();
                }
            };
        }
    }

    protected static TableLayoutPanel CreateStandardLayout(int rowCount, float[] rowHeights)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = rowCount,
            Padding = new Padding(12)
        };

        foreach (var height in rowHeights)
        {
            if (height < 0)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            }
            else
            {
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            }
        }

        return layout;
    }
}
