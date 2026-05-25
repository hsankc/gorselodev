using System.Drawing.Drawing2D;

namespace DisKlinigiYonetimSistemi.Controls;

public static class ModernUi
{
    public static readonly Color Background = Color.FromArgb(244, 247, 251);
    public static readonly Color Surface = Color.White;
    public static readonly Color Text = Color.FromArgb(28, 37, 54);
    public static readonly Color Muted = Color.FromArgb(106, 116, 133);
    public static readonly Color Primary = Color.FromArgb(50, 132, 255);
    public static readonly Color Accent = Color.FromArgb(26, 188, 156);
    public static readonly Color Danger = Color.FromArgb(229, 80, 80);
    public static readonly Color Warning = Color.FromArgb(247, 183, 49);
    public static readonly Font TitleFont = new("Segoe UI Semibold", 22F);
    public static readonly Font HeaderFont = new("Segoe UI Semibold", 14F);
    public static readonly Font BodyFont = new("Segoe UI", 10F);
    public static readonly Font SmallFont = new("Segoe UI", 9F);

    public static Button PrimaryButton(string text)
    {
        var button = FlatButton(text, Primary, Color.White);
        button.Font = new Font("Segoe UI Semibold", 10F);
        return button;
    }

    public static Button FlatButton(string text, Color backColor, Color foreColor)
    {
        return new Button
        {
            Text = text,
            BackColor = backColor,
            ForeColor = foreColor,
            FlatStyle = FlatStyle.Flat,
            Height = 42,
            Cursor = Cursors.Hand,
            Font = BodyFont,
            Margin = new Padding(0, 4, 0, 4)
        }.With(button =>
        {
            button.FlatAppearance.BorderSize = 0;
        });
    }

    public static Label Label(string text, Font? font = null, Color? color = null)
    {
        return new Label
        {
            Text = text,
            Font = font ?? BodyFont,
            ForeColor = color ?? Text,
            AutoSize = true,
            Margin = new Padding(0, 4, 0, 4),
            UseCompatibleTextRendering = true
        };
    }

    public static TextBox TextBox(string placeholder = "", bool password = false)
    {
        return new TextBox
        {
            PlaceholderText = placeholder,
            UseSystemPasswordChar = password,
            Font = BodyFont,
            Height = 32,
            Margin = new Padding(0, 3, 0, 10)
        };
    }

    public static DataGridView Grid()
    {
        var grid = new DataGridView
        {
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Surface,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
            Dock = DockStyle.Fill,
            EnableHeadersVisualStyles = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            Font = BodyFont
        };

        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(232, 239, 249);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Text;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.5F);
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(218, 234, 255);
        grid.DefaultCellStyle.SelectionForeColor = Text;
        grid.RowTemplate.Height = 36;
        return grid;
    }

    public static Panel Card()
    {
        return new RoundedPanel
        {
            BackColor = Surface,
            Padding = new Padding(18),
            Margin = new Padding(8)
        };
    }

    public static T With<T>(this T control, Action<T> configure)
    {
        configure(control);
        return control;
    }
}

public sealed class RoundedPanel : Panel
{
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var path = RoundedRect(ClientRectangle, 8);
        Region = new Region(path);
    }

    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

public sealed class ToothLogo : Control
{
    public ToothLogo()
    {
        DoubleBuffered = true;
        Size = new Size(82, 64);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(ModernUi.Primary, 4F);
        using var path = new GraphicsPath();
        path.AddBezier(41, 12, 19, 0, 11, 18, 22, 31);
        path.AddBezier(22, 31, 21, 45, 29, 56, 35, 43);
        path.AddBezier(35, 43, 38, 36, 44, 36, 47, 43);
        path.AddBezier(47, 43, 53, 56, 61, 45, 60, 31);
        path.AddBezier(60, 31, 71, 18, 63, 0, 41, 12);
        e.Graphics.DrawPath(pen, path);
    }
}
