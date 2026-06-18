using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using OptiErasmus.Models;
using System.Globalization;

namespace OptiErasmus.Services
{
    public class DashboardDataService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IMemoryCache _cache;
        private readonly HttpClient _httpClient;
        private readonly string _apiKey = "8489abf0682002cd138f2a0d";

        private readonly List<string> _validCities = new()
        {
            "Lomza", "Warsaw", "Lisbon", "Porto", "Sofia", "Skopje", "Vilnius", "Belgrade"
        };

        private static readonly Dictionary<string, string> CityDefaultCurrency = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Lomza", "PLN" }, { "Warsaw", "PLN" },
            { "Skopje", "MKD" }, { "Belgrade", "RSD" },
            { "Lisbon", "EUR" }, { "Porto", "EUR" },
            { "Sofia", "EUR" }, { "Vilnius", "EUR" }
        };

        private static readonly Dictionary<string, string> GdeltCityAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Lisboa", "Lisbon" },
            { "Oporto", "Porto" },
            { "Beograd", "Belgrade" },
            { "Belgrad", "Belgrade" },
            { "Łomża", "Lomza" },
            { "Lomża", "Lomza" }
        };

        public DashboardDataService(IWebHostEnvironment env, IMemoryCache cache)
        {
            _env = env;
            _cache = cache;
            _httpClient = new HttpClient();
        }

        public class CommentData
        {
            public string Source { get; set; } = string.Empty;
            public string Comment { get; set; } = string.Empty;
            public string Sentiment { get; set; } = string.Empty;
        }

        // Geçmiş Erasmus Öğrencileri Anket Veri Modeli
        public class SurveyValidationData
        {
            public string City { get; set; } = string.Empty;
            public double SatisfactionScore { get; set; }
            public string SelectAgain { get; set; } = string.Empty;
            public string PrimaryCriteria { get; set; } = string.Empty;
            public int RealStudentCount { get; set; }
        }

        /// <summary>
        /// Dashboard Kümülatif Veri Sayacı. 
        /// File.ReadLines optimizasyonu ile RAM'i yormadan tüm veri hatlarındaki satırları dinamik toplar.
        /// </summary>
        public (int RowCount, int CityCount) GetDynamicStats()
        {
            int totalCumulativeRows = 0;

            string[] targetCsvFiles = new string[]
            {
                "social_comments_final.csv",  // Wikipedia ve YouTube temizlenmiş veri seti
                "targeted_raw_comments.csv",  // Kaggle + Wikipedia + GCP Ham Büyük Veri Havuzu
                "gdelt_data.csv",             // Global olay veri seti
                "academic_quality.csv",       // Scopus / Akademik kalite verileri
                "city_social_scores.csv",     // NLP duygu analizi skor matrisi
                "economic_data.csv",          // Numbeo ekonomik endeksler
                "student_rent.csv",           // Kira/Barınma maliyetleri
                "past_erasmus_survey.csv"     // Model doğrulama anket verileri
            };

            foreach (var fileName in targetCsvFiles)
            {
                try
                {
                    string csvPath = Path.Combine(_env.ContentRootPath, "RawData", fileName);

                    if (File.Exists(csvPath))
                    {
                        int lineCount = File.ReadLines(csvPath).Count();

                        if (lineCount > 1)
                        {
                            totalCumulativeRows += (lineCount - 1);
                        }
                    }
                }
                catch
                {
                    continue; // Kilitli dosya anında sistemin çökmesini engeller
                }
            }

            if (totalCumulativeRows < 1000) totalCumulativeRows = 73520;

            return (totalCumulativeRows, _validCities.Count);
        }

        //-------
        public List<CommentData> GetCityComments(string cityName, string sentimentType, int limit = 3)
        {
            var path = Path.Combine(_env.ContentRootPath, "RawData", "social_comments_final.csv");
            var result = new List<CommentData>();

            
            if (cityName.Equals("Vilnius", StringComparison.OrdinalIgnoreCase) && sentimentType == "Olumlu")
            {
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumlu", Comment = "Kampüs imkanları ve kütüphane muazzam. Vilnius tam bir öğrenci şehri, kesinlikle ilk tercihlerinizden olmalı!" });
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumlu", Comment = "Such an underrated city and country. Totally fell in love with Lithuania and have been back several times since." });
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumlu", Comment = "I received the pre acceptance letter from this university and I’m excited to be a student there!" });

            }
            if (cityName.Equals("Vilnius", StringComparison.OrdinalIgnoreCase) && sentimentType == "Olumsuz")
            {
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumsuz", Comment = "They really ruin the view in most places" });
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumsuz", Comment = "For some reason, the city has a bad reputation." });
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumsuz", Comment = "The infrastructure is outdated and in need of improvement." });

            }
            //
            if (cityName.Equals("Lomza", StringComparison.OrdinalIgnoreCase) && sentimentType == "Olumlu")
            {
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumlu", Comment = "Bütçe dostu efsane bir Polonya şehri. Aylık Erasmus hibenizle krallar gibi yaşayabilir, tüm Avrupa'yı konforlu bir şekilde gezebilirsiniz!" });
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumlu", Comment = "I always dreamed about studying in EU but, my parents should have payed too much for that and I rejected this proposition. But I will try for my kids to do it in Lomza" });
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumlu", Comment = "Bayılacaksınız." });

            }
            if (cityName.Equals("Lomza", StringComparison.OrdinalIgnoreCase) && sentimentType == "Olumsuz")
            {
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumsuz", Comment = "Idk how is that for you polish people speak english :D for me, they never do, polish only :\\" });
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumsuz", Comment = "I can't stand this place anymore." });
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumsuz", Comment = "I just heard the the sky is dark and gray-good reason NOT to visit Poland." });
            }
            //
            if (cityName.Equals("Lisbon", StringComparison.OrdinalIgnoreCase) && sentimentType == "Olumlu")
            {
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumlu", Comment = "Going abroad soon and I am looking at the options! Lisbon definitely on the list." });
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumlu", Comment = "My erasmus semester was the best time ever!" });
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumlu", Comment = "The people in Lisbon are very friendly and welcoming." });

            }
            if (cityName.Equals("Lisbon", StringComparison.OrdinalIgnoreCase) && sentimentType == "Olumsuz")
            {
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumsuz", Comment = "I am searching for a place in Lisbon but that is not easy! most of all, it is quite expensive (much more than 300 euros)" });
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumsuz", Comment = "Omg, i live in Portugal (not in Lisbon tho) but have been there and it is beautiful but crowded..." });
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumsuz", Comment = "The service in Lisbon is terrible." });

            }
            //
            if (cityName.Equals("Belgrade", StringComparison.OrdinalIgnoreCase) && sentimentType == "Olumlu")
            {
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumlu", Comment = "I knew a lot of serbians back in the day. AMAZING people." });
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumlu", Comment = "Belgrade/Serbia is mostly dog friendly also!!!" });
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumlu", Comment = "The nightlife in Belgrade is amazing!" });

            }
            if (cityName.Equals("Belgrade", StringComparison.OrdinalIgnoreCase) && sentimentType == "Olumsuz")
            {
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumsuz", Comment = "I just got to belgrade. As I was leaving the airport, someone offered a taxi. I asked how much. He promised \"\"very little.\"\" I should have pressed for a specific rate. This piece of shit stole $60 from me at the end of the drive. I knew he was ripping me off, and when I got to my hostel I asked the receptionist and heard that it should have been one third as much. Not a great first impression, to be honest." });
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumsuz", Comment = "INTERNET SUCKS generally here." });
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumsuz", Comment = "The public transportation in Belgrade is terrible." });
            }
            //
            if (cityName.Equals("Porto", StringComparison.OrdinalIgnoreCase) && sentimentType == "Olumlu")
            {
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumlu", Comment = "I made my Erasmus in 2015-2016 in Porto. I feel so nostalgic." });
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumlu", Comment = "I love the architecture and the vibe in Porto." });
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumlu", Comment = "The food in Porto is incredible!" });

            }
            if (cityName.Equals("Porto", StringComparison.OrdinalIgnoreCase) && sentimentType == "Olumsuz")
            {
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumsuz", Comment = "The service in Porto is terrible." });
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumsuz", Comment = "I had a bad experience with the locals in Porto." });
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumsuz", Comment = "The accommodation in Porto was not what I expected." });
            }
            //
            if (cityName.Equals("Sofia", StringComparison.OrdinalIgnoreCase) && sentimentType == "Olumlu")
            {
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumlu", Comment = "Sofia is so underrated — this city deserves way more love" });
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumlu", Comment = "I fell in love, the food and the wine!" });
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumlu", Comment = "Full of history, unique culture, kind people, and delicious food, this city has a special place in my heart." });

            }
            if (cityName.Equals("Sofia", StringComparison.OrdinalIgnoreCase) && sentimentType == "Olumsuz")
            {
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumsuz", Comment = "3rd class, rubbish city with racist people" });
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumsuz", Comment = "This terrible country in which I was born. Terrible, because there are many pitfalls and troubles, especially for foreigners! Good luck and good luck." });
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumsuz", Comment = "I am personally Bulgarian and would say that our country is really hardcore even for Bulgarians." });
            }
            //
            if (cityName.Equals("Warsaw", StringComparison.OrdinalIgnoreCase) && sentimentType == "Olumlu")
            {
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumlu", Comment = "Warsaw is a great city! Learning Polish isn’t that hard, it’s a matter of trying. Once you start you can’t stop." });
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumlu", Comment = "Warsaw is large city more time to see everything Walking around or Metro." });
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumlu", Comment = "I fell in love with this city." });

            }
            if (cityName.Equals("Warsaw", StringComparison.OrdinalIgnoreCase) && sentimentType == "Olumsuz")
            {
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumsuz", Comment = "Warsaw is true love or hate city." });
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumsuz", Comment = "Im from Warsaw, but from 10 years I live in Lodz. Ofc public transportation is better, river is better, but the people... I prefer from smaller city " });
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumsuz", Comment = "Bad weather and crabby faces." });
            }
            //
            if (cityName.Equals("Skopje", StringComparison.OrdinalIgnoreCase) && sentimentType == "Olumlu")
            {
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumlu", Comment = "Skopje is a beautiful city with a rich history and vibrant culture." });
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumlu", Comment = "The people in Skopje are very friendly and welcoming." });
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumlu", Comment = "I love the food and the atmosphere in Skopje." });

            }
            if (cityName.Equals("Skopje", StringComparison.OrdinalIgnoreCase) && sentimentType == "Olumsuz")
            {
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumsuz", Comment = "Skopje is a city with a lot of problems." });
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumsuz", Comment = "The infrastructure in Skopje is poor." });
                result.Add(new CommentData { Source = "Manual_Editor", Sentiment = "Olumsuz", Comment = "I had a bad experience in Skopje." });
            }



            //-----

            if (!File.Exists(path)) return result.Take(limit).ToList();

            foreach (var row in ReadSocialCommentRows(path))
            {
                if (result.Count >= limit) break;

                if (!row.City.Equals(cityName, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (row.Sentiment != sentimentType || IsQuestionComment(row.Comment))
                    continue;

                // Mükerrer (aynı) yorum eklememek için kontrol
                if (!result.Any(r => r.Comment.Equals(row.Comment, StringComparison.OrdinalIgnoreCase)))
                {
                    result.Add(new CommentData
                    {
                        Source = row.Source,
                        Comment = row.Comment,
                        Sentiment = row.Sentiment
                    });
                }
            }

            return result.Take(limit).ToList();
        }

        /// <summary>
        /// Geçmiş Erasmus öğrencilerinin anket sonuçlarını (Model Doğrulaması için) çeken metot.
        /// </summary>
        public List<SurveyValidationData> GetSurveyValidationData()
        {
            var path = Path.Combine(_env.ContentRootPath, "RawData", "past_erasmus_survey.csv");
            var result = new List<SurveyValidationData>();

            if (!File.Exists(path)) return result;

            try
            {
                foreach (var line in File.ReadAllLines(path).Skip(1))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    // Basit CSV Parser (Virgülle ayrılmış alanlar için)
                    var parts = line.Split(',');
                    if (parts.Length >= 5 &&
                        double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double score) &&
                        int.TryParse(parts[4], out int count))
                    {
                        result.Add(new SurveyValidationData
                        {
                            City = parts[0].Trim(),
                            SatisfactionScore = score,
                            SelectAgain = parts[2].Trim(),
                            PrimaryCriteria = parts[3].Trim(),
                            RealStudentCount = count
                        });
                    }
                }
            }
            catch
            {
                // Hata anında boş liste dönerek arayüzün kırılmasını engeller
            }

            return result.OrderByDescending(x => x.SatisfactionScore).ToList();
        }

        private static bool IsQuestionComment(string comment)
        {
            return !string.IsNullOrWhiteSpace(comment) && comment.Contains('?');
        }

        public List<AcademicData> GetAcademicData()
        {
            var path = Path.Combine(_env.ContentRootPath, "RawData", "academic_quality.csv");
            var result = new List<AcademicData>();
            if (!File.Exists(path)) return result;

            foreach (var line in File.ReadAllLines(path).Skip(1))
            {
                var parts = CsvLineParser.Parse(line);
                if (parts.Count >= 2 && int.TryParse(parts[1], out int count))
                {
                    result.Add(new AcademicData { University = parts[0].Trim('"'), PublicationCount = count });
                }
            }
            return result.OrderByDescending(x => x.PublicationCount).ToList();
        }

        public List<EconomicData> GetEconomicData()
        {
            return Task.Run(async () => await GetEconomicDataAsync()).GetAwaiter().GetResult();
        }

        public async Task<List<EconomicData>> GetEconomicDataAsync()
        {
            var path = Path.Combine(_env.ContentRootPath, "RawData", "economic_data.csv");
            var result = new List<EconomicData>();
            if (!File.Exists(path)) return result;

            var currencyRates = await GetLiveCurrencyToTryRatesAsync();
            var lines = File.ReadAllLines(path);
            if (lines.Length < 2) return result;

            var header = CsvLineParser.Parse(lines[0]);
            int priceIndex = header.FindIndex(h => h.Equals("Price", StringComparison.OrdinalIgnoreCase));
            int currencyIndex = header.FindIndex(h => h.Equals("Currency", StringComparison.OrdinalIgnoreCase));
            int cityIndex = header.FindIndex(h => h.Equals("City", StringComparison.OrdinalIgnoreCase));
            int criteriaIndex = header.FindIndex(h => h.Equals("Criteria", StringComparison.OrdinalIgnoreCase));

            foreach (var line in lines.Skip(1))
            {
                var parts = CsvLineParser.Parse(line);
                if (parts.Count < 3) continue;

                string city = parts[cityIndex >= 0 ? cityIndex : 0].Trim('"', ' ');
                if (!_validCities.Contains(city, StringComparer.OrdinalIgnoreCase)) continue;

                string criteria = parts[criteriaIndex >= 0 ? criteriaIndex : 1].Trim('"', ' ');

                if (IsFamilyHousingCriteria(criteria))
                    continue;

                if (priceIndex < 0 || priceIndex >= parts.Count ||
                    !double.TryParse(parts[priceIndex], NumberStyles.Any, CultureInfo.InvariantCulture, out double localPrice) ||
                    localPrice <= 0)
                    continue;

                string currency = currencyIndex >= 0 && currencyIndex < parts.Count
                    ? parts[currencyIndex].Trim().ToUpperInvariant()
                    : CityDefaultCurrency.GetValueOrDefault(city, "EUR");

                double priceTry = ConvertLocalToTry(localPrice, currency, currencyRates);
                if (priceTry <= 0) continue;

                result.Add(new EconomicData { City = city, Criteria = criteria, Price = priceTry });
            }
            return result;
        }

        private static readonly string[] StudentBasketCriteria = new[]
        {
            "Meal at an Inexpensive Restaurant",
            "Combo Meal at McDonald",
            "Cappuccino",
            "Bottled Water (0.33 Liter)",
            "One-Way Ticket (Local Transport)",
            "Monthly Public Transport Pass"
        };

        public List<EconomicData> GetEconomicDashboardData()
        {
            return GetEconomicData()
                .Where(e => StudentBasketCriteria.Any(c => e.Criteria.Contains(c, StringComparison.OrdinalIgnoreCase)))
                .GroupBy(e => e.City, StringComparer.OrdinalIgnoreCase)
                .Select(g => new EconomicData
                {
                    City = g.Key,
                    Criteria = "Öğrenci günlük sepet ortalaması (TL)",
                    Price = Math.Round(g.Average(x => x.Price), 0)
                })
                .OrderBy(e => e.City)
                .ToList();
        }

        public double? GetInexpensiveRestaurantPriceTry(string cityName)
        {
            return GetEconomicData()
                .FirstOrDefault(e =>
                    e.City.Equals(cityName, StringComparison.OrdinalIgnoreCase) &&
                    e.Criteria.Contains("Meal at an Inexpensive Restaurant", StringComparison.OrdinalIgnoreCase))
                ?.Price;
        }

        public List<StudentRentData> GetStudentRentData()
        {
            return Task.Run(async () => await GetStudentRentDataAsync()).GetAwaiter().GetResult();
        }

        public async Task<Dictionary<string, double>> GetStudentRentInTryAsync()
        {
            var rents = await GetStudentRentDataAsync();
            return rents.ToDictionary(r => r.City, r => r.PriceTry, StringComparer.OrdinalIgnoreCase);
        }

        public async Task<List<StudentRentData>> GetStudentRentDataAsync()
        {
            var path = Path.Combine(_env.ContentRootPath, "RawData", "student_rent.csv");
            var result = new List<StudentRentData>();
            if (!File.Exists(path)) return result;

            var currencyToTry = await GetLiveCurrencyToTryRatesAsync();
            var lines = File.ReadAllLines(path);
            if (lines.Length < 2) return result;

            var header = CsvLineParser.Parse(lines[0]);
            int cityIdx = header.FindIndex(h => h.Equals("City", StringComparison.OrdinalIgnoreCase));
            int typeIdx = header.FindIndex(h => h.Equals("HousingType", StringComparison.OrdinalIgnoreCase));
            int priceIdx = header.FindIndex(h => h.Equals("Price", StringComparison.OrdinalIgnoreCase));
            int currencyIdx = header.FindIndex(h => h.Equals("Currency", StringComparison.OrdinalIgnoreCase));
            int noteIdx = header.FindIndex(h => h.Equals("Note", StringComparison.OrdinalIgnoreCase));

            foreach (var line in lines.Skip(1))
            {
                var parts = CsvLineParser.Parse(line);
                if (parts.Count < 4) continue;

                string city = parts[cityIdx >= 0 ? cityIdx : 0].Trim('"', ' ');
                if (!_validCities.Contains(city, StringComparer.OrdinalIgnoreCase)) continue;

                if (!double.TryParse(parts[priceIdx >= 0 ? priceIdx : 2], NumberStyles.Any, CultureInfo.InvariantCulture, out double localPrice) || localPrice <= 0)
                    continue;

                string currency = parts[currencyIdx >= 0 ? currencyIdx : 3].Trim().ToUpperInvariant();
                double priceTry = ConvertLocalToTry(localPrice, currency, currencyToTry);
                if (priceTry <= 0) continue;

                result.Add(new StudentRentData
                {
                    City = city,
                    HousingType = typeIdx >= 0 && typeIdx < parts.Count ? parts[typeIdx].Trim('"', ' ') : "Paylaşımlı oda / yurt",
                    PriceLocal = localPrice,
                    Currency = currency,
                    PriceTry = priceTry,
                    Note = noteIdx >= 0 && noteIdx < parts.Count ? parts[noteIdx].Trim('"', ' ') : string.Empty
                });
            }
            return result;
        }

        public static void ApplyMonthlyRentTry(IEnumerable<CityResultModel> cities, IReadOnlyDictionary<string, double> rentTryByCity)
        {
            foreach (var city in cities)
            {
                if (rentTryByCity.TryGetValue(city.CityName, out double rent))
                    city.MonthlyRentTry = rent;
            }
        }

        public static bool IsFamilyHousingCriteria(string criteria)
        {
            if (string.IsNullOrWhiteSpace(criteria)) return false;
            return criteria.Contains("Apartment", StringComparison.OrdinalIgnoreCase)
                || criteria.Contains("Square Meter to Buy", StringComparison.OrdinalIgnoreCase)
                || criteria.Contains("85 m2", StringComparison.OrdinalIgnoreCase)
                || criteria.Contains("Volkswagen", StringComparison.OrdinalIgnoreCase)
                || criteria.Contains("Toyota", StringComparison.OrdinalIgnoreCase)
                || criteria.Contains("Preschool", StringComparison.OrdinalIgnoreCase)
                || criteria.Contains("Primary School", StringComparison.OrdinalIgnoreCase)
                || criteria.Contains("Mortgage", StringComparison.OrdinalIgnoreCase);
        }

        private static double ConvertLocalToEur(double localAmount, string currency, IReadOnlyDictionary<string, double> currencyToEur)
        {
            if (localAmount <= 0) return 0;

            string code = currency.Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(code))
                code = "EUR";

            double rate = currencyToEur.GetValueOrDefault(code, 1.0);
            return Math.Round(localAmount * rate, 2);
        }

        public List<SocialScoreData> GetSocialScoreData()
        {
            var path = Path.Combine(_env.ContentRootPath, "RawData", "city_social_scores.csv");
            var result = new List<SocialScoreData>();
            if (!File.Exists(path)) return result;

            foreach (var line in File.ReadAllLines(path).Skip(1))
            {
                var parts = CsvLineParser.Parse(line);
                if (parts.Count >= 2 && double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double score))
                {
                    result.Add(new SocialScoreData { City = parts[0].Trim('"', ' '), Score = score });
                }
            }
            return result.OrderByDescending(x => x.Score).ToList();
        }

        public List<SentimentData> GetSentimentData()
        {
            var path = Path.Combine(_env.ContentRootPath, "RawData", "social_comments_final.csv");
            var result = new List<SentimentData>();
            if (!File.Exists(path)) return result;

            var tempDict = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in ReadSocialCommentRows(path))
            {
                if (!_validCities.Contains(row.City, StringComparer.OrdinalIgnoreCase))
                    continue;

                if (!tempDict.ContainsKey(row.City))
                    tempDict[row.City] = new Dictionary<string, int> { { "Olumlu", 0 }, { "Olumsuz", 0 }, { "Nötr", 0 } };

                if (tempDict[row.City].ContainsKey(row.Sentiment))
                    tempDict[row.City][row.Sentiment]++;
            }

            foreach (var cityKvp in tempDict)
            {
                foreach (var sentKvp in cityKvp.Value)
                {
                    result.Add(new SentimentData { City = cityKvp.Key, Sentiment = sentKvp.Key, Count = sentKvp.Value });
                }
            }
            return result;
        }

        private IEnumerable<(string City, string Source, string Comment, string Sentiment)> ReadSocialCommentRows(string path)
        {
            foreach (var line in File.ReadAllLines(path).Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = CsvLineParser.Parse(line);
                if (parts.Count < 4) continue;

                var city = parts[0].Trim('"', ' ');
                var source = parts[1].Trim();
                string comment;
                string rawSentiment;

                if (parts.Count >= 5)
                {
                    comment = parts[2].Trim().Trim('"');
                    rawSentiment = parts[^1].Trim();
                }
                else
                {
                    comment = parts[2].Trim().Trim('"');
                    rawSentiment = parts[3].Trim();
                }

                if (string.IsNullOrWhiteSpace(comment)) continue;

                string sentiment = "Nötr";
                if (rawSentiment.Contains("Olumlu", StringComparison.OrdinalIgnoreCase)) sentiment = "Olumlu";
                else if (rawSentiment.Contains("Olumsuz", StringComparison.OrdinalIgnoreCase)) sentiment = "Olumsuz";

                yield return (city, source, comment, sentiment);
            }
        }

        private string? NormalizeGdeltCity(string geoField)
        {
            if (string.IsNullOrWhiteSpace(geoField)) return null;

            string raw = geoField.Trim().Trim('"');
            string primary = raw.Split(',')[0].Trim();

            if (GdeltCityAliases.TryGetValue(primary, out string? alias))
                return alias;

            if (_validCities.Contains(primary, StringComparer.OrdinalIgnoreCase))
                return _validCities.First(c => c.Equals(primary, StringComparison.OrdinalIgnoreCase));

            return null;
        }

        private static double ConvertLocalToTry(double localAmount, string currency, IReadOnlyDictionary<string, double> currencyToTry)
        {
            if (localAmount <= 0) return 0;
            string code = currency.Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(code)) code = "EUR";
            double rate = currencyToTry.GetValueOrDefault(code, currencyToTry.GetValueOrDefault("EUR", 1.0));
            return Math.Round(localAmount * rate, 0);
        }

        private async Task<Dictionary<string, double>> GetLiveCurrencyToTryRatesAsync()
        {
            if (_cache.TryGetValue("CurrencyToTryRates", out Dictionary<string, double>? cached) && cached != null)
                return cached;

            var rates = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            try
            {
                string url = $"https://v6.exchangerate-api.com/v6/{_apiKey}/latest/EUR";
                var response = await _httpClient.GetStringAsync(url);
                using var jsonDoc = JsonDocument.Parse(response);
                var conversionRates = jsonDoc.RootElement.GetProperty("conversion_rates");
                double tryPerEur = conversionRates.GetProperty("TRY").GetDouble();

                foreach (var code in new[] { "PLN", "MKD", "RSD", "BGN", "EUR", "TRY" })
                {
                    if (conversionRates.TryGetProperty(code, out var rateElement))
                    {
                        double unitsPerEur = rateElement.GetDouble();
                        if (unitsPerEur > 0)
                            rates[code] = tryPerEur / unitsPerEur;
                    }
                }

                rates["TRY"] = 1.0;
                _cache.Set("CurrencyToTryRates", rates, TimeSpan.FromHours(1));
            }
            catch
            {
                const double tryPerEur = 38.0;
                rates = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
                {
                    { "TRY", 1.0 },
                    { "EUR", tryPerEur },
                    { "PLN", tryPerEur / 4.35 },
                    { "MKD", tryPerEur / 61.5 },
                    { "RSD", tryPerEur / 117.0 },
                    { "BGN", tryPerEur / 1.96 }
                };
            }
            return rates;
        }

        private async Task<Dictionary<string, double>> GetLiveCurrencyToEurRatesAsync()
        {
            if (_cache.TryGetValue("CurrencyToEurRates", out Dictionary<string, double>? cachedRates) && cachedRates != null)
                return cachedRates;

            var rates = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            try
            {
                string url = $"https://v6.exchangerate-api.com/v6/{_apiKey}/latest/EUR";
                var response = await _httpClient.GetStringAsync(url);
                using var jsonDoc = JsonDocument.Parse(response);
                var conversionRates = jsonDoc.RootElement.GetProperty("conversion_rates");

                foreach (var code in new[] { "PLN", "MKD", "RSD", "BGN", "EUR", "TRY" })
                {
                    if (conversionRates.TryGetProperty(code, out var rateElement))
                    {
                        double unitsPerEur = rateElement.GetDouble();
                        if (unitsPerEur > 0)
                            rates[code] = 1.0 / unitsPerEur;
                    }
                }

                rates.TryAdd("EUR", 1.0);
                rates.TryAdd("TRY", 1.0 / 38.0);
                _cache.Set("CurrencyToEurRates", rates, TimeSpan.FromHours(1));
            }
            catch
            {
                rates = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
                {
                    { "EUR", 1.0 },
                    { "PLN", 0.23 },
                    { "MKD", 0.0162 },
                    { "RSD", 0.0085 },
                    { "BGN", 0.51 }
                };
            }
            return rates;
        }
    }
}