using System.Text.Json;
using DisKlinigiYonetimSistemi.Models;

namespace DisKlinigiYonetimSistemi.Data;

public sealed class ClinicDataStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _dataPath;
    private readonly string _settingsPath;
    private SupabaseSettings _supabaseSettings = new();
    private SupabaseClinicClient? _supabaseClient;

    public ClinicDataStore()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "App_Data");
        Directory.CreateDirectory(folder);
        _dataPath = Path.Combine(folder, "clinic-data.json");
        _settingsPath = Path.Combine(folder, "supabase-settings.json");
    }

    public DataSnapshot Snapshot { get; private set; } = new();
    public string DataPath => _dataPath;
    public bool SupabaseEnabled => _supabaseSettings.Enabled;
    public string? LastSyncError { get; private set; }
    public SupabaseSettings CurrentSupabaseSettings => new()
    {
        Url = _supabaseSettings.Url,
        ApiKey = _supabaseSettings.ApiKey
    };

    public async Task InitializeAsync()
    {
        await LoadLocalSnapshotAsync();
        _supabaseSettings = await LoadSupabaseSettingsFileAsync();
        _supabaseClient = CreateSupabaseClient(_supabaseSettings);

        if (_supabaseClient is not null)
        {
            await TryWarmUpSupabaseAsync();
        }
    }

    private async Task LoadLocalSnapshotAsync()
    {
        if (!File.Exists(_dataPath))
        {
            Snapshot = CreateSeedData();
            await SaveLocalAsync();
            return;
        }

        await using (var stream = File.OpenRead(_dataPath))
        {
            Snapshot = await JsonSerializer.DeserializeAsync<DataSnapshot>(stream, JsonOptions) ?? CreateSeedData();
        }

        if (!IsUsableSnapshot(Snapshot))
        {
            Snapshot = CreateSeedData();
            await SaveLocalAsync();
        }
    }

    public UserAccount? Authenticate(string userName, string password)
    {
        return Snapshot.Users.FirstOrDefault(user =>
            user.Active &&
            user.UserName.Equals(userName.Trim(), StringComparison.OrdinalIgnoreCase) &&
            user.Password == password);
    }

    public async Task SaveAsync()
    {
        await SaveLocalAsync();
        await TryPushSupabaseAsync();
    }

    private async Task SaveLocalAsync()
    {
        Snapshot.UpdatedAt = DateTime.Now;
        await using var stream = File.Create(_dataPath);
        await JsonSerializer.SerializeAsync(stream, Snapshot, JsonOptions);
    }

    public async Task ReplaceSnapshotAsync(DataSnapshot snapshot)
    {
        Snapshot = snapshot;
        await SaveAsync();
    }

    public async Task AddLogAsync(UserAccount actor, string actionType, string description, string? patientId = null, string? doctorUserId = null)
    {
        Snapshot.Logs.Insert(0, new SystemLog
        {
            ActorUserId = actor.Id,
            ActorName = actor.FullName,
            ActorRole = actor.Role,
            ActionType = actionType,
            PatientId = patientId,
            DoctorUserId = doctorUserId,
            Description = description
        });
        await SaveAsync();
    }

    public async Task<SupabaseSettings> LoadSupabaseSettingsAsync()
    {
        _supabaseSettings = await LoadSupabaseSettingsFileAsync();
        _supabaseClient = CreateSupabaseClient(_supabaseSettings);
        return CurrentSupabaseSettings;
    }

    private async Task<SupabaseSettings> LoadSupabaseSettingsFileAsync()
    {
        if (!File.Exists(_settingsPath))
        {
            return new SupabaseSettings();
        }

        await using var stream = File.OpenRead(_settingsPath);
        return await JsonSerializer.DeserializeAsync<SupabaseSettings>(stream, JsonOptions) ?? new SupabaseSettings();
    }

    public async Task SaveSupabaseSettingsAsync(SupabaseSettings settings)
    {
        _supabaseSettings = NormalizeSettings(settings);
        _supabaseClient = CreateSupabaseClient(_supabaseSettings);
        await SaveSupabaseSettingsFileAsync(_supabaseSettings);
    }

    private async Task SaveSupabaseSettingsFileAsync(SupabaseSettings settings)
    {
        await using var stream = File.Create(_settingsPath);
        await JsonSerializer.SerializeAsync(stream, settings, JsonOptions);
    }

    public async Task ConfigureSupabaseAsync(SupabaseSettings settings, bool pullRemote, bool pushLocal)
    {
        await SaveSupabaseSettingsAsync(settings);

        if (_supabaseClient is null)
        {
            LastSyncError = null;
            return;
        }

        if (pullRemote)
        {
            await PullFromSupabaseAsync();
        }

        if (pushLocal)
        {
            await PushToSupabaseAsync();
        }
    }

    public async Task<DataSnapshot?> PullFromSupabaseAsync()
    {
        if (_supabaseClient is null)
        {
            throw new InvalidOperationException("Supabase ayarlari kayitli degil.");
        }

        var remote = await _supabaseClient.PullAsync();
        if (!IsUsableSnapshot(remote))
        {
            LastSyncError = "Supabase tablosunda kullanilabilir klinik verisi bulunamadi.";
            return null;
        }

        Snapshot = remote!;
        await SaveLocalAsync();
        LastSyncError = null;
        return Snapshot;
    }

    public async Task PushToSupabaseAsync()
    {
        if (_supabaseClient is null)
        {
            throw new InvalidOperationException("Supabase ayarlari kayitli degil.");
        }

        await _supabaseClient.PushAsync(Snapshot);
        LastSyncError = null;
    }

    private async Task TryWarmUpSupabaseAsync()
    {
        try
        {
            var remote = await _supabaseClient!.PullAsync();
            if (IsUsableSnapshot(remote))
            {
                Snapshot = remote!;
                await SaveLocalAsync();
            }
            else
            {
                await _supabaseClient.PushAsync(Snapshot);
            }

            LastSyncError = null;
        }
        catch (Exception ex)
        {
            LastSyncError = ex.Message;
        }
    }

    private async Task TryPushSupabaseAsync()
    {
        if (_supabaseClient is null)
        {
            return;
        }

        try
        {
            await _supabaseClient.PushAsync(Snapshot);
            LastSyncError = null;
        }
        catch (Exception ex)
        {
            LastSyncError = ex.Message;
        }
    }

    private static SupabaseClinicClient? CreateSupabaseClient(SupabaseSettings settings) =>
        settings.Enabled ? new SupabaseClinicClient(settings) : null;

    private static SupabaseSettings NormalizeSettings(SupabaseSettings settings) => new()
    {
        Url = settings.Url.Trim(),
        ApiKey = settings.ApiKey.Trim()
    };

    private static bool IsUsableSnapshot(DataSnapshot? snapshot) =>
        snapshot is not null &&
        snapshot.SeedVersion >= 4 &&
        snapshot.Users.Count > 0 &&
        snapshot.Patients.Count > 0 &&
        snapshot.Medications.Count > 0;

    public IEnumerable<UserAccount> Doctors => Snapshot.Users.Where(user => user.Active && user.Role == UserRole.Doktor);
    public IEnumerable<UserAccount> Secretaries => Snapshot.Users.Where(user => user.Active && user.Role == UserRole.Sekreter);

    public string PatientName(string patientId) =>
        Snapshot.Patients.FirstOrDefault(patient => patient.Id == patientId)?.FullName ?? "Bilinmeyen Hasta";

    public string DoctorName(string doctorId) =>
        Snapshot.Users.FirstOrDefault(user => user.Id == doctorId)?.FullName ?? "Bilinmeyen Doktor";

    public string UserName(string userId) =>
        Snapshot.Users.FirstOrDefault(user => user.Id == userId)?.FullName ?? "Sistem";

    public string MedicineName(string medicineId) =>
        Snapshot.Medications.FirstOrDefault(medicine => medicine.Id == medicineId)?.Name ?? medicineId;

    private static DataSnapshot CreateSeedData()
    {
        var admin = new UserAccount
        {
            Id = "admin-demo",
            UserName = "admin",
            Password = "123456",
            FullName = "Cagri Cetin",
            Role = UserRole.Admin,
            Email = "admin@ccetyteeth.com",
            Phone = "0212 555 00 00",
            Biography = "Klinik operasyonlarini ve yetkilendirmeleri yonetir."
        };

        var doctors = new List<UserAccount>
        {
            Doctor("doctor-elif", "doktor", "Dr. Elif Demir", "Endodonti", "Kanal tedavisi ve agri yonetimi", "A-101", "elif.demir@ccetyteeth.com", "0532 100 10 10"),
            Doctor("doctor-kaan", "doktor2", "Dr. Kaan Aydin", "Ortodonti", "Seffaf plak ve tel tedavisi", "B-204", "kaan.aydin@ccetyteeth.com", "0532 200 20 20"),
            Doctor("doctor-selin", "doktor3", "Dr. Selin Aras", "Periodontoloji", "Dis eti hastaliklari ve cerrahi olmayan tedaviler", "A-103", "selin.aras@ccetyteeth.com", "0532 300 30 30"),
            Doctor("doctor-baris", "doktor4", "Dr. Baris Eren", "Cerrahi ve Implantoloji", "Gomulu dis, implant ve cerrahi cekimler", "C-301", "baris.eren@ccetyteeth.com", "0532 400 40 40")
        };

        var secretaries = new List<UserAccount>
        {
            Secretary("secretary-ayse", "sekreter", "Ayse Yilmaz", "doctor-elif", "ayse.yilmaz@ccetyteeth.com", "0541 111 11 11"),
            Secretary("secretary-zeynep", "sekreter2", "Zeynep Sahin", "doctor-kaan", "zeynep.sahin@ccetyteeth.com", "0541 222 22 22"),
            Secretary("secretary-ece", "sekreter3", "Ece Korkmaz", "doctor-selin", "ece.korkmaz@ccetyteeth.com", "0541 333 33 33"),
            Secretary("secretary-emre", "sekreter4", "Emre Koc", "doctor-baris", "emre.koc@ccetyteeth.com", "0541 444 44 44")
        };

        var patients = new List<Patient>
        {
            Patient("patient-ada", "12345678901", "Ada Kaya", Gender.Kadin, 22, "A Rh+", 168, 58, "0555 111 22 33", "ada.kaya@mail.com", "Kadikoy / Istanbul", "Penisilin alerjisi", "Yok", "D vitamini", "Kullanmiyor", "Melis Kaya", "0555 111 22 34", "Sol alt 6 kanal tedavisi takipte", "Orta", -29),
            Patient("patient-mert", "10987654321", "Mert Arslan", Gender.Erkek, 34, "0 Rh+", 178, 82, "0555 444 55 66", "mert.arslan@mail.com", "Cankaya / Ankara", "Yok", "Tip 2 diyabet", "Metformin", "Ara sira", "Nermin Arslan", "0555 444 55 67", "Kompozit dolgu sonrasi hassasiyet", "Yuksek", -25),
            Patient("patient-deren", "22446688002", "Deren Oz", Gender.Kadin, 18, "B Rh-", 164, 53, "0555 777 88 99", "deren.oz@mail.com", "Bornova / Izmir", "Lateks hassasiyeti", "Yok", "Yok", "Kullanmiyor", "Selma Oz", "0555 777 88 98", "Ortodonti baslangic olcusu alindi", "Dusuk", -21),
            Patient("patient-cem", "33445566778", "Cem Yildirim", Gender.Erkek, 41, "AB Rh+", 181, 91, "0555 222 33 44", "cem.yildirim@mail.com", "Nilufer / Bursa", "Yok", "Hipertansiyon", "Tansiyon ilaci", "Gunde 5 adet", "Buse Yildirim", "0555 222 33 45", "Gomulu 20lik dis cerrahi takip", "Yuksek", -18),
            Patient("patient-lara", "44556677889", "Lara Deniz", Gender.Kadin, 29, "A Rh-", 170, 61, "0555 999 00 11", "lara.deniz@mail.com", "Besiktas / Istanbul", "Aspirin kullanamaz", "Astim", "Inhaler", "Kullanmiyor", "Derya Deniz", "0555 999 00 12", "Periodontal temizlik ve takip", "Orta", -15),
            Patient("patient-bora", "55667788990", "Bora Ates", Gender.Erkek, 12, "0 Rh-", 151, 42, "0555 123 45 67", "bora.ates@mail.com", "Uskudar / Istanbul", "Yok", "Yok", "Yok", "Kullanmiyor", "Can Ates", "0555 123 45 68", "Ortodonti danisma talebi", "Dusuk", -12),
            Patient("patient-nil", "66778899001", "Nil Karaca", Gender.Kadin, 37, "B Rh+", 166, 64, "0555 987 65 43", "nil.karaca@mail.com", "Atasehir / Istanbul", "Yok", "Migren", "Migren ilaci", "Kullanmiyor", "Onur Karaca", "0555 987 65 44", "Dis eti cekilmesi ve hassasiyet", "Orta", -10),
            Patient("patient-umut", "77889900112", "Umut Polat", Gender.Erkek, 46, "A Rh+", 176, 87, "0555 321 45 67", "umut.polat@mail.com", "Karsiyaka / Izmir", "Klorheksidin hassasiyeti", "Yok", "Yok", "Gunde 10 adet", "Elif Polat", "0555 321 45 68", "Implant oncesi degerlendirme", "Yuksek", -9),
            Patient("patient-melis", "88990011223", "Melis Topal", Gender.Kadin, 25, "AB Rh-", 162, 55, "0555 654 32 10", "melis.topal@mail.com", "Muratpasa / Antalya", "Yok", "Yok", "Yok", "Kullanmiyor", "Aylin Topal", "0555 654 32 11", "Beyazlatma ve rutin kontrol", "Dusuk", -7),
            Patient("patient-efe", "99001122334", "Efe Sari", Gender.Erkek, 31, "0 Rh+", 183, 79, "0555 246 80 24", "efe.sari@mail.com", "Tepebasi / Eskisehir", "Yok", "Reflu", "Mide koruyucu", "Ara sira", "Cansu Sari", "0555 246 80 25", "Bruksizm plak kontrolu", "Orta", -6),
            Patient("patient-zeynep", "10112233445", "Zeynep Gunes", Gender.Kadin, 52, "A Rh+", 160, 70, "0555 135 79 24", "zeynep.gunes@mail.com", "Ortahisar / Trabzon", "Makrolid hassasiyeti", "Osteoporoz", "Kalsiyum destegi", "Kullanmiyor", "Burak Gunes", "0555 135 79 25", "Kuron ve periodontal takip", "Yuksek", -5),
            Patient("patient-arda", "21122334456", "Arda Keskin", Gender.Erkek, 27, "B Rh+", 174, 73, "0555 864 20 20", "arda.keskin@mail.com", "Meram / Konya", "Yok", "Yok", "Yok", "Kullanmiyor", "Seda Keskin", "0555 864 20 21", "Travma sonrasi on dis kontrolu", "Orta", -3)
        };

        var patientUsers = patients.Select((patient, index) => new UserAccount
        {
            Id = index == 0 ? "patient-user-demo" : $"patient-user-{index + 1}",
            UserName = index == 0 ? "hasta" : $"hasta{index + 1}",
            Password = "123456",
            FullName = patient.FullName,
            Role = UserRole.Hasta,
            Email = patient.Email,
            Phone = patient.Phone,
            LinkedPatientId = patient.Id
        }).ToList();

        var medications = CreateMedicationCatalog();
        var snapshot = new DataSnapshot
        {
            SeedVersion = 4,
            Users = [admin, ..doctors, ..secretaries, ..patientUsers],
            Patients = patients,
            Medications = medications,
            Appointments =
            [
                Appointment("app-001", "patient-ada", "doctor-elif", "patient-user-demo", -24, 10, AppointmentStatus.Tamamlandi, "Sicak soguk hassasiyeti", "Kanal tedavisi baslatildi", "secretary-ayse"),
                Appointment("app-002", "patient-mert", "doctor-elif", "patient-user-2", -21, 14, AppointmentStatus.Tamamlandi, "Dolgu sonrasi kontrol", "Dolgu stabil", "secretary-ayse"),
                Appointment("app-003", "patient-deren", "doctor-kaan", "patient-user-3", -16, 11, AppointmentStatus.Tamamlandi, "Caprasiklik muayenesi", "Ortodonti planlamasi yapildi", "secretary-zeynep"),
                Appointment("app-004", "patient-cem", "doctor-baris", "patient-user-4", -12, 15, AppointmentStatus.Tamamlandi, "20lik dis agrisi", "Cerrahi cekim planlandi", "secretary-emre"),
                Appointment("app-005", "patient-lara", "doctor-selin", "patient-user-5", -7, 12, AppointmentStatus.Geldi, "Dis eti kanamasi", "Periodontal kontrol", "secretary-ece"),
                Appointment("app-006", "patient-nil", "doctor-selin", "patient-user-7", -4, 10, AppointmentStatus.Tamamlandi, "Dis eti cekilmesi", "Hassasiyet giderici onerildi", "secretary-ece"),
                Appointment("app-007", "patient-umut", "doctor-baris", "patient-user-8", -2, 16, AppointmentStatus.Geldi, "Implant on gorusmesi", "CBCT istendi", "secretary-emre"),
                Appointment("app-008", "patient-ada", "doctor-elif", "patient-user-demo", 1, 10, AppointmentStatus.Onaylandi, "Kanal tedavisi ikinci seans", "Hasta bilgilendirildi", "secretary-ayse"),
                Appointment("app-009", "patient-mert", "doctor-elif", "patient-user-2", 2, 14, AppointmentStatus.Onaylandi, "Dolgu kontrolu", "", "secretary-ayse"),
                Appointment("app-010", "patient-bora", "doctor-kaan", "patient-user-6", 3, 16, AppointmentStatus.TalepEdildi, "Tel tedavisi hakkinda bilgi almak istiyorum", "Hasta portaldan talep etti", null),
                Appointment("app-011", "patient-lara", "doctor-selin", "patient-user-5", 4, 9, AppointmentStatus.TalepEdildi, "Dis eti temizligi icin randevu", "Hasta portaldan talep etti", null),
                Appointment("app-012", "patient-melis", "doctor-selin", "patient-user-9", 4, 13, AppointmentStatus.Onaylandi, "Rutin kontrol ve beyazlatma", "Sekreter tarafindan onaylandi", "secretary-ece"),
                Appointment("app-013", "patient-efe", "doctor-kaan", "patient-user-10", 5, 15, AppointmentStatus.TalepEdildi, "Gece dis sikma plak kontrolu", "Hasta portaldan talep etti", null),
                Appointment("app-014", "patient-zeynep", "doctor-selin", "patient-user-11", 6, 11, AppointmentStatus.Onaylandi, "Kuron ve dis eti kontrolu", "", "secretary-ece"),
                Appointment("app-015", "patient-arda", "doctor-elif", "patient-user-12", 7, 17, AppointmentStatus.TalepEdildi, "On dis travma kontrolu", "Hasta portaldan talep etti", null),
                Appointment("app-016", "patient-deren", "doctor-kaan", "patient-user-3", 8, 10, AppointmentStatus.Onaylandi, "Seffaf plak teslimi", "Olcu onayi tamamlandi", "secretary-zeynep"),
                Appointment("app-017", "patient-nil", "doctor-selin", "patient-user-7", 8, 14, AppointmentStatus.TalepEdildi, "Dis eti hassasiyeti tekrar etti", "Hasta portaldan talep etti", null),
                Appointment("app-018", "patient-umut", "doctor-baris", "patient-user-8", 9, 12, AppointmentStatus.Onaylandi, "Implant planlama kontrolu", "CBCT sonucu gorusulecek", "secretary-emre"),
                Appointment("app-019", "patient-melis", "doctor-selin", "patient-user-9", 10, 16, AppointmentStatus.TalepEdildi, "Beyazlatma sonrasi kontrol", "Hasta portaldan talep etti", null),
                Appointment("app-020", "patient-efe", "doctor-kaan", "patient-user-10", 11, 9, AppointmentStatus.Onaylandi, "Gece plagi teslimi", "Sekreter tarafindan arandi", "secretary-zeynep"),
                Appointment("app-021", "patient-zeynep", "doctor-selin", "patient-user-11", 12, 10, AppointmentStatus.TalepEdildi, "Kuron hassasiyeti", "Hasta portaldan talep etti", null),
                Appointment("app-022", "patient-arda", "doctor-elif", "patient-user-12", 12, 15, AppointmentStatus.Onaylandi, "Travma takip kontrolu", "", "secretary-ayse"),
                Appointment("app-023", "patient-bora", "doctor-kaan", "patient-user-6", 13, 11, AppointmentStatus.Onaylandi, "Ortodonti analiz sonucu", "Ailesi ile gorusulecek", "secretary-zeynep"),
                Appointment("app-024", "patient-cem", "doctor-baris", "patient-user-4", 14, 13, AppointmentStatus.TalepEdildi, "Cekim sonrasi kontrol", "Hasta portaldan talep etti", null),
                Appointment("app-025", "patient-lara", "doctor-selin", "patient-user-5", 15, 12, AppointmentStatus.Onaylandi, "Periodontal idame randevusu", "", "secretary-ece")
            ],
            Prescriptions =
            [
                Prescription("rx-001", "patient-ada", "doctor-elif", -24, "Kanal Tedavisi", "Pulpitis", ["med-ibu", "med-klor"], "Kanal sonrasi agri olursa kullanilacak."),
                Prescription("rx-002", "patient-mert", "doctor-elif", -21, "Dolgu Sonrasi Hassasiyet", "Postoperatif hassasiyet", ["med-flor"], "Sicak-soguk tetikleyicilerden kacinmasi onerildi."),
                Prescription("rx-003", "patient-deren", "doctor-kaan", -16, "Ortodontik Hazirlik", "Ortodontik baslangic", ["med-par"], "Tel takimi sonrasi gerekirse kullanacak."),
                Prescription("rx-004", "patient-lara", "doctor-selin", -7, "Dis Eti Tedavisi", "Gingivit", ["med-klor", "med-hassas"], "Fircalama teknigi anlatildi."),
                Prescription("rx-005", "patient-cem", "doctor-baris", -12, "Cerrahi Hazirlik", "Perikoronit", ["med-amok", "med-ibu"], "Tansiyon takibi yapilacak."),
                Prescription("rx-006", "patient-nil", "doctor-selin", -4, "Periodontal Takip", "Dentin hassasiyeti", ["med-hassas", "med-flor"], "Iki hafta sonra kontrol."),
                Prescription("rx-007", "patient-umut", "doctor-baris", -2, "Implant Planlama", "Cerrahi oncesi hazirlik", ["med-klor"], "Operasyon oncesi agiz hijyeni guclendirilecek."),
                Prescription("rx-008", "patient-melis", "doctor-selin", -1, "Rutin Kontrol", "Mine hassasiyeti", ["med-flor", "med-hassas"], "Beyazlatma sonrasi hassasiyet takip edilecek."),
                Prescription("rx-009", "patient-efe", "doctor-kaan", -1, "Ortodontik Hazirlik", "Bruksizm", ["med-par"], "Plak basincina bagli agri olursa kullanacak."),
                Prescription("rx-010", "patient-zeynep", "doctor-selin", -3, "Dis Eti Tedavisi", "Periodontal idame", ["med-klor", "med-pro"], "Osteoporoz ilaclari nedeniyle cerrahi plan dikkatli yapilacak."),
                Prescription("rx-011", "patient-arda", "doctor-elif", -1, "Travma Takibi", "Travmatik hassasiyet", ["med-ibu"], "On dislerde renk degisimi takip edilecek."),
                Prescription("rx-012", "patient-bora", "doctor-kaan", 0, "Ortodontik Hazirlik", "Ortodonti oncesi hassasiyet", ["med-par"], "Aile bilgilendirmesi sonrasi tedavi baslatilacak."),
                Prescription("rx-013", "patient-cem", "doctor-baris", -10, "Cerrahi Hazirlik", "20lik dis enfeksiyon riski", ["med-amok", "med-pro"], "Antibiyotik bitmeden operasyon yapilmayacak."),
                Prescription("rx-014", "patient-lara", "doctor-selin", -6, "Dis Eti Tedavisi", "Gingival kanama", ["med-klor"], "Aspirin alerjisi kartta isaretlendi."),
                Prescription("rx-015", "patient-ada", "doctor-elif", 1, "Kanal Tedavisi", "Ikinci seans sonrasi takip", ["med-ibu", "med-muko"], "Gecici dolgu yuksekligi kontrol edilecek.")
            ],
            Radiographs =
            [
                Radiograph("rad-001", "patient-ada", "doctor-elif", -24, "Sol alt 6", MockRadiographGenerator.EnsureImage("ada-sol-alt-6.png", 1), "Kok ucu lezyonu takip edilecek"),
                Radiograph("rad-002", "patient-mert", "doctor-elif", -21, "Sag ust premolar", MockRadiographGenerator.EnsureImage("mert-sag-ust.png", 2), "Dolgu kenari temiz"),
                Radiograph("rad-003", "patient-deren", "doctor-kaan", -16, "Panoramik", MockRadiographGenerator.EnsureImage("deren-panoramik.png", 3), "Ortodonti oncesi panoramik"),
                Radiograph("rad-004", "patient-cem", "doctor-baris", -12, "Sol alt 20lik", MockRadiographGenerator.EnsureImage("cem-20lik.png", 4), "Gomulu dis acisi izlendi"),
                Radiograph("rad-005", "patient-lara", "doctor-selin", -7, "Periodontal seri", MockRadiographGenerator.EnsureImage("lara-periodontal.png", 5), "Kemik seviyesi izlenecek"),
                Radiograph("rad-006", "patient-umut", "doctor-baris", -2, "Implant oncesi", MockRadiographGenerator.EnsureImage("umut-implant.png", 6), "Kemik hacmi degerlendirilecek"),
                Radiograph("rad-007", "patient-arda", "doctor-elif", -1, "On dis travma", MockRadiographGenerator.EnsureImage("arda-travma.png", 7), "Kok kirigi bulgusu izlenmedi"),
                Radiograph("rad-008", "patient-zeynep", "doctor-selin", -3, "Kuron bolgesi", MockRadiographGenerator.EnsureImage("zeynep-kuron.png", 8), "Kuron marjini takip edilecek"),
                Radiograph("rad-009", "patient-bora", "doctor-kaan", 0, "Panoramik ortodonti", MockRadiographGenerator.EnsureImage("bora-ortodonti.png", 9), "Buyume gelisimi uygun"),
                Radiograph("rad-010", "patient-melis", "doctor-selin", 1, "Rutin kontrol", MockRadiographGenerator.EnsureImage("melis-kontrol.png", 10), "Cürük bulgusu izlenmedi")
            ],
            Treatments =
            [
                Treatment("tp-001", "patient-ada", "doctor-elif", -24, "36", "Kanal Tedavisi", "Ilk seans tamamlandi, ikinci seans randevusu olusturuldu", false),
                Treatment("tp-002", "patient-mert", "doctor-elif", -21, "15", "Kompozit Dolgu", "Tedavi tamamlandi, kontrol randevusu verildi", true),
                Treatment("tp-003", "patient-deren", "doctor-kaan", -16, "Genel", "Ortodontik Analiz", "Olcu alindi, tedavi plani hazirlaniyor", false),
                Treatment("tp-004", "patient-cem", "doctor-baris", -12, "38", "Cerrahi Cekim", "Cekim icin onam formu alindi", false),
                Treatment("tp-005", "patient-lara", "doctor-selin", -7, "Genel", "Dis Tasi Temizligi", "Periodontal takip onerildi", true),
                Treatment("tp-006", "patient-umut", "doctor-baris", -2, "46", "Implant Planlama", "Rontgen ve medikal anamnez tamamlandi", false),
                Treatment("tp-007", "patient-efe", "doctor-kaan", -1, "Genel", "Gece Plagi", "Bruksizm icin plak kontrolu bekleniyor", false),
                Treatment("tp-008", "patient-zeynep", "doctor-selin", -3, "24-26", "Kuron Takibi", "Kuron kenari ve dis eti uyumu izleniyor", false),
                Treatment("tp-009", "patient-arda", "doctor-elif", -1, "11", "Travma Takibi", "Vitalite testi icin 2 hafta sonra kontrol", false),
                Treatment("tp-010", "patient-bora", "doctor-kaan", 0, "Genel", "Ortodontik Analiz", "Sefaf plak alternatifi aileye sunuldu", false),
                Treatment("tp-011", "patient-melis", "doctor-selin", 1, "Genel", "Beyazlatma Kontrolu", "Hassasiyet azaldi, flor destegi suruyor", true),
                Treatment("tp-012", "patient-nil", "doctor-selin", -4, "Genel", "Periodontal Bakim", "Fircala arayuz bakimi egitimi verildi", true)
            ]
        };

        snapshot.Logs = CreateLogs(snapshot);
        return snapshot;
    }

    private static UserAccount Doctor(string id, string userName, string fullName, string specialty, string biography, string room, string email, string phone) => new()
    {
        Id = id,
        UserName = userName,
        Password = "123456",
        FullName = fullName,
        Role = UserRole.Doktor,
        Specialty = specialty,
        Biography = biography,
        RoomName = room,
        Email = email,
        Phone = phone
    };

    private static UserAccount Secretary(string id, string userName, string fullName, string doctorId, string email, string phone) => new()
    {
        Id = id,
        UserName = userName,
        Password = "123456",
        FullName = fullName,
        Role = UserRole.Sekreter,
        AssignedDoctorUserId = doctorId,
        Email = email,
        Phone = phone
    };

    private static Patient Patient(string id, string tcNo, string fullName, Gender gender, int age, string bloodType, int height, decimal weight, string phone, string email, string address, string allergy, string chronic, string medications, string smoking, string emergencyName, string emergencyPhone, string dentalHistory, string risk, int createdDayOffset) => new()
    {
        Id = id,
        TcNo = tcNo,
        FullName = fullName,
        Gender = gender,
        BirthDate = DateTime.Today.AddYears(-age),
        BloodType = bloodType,
        HeightCm = height,
        WeightKg = weight,
        Phone = phone,
        Email = email,
        Address = address,
        AllergyNotes = allergy,
        ChronicDiseases = chronic,
        CurrentMedications = medications,
        SmokingStatus = smoking,
        EmergencyContactName = emergencyName,
        EmergencyContactPhone = emergencyPhone,
        DentalHistory = dentalHistory,
        RiskLevel = risk,
        CreatedAt = DateTime.Today.AddDays(createdDayOffset)
    };

    private static List<MedicationTemplate> CreateMedicationCatalog() =>
    [
        new() { Id = "med-ibu", Name = "Ibuprofen 400 mg", Category = "Agri kesici", DefaultUsage = "Tok karnina 8 saatte bir, en fazla 3 gun.", Warning = "Mide rahatsizligi ve kan sulandirici kullaniminda dikkat." },
        new() { Id = "med-par", Name = "Parasetamol 500 mg", Category = "Agri kesici", DefaultUsage = "Gerektiginde 6-8 saatte bir, gunde 4 tableti gecmeyecek.", Warning = "Karaciger hastaligi olanlarda dikkat." },
        new() { Id = "med-amok", Name = "Amoksisilin/Klavulanat 1000 mg", Category = "Antibiyotik", DefaultUsage = "12 saatte bir, tok karnina, 5 gun.", Warning = "Penisilin alerjisi olan hastaya verilmez." },
        new() { Id = "med-klor", Name = "Klorheksidin gargara", Category = "Antiseptik", DefaultUsage = "Sabah aksam 30 saniye, 7 gun. Kullanimdan sonra 30 dakika su icilmez.", Warning = "Uzun sureli kullanimda renklenme yapabilir." },
        new() { Id = "med-flor", Name = "Flor jel", Category = "Koruyucu", DefaultUsage = "Gece yatmadan once ince tabaka halinde, 10 gun.", Warning = "Uygulama sonrasi agiz calkalanmaz." },
        new() { Id = "med-hassas", Name = "Hassasiyet giderici macun", Category = "Destek", DefaultUsage = "Gunde 2 kez fircalama, en az 2 hafta.", Warning = "Sikayet artarsa kontrol randevusu gerekir." },
        new() { Id = "med-muko", Name = "Mukozal yara jeli", Category = "Yara bakimi", DefaultUsage = "Yemeklerden sonra bolgeye ince tabaka, 5 gun.", Warning = "Yutulmadan lokal uygulanir." },
        new() { Id = "med-pro", Name = "Probiyotik destek", Category = "Destek", DefaultUsage = "Antibiyotik saatinden 2 saat sonra, gunde 1 kez.", Warning = "Bagisiklik baskilanmasinda hekime danisilmalidir." }
    ];

    private static Appointment Appointment(string id, string patientId, string doctorId, string requesterId, int dayOffset, int hour, AppointmentStatus status, string complaint, string notes, string? approvedBy) => new()
    {
        Id = id,
        PatientId = patientId,
        DoctorUserId = doctorId,
        RequestedByUserId = requesterId,
        ApprovedByUserId = approvedBy,
        RequestedAt = DateTime.Today.AddDays(dayOffset - 2).AddHours(9),
        StartsAt = DateTime.Today.AddDays(dayOffset).AddHours(hour),
        DurationMinutes = 30,
        Status = status,
        Complaint = complaint,
        Notes = notes
    };

    private static Prescription Prescription(string id, string patientId, string doctorId, int dayOffset, string topic, string diagnosis, List<string> meds, string note)
    {
        var catalog = CreateMedicationCatalog();
        var selected = catalog.Where(med => meds.Contains(med.Id)).ToList();
        return new Prescription
        {
            Id = id,
            PatientId = patientId,
            DoctorUserId = doctorId,
            Date = DateTime.Today.AddDays(dayOffset),
            Topic = topic,
            Diagnosis = diagnosis,
            MedicationIds = meds,
            Medicines = string.Join("; ", selected.Select(med => med.Name)),
            UsageInstructions = string.Join(Environment.NewLine, selected.Select(med => $"{med.Name}: {med.DefaultUsage}")),
            DoctorNote = note
        };
    }

    private static Radiograph Radiograph(string id, string patientId, string doctorId, int dayOffset, string region, string imagePath, string notes) => new()
    {
        Id = id,
        PatientId = patientId,
        DoctorUserId = doctorId,
        Date = DateTime.Today.AddDays(dayOffset),
        ToothRegion = region,
        ImagePath = imagePath,
        Notes = notes
    };

    private static TreatmentPlan Treatment(string id, string patientId, string doctorId, int dayOffset, string toothNo, string name, string description, bool completed) => new()
    {
        Id = id,
        PatientId = patientId,
        DoctorUserId = doctorId,
        Date = DateTime.Today.AddDays(dayOffset),
        ToothNo = toothNo,
        ProcedureName = name,
        Description = description,
        Completed = completed
    };

    private static List<SystemLog> CreateLogs(DataSnapshot snapshot)
    {
        var logs = new List<SystemLog>();
        foreach (var appointment in snapshot.Appointments)
        {
            logs.Add(Log(snapshot, appointment.RequestedByUserId, "Randevu Talebi", appointment.PatientId, appointment.DoctorUserId, $"{snapshot.PatientNameForSeed(appointment.PatientId)} randevu talebi olusturdu."));
            if (!string.IsNullOrWhiteSpace(appointment.ApprovedByUserId))
            {
                logs.Add(Log(snapshot, appointment.ApprovedByUserId, "Randevu Durumu", appointment.PatientId, appointment.DoctorUserId, $"Randevu durumu {appointment.Status} olarak guncellendi."));
            }
        }

        foreach (var prescription in snapshot.Prescriptions)
        {
            logs.Add(Log(snapshot, prescription.DoctorUserId, "Recete Yazildi", prescription.PatientId, prescription.DoctorUserId, $"{prescription.Topic} konusu icin recete kaydi olusturuldu."));
        }

        foreach (var treatment in snapshot.Treatments)
        {
            logs.Add(Log(snapshot, treatment.DoctorUserId, "Tedavi Notu", treatment.PatientId, treatment.DoctorUserId, $"{treatment.ProcedureName} tedavi notu eklendi."));
        }

        return logs.OrderByDescending(log => log.Timestamp).ToList();
    }

    private static SystemLog Log(DataSnapshot snapshot, string actorId, string action, string? patientId, string? doctorId, string description)
    {
        var actor = snapshot.Users.FirstOrDefault(user => user.Id == actorId) ?? snapshot.Users.First();
        return new SystemLog
        {
            Timestamp = DateTime.Now.AddMinutes(-snapshot.Logs.Count - Random.Shared.Next(5, 8000)),
            ActorUserId = actor.Id,
            ActorName = actor.FullName,
            ActorRole = actor.Role,
            ActionType = action,
            PatientId = patientId,
            DoctorUserId = doctorId,
            Description = description
        };
    }
}

internal static class SeedSnapshotExtensions
{
    public static string PatientNameForSeed(this DataSnapshot snapshot, string patientId) =>
        snapshot.Patients.FirstOrDefault(patient => patient.Id == patientId)?.FullName ?? "Hasta";
}
