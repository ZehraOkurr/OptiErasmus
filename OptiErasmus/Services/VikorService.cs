using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using OptiErasmus.Models;
using System.Globalization;

namespace OptiErasmus.Services
{
    public class VikorService
    {
        private static readonly List<string> ValidCities = new()
        {
            "Lomza", "Warsaw", "Lisbon", "Porto", "Sofia", "Skopje", "Vilnius", "Belgrade"
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

        private static readonly Dictionary<string, string> UniToCity = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Lomza State University of Applied Sciences", "Lomza" },
            { "Vistula University", "Warsaw" },
            { "Universidade de Lisboa", "Lisbon" },
            { "University of Lisbon", "Lisbon" },
            { "Politecnico do Porto", "Porto" },
            { "University of Porto", "Porto" },
            { "University of Telecommunications and Post", "Sofia" },
            { "Ss. Cyril and Methodius University", "Skopje" },
            { "Saints Cyril and Methodius University in Skopje", "Skopje" },
            { "Vilnius University", "Vilnius" },
            { "University of Belgrade", "Belgrade" }
        };

        public List<CityResultModel> CalculateVikorTop10(
            Dictionary<string, double> weights,
            string csvPath,
            List<EconomicData> ecoData,
            List<AcademicData> acaData,
            IReadOnlyDictionary<string, double> studentRentTry,
            List<SocialScoreData>? socialScores = null)
        {
            var cityStats = LoadGdeltCityStats(csvPath);

            var cityCosts = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var cName in ValidCities)
            {
                if (studentRentTry.TryGetValue(cName, out double rentTry) && rentTry > 0)
                    cityCosts[cName] = rentTry;
                else
                    cityCosts[cName] = 0;
            }

            var missingCostCities = cityCosts.Where(c => c.Value <= 0).Select(c => c.Key).ToList();
            if (missingCostCities.Count > 0)
            {
                double avgRent = cityCosts.Values.Where(v => v > 0).DefaultIfEmpty(15000).Average();
                foreach (var city in missingCostCities)
                    cityCosts[city] = avgRent;
            }

            var cityAcaRaw = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var cName in ValidCities) cityAcaRaw[cName] = 0;

            foreach (var item in acaData)
            {
                if (UniToCity.TryGetValue(item.University, out string? mappedCity))
                    cityAcaRaw[mappedCity] += item.PublicationCount;
            }

            var socialByCity = (socialScores ?? new List<SocialScoreData>())
                .ToDictionary(s => s.City, s => s.Score, StringComparer.OrdinalIgnoreCase);

            double minC = cityCosts.Values.Min();
            double maxC = cityCosts.Values.Max();

            var acaWithData = cityAcaRaw.Values.Where(v => v > 0).ToList();
            double minA = acaWithData.Count > 0 ? acaWithData.Min() : 0;
            double maxA = acaWithData.Count > 0 ? acaWithData.Max() : 0;

            var results = new List<CityResultModel>();

            foreach (var city in ValidCities)
            {
                double sec = 50.0;
                double soc = 50.0;

                if (cityStats.TryGetValue(city, out var s) && s.Count > 0)
                {
                    double avgGoldstein = s.TotalGoldstein / s.Count;
                    double avgTone = s.TotalTone / s.Count;
                    sec = 50 + avgGoldstein * 5;
                    soc = 50 + avgTone * 5;
                }

                if (socialByCity.TryGetValue(city, out double youtubeScore))
                    soc = Math.Clamp(youtubeScore, 1, 100);

                double eScore = (maxC == minC) ? 70 : 100 - ((cityCosts[city] - minC) / (maxC - minC) * 80);

                double aScore;
                if (cityAcaRaw[city] == 0 || maxA == minA)
                    aScore = 50;
                else
                    aScore = 20 + ((cityAcaRaw[city] - minA) / (maxA - minA) * 80);

                results.Add(new CityResultModel
                {
                    CityName = city,
                    EconomyScore = Math.Clamp(Math.Round(eScore, 2), 1, 100),
                    AcademicScore = Math.Clamp(Math.Round(aScore, 2), 1, 100),
                    SecurityScore = Math.Clamp(Math.Round(sec, 2), 1, 100),
                    SocialScore = Math.Clamp(Math.Round(soc, 2), 1, 100)
                });
            }

