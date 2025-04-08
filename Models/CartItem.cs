using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RetailerWholesalerSystem.Models
{
    // Model for cart items that retailers add before placing an order
    public class CartItem
    {
        [Key]
        public int CartItemID { get; set; }

        [Required]
        public string RetailerID { get; set; }

        [Required]
        public int WholesalerProductID { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public DateTime DateAdded { get; set; }

        // Navigation properties
        [ForeignKey("RetailerID")]
        public ApplicationUser Retailer { get; set; }

        [ForeignKey("WholesalerProductID")]
        public WholesalerProduct WholesalerProduct { get; set; }
    }
}