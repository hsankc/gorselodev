using System.Drawing.Drawing2D;

namespace DisKlinigiYonetimSistemi.Data;

public static class MockRadiographGenerator
{
    public static string EnsureImage(string fileName, int variant)
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "App_Data", "Radiographs");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, fileName);
        if (File.Exists(path))
        {
            return path;
        }

        using var bitmap = new Bitmap(760, 420);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var background = new LinearGradientBrush(
            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            Color.FromArgb(15, 24, 36),
            Color.FromArgb(42, 58, 82),
            45F);
        graphics.FillRectangle(background, 0, 0, bitmap.Width, bitmap.Height);

        using var jawBrush = new SolidBrush(Color.FromArgb(62, 82, 106));
        graphics.FillEllipse(jawBrush, 54, 70, 650, 280);
        graphics.FillEllipse(new SolidBrush(Color.FromArgb(25, 34, 48)), 110, 120, 540, 180);

        var random = new Random(variant * 97);
        for (var i = 0; i < 14; i++)
        {
            var x = 92 + i * 43;
            var y = 150 + random.Next(-10, 10);
            var h = 105 + random.Next(-8, 18);
            var w = 34 + random.Next(-3, 6);
            using var toothBrush = new SolidBrush(Color.FromArgb(170 + random.Next(0, 28), 190 + random.Next(0, 25), 205 + random.Next(0, 28)));
            using var toothPath = RoundedTooth(x, y, w, h);
            graphics.FillPath(toothBrush, toothPath);
            graphics.DrawPath(new Pen(Color.FromArgb(80, 102, 130), 2F), toothPath);

            using var rootPen = new Pen(Color.FromArgb(120, 145, 165), 3F);
            graphics.DrawLine(rootPen, x + w / 2, y + h - 8, x + w / 2 - 8, y + h + 38);
            graphics.DrawLine(rootPen, x + w / 2, y + h - 8, x + w / 2 + 8, y + h + 38);
        }

        var markerX = 120 + (variant % 5) * 90;
        graphics.DrawEllipse(new Pen(Color.FromArgb(245, 211, 92), 5F), markerX, 120, 82, 132);
        graphics.DrawString($"XR-{variant:000}", new Font("Segoe UI Semibold", 22F), Brushes.WhiteSmoke, 28, 22);
        graphics.DrawString(DateTime.Today.AddDays(-variant).ToString("dd.MM.yyyy"), new Font("Segoe UI", 12F), Brushes.LightGray, 610, 374);
        bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        return path;
    }

    private static GraphicsPath RoundedTooth(int x, int y, int width, int height)
    {
        var path = new GraphicsPath();
        path.AddBezier(x, y + 12, x + 4, y - 8, x + width - 4, y - 8, x + width, y + 12);
        path.AddLine(x + width, y + 12, x + width - 6, y + height - 18);
        path.AddBezier(x + width - 6, y + height - 18, x + width - 10, y + height + 8, x + 10, y + height + 8, x + 6, y + height - 18);
        path.AddLine(x + 6, y + height - 18, x, y + 12);
        path.CloseFigure();
        return path;
    }
}
