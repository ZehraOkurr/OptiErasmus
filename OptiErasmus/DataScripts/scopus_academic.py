# -*- coding: utf-8 -*-
import requests
import pandas as pd
import time

API_KEY = "bbc64e089b30564538ef4ac247d3aa8e"

# Erasmus partner universities (Scopus affiliation IDs)
universities = {
    "Lomza State University of Applied Sciences": "60110375",
    "Vistula University": "60105342",
    "Universidade de Lisboa": "60008525",
    "University of Porto": "60017393",
    "University of Telecommunications and Post": "60028826",
    "Ss. Cyril and Methodius University": "60108663",
    "Vilnius University": "60000936",
    "University of Belgrade": "60025543",
}

def get_scopus_stats(affil_id):
    url = f"https://api.elsevier.com/content/search/scopus?query=AF-ID({affil_id})&apiKey={API_KEY}"
    headers = {'Accept': 'application/json'}

    try:
        response = requests.get(url, headers=headers)
        if response.status_code == 200:
            data = response.json()
            return int(data.get('search-results', {}).get('opensearch:totalResults', 0))
    except Exception as e:
        print(f"Hata: {e}")
    return 0

results = []
for name, affil_id in universities.items():
    print(f"-> {name} akademik verileri cekiliyor...")
    pub_count = get_scopus_stats(affil_id)
    results.append({"University": name, "PublicationCount": pub_count})
    time.sleep(1)

df = pd.DataFrame(results)
output_path = '../RawData/academic_quality.csv'
df.to_csv(output_path, index=False, encoding='utf-8-sig')
print(f"\n[BİLGİ] Akademik veriler '{output_path}' dosyasina kaydedildi.")