            return ExecuteVikorLogic(results, weights);
        }

        private static Dictionary<string, (double TotalTone, double TotalGoldstein, int Count)> LoadGdeltCityStats(string csvPath)
        {
            var cityStats = new Dictionary<string, (double TotalTone, double TotalGoldstein, int Count)>(StringComparer.OrdinalIgnoreCase);

            if (!File.Exists(csvPath)) return cityStats;

            foreach (var line in File.ReadLines(csvPath).Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = CsvLineParser.Parse(line);
                if (parts.Count < 7) continue;

                string? city = NormalizeGdeltCity(parts[2]);
                if (city == null) continue;

                if (!double.TryParse(parts[4], NumberStyles.Any, CultureInfo.InvariantCulture, out double goldstein))
                    continue;
                if (!double.TryParse(parts[6], NumberStyles.Any, CultureInfo.InvariantCulture, out double avgTone))
                    continue;

                if (!cityStats.ContainsKey(city))
                    cityStats[city] = (0, 0, 0);

                var stat = cityStats[city];
                cityStats[city] = (stat.TotalTone + avgTone, stat.TotalGoldstein + goldstein, stat.Count + 1);
            }

            return cityStats;
        }

        private static string? NormalizeGdeltCity(string geoField)
        {
            if (string.IsNullOrWhiteSpace(geoField)) return null;

            string raw = geoField.Trim().Trim('"');
            string primary = raw.Split(',')[0].Trim();

            if (GdeltCityAliases.TryGetValue(primary, out string? alias))
                return alias;

            if (ValidCities.Contains(primary, StringComparer.OrdinalIgnoreCase))
                return ValidCities.First(c => c.Equals(primary, StringComparison.OrdinalIgnoreCase));

            return null;
        }

        private List<CityResultModel> ExecuteVikorLogic(List<CityResultModel> results, Dictionary<string, double> weights)
        {
            double fStar_Eco = results.Max(x => x.EconomyScore); double fMin_Eco = results.Min(x => x.EconomyScore);
            double fStar_Sec = results.Max(x => x.SecurityScore); double fMin_Sec = results.Min(x => x.SecurityScore);
            double fStar_Aca = results.Max(x => x.AcademicScore); double fMin_Aca = results.Min(x => x.AcademicScore);
            double fStar_Soc = results.Max(x => x.SocialScore); double fMin_Soc = results.Min(x => x.SocialScore);

            double wEco = weights.GetValueOrDefault("cost", 0.25);
            double wSec = weights.GetValueOrDefault("security", 0.25);
            double wAca = weights.GetValueOrDefault("academic", 0.25);
            double wSoc = weights.GetValueOrDefault("social", 0.25);

            foreach (var r in results)
            {
                double termEco = (fStar_Eco - fMin_Eco) == 0 ? 0 : wEco * (fStar_Eco - r.EconomyScore) / (fStar_Eco - fMin_Eco);
                double termSec = (fStar_Sec - fMin_Sec) == 0 ? 0 : wSec * (fStar_Sec - r.SecurityScore) / (fStar_Sec - fMin_Sec);
                double termAca = (fStar_Aca - fMin_Aca) == 0 ? 0 : wAca * (fStar_Aca - r.AcademicScore) / (fStar_Aca - fMin_Aca);
                double termSoc = (fStar_Soc - fMin_Soc) == 0 ? 0 : wSoc * (fStar_Soc - r.SocialScore) / (fStar_Soc - fMin_Soc);

                r.S_Score = termEco + termSec + termAca + termSoc;
                r.R_Score = Math.Max(termEco, Math.Max(termSec, Math.Max(termAca, termSoc)));
            }

            double sStar = results.Min(x => x.S_Score); double sMin = results.Max(x => x.S_Score);
            double rStar = results.Min(x => x.R_Score); double rMin = results.Max(x => x.R_Score);

            foreach (var r in results)
            {
                double qS = (sMin - sStar) == 0 ? 0 : 0.5 * (r.S_Score - sStar) / (sMin - sStar);
                double qR = (rMin - rStar) == 0 ? 0 : 0.5 * (r.R_Score - rStar) / (rMin - rStar);
                r.Q_Score = Math.Round(qS + qR, 4);
            }

            var cityDetails = new Dictionary<string, (string Country, string ImageUrl)>(StringComparer.OrdinalIgnoreCase)
            {
                {"Lomza", ("Polonya", "/images/lomza.jpg")},
                {"Warsaw", ("Polonya", "/images/warsaw.jpg")},
                {"Lisbon", ("Portekiz", "/images/lizbon.jpg")},
                {"Porto", ("Portekiz", "/images/porto.jpg")},
                {"Sofia", ("Bulgaristan", "/images/sofia.jpg")},
                {"Skopje", ("Kuzey Makedonya", "/images/skopje.jpg")},
                {"Vilnius", ("Litvanya", "/images/vilnius.jpg")},
                {"Belgrade", ("Sırbistan", "/images/belgrad.jpg")}
            };

            foreach (var r in results)
            {
                if (cityDetails.TryGetValue(r.CityName, out var info))
                {
                    r.Country = info.Country;
                    r.ImageUrl = info.ImageUrl;
                }
            }

            return results.OrderBy(x => x.Q_Score).ToList();
        }
    }
}
