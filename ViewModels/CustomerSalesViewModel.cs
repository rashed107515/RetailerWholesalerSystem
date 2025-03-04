using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RetailerWholesalerSystem.ViewModels
{
    public class CustomerSalesViewModel
    {
        public string CustomerId { get; set; }

        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Registration Date")]
        [DataType(DataType.Date)]
        public DateTime RegistrationDate { get; set; }

        [Display(Name = "Total Orders")]
        public int TotalOrders { get; set; }

        [Display(Name = "Total Spent")]
        [DataType(DataType.Currency)]
        public decimal TotalSpent { get; set; }

        [Display(Name = "Average Order Value")]
        [DataType(DataType.Currency)]
        public decimal AverageOrderValue { get; set; }

        [Display(Name = "Last Purchase Date")]
        [DataType(DataType.Date)]
        public DateTime LastPurchaseDate { get; set; }

        public List<CustomerOrderHistory> OrderHistory { get; set; } = new List<CustomerOrderHistory>();

        public List<CustomerProductPreference> ProductPreferences { get; set; } = new List<CustomerProductPreference>();
        public decimal TotalAmount { get; internal set; }
        public int TransactionCount { get; internal set; }
        public Models.ApplicationUser? Customer { get; internal set; }
    }

    public class CustomerOrderHistory
    {
        public int TransactionId { get; set; }

        [DataType(DataType.Date)]
        public DateTime OrderDate { get; set; }

        [DataType(DataType.Currency)]
        public decimal OrderTotal { get; set; }

        public int ItemCount { get; set; }

        public string Status { get; set; }
    }

    public class CustomerProductPreference
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; }

        public int QuantityPurchased { get; set; }

        [DataType(DataType.Currency)]
        public decimal TotalSpent { get; set; }

        [DisplayFormat(DataFormatString = "{0:P2}")]
        public decimal PercentageOfTotalSpend { get; set; }
    }
}