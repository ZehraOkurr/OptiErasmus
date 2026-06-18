using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OptiErasmus.Services;

namespace OptiErasmus.Pages
{
    public class IndexModel : PageModel
    {
        private readonly DashboardDataService _dataService;

        // Frontend tarafında erişilecek olan dinamik değişkenler
        public int TotalRows { get; set; }
        public int TotalCities { get; set; }

        // Dependency Injection ile servisi içeri alıyoruz
        public IndexModel(DashboardDataService dataService)
        {
            _dataService = dataService;
        }

        public void OnGet()
        {
            // DashboardDataService içindeki GetDynamicStats metodunu çağırıyoruz
            var stats = _dataService.GetDynamicStats();

            // Verileri sayfa özelliklerine atıyoruz
            TotalRows = stats.RowCount;
            TotalCities = 8;
        }
    }
}