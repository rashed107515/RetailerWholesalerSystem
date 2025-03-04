using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace RetailerWholesalerSystem.ViewModels
{
    public class ProductViewModel
    {
        public int ProductID { get; set; }

        [Required(ErrorMessage = "Product name is required")]
        [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(1000, ErrorMessage = "Description cannot be longer than 1000 characters")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Category is required")]
        public int Category { get; set; }

        [Required(ErrorMessage = "Default price is required")]
        [Range(0.01, 10000, ErrorMessage = "Price must be between $0.01 and $10,000")]
        [Display(Name = "Default Price")]
        public decimal DefaultPrice { get; set; }

        [Display(Name = "Wholesaler Price")]
        [Range(0.01, 10000, ErrorMessage = "Price must be between $0.01 and $10,000")]
        public decimal? WholesalerPrice { get; set; }

        [Display(Name = "Stock Quantity")]
        [Range(0, 100000, ErrorMessage = "Stock quantity must be between 0 and 100,000")]
        public int StockQuantity { get; set; }

        [Display(Name = "Product Image")]
        public IFormFile ProductImage { get; set; }

        public string ImageURL { get; set; }

        public bool RemoveImage { get; set; }
    }
}