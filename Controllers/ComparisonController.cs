using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using RetailerWholesalerSystem.Models;
using RetailerWholesalerSystem.ViewModels;
using System.Security.Claims;
namespace RetailerWholesalerSystem.Controllers
{
    [Authorize]
    public class ComparisonController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Comparison
        public ActionResult Index(string searchTerm = "", string category = "")
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = db.Users.Find(userId);

            if (user.UserType != UserType.Retailer)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var query = db.WholesalerProducts
                .Include(wp => wp.Product)
                .Include(wp => wp.Wholesaler)
                .AsQueryable();

            // Apply search filters
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(wp => wp.Product.Name.Contains(searchTerm) ||
                                          wp.Product.Description.Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(category))
            {
                // Assuming Category is an enum
              
                    query = query.Where(wp => wp.Product.Category.Name == category);
                
            }

            var products = query.ToList();

            // Get all categories
            var categories = db.Products
     .Select(p => p.Category.Name)
     .Distinct()
     .ToList();

            var viewModel = new ProductComparisonViewModel
            {
                SearchTerm = searchTerm,
                Category = category,
                Products = products,
                Categories = categories
            };

            return View(viewModel);
        }

        // GET: Comparison/Compare
        public ActionResult Compare(int[] productIds)
        {
            if (productIds == null || productIds.Length == 0)
            {
                return RedirectToAction("Index");
            }

            var products = db.WholesalerProducts
                .Include(wp => wp.Product)
                .Include(wp => wp.Wholesaler)
                .Where(wp => productIds.Contains(wp.WholesalerProductID))
                .ToList();

            return View(products);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}