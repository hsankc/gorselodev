# Diş Kliniği Yönetim Sistemi

Visual Studio / C# WinForms final projesi. Uygulama ilk acilista demo verileriyle calisir ve veriyi `App_Data/clinic-data.json` dosyasinda saklar.

## Demo girisleri

- `admin` / `123456`
- `doktor`, `doktor2`, `doktor3`, `doktor4` / `123456`
- `sekreter`, `sekreter2`, `sekreter3` / `123456`
- `hasta`, `hasta2`, `hasta3`, `hasta4`, `hasta5`, `hasta6` / `123456`

## Moduller

- Rol bazli giris ekrani
- Hasta kayit ve hasta portali
- Hasta tarafından randevu talebi
- Doktor veya sekreter tarafından randevu onay/red akışı
- Doktor tarafından reçete yazma ve geçmiş reçete takibi
- Mock rontgen gorselleri ile rontgen arsivi
- Tedavi planlari ve klinik notlar
- Rol bazli menuler: hasta, doktor, sekreter, admin

## Veri

Uygulama yerel JSON cache ile calisir ve istenirse Supabase'e senkron olur. Internet yoksa son lokal veriyle acilir; Supabase ayari varsa acilista buluttan veriyi ceker, kayit degisikliklerinde lokal dosyayi ve Supabase'i birlikte günceller.

## Supabase kurulumu

1. Supabase'de yeni proje oluştur.
2. SQL Editor ekraninda `supabase-schema.sql` dosyasindaki kodu calistir.
3. Uygulamayi ac, `admin` kullanicisiyle gir ve sol menuden `Supabase Ayarlari` ekranini ac.
4. Project URL ve anon key gir.
5. Önce `Bağlantıyı Test Et`, sonra mevcut demo verisini buluta almak icin `Buluta Gönder` kullan.
6. Başka bilgisayarda aynı URL/key ile `Buluttan Çek` diyerek veriyi indir.

Not: Bu sema okul/demo kullanimi icin anon key ile acik RLS politikalari kullanir. Herkese dagitilacak gercek bir canli sistemde Supabase Auth, kullanici bazli RLS ve service role key'i uygulamanin icine koymayan bir backend gerekir.

## Publish

Windows icin tek klasor yayin almak:

```powershell
dotnet publish .\DisKlinigiYonetimSistemi.csproj -c Release -r win-x64 --self-contained true
```

Cikti `bin\Release\net9.0-windows\win-x64\publish` klasorune gelir. Bu klasoru hedef bilgisayara tasiyip exe'yi calistirabilirsin.
