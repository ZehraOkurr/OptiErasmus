namespace OptiErasmus.Models
{
    public class StudentRentData
    {
        public string City { get; set; } = string.Empty;
        public string HousingType { get; set; } = string.Empty;
        public double PriceLocal { get; set; }
        public string Currency { get; set; } = "EUR";
        public double PriceTry { get; set; }
        public string Note { get; set; } = string.Empty;
    }
}
