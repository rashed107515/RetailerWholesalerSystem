using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RetailerWholesalerSystem.Models;

namespace RetailerWholesalerSystem.Controllers
{
    public class BaseController : Controller
    {
        protected readonly ApplicationDbContext _db;
        protected readonly UserManager<ApplicationUser> _userManager;

        public BaseController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _db = context;
            _userManager = userManager;
        }

        protected async Task SetCartCountAsync()
        {
            if (User.Identity.IsAuthenticated)
            {
                string userId = _userManager.GetUserId(User);
                var user = await _db.Users.FindAsync(userId);

                if (user != null && user.UserType == UserType.Retailer)
                {
                    int cartCount = await _db.CartItems
                        .Where(ci => ci.RetailerID == userId)
                        .SumAsync(ci => ci.Quantity);

                    ViewBag.CartCount = cartCount;
                }
            }
        }

        // You could add more shared controller methods here
    }
}