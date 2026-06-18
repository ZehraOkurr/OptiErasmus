using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OptiErasmus.Models;
using OptiErasmus.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OptiErasmus.Pages
{
    [IgnoreAntiforgeryToken]
    public class DeneyimModel : PageModel
    {
        private readonly IWebHostEnvironment _env;
        private readonly DashboardDataService _dashboardService;
        private readonly VikorService _vikorService;
        private readonly AlumniSurveyService _alumniService;

        public DeneyimModel(
            IWebHostEnvironment env,
            DashboardDataService dashboardService,
            VikorService vikorService,
            AlumniSurveyService alumniService)
        {
            _env = env;
            _dashboardService = dashboardService;
            _vikorService = vikorService;
            _alumniService = alumniService;
        }

        public AlumniComparisonResult Comparison { get; set; } = new();
        public IReadOnlyList<string> Cities { get; set; } = new List<string>();

        public void OnGet()
        {
            Cities = _alumniService.GetValidCities();
            Comparison = BuildComparison();
        }

        [HttpPost]
        public IActionResult OnPostSubmit([FromBody] AlumniSurveySubmitDto dto)
        {
            try
            {
                _alumniService.AddResponse(dto);
                var comparison = BuildComparison();
                return new JsonResult(new
                {
                    success = true,
                    message = "Deneyiminiz kaydedildi. Teşekkürler!",
                    comparison
                });
            }
            catch (System.ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private AlumniComparisonResult BuildComparison()
        {
            var weights = new Dictionary<string, double>
            {
                { "cost", 0.25 },
                { "security", 0.25 },
                { "academic", 0.25 },
                { "social", 0.25 }
            };

            string csvPath = Path.Combine(_env.ContentRootPath, "RawData", "gdelt_data.csv");
            var ekonomikVeriler = _dashboardService.GetEconomicData();
            var akademikVeriler = _dashboardService.GetAcademicData();
            var sosyalSkorlar = _dashboardService.GetSocialScoreData();
            var ogrenciKiraTry = _dashboardService.GetStudentRentData()
                .ToDictionary(r => r.City, r => r.PriceTry, System.StringComparer.OrdinalIgnoreCase);

            var modelRanking = _vikorService.CalculateVikorTop10(
                weights, csvPath, ekonomikVeriler, akademikVeriler, ogrenciKiraTry, sosyalSkorlar);

            DashboardDataService.ApplyMonthlyRentTry(modelRanking, ogrenciKiraTry);
            return _alumniService.BuildComparison(modelRanking);
        }
    }
}
