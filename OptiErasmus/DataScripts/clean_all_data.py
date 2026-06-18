# -*- coding: utf-8 -*-
"""
Tüm RawData CSV dosyalarını temizler:
- Emojiler, özel karakterler, fazla boşluklar
- Sosyal yorumlarda alakasız / yanlış şehir eşleşmeleri
- GDELT: sadece 8 hedef şehir
- Ekonomi: doğru fiyat parse + EUR dönüşümü (kira dahil)
"""
from __future__ import annotations

import os
import re
import unicodedata
from pathlib import Path

import pandas as pd

RAW_DIR = Path(__file__).resolve().parent.parent / "RawData"
DATA_SCRIPTS_DIR = Path(__file__).resolve().parent

VALID_CITIES = [
    "Lomza", "Warsaw", "Lisbon", "Porto", "Sofia", "Skopje", "Vilnius", "Belgrade"
]

CITY_NAME_ALIASES = {
    "Lomza": ["lomza", "łomża", "lomża"],
    "Warsaw": ["warsaw", "warszawa", "varsovia"],
    "Lisbon": ["lisbon", "lisboa"],
    "Porto": ["porto", "oporto"],
    "Sofia": ["sofia", "софия"],
    "Skopje": ["skopje", "скопје"],
    "Vilnius": ["vilnius", "vilna"],
    "Belgrade": ["belgrade", "beograd", "belgrad"],
}

CITY_CONTEXT_ALIASES = {
    **CITY_NAME_ALIASES,
    "Lomza": CITY_NAME_ALIASES["Lomza"] + ["poland", "polish"],
    "Warsaw": CITY_NAME_ALIASES["Warsaw"] + ["poland", "polish"],
    "Lisbon": CITY_NAME_ALIASES["Lisbon"] + ["portugal", "portuguese"],
    "Porto": CITY_NAME_ALIASES["Porto"] + ["portugal", "portuguese"],
    "Sofia": CITY_NAME_ALIASES["Sofia"] + ["bulgaria", "bulgarian"],
    "Skopje": CITY_NAME_ALIASES["Skopje"] + ["macedonia", "north macedonia"],
    "Vilnius": CITY_NAME_ALIASES["Vilnius"] + ["lithuania", "lithuanian"],
    "Belgrade": CITY_NAME_ALIASES["Belgrade"] + ["serbia", "serbian", "srbija"],
}

# Öğrenci odaklı ekonomi kalemleri (market, maaş, giyim, sigara vb. hariç)
STUDENT_ECONOMIC_KEYWORDS = (
    "meal at an inexpensive restaurant",
    "combo meal at mcdonald",
    "cappuccino",
    "bottled water",
    "soft drink",
    "domestic draft beer",
    "domestic beer",
    "imported beer",
    "one-way ticket (local transport)",
    "monthly public transport pass",
)

ERASMUS_CONTEXT_KEYWORDS = [
    "erasmus", "student", "university", "campus", "dorm", "dormitory", "exchange",
    "semester", "nightlife", "rent", "living", "cost", "affordable", "expensive",
    "budget", "party", "transport", "bus", "metro", "tram", "hostel", "study",
    "abroad", "international", "tuition", "flat", "apartment", "roommate", "visa",
    "scholarship", "öğrenci", "üniversite", "yurt", "kira", "ulaşım", "ulasim",
    "food", "restaurant", "grocery", "cafe", "beer", "weather", "locals",
    "foreigner", "expat", "language", "english", "safe", "dangerous", "walk",
    "club", "bar", "taxi", "airport", "wifi", "smoking", "night", "culture",
    "museum", "travel", "backpack", "degree", "lecture", "class", "exam",
]

SPAM_PHRASES = [
    "subscribe", "like and subscribe", "check my channel", "nice video", "great video",
    "thanks for sharing", "billy shot", "bang billy", "billy-", "the best youtuber",
    "wait for me", "hey girl", "my name is blair", "upload juga", "doain gua",
    "social media or contact", "made lot of views", "black women abroad",
    "african slavery", "hated white people", "hypocrisy at its finest",
    "please stop with these videos", "tourist visa", "false hope of a better life",
    "claiming to have relocated", "you made lot of views", "common courtesy",
    "bolca gelmesi dileğiyle",
]

