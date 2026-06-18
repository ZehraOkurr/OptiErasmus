using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Hosting;
using OptiErasmus.Models;
using OptiErasmus.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DinkToPdf;
using DinkToPdf.Contracts;

namespace OptiErasmus.Pages
{
    [IgnoreAntiforgeryToken]
    public class AnketModel : PageModel
    {
        private readonly IWebHostEnvironment _env;
        private readonly DashboardDataService _dashboardService;
        private readonly VikorService _vikorService;
        private readonly IConverter _pdfConverter; // DinkToPdf servisini ekledik

        // 1. ADIM: Servisleri (Dashboard, Vikor ve PDF) sisteme enjekte ediyoruz
        public AnketModel(IWebHostEnvironment env, DashboardDataService dashboardService, VikorService vikorService, IConverter pdfConverter)
        {
            _env = env;
            _dashboardService = dashboardService;
            _vikorService = vikorService;
            _pdfConverter = pdfConverter;
        }

        public void OnGet()
        {
        }

        [HttpPost]
        public IActionResult OnPost([FromBody] BwmComparisonViewModel model)
        {
            if (model == null || model.BestToOthers == null)
            {
                return BadRequest("Geçersiz veya boş anket verisi.");
            }

            var weights = CalculateBwmWeights(model);

            string csvPath = Path.Combine(_env.ContentRootPath, "RawData", "gdelt_data.csv");

            var ekonomikVeriler = _dashboardService.GetEconomicData();
            var akademikVeriler = _dashboardService.GetAcademicData();
            var sosyalSkorlar = _dashboardService.GetSocialScoreData();
            var ogrenciKiraTry = _dashboardService.GetStudentRentData()
                .ToDictionary(r => r.City, r => r.PriceTry, StringComparer.OrdinalIgnoreCase);

            var rankedCities = _vikorService.CalculateVikorTop10(weights, csvPath, ekonomikVeriler, akademikVeriler, ogrenciKiraTry, sosyalSkorlar);
            DashboardDataService.ApplyMonthlyRentTry(rankedCities, ogrenciKiraTry);

            return new JsonResult(new
            {
                Weights = weights,
                Cities = rankedCities
            });
        }

