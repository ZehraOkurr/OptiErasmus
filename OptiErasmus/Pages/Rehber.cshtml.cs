using Microsoft.AspNetCore.Mvc.RazorPages;
using OptiErasmus.Models;
using OptiErasmus.Services;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace OptiErasmus.Pages
{
    public class RehberModel : PageModel
    {
        private readonly IWebHostEnvironment _env;
        private readonly DashboardDataService _dataService;
        private readonly VikorService _vikorService;

        public List<CityResultModel> Cities { get; set; } = new List<CityResultModel>();

        // Tıpkı Anket sayfasında yaptığımız gibi servisleri buraya tanımlıyoruz (Dependency Injection)
        public RehberModel(IWebHostEnvironment env, DashboardDataService dataService, VikorService vikorService)
        {
            _env = env;
            _dataService = dataService;
            _vikorService = vikorService;
        }

        public void OnGet()
        {
            string csvPath = Path.Combine(_env.ContentRootPath, "RawData", "gdelt_data.csv");

            var weights = new Dictionary<string, double> {
                {"cost", 0.25}, {"security", 0.25}, {"academic", 0.25}, {"social", 0.25}
            };

            // Artık servisleri tanıdığı için bu verileri hatasız çekebilir!
            var ekonomikVeriler = _dataService.GetEconomicData();
            var akademikVeriler = _dataService.GetAcademicData();
            var sosyalSkorlar = _dataService.GetSocialScoreData();
            var ogrenciKiraTry = _dataService.GetStudentRentData()
                .ToDictionary(r => r.City, r => r.PriceTry, StringComparer.OrdinalIgnoreCase);

            var rankedCities = _vikorService.CalculateVikorTop10(weights, csvPath, ekonomikVeriler, akademikVeriler, ogrenciKiraTry, sosyalSkorlar);
            DashboardDataService.ApplyMonthlyRentTry(rankedCities, ogrenciKiraTry);
            Cities = rankedCities;
        }
    }
}