GENERIC_COMMENT_PATTERNS = [
    re.compile(r"^eastern europe\b"),
    re.compile(r"^western europe\b"),
    re.compile(r"^wait for me\b"),
    re.compile(r"^billy shot\b"),
    re.compile(r"^the best youtuber\b"),
    re.compile(r"^wow my name\b"),
    re.compile(r"^love that we are still not\b"),
]

# Hedef 8 şehir dışı popüler Erasmus/varış noktaları
OTHER_DESTINATION_KEYWORDS = [
    "krakow", "kraków", "athens", "rimini", "verona", "riga", "latvia", "rijeka",
    "barcelona", "berlin", "paris", "milan", "milano", "rome", "roma", "munich",
    "budapest", "prague", "kraków", "turin", "torino", "naples", "napoli",
    "stockholm", "copenhagen", "amsterdam", "dublin", "london", "newcastle",
    "patrasso", "patras", "granada", "seville", "valencia", "florence", "firenze",
]

PROMOTIONAL_PHRASES = [
    "join our", "apply now", "webinar", "are you interested in studying",
    "sign up", "register here", "click the link", "discount code",
]

INFLUENCER_NOISE_PHRASES = [
    "i'm impressed", "im impressed", "what a beautiful", "classy", "dynamic lady",
    "your radiance", "great video", "nice video", "thanks for the video",
    "who is she", "she is so", "love your content",
]

# Kirli ham toplu dosyalar — birleştirmeye dahil edilmez
EXCLUDED_BULK_SOURCES = {
    "social_comments_raw_unfiltered.csv",
    "targeted_raw_comments.csv",
}

VALID_UNIVERSITIES = {
    "Lomza": "Lomza State University of Applied Sciences",
    "Warsaw": "Vistula University",
    "Lisbon": "Universidade de Lisboa",
    "Porto": "University of Porto",
    "Sofia": "University of Telecommunications and Post",
    "Skopje": "Ss. Cyril and Methodius University",
    "Vilnius": "Vilnius University",
    "Belgrade": "University of Belgrade",
}

GDELT_CITY_ALIASES = {
    "Lisboa": "Lisbon",
    "Oporto": "Porto",
    "Beograd": "Belgrade",
    "Belgrad": "Belgrade",
    "Łomża": "Lomza",
    "Lomża": "Lomza",
}

# 1 birim yerel para = X EUR
CURRENCY_TO_EUR = {
    "PLN": 0.23,
    "EUR": 1.0,
    "MKD": 0.0162,
    "RSD": 0.0085,
    "BGN": 0.51,
}

CURRENCY_FROM_SYMBOL = [
    (r"zł|zl|pln", "PLN"),
    (r"€|eur", "EUR"),
    (r"ден|mkd", "MKD"),
    (r"дин|rsd|дин\.?", "RSD"),
    (r"лв|bgn", "BGN"),
]

EMOJI_PATTERN = re.compile(
    "["
    "\U0001F300-\U0001FAFF"
    "\U00002700-\U000027BF"
    "\U0001F600-\U0001F64F"
    "\U00002600-\U000026FF"
    "\U0000FE00-\U0000FE0F"
    "\U0000200D"
    "]+",
    flags=re.UNICODE,
)

URL_PATTERN = re.compile(r"https?://\S+|www\.\S+", re.IGNORECASE)
MULTISPACE = re.compile(r"\s+")


def strip_emojis_and_noise(text: str) -> str:
    if not isinstance(text, str):
        text = str(text) if pd.notna(text) else ""
    text = EMOJI_PATTERN.sub("", text)
    text = URL_PATTERN.sub("", text)
    text = unicodedata.normalize("NFKC", text)
    text = MULTISPACE.sub(" ", text).strip()
    return text


def normalize_text(text: str) -> str:
    text = strip_emojis_and_noise(text)
    return text.lower()


def parse_price_value(price_raw: str) -> tuple[float, str]:
    """Numbeo fiyat stringinden sayı ve para birimi çıkar."""
    if not isinstance(price_raw, str):
        price_raw = str(price_raw) if pd.notna(price_raw) else ""

    raw = strip_emojis_and_noise(price_raw)
    raw_lower = raw.lower()

    currency = "EUR"
    for pattern, code in CURRENCY_FROM_SYMBOL:
        if re.search(pattern, raw_lower):
            currency = code
            break

    num_part = re.sub(r"[^\d,.\-]", "", raw)
    if not num_part:
        return 0.0, currency

    last_dot = num_part.rfind(".")
    last_comma = num_part.rfind(",")

    if last_dot > last_comma:
        normalized = num_part.replace(",", "")
    elif last_comma > last_dot:
        normalized = num_part.replace(".", "").replace(",", ".")
    else:
        normalized = num_part.replace(",", "")

    try:
        value = float(normalized)
    except ValueError:
        value = 0.0

    return value, currency


