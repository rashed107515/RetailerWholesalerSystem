using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace RetailerWholesalerSystem.Models
{
    public enum UserType
    {
        Retailer,
        Wholesaler
    }

    public class ApplicationUser : IdentityUser
    {
        [Required]
        [Display(Name = "Business Name")]
        public string BusinessName { get; set; }

        [Required]
        public string Address { get; set; }

        [Required]
        [Display(Name = "Contact Information")]
        public string ContactInfo { get; set; }

        [Required]
        [Display(Name = "User Type")]
        public UserType UserType { get; set; }

        // Navigation properties
        public virtual ICollection<Transaction> RetailerTransactions { get; set; }
        public virtual ICollection<Transaction> WholesalerTransactions { get; set; }
        public virtual ICollection<WholesalerProduct> WholesalerProducts { get; set; }
        public virtual ICollection<Order> RetailerOrders { get; set; }
        public virtual ICollection<Order> WholesalerOrders { get; set; }
    }
}