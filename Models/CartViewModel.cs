using System.Collections.Generic;

namespace RetailerWholesalerSystem.Models
{
    // View model for displaying cart items grouped by wholesaler
    public class CartViewModel
    {
        public List<CartItemGroupViewModel> CartGroups { get; set; }
        public decimal TotalAmount { get; set; }
    }

    // View model for grouping cart items by wholesaler
    public class CartItemGroupViewModel
    {
        public string WholesalerId { get; set; }
        public string WholesalerName { get; set; }
        public List<CartItem> Items { get; set; }
        public decimal SubTotal { get; set; }
    }
}