using Microsoft.AspNetCore.Hosting;
using OptiErasmus.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace OptiErasmus.Services
{
    public class AlumniSurveyService
    {
        private static readonly string[] ValidCities =
        {
            "Lomza", "Warsaw", "Lisbon", "Porto", "Sofia", "Skopje", "Vilnius", "Belgrade"
        };

        private static readonly HashSet<string> ValidCriteria = new(StringComparer.OrdinalIgnoreCase)
        {
            "cost", "security", "academic", "social"
        };

        private static readonly Dictionary<string, string> CriterionLabels = new(StringComparer.OrdinalIgnoreCase)
        {
            { "cost", "Ekonomi (Uygunluk)" },
            { "security", "Güvenlik" },
            { "academic", "Akademik Kalite" },
            { "social", "Sosyal Yaşam" }
        };

        private readonly IWebHostEnvironment _env;

        public AlumniSurveyService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public static string GetCriterionLabel(string key) =>
            CriterionLabels.TryGetValue(key, out string? label) ? label : key;

        private string CsvPath => Path.Combine(_env.ContentRootPath, "RawData", "erasmus_alumni_survey.csv");

        public IReadOnlyList<string> GetValidCities() => ValidCities;

        public List<AlumniSurveyEntry> GetResponses()
        {
            EnsureFileExists();
            var result = new List<AlumniSurveyEntry>();
            var lines = File.ReadAllLines(CsvPath);
            if (lines.Length < 2) return result;

            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = CsvLineParser.Parse(line);
                if (parts.Count < 5) continue;

                if (!int.TryParse(parts[0], out int id)) continue;
                string city = parts[1].Trim('"', ' ');
                if (!ValidCities.Contains(city, StringComparer.OrdinalIgnoreCase)) continue;
                if (!int.TryParse(parts[2], out int satisfaction)) continue;

                bool wouldAgain = parts[3].Trim().Equals("Evet", StringComparison.OrdinalIgnoreCase);
                string criterion = parts[4].Trim().ToLowerInvariant();
                if (!ValidCriteria.Contains(criterion)) continue;

                DateTime submitted = DateTime.UtcNow;
                if (parts.Count >= 6 && DateTime.TryParse(parts[5], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dt))
                    submitted = dt;

                result.Add(new AlumniSurveyEntry
                {
                    Id = id,
                    City = city,
                    Satisfaction = Math.Clamp(satisfaction, 1, 5),
                    WouldChooseAgain = wouldAgain,
                    TopCriterion = criterion,
                    SubmittedAt = submitted
                });
            }

            return result;
        }

        public AlumniSurveyEntry AddResponse(AlumniSurveySubmitDto dto)
        {
            if (!ValidCities.Contains(dto.City, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException("Geçersiz şehir seçimi.");

            if (dto.Satisfaction < 1 || dto.Satisfaction > 5)
                throw new ArgumentException("Memnuniyet 1-5 arasında olmalıdır.");

            string criterion = dto.TopCriterion?.Trim().ToLowerInvariant() ?? string.Empty;
            if (!ValidCriteria.Contains(criterion))
                throw new ArgumentException("Geçersiz kriter seçimi.");

            var responses = GetResponses();
            int nextId = responses.Count > 0 ? responses.Max(r => r.Id) + 1 : 1;

            var entry = new AlumniSurveyEntry
            {
                Id = nextId,
                City = ValidCities.First(c => c.Equals(dto.City, StringComparison.OrdinalIgnoreCase)),
                Satisfaction = dto.Satisfaction,
                WouldChooseAgain = dto.WouldChooseAgain,
                TopCriterion = criterion,
                SubmittedAt = DateTime.UtcNow
            };

            AppendToCsv(entry);
            return entry;
        }

        public AlumniComparisonResult BuildComparison(List<CityResultModel> modelRanking)
        {
            var responses = GetResponses();
            var alumniStats = BuildAlumniAggregates(responses);
            var modelByCity = modelRanking
                .Select((c, i) => new { c.CityName, Rank = i + 1, c.Q_Score })
                .ToDictionary(x => x.CityName, x => (x.Rank, x.Q_Score), StringComparer.OrdinalIgnoreCase);

            var rows = new List<ModelAlumniComparisonRow>();
            foreach (var city in ValidCities)
            {
                modelByCity.TryGetValue(city, out var modelInfo);
                var alumni = alumniStats.FirstOrDefault(a => a.City.Equals(city, StringComparison.OrdinalIgnoreCase));
                int modelRank = modelInfo.Rank > 0 ? modelInfo.Rank : 8;
                int alumniRank = alumni?.AlumniRank ?? 8;
                int diff = Math.Abs(modelRank - alumniRank);

                rows.Add(new ModelAlumniComparisonRow
                {
                    City = city,
                    ModelRank = modelRank,
                    AlumniRank = alumniRank,
                    RankDifference = diff,
                    ModelQScore = modelInfo.Q_Score,
                    AlumniSatisfaction = alumni?.AvgSatisfaction ?? 0,
                    WouldChooseAgainPercent = alumni?.WouldChooseAgainPercent ?? 0,
                    Alignment = diff == 0 ? "Tam Uyum" : diff <= 2 ? "Yakın Uyum" : "Farklı Algı"
                });
            }

            var criterionDist = responses
                .GroupBy(r => r.TopCriterion, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            return new AlumniComparisonResult
            {
                Rows = rows.OrderBy(r => r.ModelRank).ToList(),
                AlumniStats = alumniStats,
                ModelRanking = modelRanking,
                CriterionDistribution = criterionDist,
                RankCorrelation = CalculateRankCorrelation(rows),
                TotalResponses = responses.Count
            };
        }

        private List<AlumniCityAggregate> BuildAlumniAggregates(List<AlumniSurveyEntry> responses)
        {
            var grouped = ValidCities.Select(city =>
            {
                var subset = responses.Where(r => r.City.Equals(city, StringComparison.OrdinalIgnoreCase)).ToList();
                if (subset.Count == 0)
                {
                    return new AlumniCityAggregate
                    {
                        City = city,
                        ResponseCount = 0,
                        AvgSatisfaction = 0,
                        WouldChooseAgainPercent = 0,
                        MostImportantCriterion = "-"
                    };
                }

                var criterionMode = subset
                    .GroupBy(s => s.TopCriterion, StringComparer.OrdinalIgnoreCase)
                    .OrderByDescending(g => g.Count())
                    .First().Key;

                return new AlumniCityAggregate
                {
                    City = city,
                    ResponseCount = subset.Count,
                    AvgSatisfaction = Math.Round(subset.Average(s => s.Satisfaction), 2),
                    WouldChooseAgainPercent = Math.Round(subset.Count(s => s.WouldChooseAgain) * 100.0 / subset.Count, 1),
                    MostImportantCriterion = GetCriterionLabel(criterionMode)
                };
            }).ToList();

            var ranked = grouped
                .Where(g => g.ResponseCount > 0)
                .OrderByDescending(g => g.AvgSatisfaction)
                .ThenByDescending(g => g.WouldChooseAgainPercent)
                .ToList();

            int rank = 1;
            foreach (var item in ranked)
            {
                item.AlumniRank = rank++;
                var target = grouped.First(g => g.City == item.City);
                target.AlumniRank = item.AlumniRank;
            }

            return grouped;
        }

        private static double CalculateRankCorrelation(List<ModelAlumniComparisonRow> rows)
        {
            var valid = rows.Where(r => r.AlumniSatisfaction > 0).ToList();
            if (valid.Count < 3) return 0;

            double n = valid.Count;
            double sumD2 = valid.Sum(r =>
            {
                double d = r.ModelRank - r.AlumniRank;
                return d * d;
            });

            // Spearman rho = 1 - (6 * sum(d²)) / (n * (n² - 1))
            return Math.Round(1.0 - (6.0 * sumD2) / (n * (n * n - 1.0)), 2);
        }

        private void AppendToCsv(AlumniSurveyEntry entry)
        {
            EnsureFileExists();
            string again = entry.WouldChooseAgain ? "Evet" : "Hayir";
            string line = $"{entry.Id},{entry.City},{entry.Satisfaction},{again},{entry.TopCriterion},{entry.SubmittedAt:yyyy-MM-dd}";
            File.AppendAllText(CsvPath, Environment.NewLine + line);
        }

        private void EnsureFileExists()
        {
            if (File.Exists(CsvPath)) return;

            var dir = Path.GetDirectoryName(CsvPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(CsvPath, "Id,City,Satisfaction,WouldChooseAgain,TopCriterion,SubmittedAt" + Environment.NewLine);
        }
    }
}