def to_eur(value: float, currency: str) -> float:
    rate = CURRENCY_TO_EUR.get(currency.upper(), 1.0)
    return round(value * rate, 2)


def cities_mentioned_in_comment(comment: str) -> set[str]:
    text = normalize_text(comment)
    found = set()
    for city, aliases in CITY_NAME_ALIASES.items():
        for alias in aliases:
            if alias in text:
                found.add(city)
                break
    return found


def is_wrong_city_assignment(tagged_city: str, comment: str) -> bool:
    """Yorum başka bir hedef şehirden bahsediyorsa etiketli şehirle uyuşmaz."""
    mentioned = cities_mentioned_in_comment(comment)
    if not mentioned:
        return False
    return tagged_city not in mentioned


def city_mentioned_in_comment(city: str, comment: str) -> bool:
    text = normalize_text(comment)
    for alias in CITY_CONTEXT_ALIASES.get(city, [city.lower()]):
        if alias in text:
            return True
    return False


def has_erasmus_context(comment: str) -> bool:
    text = normalize_text(comment)
    return any(kw in text for kw in ERASMUS_CONTEXT_KEYWORDS)


def is_question_comment(comment: str) -> bool:
    text = strip_emojis_and_noise(comment)
    if "?" in text:
        return True
    text_l = text.lower().strip()
    return bool(re.match(
        r"^(how|what|why|when|where|who|is|are|can|could|does|do|did|will|would|hi|hello)\b",
        text_l,
    ))


def mentions_other_destination(city: str, comment: str) -> bool:
    text = normalize_text(comment)
    mentioned_valid = cities_mentioned_in_comment(comment)
    if mentioned_valid and city not in mentioned_valid:
        return True
    if any(dest in text for dest in OTHER_DESTINATION_KEYWORDS):
        if not city_mentioned_in_comment(city, comment):
            return True
    return False


def has_low_text_quality(comment: str) -> bool:
    text = strip_emojis_and_noise(comment)
    if len(text) < 30:
        return True
    letters = sum(ch.isalpha() for ch in text)
    if letters / max(len(text), 1) < 0.4:
        return True
    words = text.split()
    if len(words) < 6:
        return True
    return False


def is_noisy_comment(city: str, comment: str) -> bool:
    """YouTube gürültüsü, konu dışı ve düşük kaliteli yorumları ayıklar."""
    text = normalize_text(comment)

    if is_question_comment(comment):
        return True

    if any(phrase in text for phrase in SPAM_PHRASES):
        return True

    if any(phrase in text for phrase in PROMOTIONAL_PHRASES):
        return True

    if any(phrase in text for phrase in INFLUENCER_NOISE_PHRASES):
        return True

    if re.search(r"(\?|!){5,}", comment):
        return True

    if re.fullmatch(r"[\W\d_]+", text):
        return True

    for pattern in GENERIC_COMMENT_PATTERNS:
        if pattern.search(text):
            return True

    if mentions_other_destination(city, comment):
        return True

    if has_low_text_quality(comment):
        return True

    # Şehir veya öğrenci/yaşam bağlamı olmayan yorumlar
    if not has_erasmus_context(comment) and not city_mentioned_in_comment(city, comment):
        return True

    return False


def is_student_economic_criteria(criteria: str) -> bool:
    c = criteria.lower()
    return any(k in c for k in STUDENT_ECONOMIC_KEYWORDS)


def normalize_gdelt_city(geo: str) -> str | None:
    if not isinstance(geo, str) or not geo.strip():
        return None
    primary = geo.strip().strip('"').split(",")[0].strip()
    if primary in GDELT_CITY_ALIASES:
        return GDELT_CITY_ALIASES[primary]
    for city in VALID_CITIES:
        if primary.lower() == city.lower():
            return city
    return None


FAMILY_HOUSING_KEYWORDS = (
    "apartment", "square meter to buy", "85 m2", "buy apartment",
    "volkswagen", "toyota", "preschool", "primary school", "mortgage"
)


