# 🎓 OptiErasmus

[![Build Status](https://img.shields.io/github/actions/workflow/status/username/repo/ci.yml?branch=main&style=for-the-badge&logo=github&logoColor=white)](https://github.com/username/repo)
[![License](https://img.shields.io/github/license/username/repo?style=for-the-badge&color=blue)](LICENSE)
[![Issues](https://img.shields.io/github/issues/username/repo?style=for-the-badge&color=red)](https://github.com/username/repo/issues)
[![Stars](https://img.shields.io/github/stars/username/repo?style=for-the-badge&color=yellow)](https://github.com/username/repo/stargazers)
[![.NET Version](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Python Version](https://img.shields.io/badge/Python-3.10%2B-3776AB?style=for-the-badge&logo=python&logoColor=white)](https://www.python.org/)

OptiErasmus, üniversite öğrencilerinin en ideal Erasmus değişim destinasyonlarını seçmelerine yardımcı olmak amacıyla geliştirilmiş, veri odaklı ve profesyonel bir **Çok Kriterli Karar Verme (MCDM)** öneri sistemi ve gösterge panelidir (dashboard). Güvenlik, yaşam maliyeti, akademik başarı ve sosyal yorumları matematiksel olarak analiz ederek, öğrencilere kişiselleştirilmiş ve uzlaşık-optimal destinasyon sıralaması sunar.

---

## 📌 İçindekiler
- [🎓 OptiErasmus](#-optierasmus)
  - [📌 İçindekiler](#-i̇çindekiler)
  - [🌟 Projenin Amacı](#-projenin-amacı)
  - [✨ Temel Özellikler](#-temel-özellikler)
  - [🚀 Öğrenciler İçin Faydaları](#-öğrenciler-i̇çin-faydaları)
  - [🛠️ Teknoloji Yığını ve Mimari](#️-teknoloji-yığını-ve-mimari)
  - [📈 Veri Toplama ve ETL Süreçleri](#-veri-toplama-ve-etl-süreçleri)
  - [📥 Kurulum ve Çalıştırma](#-kurulum-ve-çalıştırma)
    - [Gereksinimler](#gereksinimler)
    - [Adım Adım Kurulum](#adım-adım-kurulum)
  - [💡 Kullanım Kılavuzu](#-kullanım-kılavuzu)
  - [📁 Proje Yapısı](#-proje-yapısı)
  - [🔌 API Entegrasyonları](#-api-entegrasyonları)
  - [📸 Ekran Görüntüleri ve Arayüz Önizlemesi](#-ekran-görüntüleri-ve-arayüz-önizlemesi)
  - [🗺️ Yol Haritası](#️-yol-haritası)
  - [🤝 Katkıda Bulunma](#-katkıda-bulunma)
  - [📄 Lisans](#-lisans)
  - [📧 İletişim](#-iletişim)

---

## 🌟 Projenin Amacı

Erasmus destinasyonu seçmek, bir öğrencinin üniversite hayatındaki en kritik kararlardan biridir. Çoğu zaman öğrenciler kulaktan dolma bilgilere veya internet üzerindeki rastgele yorumlara dayanarak karar vermeye çalışırlar.

**OptiErasmus**, bu süreci bilimsel ve veri odaklı bir temele oturtur. Gerçek dünya verilerini toplayarak Çok Kriterli Karar Verme algoritmalarıyla analiz eder:
1. **Güvenlik ve Emniyet**: Dünya genelindeki haber kaynaklarını izleyen **GDELT Project** veritabanındaki haber olaylarının Goldstein ve Tone (duyarlılık) skorları kullanılarak asayiş durumu incelenir.
2. **Ekonomik Faktörler**: **Numbeo** veritabanından çekilen yaşam maliyetleri ve konaklama/kira fiyatları, anlık döviz kurları ile dinamik olarak Türk Lirası'na (TRY) dönüştürülür.
3. **Akademik Kalite**: **Scopus** API üzerinden partner üniversitelerin yıllık yayın sayıları analiz edilerek akademik başarıları ölçülür.
4. **Sosyal Duyarlılık (NLP)**: Wikipedia, YouTube incelemeleri, Kaggle yorum havuzları ve geçmiş Erasmus mezun anketleri üzerinden NLP duygu analizi yapılarak şehirlerin sosyal profilleri çıkarılır.

---

## ✨ Temel Özellikler

- **🧠 BWM (Best-Worst Method) Tercih Anketi**: Öğrencinin bütçe, güvenlik, akademik başarı ve sosyal yaşam gibi kriterler arasındaki önceliklerini 3 adımda belirler. En iyi ve en kötü kriterlerin diğerlerine göre önem derecelerini alarak tutarlı ağırlık katsayılarını hesaplar.
- **⚡ VIKOR Karar Motoru**: Hesaplanan ağırlıklara dayanarak partner şehirleri (Lomza, Varşova, Lizbon, Porto, Sofya, Üsküp, Vilnius, Belgrad) analiz eder ve uzlaşık-optimal sıralamayı sunar ($Q_i$ uzlaşı değerleri).
- **📊 Gerçek Zamanlı Gösterge Paneli**: ApexCharts.js grafikleriyle bütçe dağılımlarını, asayiş puanlarını, akademik başarıları ve sosyal memnuniyet oranlarını görselleştirir.
- **💸 Canlı Döviz Kuru Entegrasyonu**: ExchangeRate-API ile Avrupa şehirlerindeki yaşam maliyetlerini anlık olarak TRY'ye çevirerek Türk öğrencilere bütçe planlamasında netlik kazandırır.
- **📄 PDF Karar Raporu**: DinkToPdf ve wkhtmltopdf kullanarak öğrencinin anket sonuçlarını ve karşılaştırma analizlerini içeren profesyonel bir PDF raporu üretir.
- **🐍 Python ETL Kütüphanesi**: Numbeo, Scopus, YouTube ve Wikipedia'dan veri çeken ve temizleyen hazır otomasyon scriptleri içerir.

---

## 🚀 Öğrenciler İçin Faydaları

- **Bilimsel Kararlar**: Sübjektif yorumlar yerine matematiksel kanıtlara dayalı tercihler yapmanızı sağlar.
- **Kişiselleştirilmiş Sonuçlar**: Kriter ağırlıklarınızı değiştirerek size en uygun şehri bulun (örn. akademik odaklı ya da bütçe odaklı).
- **Mali Netlik**: Canlı döviz dönüşümleri sayesinde beklenmedik harcama sürprizlerinin önüne geçin.
- **Bütünleşik Analiz**: Şehrin asayişinden, üniversitenin yayın başarısına, konaklama maliyetlerinden mezun memnuniyetine kadar her detayı tek ekranda inceleyin.

---

## 🛠️ Teknoloji Yığını ve Mimari

- **Backend**: C# 13, .NET 10.0 ASP.NET Core (Razor Pages)
- **Frontend**: HTML5, CSS3 (Cam Tasarım - Glassmorphic Arayüz), ES6 JavaScript, [Bootstrap 5](https://getbootstrap.com/), [ApexCharts.js](https://apexcharts.com/)
- **Veri Kazıma ve ETL**: Python 3.10+, Pandas, BeautifulSoup4, Scrapy
- **Karar Algoritmaları**: C# ile yazılmış yerel Best-Worst Method (BWM) ve VIKOR algoritma motorları
- **PDF Motoru**: DinkToPdf (wkhtmltopdf C# wrapper)

---

## 📈 Veri Toplama ve ETL Süreçleri

Proje, `DataScripts/` dizini altında konumlanan zengin bir Python altyapısına sahiptir:
* **`numbeo_scraper.py`**: Numbeo üzerinden yaşam maliyeti indekslerini çeker.
* **`scopus_academic.py`**: Elsevier Scopus API aracılığıyla üniversitelerin yayın sayılarını çeker.
* **`youtube_targeted_scraper.py`** & **`wikipedia_mega_scraper.py`**: Öğrenci deneyimlerini, yorumlarını ve şehirlerin kültürel profillerini toplar.
* **`clean_all_data.py`** & **`targeted_cleaner.py`**: Ham verileri temizler, yapılandırır ve doğrudan web uygulamasının `RawData/` klasörüne aktarır.

---

## 📥 Kurulum ve Çalıştırma

### Gereksinimler
OptiErasmus'u bilgisayarınızda çalıştırmak için aşağıdaki araçların kurulu olması gerekir:
- [.NET 10.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- [Python 3.10+](https://www.python.org/downloads/)
- [Node.js & npm](https://nodejs.org/)
- [wkhtmltopdf kütüphanesi](https://wkhtmltopdf.org/) (DinkToPdf kütüphanesinin PDF üretebilmesi için gereklidir)

### Adım Adım Kurulum

1. **Depoyu Klonlayın**
   ```bash
   git clone https://github.com/username/OptiErasmus.git
   cd OptiErasmus
   ```

2. **Döviz Kuru API Anahtarını Yapılandırın**
   `.env` dosyası oluşturun veya projenin `appsettings.json` dosyasına API anahtarınızı girin:
   ```json
   {
     "ExchangeRateApiKey": "api_anahtariniz_buraya"
   }
   ```

3. **Python ETL Kütüphanelerini Yükleyin**
   ```bash
   pip install pandas beautifulsoup4 requests lxml
   ```

4. **Verileri Güncelleyin (İsteğe Bağlı)**
   Eğer kazıyıcıları çalıştırıp verileri sıfırdan çekmek isterseniz:
   ```bash
   cd OptiErasmus/DataScripts
   python clean_all_data.py
   cd ..
   ```

5. **Uygulamayı Derleyin ve Çalıştırın**
   ```bash
   dotnet restore
   dotnet build
   dotnet run --project OptiErasmus
   ```
   Tarayıcınızı açın ve `https://localhost:5001` veya `http://localhost:5000` adresine gidin.

---

## 💡 Kullanım Kılavuzu

### 1. Şehirleri İncele (`/Rehber`)
Anlaşmalı üniversiteler, maliyet göstergeleri, güvenlik düzeyleri ve sosyal puanlar hakkında genel bilgileri okuyun.

### 2. Tercih Anketini Tamamla (`/Anket`)
Kriterlerinizin önem derecelerini belirleyin:
- **En İyi (En Önemli)** kriterinizi seçin (Örn: *Yaşam Maliyeti*).
- **En Kötü (En Önemsiz)** kriterinizi seçin (Örn: *Sosyal Yaşam*).
- 1'den 9'a kadar olan ölçekte kriterlerinizi birbiriyle kıyaslayın. Sistem sizin için tutarlı ağırlıkları otomatik hesaplar.

### 3. Kıyasla & Sırala (`/Compare`)
VIKOR optimizasyonunu çalıştırın:
- Anketten gelen ağırlıklarınızı kontrol edin.
- Sıralamaya sokmak istediğiniz aday şehirleri seçin.
- **"Uzlaşık Sıralamayı Hesapla"** butonuna basarak en yakın alternatiften uzağa doğru sıralanmış tabloyu ($Q_i$ skorları ile) görüntüleyin.

### 4. PDF Rapor Al
**"Raporu Dışa Aktar"** butonuna tıklayarak yaptığınız MCDM analizini şık bir PDF formatında bilgisayarınıza kaydedin.

---

## 📁 Proje Yapısı

```
OptiErasmus/
│
├── OptiErasmus.slnx          # .NET Çözüm yapısı dosyası
│
└── OptiErasmus/              # Ana Web Projesi
    ├── DataScripts/          # Python veri kazıma ve ön işleme scriptleri
    │   ├── RawData/          # ETL scriptlerinin çıktı dizini
    │   └── *.py              # Python veri scriptleri
    │
    ├── Models/               # C# Veri modelleri ve ViewModeller
    │   ├── AlumniSurveyModels.cs
    │   ├── CityResultModel.cs
    │   └── DashboardDataModels.cs
    │
    ├── Pages/                # ASP.NET Core Razor Pages
    │   ├── Index.cshtml      # Ana Sayfa / Tanıtım
    │   ├── Dashboard.cshtml  # Gösterge Paneli ve Veri Analizi
    │   ├── Anket.cshtml      # BWM Ağırlık Sihirbazı
    │   ├── Compare.cshtml    # VIKOR Karşılaştırma Sayfası
    │   └── Shared/           # Ortak düzenler ve kısmi sayfalar
    │
    ├── Services/             # İş Mantığı & Algoritmalar
    │   ├── AlumniSurveyService.cs
    │   ├── DashboardDataService.cs   # Veri okuma ve dinamik istatistikler
    │   └── VikorService.cs           # VIKOR karar algoritması
    │
    ├── RawData/              # Web uygulaması tarafından okunan temiz CSV dosyaları
    ├── wwwroot/              # CSS, JS, görseller ve yazı tipleri (Fonts)
    ├── Program.cs            # ASP.NET Core başlangıç ayarları
    └── OptiErasmus.csproj    # Bağımlılıklar ve NuGet paketleri
```

---

## 🔌 API Entegrasyonları

- **ExchangeRate-API**: EUR, PLN, RSD, MKD para birimlerini TRY'ye dönüştürmek için kullanılır. Döviz dalgalanmalarına karşı bütçenin doğru hesaplanmasını sağlar.
- **GDELT Olay Veritabanı**: Şehirlerin asayiş ve sosyal barış puanlarını ölçmek amacıyla, geçmiş haber verilerinden derlenmiş güvenlik skorları entegre edilmiştir.

---

## 📸 Ekran Görüntüleri ve Arayüz Önizlemesi

*OptiErasmus portal arayüzünden görsel kesitler:*

| **Özellik / Sayfa** | **Önizleme Görseli** |
|---|---|
| **Yönetici Gösterge Paneli (Dashboard)** | ![Gösterge Paneli Arayüzü](<img width="828" height="777" alt="göstergepaneli" src="https://github.com/user-attachments/assets/e10a1ca7-2253-4597-96a8-cbed03a79742" />![Uploading anket1.png…]()
) |
| **BWM Tercih Anketi - Adım 1 (Kriter İnceleme)** | ![BWM Tercih Anketi Adım 1](images/anket1.png) |
| **BWM Tercih Anketi - Adım 2 (Öncelik Seçimi)** | ![BWM Tercih Anketi Adım 2](images/anket2.png) |
| **BWM Tercih Anketi - Adım 3 (Kıyaslama ve Puanlama)** | ![BWM Tercih Anketi Adım 3](images/anket3.png) |

---

## 🗺️ Yol Haritası

- [ ] **Aşama 1: İleri NLP Analizi**
  - Hazır indeksler yerine, öğrenci yorumlarını anlık analiz eden Python tabanlı BERT modeli entegrasyonu.
- [ ] **Aşama 2: Lokasyon Genişletme**
  - Veri tabanını 8 şehirden 100+ Avrupa Erasmus ortağı konumuna yükseltmek.
- [ ] **Aşama 3: Gerçek Zamanlı GDELT Akış Entegrasyonu**
  - Statik dosyalar yerine doğrudan GDELT API'ye bağlanıp anlık güvenlik güncellemeleri çekmek.
- [ ] **Aşama 4: Kullanıcı Kimlik Doğrulama**
  - Öğrenci girişi ekleyerek anket ağırlıklarını kaydetmek, şehirleri favorilere eklemek ve geçmiş karşılaştırmaları saklamak.

---

## 🤝 Katkıda Bulunma

Açık kaynak topluluğuna katkıda bulunmak, öğrenmek ve ilham vermek için harika bir yoldur. Katkılarınız **büyük bir memnuniyetle karşılanır**.

1. Projeyi Fork edin.
2. Özellik dalı oluşturun (`git checkout -b feature/AmazingFeature`).
3. Değişikliklerinizi commit edin (`git commit -m 'Add some AmazingFeature'`).
4. Dalınızı push edin (`git push origin feature/AmazingFeature`).
5. Bir Pull Request (Çekme İsteği) açın.

---

## 📄 Lisans

Bu proje MIT Lisansı altında dağıtılmaktadır. Daha fazla bilgi için `LICENSE` dosyasına göz atabilirsiniz.

---

## 📧 İletişim

**Proje Yöneticisi** - [E-posta Adresiniz](mailto:email@example.com)

Proje Bağlantısı: [https://github.com/username/OptiErasmus](https://github.com/username/OptiErasmus)
