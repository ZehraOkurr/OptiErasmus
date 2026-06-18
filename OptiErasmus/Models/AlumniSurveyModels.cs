using System;
using System.Collections.Generic;

namespace OptiErasmus.Models
{
    public class AlumniSurveyEntry
    {
        public int Id { get; set; }
        public string City { get; set; } = string.Empty;
        public int Satisfaction { get; set; }
        public bool WouldChooseAgain { get; set; }
        public string TopCriterion { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
    }

    public class AlumniSurveySubmitDto
    {
        public string City { get; set; } = string.Empty;
        public int Satisfaction { get; set; }
        public bool WouldChooseAgain { get; set; }
        public string TopCriterion { get; set; } = string.Empty;
    }

    public class AlumniCityAggregate
    {
        public string City { get; set; } = string.Empty;
        public int ResponseCount { get; set; }
        public double AvgSatisfaction { get; set; }
        public double WouldChooseAgainPercent { get; set; }
        public string MostImportantCriterion { get; set; } = string.Empty;
        public int AlumniRank { get; set; }
    }

    public class ModelAlumniComparisonRow
    {
        public string City { get; set; } = string.Empty;
        public int ModelRank { get; set; }
        public int AlumniRank { get; set; }
        public int RankDifference { get; set; }
        public double ModelQScore { get; set; }
        public double AlumniSatisfaction { get; set; }
        public double WouldChooseAgainPercent { get; set; }
        public string Alignment { get; set; } = string.Empty;
    }

    public class AlumniComparisonResult
    {
        public List<ModelAlumniComparisonRow> Rows { get; set; } = new();
        public List<AlumniCityAggregate> AlumniStats { get; set; } = new();
        public List<CityResultModel> ModelRanking { get; set; } = new();
        public Dictionary<string, int> CriterionDistribution { get; set; } = new();
        public double RankCorrelation { get; set; }
        public int TotalResponses { get; set; }
    }
}
