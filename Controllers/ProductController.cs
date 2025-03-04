using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailerWholesalerSystem.Models;

namespace RetailerWholesalerSystem.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProductController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _db = context;
            _userManager = userManager;
        }

        // GET: Products
        public ActionResult Index()
        {
            string userId = _userManager.GetUserId(User);
            var user = _db.Users.Find(userId);

            if (user.UserType == UserType.Wholesaler)
            {
                // For wholesalers: show their products
                var wholesalerProducts = _db.WholesalerProducts
                    .Include(wp => wp.Product)
                    .Where(wp => wp.WholesalerID == userId)
                    .ToList();

                return View("WholesalerProducts", wholesalerProducts);
            }
            else
            {
                // For retailers: show all products
                var products = _db.Products.ToList();
                return View(products);
            }
        }

        // GET: Products/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }
            Product product = _db.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // GET: Products/Create
        [Authorize(Roles = "Admin")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Create([Bind(include:"Name,Description,Category,DefaultPrice,ImageURL")] Product product)
        {
            if (ModelState.IsValid)
            {
                _db.Products.Add(product);
                _db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(product);
        }

        // GET: Products/Edit/5
        [Authorize(Roles = "Admin")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }
            Product product = _db.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Edit([Bind(include:"ProductID,Name,Description,Category,DefaultPrice,ImageURL")] Product product)
        {
            if (ModelState.IsValid)
            {
                _db.Entry(product).State = EntityState.Modified;
                _db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(product);
        }

        // GET: Products/AddToWholesaler/5
        [Authorize]
        public ActionResult AddToWholesaler(int? id)
        {
            string userId = _userManager.GetUserId(User);
            var user = _db.Users.Find(userId);

            if (user.UserType != UserType.Wholesaler)
            {
                return Unauthorized();
            }

            if (id == null)
            {
                return BadRequest();
            }

            Product product = _db.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }

            var wholesalerProduct = new WholesalerProduct
            {
                ProductID = product.ProductID,
                WholesalerID = userId,
                Price = product.DefaultPrice,
                AvailableQuantity = 0,
                MinimumOrderQuantity = 1
            };

            return View(wholesalerProduct);
        }

        // POST: Products/AddToWholesaler
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult AddToWholesaler([Bind(include:"ProductID,Price,AvailableQuantity,MinimumOrderQuantity")] WholesalerProduct wholesalerProduct)
        {
            string userId = _userManager.GetUserId(User);
            var user = _db.Users.Find(userId);

            if (user.UserType != UserType.Wholesaler)
            {
                return Unauthorized();
            }

            if (ModelState.IsValid)
            {
                wholesalerProduct.WholesalerID = userId;
                _db.WholesalerProducts.Add(wholesalerProduct);
                _db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(wholesalerProduct);
        }

        // GET: Products/EditWholesalerProduct/5
        [Authorize]
        public ActionResult EditWholesalerProduct(int? id)
        {
            string userId = _userManager.GetUserId(User);
            var user = _db.Users.Find(userId);

            if (user.UserType != UserType.Wholesaler)
            {
                return Unauthorized();
            }

            if (id == null)
            {
                return BadRequest();
            }

            WholesalerProduct wholesalerProduct = _db.WholesalerProducts
                .Include(wp => wp.Product)
                .FirstOrDefault(wp => wp.WholesalerProductID == id && wp.WholesalerID == userId);

            if (wholesalerProduct == null)
            {
                return NotFound();
            }

            return View(wholesalerProduct);
        }

        // POST: Products/EditWholesalerProduct/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult EditWholesalerProduct([Bind(include:"WholesalerProductID,ProductID,Price,AvailableQuantity,MinimumOrderQuantity")] WholesalerProduct wholesalerProduct)
        {
            string userId = _userManager.GetUserId(User);
            var user = _db.Users.Find(userId);

            if (user.UserType != UserType.Wholesaler)
            {
                return Unauthorized();
            }

            if (ModelState.IsValid)
            {
                wholesalerProduct.WholesalerID = userId;
                _db.Entry(wholesalerProduct).State = EntityState.Modified;
                _db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(wholesalerProduct);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}