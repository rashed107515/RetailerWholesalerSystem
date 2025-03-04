using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using RetailerWholesalerSystem.Models;

namespace RetailerWholesalerSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _db = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                string userId = _userManager.GetUserId(User);
                var user = await _db.Users.FindAsync(userId);

                if (user != null)
                {
                    return RedirectToAction("Index", "Dashboard");
                }
            }

            return View();
        }

        public IActionResult About()
        {
            ViewBag.Message = "Retailer-Wholesaler Cash Book System";

            return View();
        }

        public IActionResult Contact()
        {
            ViewBag.Message = "Contact us for support.";

            return View();
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            string userId = _userManager.GetUserId(User);
            var user = await _db.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([Bind(include:"Id,BusinessName,Address,ContactInfo")] ApplicationUser userModel)
        {
            string userId = _userManager.GetUserId(User);
            var user = await _db.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                user.BusinessName = userModel.BusinessName;
                user.Address = userModel.Address;
                user.ContactInfo = userModel.ContactInfo;

                _db.Entry(user).State = EntityState.Modified;
                await _db.SaveChangesAsync();

                return RedirectToAction("Profile");
            }

            return View("Profile", user);
        }
    }
}