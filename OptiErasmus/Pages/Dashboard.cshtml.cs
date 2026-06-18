using Microsoft.AspNetCore.Mvc.RazorPages;
using OptiErasmus.Models;
using OptiErasmus.Services;
using System.Collections.Generic;

namespace OptiErasmus.Pages
{
    public class DashboardModel : PageModel
    {
        private readonly DashboardDataService _dataService;

        // Grafiklerin beslendiği veri listeleri
        public List<AcademicData> AcademicData { get; set; } = new();
        public List<EconomicData> EconomicData { get; set; } = new();
        public List<EconomicData> EconomicDashboardData { get; set; } = new();
        public List<SocialScoreData> SocialScoreData { get; set; } = new();
        public List<SentimentData> SentimentData { get; set; } = new();

        // Üst kısım kartları için dinamik veriler
        public int TotalRows { get; set; }
        public int TotalCities { get; set; }

        public DashboardModel(DashboardDataService dataService)
        {
            _dataService = dataService;
        }

        public void OnGet()
        {
            // 1. Üst kart istatistiklerini doldur (84k satır ve 8 lokasyon)
            var stats = _dataService.GetDynamicStats();
            TotalRows = stats.RowCount;
            TotalCities = 8;

            // 2. Grafiklerin içini doldur (Boş gelmemesi için bu satırlar kritiktir)
            AcademicData = _dataService.GetAcademicData();
            EconomicData = _dataService.GetEconomicData();
            EconomicDashboardData = _dataService.GetEconomicDashboardData();
            SocialScoreData = _dataService.GetSocialScoreData();
            SentimentData = _dataService.GetSentimentData();
        }
    }
}