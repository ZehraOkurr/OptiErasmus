import requests
from bs4 import BeautifulSoup
import pandas as pd
import time
import os

# Şehir listesi (Anlaşmalı üniversitelerin olduğu şehirler)
cities = ["Lomza", "Warsaw", "Lisbon", "Sofia", "Skopje", "Vilnius", "Belgrade", "Porto"]

def scrape_numbeo(city_name):
    url = f"https://www.numbeo.com/cost-of-living/in/{city_name}"
    headers = {'User-Agent': 'Mozilla/5.0'}
    
    print(f"-> {city_name} verileri toplanıyor...")
    try:
        response = requests.get(url, headers=headers)
        if response.status_code == 200:
            soup = BeautifulSoup(response.content, 'html.parser')
            data = []
            table = soup.find('table', {'class': 'data_wide_table'})
            if table:
                for row in table.find_all('tr'):
                    cols = row.find_all('td')
                    if len(cols) > 1:
                        data.append({
                            "City": city_name,
                            "Criteria": cols[0].text.strip(),
                            "Price": cols[1].text.strip()
                        })
            return data
    except Exception as e:
        print(f"Hata oluştu ({city_name}): {e}")
    return []

# Veri toplama işlemi
final_results = []
for city in cities:
    res = scrape_numbeo(city)
    final_results.extend(res)
    time.sleep(1) # Engellenmemek için kısa bir bekleme

# Klasör kontrolü ve Kaydetme
if not os.path.exists('RawData'):
    os.makedirs('RawData')

df = pd.DataFrame(final_results)
raw_path = '../RawData/economic_data.csv'
df.to_csv(raw_path, index=False, encoding='utf-8-sig')
print(f"\nHam veri kaydedildi: {raw_path}")

from clean_all_data import clean_economic_data
clean_economic_data()
print("Fiyatlar temizlendi ve EUR'a çevrildi.")