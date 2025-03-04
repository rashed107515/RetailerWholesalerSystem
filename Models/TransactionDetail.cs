using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RetailerWholesalerSystem.Models
{
    public class TransactionDetail
    {
        [Key]
        public int TransactionDetailID { get; set; }

        [Required]
        [ForeignKey("Transaction")]
        public int TransactionID { get; set; }

        [Required]
        [ForeignKey("Product")]
        public int ProductID { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [Display(Name = "Unit Price")]
        [DataType(DataType.Currency)]
        public decimal UnitPrice { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        public decimal Subtotal { get; set; }

        // Navigation properties
        public virtual Transaction Transaction { get; set; }
        public virtual Product Product { get; set; }
    }
}