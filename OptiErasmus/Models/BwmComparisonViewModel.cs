using System.Collections.Generic;

namespace OptiErasmus.Models
{
    public class BwmComparisonViewModel
    {
        public string Best { get; set; }
        public string Worst { get; set; }
        public Dictionary<string, int> BestToOthers { get; set; }
        public Dictionary<string, int> OthersToWorst { get; set; }
    }
}
