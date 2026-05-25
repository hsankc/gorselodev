using System.ComponentModel;
using DisKlinigiYonetimSistemi.Controls;
using DisKlinigiYonetimSistemi.Data;
using DisKlinigiYonetimSistemi.Models;

namespace DisKlinigiYonetimSistemi.Forms;

public sealed class PatientEditorForm : Form
{
    private readonly bool _withAccount;
    private readonly TextBox _tcNo = ModernUi.TextBox("TC kimlik no");
    private readonly TextBox _fullName = ModernUi.TextBox("Ad soyad");
    private readonly ComboBox _gender = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = ModernUi.BodyFont };
    private readonly DateTimePicker _birthDate = new() { Format = DateTimePickerFormat.Short, Font = ModernUi.BodyFont };
    private readonly TextBox _phone = ModernUi.TextBox("Telefon");
    private readonly TextBox _email = ModernUi.TextBox("E-posta");
    private readonly TextBox _address = ModernUi.TextBox("Adres");
    private readonly TextBox _bloodType = ModernUi.TextBox("Kan grubu");
    private readonly NumericUpDown _height = new() { Minimum = 0, Maximum = 250, Font = ModernUi.BodyFont };
    private readonly NumericUpDown _weight = new() { Minimum = 0, Maximum = 300, DecimalPlaces = 1, Font = ModernUi.BodyFont };
    private readonly TextBox _allergy = ModernUi.TextBox("Alerji notu");
    private readonly TextBox _chronic = ModernUi.TextBox("Kronik hastalik");
    private readonly TextBox _currentMedications = ModernUi.TextBox("Kullandigi ilaclar");
    private readonly TextBox _smoking = ModernUi.TextBox("Sigara durumu");
    private readonly TextBox _emergencyName = ModernUi.TextBox("Acil kisi");
    private readonly TextBox _emergencyPhone = ModernUi.TextBox("Acil telefon");
    private readonly TextBox _dentalHistory = ModernUi.TextBox("Dis gecmisi");
    private readonly TextBox _riskLevel = ModernUi.TextBox("Risk seviyesi");
    private readonly TextBox _userName = ModernUi.TextBox("Kullanici adi");
    private readonly TextBox _password = ModernUi.TextBox("Sifre", true);

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Patient? Patient { get; private set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string CreatedUserName => _userName.Text.Trim();

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string CreatedPassword => _password.Text;

    private PatientEditorForm(ClinicDataStore store, Patient? patient, bool withAccount)
    {
        _withAccount = withAccount;
        Patient = patient is null ? new Patient() : Clone(patient);
        Text = patient is null ? "Hasta Kaydi" : "Hasta Duzenle";
        Size = new Size(620, withAccount ? 820 : 760);
        MinimumSize = Size;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = ModernUi.Background;
        Font = ModernUi.BodyFont;
        BuildUi(store);
        FillFields();
    }

    public static PatientEditorForm Create(ClinicDataStore store, Patient? patient, bool withAccount = false) =>
        new(store, patient, withAccount);

    private void BuildUi(ClinicDataStore store)
    {
        _gender.Items.AddRange(Enum.GetNames<Gender>());

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            AutoScroll = true,
            Padding = new Padding(24)
        };
        Controls.Add(panel);

        panel.Controls.Add(ModernUi.Label(Text, ModernUi.HeaderFont));
        AddField(panel, "TC No", _tcNo);
        AddField(panel, "Ad Soyad", _fullName);
        AddField(panel, "Cinsiyet", _gender);
        AddField(panel, "Dogum Tarihi", _birthDate);
        AddField(panel, "Telefon", _phone);
        AddField(panel, "E-posta", _email);
        AddField(panel, "Adres", _address);
        AddField(panel, "Kan Grubu", _bloodType);
        AddField(panel, "Boy (cm)", _height);
        AddField(panel, "Kilo (kg)", _weight);
        AddField(panel, "Alerji Notlari", _allergy);
        AddField(panel, "Kronik Hastaliklar", _chronic);
        AddField(panel, "Kullandigi Ilaclar", _currentMedications);
        AddField(panel, "Sigara", _smoking);
        AddField(panel, "Acil Kisi", _emergencyName);
        AddField(panel, "Acil Telefon", _emergencyPhone);
        AddField(panel, "Dis Gecmisi", _dentalHistory);
        AddField(panel, "Risk Seviyesi", _riskLevel);

        if (_withAccount)
        {
            _userName.Text = SuggestUserName(store);
            _password.Text = "123456";
            AddField(panel, "Portal Kullanici Adi", _userName);
            AddField(panel, "Portal Sifresi", _password);
        }

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 54, FlowDirection = FlowDirection.RightToLeft };
        var save = ModernUi.PrimaryButton("Kaydet");
        var cancel = ModernUi.FlatButton("Vazgec", Color.FromArgb(230, 236, 244), ModernUi.Text);
        save.Width = cancel.Width = 120;
        save.Click += (_, _) => SaveAndClose(store);
        cancel.Click += (_, _) => DialogResult = DialogResult.Cancel;
        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);
        panel.Controls.Add(buttons);
        AcceptButton = save;
        CancelButton = cancel;
    }

    private static void AddField(Control parent, string label, Control input)
    {
        parent.Controls.Add(ModernUi.Label(label, new Font("Segoe UI Semibold", 9.2F), ModernUi.Text));
        input.Dock = DockStyle.Top;
        input.Margin = new Padding(0, 2, 0, 10);
        parent.Controls.Add(input);
    }

    private void FillFields()
    {
        if (Patient is null) return;
        _tcNo.Text = Patient.TcNo;
        _fullName.Text = Patient.FullName;
        _gender.SelectedItem = Patient.Gender.ToString();
        _birthDate.Value = Patient.BirthDate;
        _phone.Text = Patient.Phone;
        _email.Text = Patient.Email;
        _address.Text = Patient.Address;
        _bloodType.Text = Patient.BloodType;
        _height.Value = Math.Clamp(Patient.HeightCm, (int)_height.Minimum, (int)_height.Maximum);
        _weight.Value = Math.Clamp(Patient.WeightKg, _weight.Minimum, _weight.Maximum);
        _allergy.Text = Patient.AllergyNotes;
        _chronic.Text = Patient.ChronicDiseases;
        _currentMedications.Text = Patient.CurrentMedications;
        _smoking.Text = Patient.SmokingStatus;
        _emergencyName.Text = Patient.EmergencyContactName;
        _emergencyPhone.Text = Patient.EmergencyContactPhone;
        _dentalHistory.Text = Patient.DentalHistory;
        _riskLevel.Text = Patient.RiskLevel;
    }

    private void SaveAndClose(ClinicDataStore store)
    {
        if (Patient is null) return;
        if (string.IsNullOrWhiteSpace(_fullName.Text))
        {
            MessageBox.Show("Ad soyad bos olamaz.", "Eksik Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_withAccount && (string.IsNullOrWhiteSpace(_userName.Text) || string.IsNullOrWhiteSpace(_password.Text)))
        {
            MessageBox.Show("Hasta portali icin kullanici adi ve sifre girin.", "Eksik Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_withAccount && store.Snapshot.Users.Any(user => user.UserName.Equals(_userName.Text.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show("Bu kullanici adi zaten var.", "Kayit Uyarisi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Patient.TcNo = _tcNo.Text.Trim();
        Patient.FullName = _fullName.Text.Trim();
        Patient.Gender = Enum.TryParse<Gender>(_gender.Text, out var gender) ? gender : Gender.Belirtilmedi;
        Patient.BirthDate = _birthDate.Value.Date;
        Patient.Phone = _phone.Text.Trim();
        Patient.Email = _email.Text.Trim();
        Patient.Address = _address.Text.Trim();
        Patient.BloodType = _bloodType.Text.Trim();
        Patient.HeightCm = (int)_height.Value;
        Patient.WeightKg = _weight.Value;
        Patient.AllergyNotes = _allergy.Text.Trim();
        Patient.ChronicDiseases = _chronic.Text.Trim();
        Patient.CurrentMedications = _currentMedications.Text.Trim();
        Patient.SmokingStatus = _smoking.Text.Trim();
        Patient.EmergencyContactName = _emergencyName.Text.Trim();
        Patient.EmergencyContactPhone = _emergencyPhone.Text.Trim();
        Patient.DentalHistory = _dentalHistory.Text.Trim();
        Patient.RiskLevel = _riskLevel.Text.Trim();
        DialogResult = DialogResult.OK;
    }

    private static string SuggestUserName(ClinicDataStore store)
    {
        var number = store.Snapshot.Users.Count(user => user.Role == UserRole.Hasta) + 1;
        return $"hasta{number}";
    }

    private static Patient Clone(Patient patient) => new()
    {
        Id = patient.Id,
        TcNo = patient.TcNo,
        FullName = patient.FullName,
        Gender = patient.Gender,
        BirthDate = patient.BirthDate,
        Phone = patient.Phone,
        Email = patient.Email,
        Address = patient.Address,
        BloodType = patient.BloodType,
        HeightCm = patient.HeightCm,
        WeightKg = patient.WeightKg,
        AllergyNotes = patient.AllergyNotes,
        ChronicDiseases = patient.ChronicDiseases,
        CurrentMedications = patient.CurrentMedications,
        SmokingStatus = patient.SmokingStatus,
        EmergencyContactName = patient.EmergencyContactName,
        EmergencyContactPhone = patient.EmergencyContactPhone,
        DentalHistory = patient.DentalHistory,
        RiskLevel = patient.RiskLevel,
        CreatedAt = patient.CreatedAt
    };
}
