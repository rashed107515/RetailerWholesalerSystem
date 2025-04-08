﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RetailerWholesalerSystem.Models
{
    public class Order
    {
        [Key]
        public int OrderID { get; set; }

        [Required]
        public DateTime OrderDate { get; set; }

        [Required]
        public string RetailerID { get; set; }

        [Required]
        public string WholesalerID { get; set; }

        [Required]
        public OrderStatus Status { get; set; }

        public string TrackingNumber { get; set; }

        public DateTime? ShippedDate { get; set; }

        public DateTime? DeliveredDate { get; set; }

        // Navigation properties
        [ForeignKey("RetailerID")]
        public ApplicationUser Retailer { get; set; }

        [ForeignKey("WholesalerID")]
        public ApplicationUser Wholesaler { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; }

    }
}