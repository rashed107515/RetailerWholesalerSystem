using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailerWholesalerSystem.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using RetailerWholesalerSystem.ViewModels;

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
        // GET: Products
        // GET: Products/Index
        [Authorize]
        public ActionResult Index()
        {
            string userId = _userManager.GetUserId(User);
            var user = _db.Users.Find(userId);

            if (user.UserType == UserType.Wholesaler)
            {
                return RedirectToAction("IndexWholesaler");
            }
            else if (user.UserType == UserType.Retailer)
            {
                return RedirectToAction("IndexRetailer");
            }

            // Default view for Admin or other roles
            var products = _db.Products.ToList();
            return View(products);
        }
        public ActionResult IndexWholesaler(bool viewCatalog = false)
        {
            string userId = _userManager.GetUserId(User);
            var user = _db.Users.Find(userId);

            if (user.UserType == UserType.Wholesaler && !viewCatalog)
            {
                // For wholesalers: show their products
                var wholesalerProducts = _db.WholesalerProducts
                    .Include(wp => wp.Product)
                    .Where(wp => wp.WholesalerID == userId)
                    .ToList();

                return View("IndexWholesaler", wholesalerProducts);
            }
            else
            {
                // For everyone: show all products (catalog view)
                var products = _db.Products.ToList();

                // If the user is a wholesaler, load their current products for the UI
                if (user.UserType == UserType.Wholesaler)
                {
                    ViewBag.CurrentUserProducts = _db.WholesalerProducts
                        .Where(wp => wp.WholesalerID == userId)
                        .Select(wp => wp.ProductID)
                        .ToHashSet();

                    // Also add product quantities for display
                    var productStocks = _db.WholesalerProducts
                        .Where(wp => wp.WholesalerID == userId)
                        .ToDictionary(wp => wp.ProductID, wp => wp.AvailableQuantity);

                    ViewBag.ProductStocks = productStocks;
                }

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


        [Authorize]
        public ActionResult Create()
        {
            string userId = _userManager.GetUserId(User);

            // Get categories the user can access (global ones + their own)
            var categories = _db.Categories
                .Where(c => c.IsGlobal || c.CreatedByUserID == userId)
                .ToList();

            ViewBag.Categories = new SelectList(categories, "CategoryID", "Name");
            return View(new ViewModels.ProductViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductViewModel productViewModel)
        {
            if (!ModelState.IsValid)
            {
                // Debugging: Print validation errors to the console/log
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"Validation error in {state.Key}: {error.ErrorMessage}");
                    }
                }

                // Get categories for the dropdown since we're returning to the view
                var categories = _db.Categories
                    .Where(c => c.IsGlobal || c.CreatedByUserID == _userManager.GetUserId(User))
                    .ToList();
                ViewBag.Categories = new SelectList(categories, "CategoryID", "Name");

                return View(productViewModel); // Return the view with validation messages
            }

            var product = new Product
            {
                Name = productViewModel.Name,
                Description = productViewModel.Description,
                CategoryID = productViewModel.CategoryID,
                DefaultPrice = productViewModel.DefaultPrice
            };

            // Handle Image Upload
            if (productViewModel.ProductImage != null)
            {
                // Sanitize the filename
                string originalFileName = productViewModel.ProductImage.FileName;
                string safeFileName = string.Join("_", originalFileName.Split(Path.GetInvalidFileNameChars()));

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + safeFileName;
                string imagesDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");

                // Ensure directory exists
                if (!Directory.Exists(imagesDirectory))
                {
                    Directory.CreateDirectory(imagesDirectory);
                }

                string filePath = Path.Combine(imagesDirectory, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await productViewModel.ProductImage.CopyToAsync(fileStream);
                }

                product.ImageURL = "/images/" + uniqueFileName;
            }
            else
            {
                ModelState.AddModelError("ProductImage", "Please upload an image.");

                // Get categories for the dropdown since we're returning to the view
                var categories = _db.Categories
                    .Where(c => c.IsGlobal || c.CreatedByUserID == _userManager.GetUserId(User))
                    .ToList();
                ViewBag.Categories = new SelectList(categories, "CategoryID", "Name");

                return View(productViewModel);
            }

            _db.Products.Add(product);
            await _db.SaveChangesAsync();

            string userId = _userManager.GetUserId(User);
            var user = _db.Users.Find(userId);

            // Removed the automatic inventory addition
            // If successful and user is a wholesaler, redirect to Add To Inventory
            if (user.UserType == UserType.Wholesaler)
            {
                // Redirect to AddToWholesaler with the new product pre-selected
                return RedirectToAction("AddToWholesaler", new { id = product.ProductID });
            }
            else
            {
                // For other user types, just go to the index
                return RedirectToAction("Index");
            }
        }


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

        // GET: Products/AddToWholesaler
        //[Authorize]
        //public ActionResult AddToWholesaler(int? id = null)
        //{
        //    string userId = _userManager.GetUserId(User);
        //    var user = _db.Users.Find(userId);

        //    if (user.UserType != UserType.Wholesaler)
        //    {
        //        return Unauthorized();
        //    }

        //    // Get all products for dropdown
        //    ViewBag.Products = new SelectList(_db.Products, "ProductID", "Name");

        //    // If id is provided, pre-select that product
        //    if (id.HasValue)
        //    {
        //        Product product = _db.Products.Find(id);
        //        if (product == null)
        //        {
        //            return NotFound();
        //        }

        //        var wholesalerProduct = new WholesalerProduct
        //        {
        //            ProductID = product.ProductID,
        //            WholesalerID = userId,
        //            Price = product.DefaultPrice,
        //            AvailableQuantity = 0,
        //            MinimumOrderQuantity = 1
        //        };

        //        return View(wholesalerProduct);
        //    }

        //    // If no id provided, just return the view with empty model
        //    return View(new WholesalerProduct
        //    {
        //        WholesalerID = userId,
        //        AvailableQuantity = 0,
        //        MinimumOrderQuantity = 1
        //    });
        //}
        // POST: Products/AddToWholesaler
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //[Authorize]
        //public ActionResult AddToWholesaler([Bind(include:"ProductID,Price,AvailableQuantity,MinimumOrderQuantity")] WholesalerProduct wholesalerProduct)
        //{
        //    string userId = _userManager.GetUserId(User);
        //    var user = _db.Users.Find(userId);

        //    if (user.UserType != UserType.Wholesaler)
        //    {
        //        return Unauthorized();
        //    }

        //    if (ModelState.IsValid)
        //    {
        //        wholesalerProduct.WholesalerID = userId;
        //        _db.WholesalerProducts.Add(wholesalerProduct);
        //        _db.SaveChanges();
        //        return RedirectToAction("IndexWholesaler");
        //    }

        //    return View(wholesalerProduct);
        //}

        // GET: Products/AddToWholesaler

        // GET: Products/GetProductDetails
        [HttpGet]
        public JsonResult GetProductDetails(int id)
        {
            var product = _db.Products.Find(id);
            if (product == null)
            {
                return Json(new { success = false });
            }

            return Json(new
            {
                success = true,
                defaultPrice = product.DefaultPrice,
                name = product.Name,
                description = product.Description
            });
        }

        //[Authorize]
        //public ActionResult AddToWholesaler(int? id = null)
        //{
        //    string userId = _userManager.GetUserId(User);
        //    var user = _db.Users.Find(userId);

        //    if (user.UserType != UserType.Wholesaler)
        //    {
        //        return Unauthorized();
        //    }

        //    // Get all products for dropdown - ensure this query returns data
        //    var products = _db.Products.ToList();  // Add this line to verify data
        //    ViewBag.Products = new SelectList(products, "ProductID", "Name");

        //    // If id is provided, pre-select that product
        //    if (id.HasValue)
        //    {
        //        Product product = _db.Products.Find(id);
        //        if (product == null)
        //        {
        //            return NotFound();
        //        }

        //        var wholesalerProduct = new WholesalerProduct
        //        {
        //            ProductID = product.ProductID,
        //            WholesalerID = userId,
        //            Price = product.DefaultPrice,
        //            AvailableQuantity = 0,
        //            MinimumOrderQuantity = 1
        //        };

        //        return View(wholesalerProduct);
        //    }

        //    // If no id provided, just return the view with empty model
        //    return View(new WholesalerProduct
        //    {
        //        WholesalerID = userId,
        //        AvailableQuantity = 0,
        //        MinimumOrderQuantity = 1
        //    });
        //}
        [Authorize]
        public ActionResult AddToWholesaler(int? id = null)
        {
            string userId = _userManager.GetUserId(User);
            var user = _db.Users.Find(userId);

            if (user.UserType != UserType.Wholesaler)
            {
                return Unauthorized();
            }

            // Get products already in wholesaler's inventory
            var existingProductIds = _db.WholesalerProducts
                .Where(wp => wp.WholesalerID == userId)
                .Select(wp => wp.ProductID)
                .ToList();

            // Get all products NOT in the wholesaler's inventory
            var availableProducts = _db.Products
                .Where(p => !existingProductIds.Contains(p.ProductID))
                .ToList();

            Console.WriteLine($"Found {availableProducts.Count} available products for dropdown");

            // Set the selected product ID if we have one
            int? selectedProductId = id;
            ViewBag.Products = new SelectList(availableProducts, "ProductID", "Name", selectedProductId);

            // If id is provided, pre-select that product
            if (id.HasValue)
            {
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

            // If no id provided, just return the view with empty model
            return View(new WholesalerProduct
            {
                WholesalerID = userId,
                AvailableQuantity = 0,
                MinimumOrderQuantity = 1
            });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult AddToWholesaler(WholesalerProduct wholesalerProduct)
        {
            string userId = _userManager.GetUserId(User);
            var user = _db.Users.Find(userId);

            if (user.UserType != UserType.Wholesaler)
            {
                return Unauthorized();
            }

            wholesalerProduct.WholesalerID = userId;

            if (ModelState.IsValid)
            {
                // Check if this product is already in inventory
                var existingProduct = _db.WholesalerProducts
                    .FirstOrDefault(wp => wp.WholesalerID == userId && wp.ProductID == wholesalerProduct.ProductID);

                if (existingProduct != null)
                {
                    // Update existing product instead of adding new one
                    existingProduct.Price = wholesalerProduct.Price;
                    existingProduct.AvailableQuantity += wholesalerProduct.AvailableQuantity;
                    existingProduct.MinimumOrderQuantity = wholesalerProduct.MinimumOrderQuantity;
                    _db.SaveChanges();
                }
                else
                {
                    // Add new product
                    _db.WholesalerProducts.Add(wholesalerProduct);
                    _db.SaveChanges();
                }

                // Return to the inventory view
                return RedirectToAction("IndexWholesaler");
            }

            // If we got this far, something failed - repopulate the dropdown
            // Make sure to select the current ProductID when repopulating
            ViewBag.Products = new SelectList(_db.Products, "ProductID", "Name", wholesalerProduct.ProductID);
            return View(wholesalerProduct);
        }

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
        public ActionResult EditWholesalerProduct([Bind(include: "WholesalerProductID,WholesalerID,ProductID,Price,AvailableQuantity,MinimumOrderQuantity")] WholesalerProduct wholesalerProduct)
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
                return RedirectToAction("IndexWholesaler");
            }

            return View(wholesalerProduct);
        }

        [Authorize]
        public ActionResult BrowseForWholesaler()
        {
            string userId = _userManager.GetUserId(User);
            var user = _db.Users.Find(userId);

            if (user.UserType != UserType.Wholesaler)
            {
                return RedirectToAction("WholesalerProducts");
            }

            // Get all products
            var products = _db.Products.ToList();

            // Get the current user's products for comparison
            var currentUserProducts = _db.WholesalerProducts
                .Where(wp => wp.WholesalerID == userId)
                .Select(wp => wp.ProductID)
                .ToHashSet();

            ViewBag.CurrentUserProducts = currentUserProducts;

            return View(products);
        }

        // GET: Products/BrowseWholesalerProducts
        [Authorize]
        public ActionResult BrowseWholesalerProducts()
        {
            string userId = _userManager.GetUserId(User);
            var user = _db.Users.Find(userId);

            if (user.UserType != UserType.Retailer)
            {
                return RedirectToAction("Index");
            }

            // Get all available wholesaler products with quantity > 0
            var wholesalerProducts = _db.WholesalerProducts
                .Include(wp => wp.Product)
                .Include(wp => wp.Wholesaler)
                .Where(wp => wp.AvailableQuantity > 0)
                .ToList();

            return View(wholesalerProducts);
        }

        // GET: Products/BrowseForRetailer
        //[Authorize]
        //public ActionResult BrowseForRetailer()
        //{
        //    string userId = _userManager.GetUserId(User);
        //    var user = _db.Users.Find(userId);

        //    if (user.UserType != UserType.Retailer)
        //    {
        //        return RedirectToAction("Index");
        //    }

        //    // Get all available wholesaler products
        //    var wholesalerProducts = _db.WholesalerProducts
        //        .Include(wp => wp.Product)
        //        .Include(wp => wp.Wholesaler)
        //        .Where(wp => wp.AvailableQuantity > 0)
        //        .ToList();

        //    // Get the current user's products for comparison
        //    var currentUserProducts = _db.RetailerProducts
        //        .Where(rp => rp.RetailerID == userId)
        //        .Select(rp => rp.ProductID)
        //        .ToHashSet();

        //    ViewBag.CurrentUserProducts = currentUserProducts;

        //    return View(wholesalerProducts);
        //}
        // GET: Products/DeleteWholesalerProduct/5
        [Authorize]
        public ActionResult DeleteWholesalerProduct(int? id)
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

        // POST: Products/DeleteWholesalerProduct/5
        [HttpPost, ActionName("DeleteWholesalerProduct")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult DeleteWholesalerProductConfirmed(int WholesalerProductID)
        {
            string userId = _userManager.GetUserId(User);
            var user = _db.Users.Find(userId);

            if (user.UserType != UserType.Wholesaler)
            {
                return Unauthorized();
            }

            WholesalerProduct wholesalerProduct = _db.WholesalerProducts
                .FirstOrDefault(wp => wp.WholesalerProductID == WholesalerProductID && wp.WholesalerID == userId);

            if (wholesalerProduct == null)
            {
                return NotFound();
            }

            _db.WholesalerProducts.Remove(wholesalerProduct);
            _db.SaveChanges();
            return RedirectToAction("IndexWholesaler");
        }

        // GET: Products/Index for Retailers
        public ActionResult IndexRetailer(bool viewCatalog = false)
        {
            string userId = _userManager.GetUserId(User);
            var user = _db.Users.Find(userId);

            if (user.UserType == UserType.Wholesaler && !viewCatalog)
            {
                // For wholesalers: show their products
                var wholesalerProducts = _db.WholesalerProducts
                    .Include(wp => wp.Product)
                    .Where(wp => wp.WholesalerID == userId)
                    .ToList();

                return View("WholesalerProducts", wholesalerProducts);
            }
            else if (user.UserType == UserType.Retailer && !viewCatalog)
            {
                // For retailers: show their products
                var retailerProducts = _db.RetailerProducts
                    .Include(rp => rp.Product)
                    .Where(rp => rp.RetailerID == userId)
                    .ToList();

                return View("RetailerProducts", retailerProducts);
            }
            else
            {
                // For everyone: show all products (catalog view)
                var products = _db.Products.ToList();

                // If the user is a wholesaler, load their current products for the UI
                if (user.UserType == UserType.Wholesaler)
                {
                    ViewBag.CurrentUserProducts = _db.WholesalerProducts
                        .Where(wp => wp.WholesalerID == userId)
                        .Select(wp => wp.ProductID)
                        .ToHashSet();

                    // Also add product quantities for display
                    var productStocks = _db.WholesalerProducts
                        .Where(wp => wp.WholesalerID == userId)
                        .ToDictionary(wp => wp.ProductID, wp => wp.AvailableQuantity);

                    ViewBag.ProductStocks = productStocks;
                }
                // If the user is a retailer, load their current products for the UI
                else if (user.UserType == UserType.Retailer)
                {
                    ViewBag.CurrentUserProducts = _db.RetailerProducts
                        .Where(rp => rp.RetailerID == userId)
                        .Select(rp => rp.ProductID)
                        .ToHashSet();

                    // Also add product quantities for display
                    var productStocks = _db.RetailerProducts
                        .Where(rp => rp.RetailerID == userId)
                        .ToDictionary(rp => rp.ProductID, rp => rp.StockQuantity);

                    ViewBag.ProductStocks = productStocks;
                }

                return View(products);
            }
        }

        // GET: Products/AddToRetailer/5
        [Authorize]
        public ActionResult AddToRetailer(int? id)
        {
            string userId = _userManager.GetUserId(User);
            var user = _db.Users.Find(userId);

            if (user.UserType != UserType.Retailer)
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

            var retailerProduct = new RetailerProduct
            {
                ProductID = product.ProductID,
                RetailerID = userId,
                Price = product.DefaultPrice,
                StockQuantity = 0
            };

            return View(retailerProduct);
        }

        // POST: Products/AddToRetailer
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult AddToRetailer([Bind(include: "ProductID,Price,StockQuantity")] RetailerProduct retailerProduct)
        {
            string userId = _userManager.GetUserId(User);
            var user = _db.Users.Find(userId);

            if (user.UserType != UserType.Retailer)
            {
                return Unauthorized();
            }

            if (ModelState.IsValid)
            {
                retailerProduct.RetailerID = userId;
                _db.RetailerProducts.Add(retailerProduct);
                _db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(retailerProduct);
        }


        // GET: Products/EditRetailerProduct/5
        [Authorize]
        public ActionResult EditRetailerProduct(int? id)
        {
            string userId = _userManager.GetUserId(User);
            var user = _db.Users.Find(userId);

            if (user.UserType != UserType.Retailer)
            {
                return Unauthorized();
            }

            if (id == null)
            {
                return BadRequest();
            }

            RetailerProduct retailerProduct = _db.RetailerProducts
                .Include(rp => rp.Product)
                .FirstOrDefault(rp => rp.RetailerProductID == id && rp.RetailerID == userId);

            if (retailerProduct == null)
            {
                return NotFound();
            }

            return View(retailerProduct);
        }

        // POST: Products/EditRetailerProduct/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult EditRetailerProduct([Bind(include: "RetailerProductID,ProductID,Price,StockQuantity")] RetailerProduct retailerProduct)
        {
            string userId = _userManager.GetUserId(User);
            var user = _db.Users.Find(userId);

            if (user.UserType != UserType.Retailer)
            {
                return Unauthorized();
            }

            if (ModelState.IsValid)
            {
                retailerProduct.RetailerID = userId;
                _db.Entry(retailerProduct).State = EntityState.Modified;
                _db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(retailerProduct);
        }

        // GET: Products/DeleteRetailerProduct/5
        [Authorize]
        public ActionResult DeleteRetailerProduct(int? id)
        {
            string userId = _userManager.GetUserId(User);
            var user = _db.Users.Find(userId);

            if (user.UserType != UserType.Retailer)
            {
                return Unauthorized();
            }

            if (id == null)
            {
                return BadRequest();
            }

            RetailerProduct retailerProduct = _db.RetailerProducts
                .Include(rp => rp.Product)
                .FirstOrDefault(rp => rp.RetailerProductID == id && rp.RetailerID == userId);

            if (retailerProduct == null)
            {
                return NotFound();
            }

            return View(retailerProduct);
        }

        // POST: Products/DeleteRetailerProduct/5
        [HttpPost, ActionName("DeleteRetailerProduct")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult DeleteRetailerProductConfirmed(int RetailerProductID)
        {
            string userId = _userManager.GetUserId(User);
            var user = _db.Users.Find(userId);

            if (user.UserType != UserType.Retailer)
            {
                return Unauthorized();
            }

            RetailerProduct retailerProduct = _db.RetailerProducts
                .FirstOrDefault(rp => rp.RetailerProductID == RetailerProductID && rp.RetailerID == userId);

            if (retailerProduct == null)
            {
                return NotFound();
            }

            _db.RetailerProducts.Remove(retailerProduct);
            _db.SaveChanges();
            return RedirectToAction("Index");
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