def is_family_housing(criteria: str) -> bool:
    c = criteria.lower()
    return any(k in c for k in FAMILY_HOUSING_KEYWORDS)


def clean_economic_data() -> pd.DataFrame:
    path = RAW_DIR / "economic_data.csv"
    if not path.exists():
        print("  [ATLA] economic_data.csv bulunamadı")
        return pd.DataFrame()

    backup_file(path)
    bak = path.with_suffix(path.suffix + ".bak")
    source = bak if bak.exists() else path
    df = pd.read_csv(source, encoding="utf-8-sig")
    df.columns = [c.strip() for c in df.columns]

    rows = []
    for _, row in df.iterrows():
        city = strip_emojis_and_noise(str(row.get("City", "")))
        criteria = strip_emojis_and_noise(str(row.get("Criteria", "")))
        price_raw = str(row.get("Price", ""))
        currency_raw = str(row.get("Currency", "")) if "Currency" in df.columns else ""

        if city not in VALID_CITIES or not criteria:
            continue

        if is_family_housing(criteria):
            continue

        if not is_student_economic_criteria(criteria):
            continue

        value, currency = parse_price_value(price_raw)
        if value <= 0:
            continue

        # If a currency column existed and has a valid code, use it
        if currency_raw:
            currency_raw_clean = strip_emojis_and_noise(currency_raw).upper()
            if currency_raw_clean in CURRENCY_TO_EUR:
                currency = currency_raw_clean

        # Fallback to city default currency if currency is still EUR
        city_defaults = {
            "Lomza": "PLN", "Warsaw": "PLN",
            "Skopje": "MKD", "Belgrade": "RSD",
            "Lisbon": "EUR", "Porto": "EUR",
            "Sofia": "EUR", "Vilnius": "EUR"
        }
        if currency == "EUR" and city in city_defaults:
            if not (currency_raw and currency_raw.strip().upper() == "EUR"):
                currency = city_defaults[city]

        rows.append({
            "City": city,
            "Criteria": criteria,
            "Price": round(value, 2),
            "Currency": currency,
            # PriceEUR yalnizca referans; uygulama guncel kurla Price+Currency'den hesaplar
            "PriceEUR": to_eur(value, currency),
        })

    out = pd.DataFrame(rows)
    out = out.drop_duplicates(subset=["City", "Criteria"], keep="first")
    out.to_csv(path, index=False, encoding="utf-8-sig")
    print(f"  economic_data.csv: {len(out)} satir (ogrenci sepeti: yemek/icecek/ulasim)")
    return out


def ensure_student_rent_file() -> None:
    path = RAW_DIR / "student_rent.csv"
    if not path.exists():
        print("  [UYARI] student_rent.csv bulunamadi")
        return

    backup_file(path)
    df = pd.read_csv(path, encoding="utf-8-sig")
    df["City"] = df["City"].apply(strip_emojis_and_noise)

    rows = []
    for _, row in df.iterrows():
        city = str(row.get("City", "")).strip()
        if city not in VALID_CITIES:
            continue
        try:
            price = float(row.get("Price", 0))
        except (TypeError, ValueError):
            continue
        if price <= 0:
            continue
        currency = strip_emojis_and_noise(str(row.get("Currency", "EUR"))).upper()
        if currency not in CURRENCY_TO_EUR:
            continue
        rows.append({
            "City": city,
            "HousingType": strip_emojis_and_noise(str(row.get("HousingType", "Paylasimli oda / yurt"))),
            "Price": round(price, 2),
            "Currency": currency,
            "Note": strip_emojis_and_noise(str(row.get("Note", "Erasmus ogrenci konutu ortalamasi"))),
        })

    out = pd.DataFrame(rows).drop_duplicates(subset=["City"], keep="first")
    for city in VALID_CITIES:
        if city not in out["City"].values:
            print(f"  [UYARI] student_rent.csv eksik sehir: {city}")

    out = out.sort_values("City").reset_index(drop=True)
    out.to_csv(path, index=False, encoding="utf-8-sig")
    print(f"  student_rent.csv: {len(out)} sehir (ogrenci yurt/paylasimli oda)")


def backup_file(path: Path) -> None:
    bak = path.with_suffix(path.suffix + ".bak")
    if path.exists() and not bak.exists():
        import shutil
        shutil.copy2(path, bak)
        print(f"  Yedek: {bak.name}")


