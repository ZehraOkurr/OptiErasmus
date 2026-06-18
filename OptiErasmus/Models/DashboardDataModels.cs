namespace OptiErasmus.Models
{
    public class AcademicData
    {
        public string University { get; set; } = string.Empty;
        public int PublicationCount { get; set; }
    }

    public class EconomicData
    {
        public string City { get; set; } = string.Empty;
        public string Criteria { get; set; } = string.Empty;
        public double Price { get; set; }
    }

    public class SocialScoreData
    {
        public string City { get; set; } = string.Empty;
        public double Score { get; set; }
    }

    public class SentimentData
    {
        public string City { get; set; } = string.Empty;
        public string Sentiment { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
