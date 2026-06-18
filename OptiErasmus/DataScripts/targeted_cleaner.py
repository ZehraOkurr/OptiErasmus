# -*- coding: utf-8 -*-
import os
import pandas as pd
from textblob import TextBlob

def process_sentiment():
    # Python scriptinin çalıştığı klasörün tam yolunu al (C:\...\DataScripts)
    current_dir = os.path.dirname(os.path.abspath(__file__))
    
    # Aranacak ve kaydedilecek dosyaların tam konumlarını eşitle
    input_file = os.path.join(current_dir, 'targeted_raw_comments.csv')
    final_comments_file = os.path.join(current_dir, 'social_comments_final.csv')
    final_scores_file = os.path.join(current_dir, 'city_social_scores.csv')

    # 1. Dosya kontrolü ve okuma
    if not os.path.exists(input_file):
        # Eğer hâlâ bulamazsa bir üst klasöre de baksın (Hata koruması)
        parent_dir = os.path.dirname(current_dir)
        input_file = os.path.join(parent_dir, 'targeted_raw_comments.csv')
        
        if not os.path.exists(input_file):
            print(f"❌ '{input_file}' konumunda ham veri dosyası bulunamadı!")
            print("💡 İpucu: Lütfen bilgisayarında 'targeted_raw_comments.csv' dosyasının nereye oluştuğunu kontrol et.")
            return

    print(f"🔄 Veri başarıyla okundu. Konum: {input_file}")
    df = pd.read_csv(input_file)

    if df.empty:
        print("⚠️ Ham veri dosyası boş! Filtre kelimelerine uygun yorum çekilememiş olabilir.")
        return

    # 2. Duygu analizi yap ve etiketle (NLP Sınıflandırması)
    def get_label(text):
        analysis = TextBlob(str(text))
        score = analysis.sentiment.polarity
        if score > 0.15: return "Olumlu"
        elif score < -0.15: return "Olumsuz"
        else: return "Nötr"

    df['Sentiment'] = df['Comment'].apply(get_label)

    # 3. Şehir bazlı nihai Sosyal Tatmin Skorlarını oluştur
    scores = df.groupby('City')['Sentiment'].apply(lambda x: (x == 'Olumlu').sum() / len(x) * 100).reset_index()
    scores.columns = ['City', 'SocialSatisfactionScore']

    # 4. Sonuçları tam yollarına kaydet
    df.to_csv(final_comments_file, index=False, encoding='utf-8-sig')
    scores.to_csv(final_scores_file, index=False, encoding='utf-8-sig')
    
    print("\n✅ Muhteşem! Temizlik ve NLP sınıflandırması bitti.")
    print(f"📋 Skorlar başarıyla yazıldı: {final_scores_file}")
    print(scores)

if __name__ == "__main__":
    process_sentiment()