def archive_polluted_bulk_files() -> None:
    """67k+ ham toplu dosyaları birleştirme dışına taşır."""
    polluted = DATA_SCRIPTS_DIR / "social_comments_final.csv"
    archive = DATA_SCRIPTS_DIR / "social_comments_raw_unfiltered.csv"
    if polluted.exists():
        try:
            df = pd.read_csv(polluted, encoding="utf-8-sig", nrows=5)
            if len(df) > 500:
                if not archive.exists():
                    polluted.replace(archive)
                    print(f"  Ham toplu dosya arsivlendi: {archive.name}")
                else:
                    polluted.unlink()
                    print(f"  Kirli kaynak silindi (arsiv zaten var): social_comments_final.csv")
        except Exception:
            pass

    targeted = RAW_DIR / "targeted_raw_comments.csv"
    if targeted.exists() and targeted.stat().st_size > 500_000:
        print(f"  targeted_raw_comments.csv birlestirmeye dahil edilmez ({targeted.stat().st_size // 1024} KB ham veri)")


def merge_social_sources() -> pd.DataFrame:
    """Temizlenmiş RawData kaynaklarını birleştirir (8 Erasmus şehri)."""
    sources = [
        RAW_DIR / "social_comments_final2.csv",
        RAW_DIR / "social_comments_final.csv",
        RAW_DIR / "social_comments_bulk.csv",
    ]

    rows = []
    for src in sources:
        if not src.exists() or src.name in EXCLUDED_BULK_SOURCES:
            continue
        try:
            df = pd.read_csv(src, encoding="utf-8-sig")
        except Exception:
            continue
        if "Comment" not in df.columns:
            continue
        if len(df) > 2000:
            print(f"  [ATLA] Cok buyuk/kirli kaynak atlandi: {src.name} ({len(df)} satir)")
            continue

        for _, row in df.iterrows():
            city = strip_emojis_and_noise(str(row.get("City", "")))
            if city not in VALID_CITIES:
                continue

            comment = strip_emojis_and_noise(str(row.get("Comment", "")))
            if is_noisy_comment(city, comment):
                continue

            source = strip_emojis_and_noise(str(row.get("Source", "YouTube")))
            sentiment = str(row.get("Sentiment", "Nötr")).strip()
            if sentiment not in ("Olumlu", "Olumsuz", "Nötr"):
                sentiment = "Nötr"

            if is_wrong_city_assignment(city, comment):
                continue

            rows.append({
                "City": city,
                "Source": source,
                "Comment": comment,
                "CleanComment": normalize_text(comment),
                "Sentiment": sentiment,
            })

    out = pd.DataFrame(rows)
    if out.empty:
        return out

    out = out.drop_duplicates(subset=["City", "Comment"], keep="first")
    out = out.sort_values(["City", "Sentiment"]).reset_index(drop=True)
    return out


def clean_social_comments() -> None:
    backup_file(RAW_DIR / "social_comments_final.csv")
    merged = merge_social_sources()
    if merged.empty:
        print("  [UYARI] Birleştirilecek sosyal yorum bulunamadı")
        return

    merged.to_csv(RAW_DIR / "social_comments_final.csv", index=False, encoding="utf-8-sig")
    merged.to_csv(RAW_DIR / "social_comments_bulk.csv", index=False, encoding="utf-8-sig")
    merged.to_csv(DATA_SCRIPTS_DIR / "social_comments_final.csv", index=False, encoding="utf-8-sig")

    print(f"  social_comments_final.csv: {len(merged)} satir (gurultu ayiklandi, 8 sehir)")
    for city in VALID_CITIES:
        count = len(merged[merged["City"] == city])
        print(f"    {city}: {count}")


def rebuild_social_scores() -> None:
    path = RAW_DIR / "social_comments_final.csv"
    if not path.exists():
        return

    df = pd.read_csv(path, encoding="utf-8-sig")
    if df.empty:
        return

    scores = []
    for city in VALID_CITIES:
        subset = df[df["City"] == city]
        if len(subset) == 0:
            scores.append({"City": city, "SocialSatisfactionScore": 50.0})
            continue
        weights = subset["Sentiment"].map({
            "Olumlu": 1.0,
            "Nötr": 0.45,
            "Olumsuz": 0.0,
        }).fillna(0.45)
        score = round(weights.mean() * 100, 2)
        scores.append({"City": city, "SocialSatisfactionScore": score})

    out = pd.DataFrame(scores)
    out_path = RAW_DIR / "city_social_scores.csv"
    out.to_csv(out_path, index=False, encoding="utf-8-sig")
    print(f"  city_social_scores.csv: {len(out)} şehir")