        // --- YENİ ADIM: PDF EXPORT METODU ---
        [HttpPost]
        public IActionResult OnPostExportPdf([FromBody] List<CityResultModel> rankedCities)
        {
            if (rankedCities == null || !rankedCities.Any())
            {
                return BadRequest("Yazdırılacak sıralama verisi bulunamadı.");
            }

            // Kurumsal HTML Tasarımı oluşturuluyor
            var htmlBuilder = new StringBuilder();
            htmlBuilder.Append(@"
            <html>
            <head>
                <style>
                    body { font-family: 'Arial', sans-serif; color: #2d3748; margin: 20px; }
                    .header { border-bottom: 3px solid #2b6cb0; padding-bottom: 10px; margin-bottom: 20px; }
                    .title { color: #1a365d; font-size: 24px; font-weight: bold; margin: 0; text-transform: uppercase; }
                    .subtitle { color: #4a5568; font-size: 13px; margin: 5px 0 0 0; font-style: italic; }
                    .info-box { background-color: #ebf8ff; border-left: 4px solid #3182ce; padding: 10px; margin-bottom: 20px; font-size: 12px; }
                    .report-table { width: 100%; border-collapse: collapse; margin-top: 15px; }
                    .report-table th { background-color: #2b6cb0; color: white; padding: 10px; text-align: left; font-size: 12px; }
                    .report-table td { padding: 10px; border: 1px solid #e2e8f0; font-size: 11px; }
                    .report-table tr:nth-child(even) { background-color: #f7fafc; }
                    .highlight { background-color: #f0fff4; font-weight: bold; }
                    .score { font-weight: bold; color: #2c5282; }
                </style>
            </head>
            <body>
                <div class='header'>
                    <h1 class='title'>OptiErasmus</h1>
                    <h2 class='subtitle'>Kişiselleştirilmiş Erasmus Üniversite Seçim ve Yol Haritası Raporu</h2>
                </div>
                
                <div class='info-box'>
                    <strong>Öğrenci:</strong> Zehra Okur &nbsp;|&nbsp; 
                    <strong>Tarih:</strong> 11 Haziran 2026 &nbsp;|&nbsp; 
                    <strong>Yöntem:</strong> BWM & VIKOR Hibrit Karar Modeli
                </div>

                <p style='font-size:12px;'>Anket tercihleriniz doğrultusunda, güncel bütçe (kira/yemek), GDELT güvenlik endeksleri ve YouTube NLP sosyal tatmin skorları harmanlanarak hesaplanan optimum sıralamanız aşağıdadır:</p>

                <table class='report-table'>
                    <thead>
                        <tr>
                            <th>Sıra</th>
                            <th>Şehir / Ülke</th>
                            <th>Aylık Kira (TL)</th>
                            <th>Ekonomi Skoru</th>
                            <th>Sosyal Skor</th>
                            <th>Güvenlik Skoru</th>
                            <th>Q Skoru (En Küçük İyi)</th>
                        </tr>
                    </thead>
                    <tbody>");

            int rank = 1;
            foreach (var city in rankedCities)
            {
                // En optimum seçeneğe (1. sıra) hafif yeşil vurgu veriyoruz
                string rowClass = (rank == 1) ? "class='highlight'" : "";

                htmlBuilder.Append($@"
                    <tr {rowClass}>
                        <td>{rank}</td>
                        <td>{city.CityName} / {city.Country}</td>
                        <td>{city.MonthlyRentTry:N0} TL</td>
                        <td>{city.EconomyScore:F2}</td>
                        <td>{city.SocialScore:F2}</td>
                        <td>{city.SecurityScore:F2}</td>
                        <td class='score'>{city.Q_Score:F4}</td>
                    </tr>");
                rank++;
            }

            htmlBuilder.Append(@"
                    </tbody>
                </table>
                <br/>
                <p style='font-size:10px; color:#718096; text-align:center; margin-top:30px;'>
                    OptiErasmus © 2026 - Bursa Uludağ Üniversitesi İİBF Yönetim Bilişim Sistemleri Bölümü
                </p>
            </body>
            </html>");

            // DinkToPdf Sayfa Yapılandırması
            var globalSettings = new GlobalSettings
            {
                ColorMode = ColorMode.Color,
                Orientation = Orientation.Portrait,
                PaperSize = PaperKind.A4,
                Margins = new MarginSettings { Top = 15, Bottom = 15, Left = 15, Right = 15 }
            };

            var objectSettings = new ObjectSettings
            {
                PagesCount = true,
                HtmlContent = htmlBuilder.ToString(),
                WebSettings = { DefaultEncoding = "utf-8" }
            };

            var pdf = new HtmlToPdfDocument()
            {
                GlobalSettings = globalSettings,
                Objects = { objectSettings }
            };

            var fileBytes = _pdfConverter.Convert(pdf);

            // Üretilen PDF'i tarayıcıya byte dizisi olarak fırlatıyoruz
            return File(fileBytes, "application/pdf", "OptiErasmus_Yol_Haritasi.pdf");
        }

        private static Dictionary<string, double> CalculateBwmWeights(BwmComparisonViewModel model)
        {
            var criteria = new[] { "cost", "security", "academic", "social" };
            var bestToWorst = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var othersToWorst = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            foreach (var c in criteria)
            {
                bestToWorst[c] = 1;
                othersToWorst[c] = 1;
            }

            if (model.BestToOthers != null)
            {
                foreach (var kvp in model.BestToOthers)
                {
                    if (kvp.Value > 0)
                        bestToWorst[kvp.Key] = kvp.Value;
                }
            }

            if (model.OthersToWorst != null)
            {
                foreach (var kvp in model.OthersToWorst)
                {
                    if (kvp.Value > 0)
                        othersToWorst[kvp.Key] = kvp.Value;
                }
            }

            double maxBtw = bestToWorst.Values.Max();
            double maxOtw = othersToWorst.Values.Max();

            var weights = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            double denominator = 0;

            foreach (var c in criteria)
            {
                double xi = (bestToWorst[c] / maxBtw) + (maxOtw / othersToWorst[c]);
                weights[c] = 1.0 / xi;
                denominator += weights[c];
            }

            foreach (var c in criteria)
                weights[c] /= denominator;

            return weights;
        }
    }
}