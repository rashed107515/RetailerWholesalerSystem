using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RetailerWholesalerSystem.Models
{
    public class Product
    {
        [Key]
        public int ProductID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        public int CategoryID { get; set; }

        [ForeignKey("CategoryID")]
        public virtual Category Category { get; set; }

        [Display(Name = "Default Price")]
        [DataType(DataType.Currency)]
        public decimal DefaultPrice { get; set; }

        [Display(Name = "Image URL")]
        public string ImageURL { get; set; }

        // Navigation properties
        // Add this to the Product class
        public virtual ICollection<RetailerProduct> RetailerProducts { get; set; }
        public virtual ICollection<WholesalerProduct> WholesalerProducts { get; set; }
        public virtual ICollection<TransactionDetail> TransactionDetails { get; set; }
    }
}