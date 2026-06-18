using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using OptiErasmus.Services;
using OptiErasmus.Models;

namespace OptiErasmus.Pages
{
    public class CompareModel : PageModel
    {
        // 1. ADIM: Canlı verileri çekmek için Servislerimizi ekliyoruz
        private readonly DashboardDataService _dataService;
        private readonly VikorService _vikorService;
        private readonly IWebHostEnvironment _env;

        public CompareModel(DashboardDataService dataService, VikorService vikorService, IWebHostEnvironment env)
        {
            _dataService = dataService;
            _vikorService = vikorService;
            _env = env;
        }

        [BindProperty(SupportsGet = true)]
        public string Sehir1 { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Sehir2 { get; set; }

        // Dropdown için şehir listemiz
        public List<string> SehirListesi { get; set; } = new List<string>
        {
            "Warsaw", "Lisbon", "Sofia", "Skopje", "Vilnius", "Belgrade", "Porto", "Lomza"
        };

        // NOT: O uzun statik "SehirVerileri" sözlüğünü sildik! Artık veriler canlı gelecek.

        public SehirDetay SecilenSehir1 { get; set; }
        public SehirDetay SecilenSehir2 { get; set; }

        public void OnGet()
        {
            // Sayfa ilk açıldığında varsayılan olarak iki şehir seçelim
            if (string.IsNullOrEmpty(Sehir1)) Sehir1 = "Warsaw";
            if (string.IsNullOrEmpty(Sehir2)) Sehir2 = "Lisbon";

            // 2. ADIM: Canlı API ve GDELT verilerini çekiyoruz
            var ekonomikVeriler = _dataService.GetEconomicData();
            var akademikVeriler = _dataService.GetAcademicData();
            var sosyalSkorlar = _dataService.GetSocialScoreData();
            var ogrenciKira = _dataService.GetStudentRentData();

            string csvPath = Path.Combine(_env.ContentRootPath, "RawData", "gdelt_data.csv");
            var weights = new Dictionary<string, double> { { "cost", 0.25 }, { "security", 0.25 }, { "academic", 0.25 }, { "social", 0.25 } };
            var ogrenciKiraTry = ogrenciKira.ToDictionary(r => r.City, r => r.PriceTry, StringComparer.OrdinalIgnoreCase);

            var vikorSonuclar = _vikorService.CalculateVikorTop10(weights, csvPath, ekonomikVeriler, akademikVeriler, ogrenciKiraTry, sosyalSkorlar);
            DashboardDataService.ApplyMonthlyRentTry(vikorSonuclar, ogrenciKiraTry);

            SecilenSehir1 = GetDinamikSehirDetay(Sehir1, ekonomikVeriler, ogrenciKira, vikorSonuclar);
            SecilenSehir2 = GetDinamikSehirDetay(Sehir2, ekonomikVeriler, ogrenciKira, vikorSonuclar);
        }

        private SehirDetay GetDinamikSehirDetay(string cityName, List<EconomicData> ecoData, List<StudentRentData> studentRents, List<CityResultModel> vikorData)
        {
            var cityVikor = vikorData.FirstOrDefault(c => c.CityName.Equals(cityName, System.StringComparison.OrdinalIgnoreCase));
            var rent = studentRents.FirstOrDefault(r => r.City.Equals(cityName, System.StringComparison.OrdinalIgnoreCase));
            var ucuzYemek = _dataService.GetInexpensiveRestaurantPriceTry(cityName) ?? 0;

            return new SehirDetay
            {
                Ulke = cityVikor?.Country ?? "Avrupa",
                UcuzYemek = System.Math.Round(ucuzYemek, 2),
                SosyalSkor = cityVikor?.SocialScore ?? 50,
                Kira = rent?.PriceTry ?? cityVikor?.MonthlyRentTry ?? 0,
                Resim = cityVikor?.ImageUrl ?? "/Images/warsaw.jpg"
            };
        }
    }

    public class SehirDetay
    {
        public string Ulke { get; set; }
        public double UcuzYemek { get; set; }
        public double SosyalSkor { get; set; }
        public double Kira { get; set; }
        public string Resim { get; set; }
    }
}