using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RetailerWholesalerSystem.Models
{
    public class CheckoutViewModel
    {
        public List<CartItem> CartItems { get; set; }

        [Required(ErrorMessage = "Delivery address is required")]
        [Display(Name = "Delivery Address")]
        public string DeliveryAddress { get; set; }

        [Required(ErrorMessage = "Contact phone is required")]
        [Display(Name = "Contact Phone")]
        [Phone(ErrorMessage = "Please enter a valid phone number")]
        public string ContactPhone { get; set; }

        [Required(ErrorMessage = "Preferred delivery date is required")]
        [Display(Name = "Preferred Delivery Date")]
        [DataType(DataType.Date)]
        public DateTime PreferredDeliveryDate { get; set; }

        [Display(Name = "Delivery Instructions")]
        public string DeliveryInstructions { get; set; }

        [Required(ErrorMessage = "Payment method is required")]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; }

        // For storing wholesaler-specific notes
        public Dictionary<string, string> WholesalerNotes { get; set; }

        public CheckoutViewModel()
        {
            CartItems = new List<CartItem>();
            WholesalerNotes = new Dictionary<string, string>();
            // Set default preferred delivery date to tomorrow
            PreferredDeliveryDate = DateTime.Now.AddDays(1);
        }
    }
}