def clean_gdelt_data() -> None:
    path = RAW_DIR / "gdelt_data.csv"
    if not path.exists():
        print("  [ATLA] gdelt_data.csv bulunamadı")
        return

    backup_file(path)
    bak = path.with_suffix(path.suffix + ".bak")
    source = bak if bak.exists() else path
    df = pd.read_csv(source, encoding="utf-8-sig", low_memory=False)
    before = len(df)

    geo_col = "ActionGeo_FullName" if "ActionGeo_FullName" in df.columns else df.columns[2]
    df["_City"] = df[geo_col].apply(normalize_gdelt_city)
    df = df[df["_City"].notna()].copy()
    df["City"] = df["_City"]
    df = df.drop(columns=["_City"])

    if "GLOBALEVENTID" in df.columns:
        df = df.drop_duplicates(subset=["GLOBALEVENTID"], keep="first")

    if "GoldsteinScale" in df.columns:
        df["GoldsteinScale"] = pd.to_numeric(df["GoldsteinScale"], errors="coerce")
        df = df[df["GoldsteinScale"].between(-10, 10, inclusive="both")]

    if "AvgTone" in df.columns:
        df["AvgTone"] = pd.to_numeric(df["AvgTone"], errors="coerce")
        df = df[df["AvgTone"].between(-15, 15, inclusive="both")]

    if "NumArticles" in df.columns:
        df["NumArticles"] = pd.to_numeric(df["NumArticles"], errors="coerce")
        df = df[df["NumArticles"].fillna(0) >= 1]

    for col in df.select_dtypes(include=["object", "str"]).columns:
        df[col] = df[col].apply(lambda x: strip_emojis_and_noise(x) if isinstance(x, str) else x)

    df.to_csv(path, index=False, encoding="utf-8-sig")
    print(f"  gdelt_data.csv: {before} -> {len(df)} satir (8 sehir, aykiri ton/goldstein cikarildi)")


def clean_academic_data() -> None:
    path = RAW_DIR / "academic_quality.csv"
    if not path.exists():
        return

    backup_file(path)
    df = pd.read_csv(path, encoding="utf-8-sig")
    df["University"] = df["University"].apply(strip_emojis_and_noise)
    df["PublicationCount"] = pd.to_numeric(df["PublicationCount"], errors="coerce").fillna(0).astype(int)
    df = df[df["PublicationCount"] >= 0]
    df = df.drop_duplicates(subset=["University"], keep="first")

    # 8 hedef üniversite — fazla veya eşleşmeyen kayıtları ayıkla
    canonical = set(VALID_UNIVERSITIES.values())
    df = df[df["University"].isin(canonical)]

    rows = []
    for city, uni in VALID_UNIVERSITIES.items():
        match = df[df["University"] == uni]
        if len(match) > 0:
            rows.append({
                "University": uni,
                "PublicationCount": int(match.iloc[0]["PublicationCount"]),
            })
        else:
            rows.append({"University": uni, "PublicationCount": 0})
            print(f"  [UYARI] academic_quality.csv eksik: {uni}")

    out = pd.DataFrame(rows).sort_values("PublicationCount", ascending=False)
    out.to_csv(path, index=False, encoding="utf-8-sig")
    print(f"  academic_quality.csv: {len(out)} universite (8 Erasmus)")


def main():
    os.makedirs(RAW_DIR, exist_ok=True)
    print("=== OptiErasmus veri temizliği ===\n")

    print("[1] Ekonomi (ogrenci sepeti: ucuz yemek, icecek, toplu tasima)")
    clean_economic_data()
    ensure_student_rent_file()

    print("\n[2] Sosyal yorumlar")
    archive_polluted_bulk_files()
    clean_social_comments()
    rebuild_social_scores()

    print("\n[3] GDELT")
    clean_gdelt_data()

    print("\n[4] Akademik")
    clean_academic_data()

    print("\nTamamlandı.")


if __name__ == "__main__":
    main()
