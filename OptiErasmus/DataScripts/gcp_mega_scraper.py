# -*- coding: utf-8 -*-
import pandas as pd
from googleapiclient.discovery import build
import time

# 1. Google Cloud Console'dan aldığın API Key'i buraya yapıştır
YOUTUBE_API_KEY = "AIzaSyCg8naU5ID8t94MTf3wFKhZ_YecgxXLnmM" # Kendi taze anahtarınla değiştirebilirsin
youtube = build('youtube', 'v3', developerKey=YOUTUBE_API_KEY)

# 2. Hedef Şehirler ve YouTube'da aranacak en doğru Erasmus kombinasyonları
target_queries = {
    "Lomza": "Erasmus Lomza university student life",
    "Warsaw": "Erasmus Warsaw student vlogs",
    "Lisbon": "Erasmus Lisbon experience life",
    "Skopje": "Erasmus Skopje Macedonia student",
    "Porto": "Erasmus Porto student room rent",
    "Vilnius": "Erasmus Vilnius University lithuania",
    "Sofia": "Erasmus Sofia Bulgaria university",
    "Belgrade": "Erasmus Belgrade Serbia student travel"
}

def get_video_ids_for_query(query):
    """Her şehir araması için en popüler 5 videonun ID'sini toplar"""
    video_ids = []
    try:
        search_response = youtube.search().list(
            q=query,
            type="video",
            part="id",
            maxResults=5, # Her şehir için en iyi 5 videoya odaklan
            relevanceLanguage="en"
        ).execute()

        for item in search_response.get("items", []):
            if "videoId" in item["id"]:
                video_ids.append(item["id"]["videoId"])
    except Exception as e:
        print(f"⚠️ Arama hatası ({query}): {e}")
    return video_ids

def scrape_mega_comments():
    all_mega_comments = []

    for city, query in target_queries.items():
        print(f"🔍 {city} için Google Cloud üzerinden popüler videolar aranıyor...")
        video_ids = get_video_ids_for_query(query)
        print(f"📺 {city} için {len(video_ids)} adet kaynak video bulundu. Yorumlar çekiliyor...")

        for v_id in video_ids:
            try:
                comment_response = youtube.commentThreads().list(
                    part="snippet",
                    videoId=v_id,
                    maxResults=30, # Video başına 30 yorum al (Toplamda şehir başına ~150 yorum yapar)
                    textFormat="plainText"
                ).execute()

                for c_item in comment_response.get("items", []):
                    text = c_item["snippet"]["topLevelComment"]["snippet"]["textDisplay"]
                    
                    # Çok kısa veya anlamsız emojileri eliyoruz (Veri temizliği başlangıcı)
                    if len(text.strip()) > 10:
                        all_mega_comments.append({
                            "City": city,
                            "Source": "YouTube_GCP",
                            "Comment": text.replace("\n", " ").strip()
                        })
            except Exception:
                # Bazı videoların yorumları kapalı olabilir, patlamadan sonraki videoya geç
                continue
            time.sleep(0.2) # API koruması

    # 3. DataFrame Oluşturma ve Çift Kayıtları Temizleme
    df = pd.DataFrame(all_mega_comments)
    if not df.empty:
        df = df.drop_duplicates(subset=['Comment'])
        
        # Dosyayı doğrudan çalışılan klasöre güvenle yazıyoruz
        df.to_csv('targeted_raw_comments.csv', index=False, encoding='utf-8-sig')
        print(f"\n🚀 BAŞARILI! Toplam {len(df)} adet ham yorum 'targeted_raw_comments.csv' dosyasına kazındı.")
    else:
        print("❌ Maalesef hiçbir veri çekilemedi. API anahtarını veya kotaları kontrol edin.")

if __name__ == "__main__":
    scrape_mega_comments()