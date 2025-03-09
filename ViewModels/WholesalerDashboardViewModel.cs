using System;
using System.Collections.Generic;

namespace RetailerWholesalerSystem.ViewModels
{
    public class WholesalerDashboardViewModel
    {
        public int TotalProducts { get; set; }
        public int ActiveOrders { get; set; }
        public int LowStockItems { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public List<OrderSummary> RecentOrders { get; set; }
        public List<LowStockProduct> LowStockProducts { get; set; }
    }

    public class OrderSummary
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public string RetailerName { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
    }

    public class LowStockProduct
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Quantity { get; set; }
    }
}