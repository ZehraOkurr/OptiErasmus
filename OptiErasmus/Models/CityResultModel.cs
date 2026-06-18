using System.Collections.Generic;

namespace OptiErasmus.Models
{
    public class CityResultModel
    {
        public string CityName { get; set; }
        public double EconomyScore { get; set; }
        public double SecurityScore { get; set; }
        public double AcademicScore { get; set; }
        public double SocialScore { get; set; }
        
        // VIKOR Values
        public double S_Score { get; set; }
        public double R_Score { get; set; }
        public double Q_Score { get; set; }

        public string Country { get; set; }
        public string ImageUrl { get; set; }

        /// <summary>Aylık öğrenci kirası (yurt / paylaşımlı oda), güncel kur ile TL.</summary>
        public double MonthlyRentTry { get; set; }
    }
}
