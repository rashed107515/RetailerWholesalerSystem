using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RetailerWholesalerSystem.Models
{
    public enum TransactionStatus
    {
        Pending,
        Paid,
        Cancelled
    }

    public class Transaction
    {
        [Key]
        public int TransactionID { get; set; }

        [Required]
        [ForeignKey("Retailer")]
        public string RetailerID { get; set; }

        [Required]
        [ForeignKey("Wholesaler")]
        public string WholesalerID { get; set; }

        [Required]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime Date { get; set; }

        [Required]
        [Display(Name = "Total Amount")]
        [DataType(DataType.Currency)]
        public decimal TotalAmount { get; set; }

        [Required]
        public TransactionStatus Status { get; set; }

        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; }

        public string Notes { get; set; }

        // Navigation properties
        public virtual ApplicationUser Retailer { get; set; }
        public virtual ApplicationUser Wholesaler { get; set; }
        public virtual ICollection<TransactionDetail> TransactionDetails { get; set; }
    }
}