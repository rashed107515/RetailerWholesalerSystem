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
        public int CategoryID { get; set; }  // ✅ Renamed for consistency

        [Required(ErrorMessage = "Default price is required")]
        [Range(0.01, 10000, ErrorMessage = "Price must be between $0.01 and $10,000")]
        [Display(Name = "Default Price")]
        public decimal DefaultPrice { get; set; }

        [Display(Name = "Product Image")]
        [Required(ErrorMessage = "Product image is required")]
        [DataType(DataType.Upload)]
        public IFormFile ProductImage { get; set; }  // ✅ Added validation

        public string ImageURL { get; set; } = string.Empty; // ✅ Set a default empty string to prevent null validation

        public bool RemoveImage { get; set; }
    }
}
