# CCETY Dis Klinigi Yonetim Sistemi

CCETY Dis Klinigi Yonetim Sistemi, C# Windows Forms ile gelistirilmis rol bazli bir klinik otomasyon projesidir. Uygulama; admin, doktor, sekreter ve hasta panelleri uzerinden hasta dosyalarini, randevu sureclerini, receteleri, rontgen kayitlarini, tedavi planlarini ve sistem loglarini tek merkezden yonetir.

## Proje Amaci

Bu proje, bir dis kliniginde farkli kullanici rollerinin ihtiyac duydugu temel operasyonlari masaustu uygulamasi uzerinden modellemek icin hazirlanmistir. Hasta randevu talebi olusturabilir, doktor ve sekreterler randevu akislarini yonetebilir, doktorlar klinik kayitlari guncelleyebilir, admin ise sistem genelini ve loglari takip edebilir.

## Temel Ozellikler

- Rol bazli giris sistemi
- Admin, doktor, sekreter ve hasta icin ayri panel akislari
- Hasta dosyasi goruntuleme ve hasta bilgisi duzenleme
- Randevu talebi, onaylama, reddetme ve durum takibi
- Doktor ve sekreter tarafindan randevu akis yonetimi
- Recete olusturma ve recete gecmisi takibi
- Rontgen kayitlari ve mock rontgen arsivi
- Tedavi planlari ve klinik notlar
- Sistem loglari ve kullanici hareketleri
- Lokal JSON veri saklama
- Supabase ile bulut senkronizasyonu

## Kullanici Rolleri

### Admin

Admin kullanicisi sistemin genel kontrol paneline erisir. Hasta merkezi, randevu akisi, ekip profilleri, sistem loglari ve Supabase ayarlari admin panelinden yonetilebilir.

### Doktor

Doktor kullanicisi kendisine ait hasta dosyalarini, randevulari, receteleri, rontgenleri ve tedavi sureclerini takip eder. Klinik notlar ve tedavi kayitlari doktor akisinda olusturulur.

### Sekreter

Sekreter kullanicisi randevu akislarini takip eder, hasta taleplerini yonetir ve doktorlara bagli operasyonel surecleri destekler.

### Hasta

Hasta kullanicisi kendi klinik dosyasini, yaklasan randevularini, recetelerini, rontgenlerini ve tedavi bilgilerini gorur. Hasta kendi bilgilerini duzenleyebilir ve randevu talebi olusturabilir.

## Demo Giris Bilgileri

| Rol | Kullanici Adi | Sifre |
| --- | --- | --- |
| Admin | `admin` | `123456` |
| Doktor | `doktor`, `doktor2`, `doktor3`, `doktor4` | `123456` |
| Sekreter | `sekreter`, `sekreter2`, `sekreter3`, `sekreter4` | `123456` |
| Hasta | `hasta`, `hasta2`, `hasta3`, `hasta4`, `hasta5`, `hasta6` | `123456` |

## Veri Yonetimi

Uygulama varsayilan olarak lokal JSON dosyasi ile calisir. Ilk acilista demo klinik verileri uretilir ve `App_Data/clinic-data.json` dosyasina yazilir. Bu sayede uygulama internet baglantisi olmadan da calisabilir.

Supabase ayarlari yapildiginda uygulama acilista buluttaki veriyi okur. Uygulama icinde yapilan kayit degisiklikleri hem lokal JSON dosyasina hem de Supabase tablosuna yazilir. Boylece farkli bilgisayarlarda ayni Supabase projesi kullanilarak ortak veriyle calisilabilir.

## Supabase Entegrasyonu

Supabase entegrasyonu tek bir JSONB tablo uzerinden calisir. Uygulamanin tum klinik verisi `clinic_data` tablosundaki `payload` alaninda saklanir.

Supabase kurulumu icin:

1. Supabase'de yeni bir proje olusturun.
2. SQL Editor ekraninda `DisKlinigiYonetimSistemi/supabase-schema.sql` dosyasindaki SQL kodunu calistirin.
3. Uygulamaya `admin` kullanicisiyle girin.
4. Sol menuden `Supabase Ayarlari` ekranini acin.
5. Supabase Project URL ve anon public key bilgisini girin.
6. `Baglantiyi Test Et` ile kontrol edin.
7. Lokal demo verisini buluta aktarmak icin `Buluta Gonder` butonunu kullanin.
8. Baska bilgisayarda ayni ayarlari girip `Buluttan Cek` ile veriyi indirebilirsiniz.

> Not: Bu proje okul/demo senaryosu icin anon key ve acik RLS politikalariyla hazirlanmistir. Gercek bir canli sistemde Supabase Auth, kullanici bazli RLS politikalar ve service role key'i istemci uygulamaya koymayan guvenli bir backend tercih edilmelidir.

## Kurulum

Gereksinimler:

- Windows
- .NET 9 SDK
- Visual Studio 2022 veya `dotnet` CLI

Projeyi klonlayin:

```bash
git clone https://github.com/hsankc/Dental-Clinic-System.git
cd Dental-Clinic-System
```

Projeyi derleyin:

```bash
dotnet build DisKlinigiYonetimSistemi.sln
```

Uygulamayi calistirin:

```bash
dotnet run --project DisKlinigiYonetimSistemi/DisKlinigiYonetimSistemi.csproj
```

## Release / Publish

Windows icin dagitilabilir cikti almak:

```powershell
dotnet publish .\DisKlinigiYonetimSistemi\DisKlinigiYonetimSistemi.csproj -c Release -r win-x64 --self-contained true
```

Publish ciktisi su klasore olusur:

```text
DisKlinigiYonetimSistemi/bin/Release/net9.0-windows/win-x64/publish
```

Bu klasor ziplenerek baska Windows bilgisayarlara tasinabilir.

## Proje Yapisi

```text
DisKlinigiYonetimSistemi/
  Assets/                 Giris ekrani gorselleri
  Controls/               Ortak modern UI kontrolleri
  Data/                   Veri deposu, demo veri ve Supabase istemcisi
  Forms/                  Login, ana panel ve editor ekranlari
  Models/                 Kullanici, hasta, randevu ve klinik modelleri
  supabase-schema.sql     Supabase tablo ve RLS kurulumu
```

## Teknik Detaylar

- Dil: C#
- Framework: .NET 9
- Arayuz: Windows Forms
- Veri formati: JSON
- Bulut veri tabani: Supabase REST API
- Mimari: Lokal cache + opsiyonel bulut senkronizasyonu

## Lisans

Bu proje egitim ve odev amacli hazirlanmistir.
