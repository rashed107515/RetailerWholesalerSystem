using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using RetailerWholesalerSystem.Models;

namespace RetailerWholesalerSystem.ViewModels
{
    public class ProductSalesViewModel
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; }

        public string ProductSKU { get; set; }

        public string Category { get; set; }

        [Display(Name = "Total Units Sold")]
        public int TotalUnitsSold { get; set; }

        [Display(Name = "Total Revenue")]
        [DataType(DataType.Currency)]
        public decimal TotalRevenue { get; set; }

        [Display(Name = "Average Price")]
        [DataType(DataType.Currency)]
        public decimal AveragePrice { get; set; }

        [Display(Name = "Profit Margin")]
        [DisplayFormat(DataFormatString = "{0:P2}")]
        public decimal ProfitMargin { get; set; }

        [Display(Name = "Time Period")]
        public string TimePeriod { get; set; }

        public List<MonthlySales> SalesByMonth { get; set; } = new List<MonthlySales>();

        public List<WholesalerComparison> WholesalerComparisons { get; set; } = new List<WholesalerComparison>();
        public Product? Product { get; internal set; }
        public int TotalQuantity { get; internal set; }
        public decimal TotalAmount { get; internal set; }
    }

    public class MonthlySales
    {
        public string Month { get; set; }

        public int UnitsSold { get; set; }

        [DataType(DataType.Currency)]
        public decimal Revenue { get; set; }
    }

    public class WholesalerComparison
    {
        public string WholesalerName { get; set; }

        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [DisplayFormat(DataFormatString = "{0:P2}")]
        public decimal PriceDifference { get; set; }
    }
}