# -*- coding: utf-8 -*-
import pandas as pd
import os
import re
import requests

# 8 Şehrimizin Wikipedia'daki birebir İngilizce resmi sayfa karşılıkları
cities_map = {
    "Lomza": "Łomża", 
    "Warsaw": "Warsaw",
    "Lisbon": "Lisbon",
    "Skopje": "Skopje",
    "Porto": "Porto",
    "Vilnius": "Vilnius",
    "Sofia": "Sofia",
    "Belgrade": "Belgrade"
}

def clean_and_tokenize(text):
    """HTML etiketlerini, Wikipedia başlıklarını temizler ve cümle cümle böler"""
    # HTML etiketlerini tamamen süpür
    text = re.sub(r'<[^>]+>', '', text)
    # == Başlıklar == gibi işaretleri temizle
    text = re.sub(r'==+.*?==+', '', text)
    # [1], [2] kaynak linklerini temizle
    text = re.sub(r'\[\d+\]', '', text)
    # Gereksiz boşlukları ve satır başlarını temizle
    text = text.replace("\n", " ").replace("\r", " ").strip()
    
    # NLP Cümle Bölücü (Nokta, soru işareti ve ünleme göre böler)
    sentences = re.split(r'(?<!\w\.\w.)(?<![A-Z][a-z]\.)(?<=\.|\?)\s', text)
    return [s.strip() for s in sentences if len(s.strip()) > 35]

def scrape_wikipedia_data():
    wiki_sentences_pool = []
    print("🌐 DIRECT WIKIPEDIA HTTP REST API ENGINE AKTİF. Tarama başlıyor...\n")
    
    # Wikipedia API ana URL'i
    api_url = "https://en.wikipedia.org/w/api.php"
    
    # Akademik kimlik doğrulaması
    headers = {
        'User-Agent': 'OptiErasmusAcademicBot/2.0 (zehra.okur@ogr.uludag.edu.tr)'
    }

    for city_key, wiki_title in cities_map.items():
        print(f"🚀 {city_key} için ham metinler indiriliyor...")
        
        # Wikipedia'dan tüm sayfa içeriğini düz metin (plain text) olarak isteyen parametreler
        params = {
            "action": "query",
            "format": "json",
            "prop": "extracts",
            "titles": wiki_title,
            "explaintext": True,  # HTML yerine düz metin getirir (Kritik!)
            "exlimit": "max"
        }
        
        try:
            response = requests.get(api_url, headers=headers, params=params, timeout=10)
            if response.status_code != 200:
                print(f"⚠️ {city_key} için sunucu hatası: {response.status_code}")
                continue
                
            data = response.json()
            pages = data.get("query", {}).get("pages", {})
            
            # Dinamik sayfa ID'sini yakalayıp metni çıkartıyoruz
            page_id = list(pages.keys())[0]
            if page_id == "-1":
                print(f"❌ {city_key} için Wikipedia sayfası bulunamadı.")
                continue
                
            full_text = pages[page_id].get("extract", "")
            
            if full_text:
                sentences = clean_and_tokenize(full_text)
                for sentence in sentences:
                    wiki_sentences_pool.append({
                        "City": city_key,
                        "Source": "Wikipedia_API",
                        "Comment": sentence
                    })
                print(f"✅ {city_key} başarıyla tamamlandı! {len(sentences)} akademik cümle havuzda.")
            else:
                print(f"⚠️ {city_key} için boş içerik döndü.")
                
        except Exception as e:
            print(f"⚠️ {city_key} taranırken beklenmedik bir ağ hatası oluştu: {e}")
            continue

    # Verileri Havuza Kaydetme (Append ve Deduplicate)
    if wiki_sentences_pool:
        df_new = pd.DataFrame(wiki_sentences_pool)
        df_new = df_new.drop_duplicates(subset=['Comment'])
        
        raw_file = 'targeted_raw_comments.csv'
        
        if os.path.exists(raw_file):
            df_old = pd.read_csv(raw_file)
            df_total = pd.concat([df_old, df_new], ignore_index=True)
            df_total = df_total.drop_duplicates(subset=['Comment'])
        else:
            df_total = df_new
            
        df_total.to_csv(raw_file, index=False, encoding='utf-8-sig')
        print(f"\n🔥 MÜKEMMEL SONUÇ!")
        print(f"📊 Yeni kümülatif veri havuzun şu an tam {len(df_total)} satıra yükseldi!")
    else:
        print("❌ Wikipedia REST API'den veri süzülemedi. Lütfen bağlantınızı kontrol edin.")

if __name__ == "__main__":
    scrape_wikipedia_data()