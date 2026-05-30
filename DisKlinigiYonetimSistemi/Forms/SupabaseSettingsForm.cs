using DisKlinigiYonetimSistemi.Controls;
using DisKlinigiYonetimSistemi.Data;

namespace DisKlinigiYonetimSistemi.Forms;

public sealed class SupabaseSettingsForm : Form
{
    private readonly ClinicDataStore _store;
    private readonly TextBox _urlBox = ModernUi.TextBox("Supabase URL");
    private readonly TextBox _apiKeyBox = ModernUi.TextBox("Anon key", true);
    private readonly Label _statusLabel = ModernUi.Label("", ModernUi.BodyFont, ModernUi.Muted);

    public SupabaseSettingsForm(ClinicDataStore store)
    {
        _store = store;
        Text = "Supabase Durumu";
        Size = new Size(620, 430);
        MinimumSize = new Size(560, 400);
        StartPosition = FormStartPosition.CenterParent;
        Font = ModernUi.BodyFont;
        BackColor = ModernUi.Background;
        BuildUi();
        Shown += async (_, _) => await LoadSettingsAsync();
    }

    private void BuildUi()
    {
        _urlBox.ReadOnly = true;
        _apiKeyBox.ReadOnly = true;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(26),
            BackColor = ModernUi.Background
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        Controls.Add(root);

        root.Controls.Add(ModernUi.Label("Supabase Durumu", ModernUi.TitleFont), 0, 0);
        root.Controls.Add(Field("Project URL", _urlBox), 0, 1);
        root.Controls.Add(Field("Anon Key", _apiKeyBox), 0, 2);

        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.AutoSize = false;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        root.Controls.Add(_statusLabel, 0, 3);

        var closeButton = ModernUi.FlatButton("Kapat", Color.FromArgb(105, 116, 133), Color.White);
        closeButton.Width = 120;
        closeButton.Height = 42;
        closeButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };

        var closeRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = ModernUi.Background
        };
        closeRow.Controls.Add(closeButton);
        root.Controls.Add(closeRow, 0, 5);
    }

    private async Task LoadSettingsAsync()
    {
        var settings = await _store.LoadSupabaseSettingsAsync();
        _urlBox.Text = settings.Url;
        _apiKeyBox.Text = settings.ApiKey;

        if (!_store.SupabaseEnabled)
        {
            SetStatus("Supabase bağlantısı kapalı.", ModernUi.Warning);
            return;
        }

        SetStatus(_store.LastSyncError is null
            ? "Supabase bağlı. Otomatik senkronizasyon aktif."
            : $"Supabase bağlı, son senkron hatası: {_store.LastSyncError}", _store.LastSyncError is null ? ModernUi.Accent : ModernUi.Warning);
    }

    private Control Field(string labelText, TextBox textBox)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = ModernUi.Background,
            Margin = new Padding(0, 0, 0, 8)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(ModernUi.Label(labelText, new Font("Segoe UI Semibold", 9.5F), ModernUi.Text), 0, 0);

        textBox.Dock = DockStyle.Fill;
        textBox.Height = 38;
        textBox.Margin = new Padding(0);
        panel.Controls.Add(textBox, 0, 1);
        return panel;
    }

    private void SetStatus(string text, Color color)
    {
        _statusLabel.Text = text;
        _statusLabel.ForeColor = color;
    }
}
