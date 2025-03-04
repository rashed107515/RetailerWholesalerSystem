using System.Collections.Generic;
using RetailerWholesalerSystem.Models;

namespace RetailerWholesalerSystem.ViewModels
{
    public class DashboardViewModel
    {
        public decimal TotalSpent { get; set; } // For retailers
        public decimal TotalEarned { get; set; } // For wholesalers
        public decimal OutstandingAmount { get; set; }
        public int PendingTransactions { get; set; }
        public List<Transaction> RecentTransactions { get; set; }
        public Dictionary<string, decimal> MonthlySummary { get; set; } // Month, Amount
    }
}