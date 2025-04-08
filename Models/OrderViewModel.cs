using System.Collections.Generic;

namespace RetailerWholesalerSystem.Models
{
    // ViewModel for order creation and display
    public class OrderViewModel
    {
        public string WholesalerID { get; set; }
        public string WholesalerName { get; set; }
        public List<OrderItemViewModel> OrderItems { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class OrderItemViewModel
    {
        public int WholesalerProductID { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal ItemTotal { get; set; }
    }
}