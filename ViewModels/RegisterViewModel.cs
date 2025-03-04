using System.ComponentModel.DataAnnotations;
using RetailerWholesalerSystem.Models;

namespace RetailerWholesalerSystem.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Required]
        [Display(Name = "Role")]
        public string Role { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Required]
        [Display(Name = "Business Name")]
        public string BusinessName { get; set; }

        [Required]
        public string Address { get; set; }

        [Required]
        [Display(Name = "Contact Information")]
        public string ContactInfo { get; set; }

        [Required]
        [Display(Name = "Accept Terms and Conditions")]
        public bool AcceptTerms { get; set; }
    }
}