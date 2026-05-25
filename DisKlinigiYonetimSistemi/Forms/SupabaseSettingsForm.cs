using DisKlinigiYonetimSistemi.Controls;
using DisKlinigiYonetimSistemi.Data;
using DisKlinigiYonetimSistemi.Models;

namespace DisKlinigiYonetimSistemi.Forms;

public sealed class SupabaseSettingsForm : Form
{
    private readonly ClinicDataStore _store;
    private readonly TextBox _urlBox = ModernUi.TextBox("https://proje-ref.supabase.co");
    private readonly TextBox _apiKeyBox = ModernUi.TextBox("anon veya service role key", true);
    private readonly Label _statusLabel = ModernUi.Label("", ModernUi.SmallFont, ModernUi.Muted);
    private readonly List<Button> _buttons = [];

    public SupabaseSettingsForm(ClinicDataStore store)
    {
        _store = store;
        Text = "Supabase Ayarlari";
        Size = new Size(680, 500);
        MinimumSize = new Size(620, 460);
        StartPosition = FormStartPosition.CenterParent;
        Font = ModernUi.BodyFont;
        BackColor = ModernUi.Background;
        BuildUi();
        Shown += async (_, _) => await LoadSettingsAsync();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 8,
            Padding = new Padding(26),
            BackColor = ModernUi.Background
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        root.Controls.Add(ModernUi.Label("Supabase Baglantisi", ModernUi.TitleFont), 0, 0);
        root.Controls.Add(Field("Project URL", _urlBox), 0, 1);
        root.Controls.Add(Field("API Key", _apiKeyBox), 0, 2);

        var keyHint = ModernUi.Label("Okul/demo icin anon key yeterli olur. Herkese dagitilacak gercek uygulamada service role key kullanma.", ModernUi.SmallFont, ModernUi.Muted);
        keyHint.Dock = DockStyle.Fill;
        keyHint.MaximumSize = new Size(590, 0);
        root.Controls.Add(keyHint, 0, 3);

        root.Controls.Add(ButtonRow(
            ActionButton("Kaydet", ModernUi.Primary, SaveSettingsAsync),
            ActionButton("Baglantiyi Test Et", ModernUi.Accent, TestConnectionAsync)), 0, 4);

        root.Controls.Add(ButtonRow(
            ActionButton("Buluta Gonder", ModernUi.Primary, PushLocalAsync),
            ActionButton("Buluttan Cek", Color.FromArgb(93, 101, 214), PullRemoteAsync)), 0, 5);

        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.AutoSize = false;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        root.Controls.Add(_statusLabel, 0, 6);

        var closeButton = ActionButton("Kapat", Color.FromArgb(105, 116, 133), () =>
        {
            DialogResult = DialogResult.OK;
            Close();
            return Task.CompletedTask;
        });
        closeButton.Width = 120;
        var closeRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = ModernUi.Background
        };
        closeRow.Controls.Add(closeButton);
        root.Controls.Add(closeRow, 0, 7);
    }

    private async Task LoadSettingsAsync()
    {
        var settings = await _store.LoadSupabaseSettingsAsync();
        _urlBox.Text = settings.Url;
        _apiKeyBox.Text = settings.ApiKey;

        if (_store.SupabaseEnabled)
        {
            SetStatus(_store.LastSyncError is null ? "Supabase aktif." : $"Son senkron hatasi: {_store.LastSyncError}", _store.LastSyncError is null ? ModernUi.Accent : ModernUi.Warning);
        }
        else
        {
            SetStatus("Supabase kapali. URL ve API key girip kaydet.", ModernUi.Muted);
        }
    }

    private async Task SaveSettingsAsync()
    {
        await _store.SaveSupabaseSettingsAsync(ReadSettings());
        SetStatus(_store.SupabaseEnabled ? "Ayarlar kaydedildi." : "Ayarlar temizlendi, Supabase kapali.", ModernUi.Accent);
    }

    private async Task TestConnectionAsync()
    {
        var settings = ReadSettings();
        var client = new SupabaseClinicClient(settings);
        var ok = await client.TestConnectionAsync();
        if (ok)
        {
            await _store.SaveSupabaseSettingsAsync(settings);
        }

        SetStatus(ok ? "Baglanti basarili. Ayarlar kaydedildi." : "Baglanti basarisiz. URL/key veya tablo politikasini kontrol et.", ok ? ModernUi.Accent : ModernUi.Danger);
    }

    private async Task PushLocalAsync()
    {
        await _store.SaveSupabaseSettingsAsync(ReadSettings());
        await _store.PushToSupabaseAsync();
        SetStatus("Lokal klinik verisi Supabase'e gonderildi.", ModernUi.Accent);
    }

    private async Task PullRemoteAsync()
    {
        await _store.SaveSupabaseSettingsAsync(ReadSettings());
        var snapshot = await _store.PullFromSupabaseAsync();
        SetStatus(snapshot is null ? _store.LastSyncError ?? "Supabase'den veri cekilemedi." : "Supabase verisi indirildi ve lokal cache guncellendi.", snapshot is null ? ModernUi.Warning : ModernUi.Accent);
    }

    private SupabaseSettings ReadSettings() => new()
    {
        Url = _urlBox.Text,
        ApiKey = _apiKeyBox.Text
    };

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
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(ModernUi.Label(labelText, new Font("Segoe UI Semibold", 9.5F), ModernUi.Text), 0, 0);

        textBox.Dock = DockStyle.Fill;
        textBox.Height = 38;
        textBox.Margin = new Padding(0);
        panel.Controls.Add(textBox, 0, 1);
        return panel;
    }

    private Control ButtonRow(params Button[] buttons)
    {
        var row = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = ModernUi.Background,
            Padding = new Padding(0, 8, 0, 0)
        };

        foreach (var button in buttons)
        {
            row.Controls.Add(button);
        }

        return row;
    }

    private Button ActionButton(string text, Color color, Func<Task> action)
    {
        var button = ModernUi.FlatButton(text, color, Color.White);
        button.Width = 180;
        button.Height = 42;
        button.Margin = new Padding(0, 0, 12, 0);
        button.Click += async (_, _) => await RunSafelyAsync(action);
        _buttons.Add(button);
        return button;
    }

    private async Task RunSafelyAsync(Func<Task> action)
    {
        try
        {
            SetBusy(true);
            SetStatus("Islem yapiliyor...", ModernUi.Muted);
            await action();
        }
        catch (Exception ex)
        {
            SetStatus($"Hata: {ex.Message}", ModernUi.Danger);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        foreach (var button in _buttons)
        {
            button.Enabled = !busy;
        }

        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
    }

    private void SetStatus(string text, Color color)
    {
        _statusLabel.Text = text;
        _statusLabel.ForeColor = color;
    }
}
