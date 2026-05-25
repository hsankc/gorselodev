using DisKlinigiYonetimSistemi.Controls;
using DisKlinigiYonetimSistemi.Data;
using DisKlinigiYonetimSistemi.Models;

namespace DisKlinigiYonetimSistemi.Forms;

public static class EntityEditorForms
{
    public static Appointment? Appointment(ClinicDataStore store, Appointment? source, UserAccount currentUser)
    {
        var entity = source is null ? new Appointment { RequestedByUserId = currentUser.Id } : Clone(source);
        using var form = Dialog("Randevu Bilgisi", 560, 560);
        var patient = Lookup(store.Snapshot.Patients, item => item.FullName, item => item.Id);
        var doctor = Lookup(store.Doctors.ToList(), item => item.FullName, item => item.Id);
        var patientBox = Combo(patient, entity.PatientId);
        var doctorBox = Combo(doctor, entity.DoctorUserId);
        var date = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "dd.MM.yyyy HH:mm", Value = entity.StartsAt, Font = ModernUi.BodyFont };
        var duration = Number(entity.DurationMinutes, 10, 240);
        var status = Combo(Enum.GetNames<AppointmentStatus>().Select(x => new LookupItem(x, x)).ToList(), entity.Status.ToString());
        var complaint = Text(entity.Complaint, "Sikayet");
        var notes = Text(entity.Notes, "Notlar", true);

        if (currentUser.Role == UserRole.Hasta && currentUser.LinkedPatientId is not null)
        {
            patientBox.SelectedValue = currentUser.LinkedPatientId;
            patientBox.Enabled = false;
            status.SelectedValue = AppointmentStatus.TalepEdildi.ToString();
            status.Enabled = false;
        }

