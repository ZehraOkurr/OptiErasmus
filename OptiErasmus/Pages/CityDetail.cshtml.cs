using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Hosting;
using OptiErasmus.Models;
using OptiErasmus.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;

namespace OptiErasmus.Pages
{
    public class CityDetailModel : PageModel
    {
        private readonly DashboardDataService _dataService;
        private readonly VikorService _vikorService;
        private readonly IWebHostEnvironment _env;

        public CityDetailModel(DashboardDataService dataService, VikorService vikorService, IWebHostEnvironment env)
        {
            _dataService = dataService;
            _vikorService = vikorService;
            _env = env;
        }

        public CityResultModel City { get; set; }
        public List<SentimentData> CitySentiment { get; set; } = new();
        public List<DashboardDataService.CommentData> PositiveComments { get; set; } = new();
        public List<DashboardDataService.CommentData> NegativeComments { get; set; } = new();

        public IActionResult OnGet(string cityName)
        {
            if (string.IsNullOrEmpty(cityName))
            {
                return RedirectToPage("/Rehber");
            }

            // --- KRİTİK DÜZELTME ADIMI: STRING NORMALIZATION ---
            // Gelen kelimenin baş harfini her zaman BÜYÜK, kalanını küçük yapıyoruz (Örn: 'belgrade' -> 'Belgrade')
            cityName = char.ToUpper(cityName[0]) + cityName.Substring(1).ToLower();

            var weights = new Dictionary<string, double> {
                {"cost", 0.25}, {"security", 0.25}, {"academic", 0.25}, {"social", 0.25}
            };

            string csvPath = Path.Combine(_env.ContentRootPath, "RawData", "gdelt_data.csv");

            var ekonomikVeriler = _dataService.GetEconomicData();
            var akademikVeriler = _dataService.GetAcademicData();
            var sosyalSkorlar = _dataService.GetSocialScoreData();
            var ogrenciKiraTry = _dataService.GetStudentRentData()
                .ToDictionary(r => r.City, r => r.PriceTry, StringComparer.OrdinalIgnoreCase);

            var allCities = _vikorService.CalculateVikorTop10(weights, csvPath, ekonomikVeriler, akademikVeriler, ogrenciKiraTry, sosyalSkorlar);

            City = allCities.FirstOrDefault(c => c.CityName.Equals(cityName, StringComparison.OrdinalIgnoreCase));
            DashboardDataService.ApplyMonthlyRentTry(allCities, ogrenciKiraTry);

            if (City == null)
            {
                City = new CityResultModel { CityName = cityName, Country = "Bilinmiyor", ImageUrl = "/Images/warsaw.jpg" };
            }

            // Sentiment ve yorum verilerini çekerken artık temizlenmiş 'cityName' gidiyor!
            CitySentiment = _dataService.GetSentimentData()
                .Where(s => s.City.Equals(cityName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var rawPositive = _dataService.GetCityComments(cityName, "Olumlu", 3);
            var rawNegative = _dataService.GetCityComments(cityName, "Olumsuz", 3);
            if (rawNegative.Count == 0) rawNegative = _dataService.GetCityComments(cityName, "Nötr", 3);

            PositiveComments = TranslateCommentsToTurkish(rawPositive);
            NegativeComments = TranslateCommentsToTurkish(rawNegative);

            return Page();
        }

        private List<DashboardDataService.CommentData> TranslateCommentsToTurkish(List<DashboardDataService.CommentData> comments)
        {
            var translatedList = new List<DashboardDataService.CommentData>();
            using var client = new HttpClient();

            foreach (var item in comments)
            {
                try
                {
                    string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=en&tl=tr&dt=t&q={Uri.EscapeDataString(item.Comment)}";
                    var responseText = client.GetStringAsync(url).Result;

                    using var doc = JsonDocument.Parse(responseText);
                    var mainArray = doc.RootElement[0];

                    string translatedText = "";
                    foreach (var part in mainArray.EnumerateArray())
                    {
                        translatedText += part[0].GetString();
                    }

                    if (!string.IsNullOrEmpty(translatedText))
                    {
                        translatedList.Add(new DashboardDataService.CommentData
                        {
                            Sentiment = item.Sentiment,
                            Comment = translatedText
                        });
                    }
                    else
                    {
                        translatedList.Add(item);
                    }
                }
                catch
                {
                    translatedList.Add(item);
                }
            }
            return translatedList;
        }
    }
}