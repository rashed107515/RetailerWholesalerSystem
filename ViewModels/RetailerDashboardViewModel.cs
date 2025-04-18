using System;
using System.Collections.Generic;
using RetailerWholesalerSystem.Models;

namespace RetailerWholesalerSystem.ViewModels
{
    public class RetailerDashboardViewModel
    {
        public int TotalProducts { get; set; }
        public int TotalStock { get; set; }
        public int LowStockProducts { get; set; }
        public decimal InventoryValue { get; set; }
        public List<Order> RecentOrders { get; set; }
        public int PendingOrdersCount { get; set; }
        public int CompletedOrdersCount { get; set; }
    }
}