# -*- coding: utf-8 -*-
import pandas as pd
from googleapiclient.discovery import build
import time

# 1. API Ayarı
YOUTUBE_API_KEY = "AIzaSyBCyB6OoPyRYDNMWJKmPFTYv5hqB1zBXvU"
youtube = build('youtube', 'v3', developerKey=YOUTUBE_API_KEY)


target_videos = [
    {"City": "Lomza", "VideoId": "V4DLlFN9pOQ"},
    {"City": "Warsaw", "VideoId": "CkZdOLTiKeY"},
    {"City": "Lisbon", "VideoId": "6fRJ3dNlqNA"},
    {"City": "Skopje", "VideoId": "EH3d0-kE08M"},
    {"City": "Porto", "VideoId": "AfU58NV5xZQ"},
    {"City": "Vilnius", "VideoId": "C5IlWttyOdQ"},
    {"City": "Sofia", "VideoId": "0FwFRNWoicc"},
    {"City": "Belgrade", "VideoId": "R735Cnkyx2w"},

    # Diğer şehirler için de ekleyebilirsin...
]

# Filtreleme kelimeleri
erasmus_keywords = [
    # Erasmus & education
    'erasmus', 'erasmus+', 'exchange student', 'study abroad',
    'international student', 'mobility program', 'student exchange',
    'university', 'faculty', 'department', 'course', 'credits',
    'ects', 'lecture', 'exam', 'professor', 'education system',
    'academic calendar', 'semester', 'master degree', 'bachelor degree',

    # Campus life
    'campus', 'student life', 'social life', 'student club',
    'library', 'cafeteria', 'canteen', 'gym', 'sports center',
    'student activities', 'orientation week', 'internship',

    # Accommodation
    'dorm', 'dormitory', 'student housing', 'accommodation',
    'apartment', 'flat', 'shared flat', 'room', 'studio apartment',
    'rent', 'rental price', 'housing cost', 'utilities', 'wifi',
    'electricity bill', 'water bill', 'heating',

    # Cost of living
    'living cost', 'cost of living', 'cheap city', 'expensive city',
    'monthly expenses', 'budget', 'food prices', 'transportation cost',
    'grocery', 'market prices', 'restaurant prices', 'student discount',

    # Transportation
    'public transport', 'metro', 'bus', 'tram', 'bike rental',
    'transport card', 'walkability', 'airport',

    # City & lifestyle
    'city', 'city life', 'nightlife', 'safety', 'safe city',
    'weather', 'climate', 'culture', 'language barrier',
    'english speaking', 'local people', 'international community',
    'tourism', 'events', 'festivals',

    # Student opinions & experiences
    'review', 'experience', 'student review', 'erasmus experience',
    'recommendation', 'pros and cons', 'best university',
    'worst university', 'ranking', 'university ranking',

    # Career & opportunities
    'career opportunities', 'part time job', 'student job',
    'internship opportunities', 'networking', 'research opportunities',

    # Digital / online sources
    'reddit', 'youtube', 'discord', 'facebook group',
    'telegram group', 'student forum', 'blog', 'vlog'
]
def scrape_targeted_videos():
    new_comments = []
    
    for item in target_videos:
        city = item["City"]
        v_id = item["VideoId"]
        print(f"-> {city} için {v_id} ID'li videonun yorumları çekiliyor...")
        
        try:
            request = youtube.commentThreads().list(
                part="snippet",
                videoId=v_id,
                maxResults=100,
                textFormat="plainText"
            ).execute()

            for c_item in request.get("items", []):
                text = c_item["snippet"]["topLevelComment"]["snippet"]["textDisplay"]
                text_lower = text.lower()
                
                # "Nice video" gibi boş övgüleri ve alakasızları daha çekerken ele!
                if any(word in text_lower for word in erasmus_keywords) and "nice video" not in text_lower:
                    new_comments.append({
                        "City": city,
                        "Source": "YouTubeTargeted",
                        "Comment": text.strip()
                    })
        except Exception as e:
            print(f"❌ {city} videosunda hata oluştu (Yorumlar kapalı olabilir): {e}")
            continue
            
    # Verileri kaydetme (Yol karmaşası olmaması için bağımlı yol yerine net '../wwwroot' kullandık)
    df = pd.DataFrame(new_comments)
    df = df.drop_duplicates(subset=['Comment'])
    
    # Başındaki ../wwwroot/data/ kısmını tamamen sildik!
    df.to_csv('targeted_raw_comments.csv', index=False, encoding='utf-8-sig')
    print(f"\n✅ İşlem Tamam! {len(df)} adet nokta atışı yorum 'targeted_raw_comments.csv' dosyasına yazıldı.")

if __name__ == "__main__":
    scrape_targeted_videos()