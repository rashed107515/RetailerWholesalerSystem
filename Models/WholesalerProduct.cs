using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RetailerWholesalerSystem.Models
{
    public class WholesalerProduct
    {
        [Key]
        public int WholesalerProductID { get; set; }

        [Required]
        [ForeignKey("Product")]
        public int ProductID { get; set; }

        [Required]
        [ForeignKey("Wholesaler")]
        public string WholesalerID { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [Required]
        [Display(Name = "Available Quantity")]
        public int AvailableQuantity { get; set; }

        [Required]
        [Display(Name = "Minimum Order Quantity")]
        public int MinimumOrderQuantity { get; set; }

        // Navigation properties
        public virtual Product Product { get; set; }
        public virtual ApplicationUser Wholesaler { get; set; }
    }
}