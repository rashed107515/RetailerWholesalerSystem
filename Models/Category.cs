using System.ComponentModel.DataAnnotations;

namespace RetailerWholesalerSystem.Models
{
    public class Category
    {
        public int CategoryID { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        // Remove the [Required] constraint that's causing the issue
        public string? CreatedByUserID { get; set; } = null!;

        // Optional: determine if this is a system-wide category or user-specific
        public bool IsGlobal { get; set; } = false;
    }
}