namespace RentFlowApp.Forms;

public abstract class BaseTabControl : UserControl
{
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
