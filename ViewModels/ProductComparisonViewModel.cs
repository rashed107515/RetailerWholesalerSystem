using System.Collections.Generic;
using RetailerWholesalerSystem.Models;

namespace RetailerWholesalerSystem.ViewModels
{
    public class ProductComparisonViewModel
    {
        public string SearchTerm { get; set; }
        public string Category { get; set; }
        public List<WholesalerProduct> Products { get; set; }
        public List<string> Categories { get; set; }
    }
}