        AddRows(form, ("Hasta", patientBox), ("Doktor", doctorBox), ("Tarih/Saat", date), ("Sure (dk)", duration), ("Durum", status), ("Sikayet", complaint), ("Not", notes));
        return Show(form, () =>
        {
            entity.PatientId = Value(patientBox);
            entity.DoctorUserId = Value(doctorBox);
            entity.RequestedByUserId = string.IsNullOrWhiteSpace(entity.RequestedByUserId) ? currentUser.Id : entity.RequestedByUserId;
            entity.StartsAt = date.Value;
            entity.DurationMinutes = (int)duration.Value;
            entity.Status = Enum.TryParse<AppointmentStatus>(Value(status), out var parsed) ? parsed : AppointmentStatus.TalepEdildi;
            entity.Complaint = complaint.Text.Trim();
            entity.Notes = notes.Text.Trim();
            return entity;
        });
    }

    public static Prescription? Prescription(ClinicDataStore store, Prescription? source, UserAccount currentUser)
    {
        var entity = source is null ? new Prescription { DoctorUserId = currentUser.Id } : Clone(source);
        using var form = Dialog("Akilli Recete", 700, 720);
        var patientBox = Combo(Lookup(store.Snapshot.Patients, item => item.FullName, item => item.Id), entity.PatientId);
        var doctorBox = Combo(Lookup(store.Doctors.ToList(), item => item.FullName, item => item.Id), entity.DoctorUserId);
        var date = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = entity.Date, Font = ModernUi.BodyFont };
        var topicBox = Combo(
            new[] { "Kanal Tedavisi", "Dolgu Sonrasi", "Dis Eti Tedavisi", "Cerrahi Hazirlik", "Ortodontik Hazirlik", "Implant Planlama", "Rutin Kontrol" }
                .Select(topic => new LookupItem(topic, topic)).ToList(),
            entity.Topic);
        var diagnosis = Text(entity.Diagnosis, "Tani / klinik konu");
        var medicines = new CheckedListBox
        {
            CheckOnClick = true,
            Font = ModernUi.BodyFont,
            Height = 180,
            DisplayMember = nameof(MedicationTemplate.Name),
            ValueMember = nameof(MedicationTemplate.Id)
        };
        foreach (var medication in store.Snapshot.Medications)
        {
            var index = medicines.Items.Add(medication);
            if (entity.MedicationIds.Contains(medication.Id))
            {
                medicines.SetItemChecked(index, true);
            }
        }

        var usage = Text(entity.UsageInstructions, "Secilen ilaclarin talimatlari otomatik gelir", true);
        usage.Height = 120;
        usage.ReadOnly = true;
        var note = Text(entity.DoctorNote, "Doktor notu", true);
        note.Height = 84;

        void RefreshUsage()
        {
            var selected = medicines.CheckedItems.Cast<MedicationTemplate>().ToList();
            usage.Text = string.Join(Environment.NewLine, selected.Select(med => $"{med.Name}: {med.DefaultUsage}"));
        }

        medicines.ItemCheck += (_, _) => form.BeginInvoke(RefreshUsage);
        topicBox.SelectedIndexChanged += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(diagnosis.Text))
            {
                diagnosis.Text = Value(topicBox);
            }
        };
        RefreshUsage();
        AddRows(form, ("Hasta", patientBox), ("Doktor", doctorBox), ("Tarih", date), ("Recete Konusu", topicBox), ("Tani", diagnosis), ("Ilac Secimi", medicines), ("Otomatik Kullanim Talimati", usage), ("Doktor Notu", note));
        return Show(form, () =>
        {
            var selected = medicines.CheckedItems.Cast<MedicationTemplate>().ToList();
            entity.PatientId = Value(patientBox);
            entity.DoctorUserId = Value(doctorBox);
            entity.Date = date.Value.Date;
            entity.Topic = Value(topicBox);
            entity.Diagnosis = diagnosis.Text.Trim();
            entity.MedicationIds = selected.Select(med => med.Id).ToList();
            entity.Medicines = string.Join("; ", selected.Select(med => med.Name));
            entity.UsageInstructions = usage.Text.Trim();
            entity.DoctorNote = note.Text.Trim();
            return entity;
        });
    }

    public static Radiograph? Radiograph(ClinicDataStore store, Radiograph? source, UserAccount currentUser)
    {
        var entity = source is null ? new Radiograph { DoctorUserId = currentUser.Id } : Clone(source);
        using var form = Dialog("Dis Rontgeni", 560, 560);
        var patientBox = Combo(Lookup(store.Snapshot.Patients, item => item.FullName, item => item.Id), entity.PatientId);
        var doctorBox = Combo(Lookup(store.Doctors.ToList(), item => $"{item.FullName} - {item.Specialty}", item => item.Id), entity.DoctorUserId);
        var date = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = entity.Date, Font = ModernUi.BodyFont };
        var region = Text(entity.ToothRegion, "Dis bolgesi");
        var imagePath = Text(entity.ImagePath, "Gorsel dosya yolu");
        var choose = ModernUi.FlatButton("Gorsel Sec", Color.FromArgb(230, 236, 244), ModernUi.Text);
        choose.Click += (_, _) =>
        {
            using var file = new OpenFileDialog
            {
                Filter = "Gorsel Dosyalari|*.png;*.jpg;*.jpeg;*.bmp|Tum Dosyalar|*.*",
                Title = "Rontgen gorseli sec"
            };
            if (file.ShowDialog(form) == DialogResult.OK)
            {
                imagePath.Text = file.FileName;
            }
        };
        var notes = Text(entity.Notes, "Rontgen notu", true);
        AddRows(form, ("Hasta", patientBox), ("Doktor", doctorBox), ("Tarih", date), ("Bolge", region), ("Gorsel", imagePath), ("", choose), ("Not", notes));
        return Show(form, () =>
        {
            entity.PatientId = Value(patientBox);
            entity.DoctorUserId = Value(doctorBox);
            entity.Date = date.Value.Date;
            entity.ToothRegion = region.Text.Trim();
            entity.ImagePath = imagePath.Text.Trim();
            entity.Notes = notes.Text.Trim();
            return entity;
        });
    }

    public static TreatmentPlan? Treatment(ClinicDataStore store, TreatmentPlan? source, UserAccount currentUser)
    {
        var entity = source is null ? new TreatmentPlan { DoctorUserId = currentUser.Id } : Clone(source);
        using var form = Dialog("Tedavi Plani", 560, 620);
        var patientBox = Combo(Lookup(store.Snapshot.Patients, item => item.FullName, item => item.Id), entity.PatientId);
        var doctorBox = Combo(Lookup(store.Doctors.ToList(), item => item.FullName, item => item.Id), entity.DoctorUserId);
        var date = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = entity.Date, Font = ModernUi.BodyFont };
        var toothNo = Text(entity.ToothNo, "Dis no");
        var procedure = Text(entity.ProcedureName, "Islem");
        var description = Text(entity.Description, "Aciklama", true);
        var completed = new CheckBox { Text = "Tedavi tamamlandi", Checked = entity.Completed, AutoSize = true, ForeColor = ModernUi.Text };
        AddRows(form, ("Hasta", patientBox), ("Doktor", doctorBox), ("Tarih", date), ("Dis No", toothNo), ("Islem", procedure), ("Aciklama", description), ("Durum", completed));
        return Show(form, () =>
        {
            entity.PatientId = Value(patientBox);
            entity.DoctorUserId = Value(doctorBox);
            entity.Date = date.Value.Date;
            entity.ToothNo = toothNo.Text.Trim();
            entity.ProcedureName = procedure.Text.Trim();
            entity.Description = description.Text.Trim();
            entity.Completed = completed.Checked;
            return entity;
        });
    }

    private static Form Dialog(string title, int width, int height)
    {
        return new Form
        {
            Text = title,
            Size = new Size(width, height),
            MinimumSize = new Size(width, height),
            StartPosition = FormStartPosition.CenterParent,
            BackColor = ModernUi.Background,
            Font = ModernUi.BodyFont
        };
    }

    private static void AddRows(Form form, params (string Label, Control Control)[] rows)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            AutoScroll = true,
            Padding = new Padding(24)
        };
        form.Controls.Add(panel);
        panel.Controls.Add(ModernUi.Label(form.Text, ModernUi.HeaderFont));
        foreach (var row in rows)
        {
            if (!string.IsNullOrWhiteSpace(row.Label))
            {
                panel.Controls.Add(ModernUi.Label(row.Label, new Font("Segoe UI Semibold", 9.2F), ModernUi.Text));
            }

            row.Control.Dock = DockStyle.Top;
            row.Control.Margin = new Padding(0, 2, 0, 10);
            panel.Controls.Add(row.Control);
        }

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 54, FlowDirection = FlowDirection.RightToLeft };
        var save = ModernUi.PrimaryButton("Kaydet");
        var cancel = ModernUi.FlatButton("Vazgec", Color.FromArgb(230, 236, 244), ModernUi.Text);
        save.Width = cancel.Width = 120;
        save.Click += (_, _) => form.DialogResult = DialogResult.OK;
        cancel.Click += (_, _) => form.DialogResult = DialogResult.Cancel;
        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);
        panel.Controls.Add(buttons);
        form.AcceptButton = save;
        form.CancelButton = cancel;
    }

    private static T? Show<T>(Form form, Func<T> factory) where T : class
    {
        return form.ShowDialog() == DialogResult.OK ? factory() : null;
    }

    private static TextBox Text(string value, string placeholder, bool multiline = false)
    {
        var box = ModernUi.TextBox(placeholder);
        box.Text = value;
        box.Multiline = multiline;
        if (multiline)
        {
            box.Height = 78;
            box.ScrollBars = ScrollBars.Vertical;
        }

        return box;
    }

    private static NumericUpDown Number(decimal value, decimal min, decimal max)
    {
        return new NumericUpDown
        {
            Minimum = min,
            Maximum = max,
            Value = Math.Min(Math.Max(value, min), max),
            DecimalPlaces = max > 500 ? 2 : 0,
            Font = ModernUi.BodyFont,
            Height = 32
        };
    }

    private static ComboBox Combo(List<LookupItem> items, string selectedValue)
    {
        var combo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = ModernUi.BodyFont,
            DisplayMember = nameof(LookupItem.Text),
            ValueMember = nameof(LookupItem.Value),
            DataSource = items
        };

        if (!string.IsNullOrWhiteSpace(selectedValue))
        {
            combo.SelectedValue = selectedValue;
        }

        return combo;
    }

    private static List<LookupItem> Lookup<T>(IEnumerable<T> source, Func<T, string> text, Func<T, string> value) =>
        source.Select(item => new LookupItem(text(item), value(item))).ToList();

    private static string Value(ComboBox combo) => combo.SelectedValue?.ToString() ?? "";

    private sealed record LookupItem(string Text, string Value);

    private static Appointment Clone(Appointment item) => new()
    {
        Id = item.Id,
        PatientId = item.PatientId,
        DoctorUserId = item.DoctorUserId,
        RequestedByUserId = item.RequestedByUserId,
        ApprovedByUserId = item.ApprovedByUserId,
        RequestedAt = item.RequestedAt,
        StartsAt = item.StartsAt,
        DurationMinutes = item.DurationMinutes,
        Status = item.Status,
        Complaint = item.Complaint,
        Notes = item.Notes
    };

    private static Prescription Clone(Prescription item) => new()
    {
        Id = item.Id,
        PatientId = item.PatientId,
        DoctorUserId = item.DoctorUserId,
        Date = item.Date,
        Topic = item.Topic,
        MedicationIds = [..item.MedicationIds],
        Diagnosis = item.Diagnosis,
        Medicines = item.Medicines,
        UsageInstructions = item.UsageInstructions,
        DoctorNote = item.DoctorNote
    };

    private static Radiograph Clone(Radiograph item) => new()
    {
        Id = item.Id,
        PatientId = item.PatientId,
        DoctorUserId = item.DoctorUserId,
        Date = item.Date,
        ToothRegion = item.ToothRegion,
        ImagePath = item.ImagePath,
        Notes = item.Notes
    };

    private static TreatmentPlan Clone(TreatmentPlan item) => new()
    {
        Id = item.Id,
        PatientId = item.PatientId,
        DoctorUserId = item.DoctorUserId,
        Date = item.Date,
        ToothNo = item.ToothNo,
        ProcedureName = item.ProcedureName,
        Description = item.Description,
        Completed = item.Completed
    };
}
