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
using Microsoft.Data.SqlClient;

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
        // This should be your action method for the IndexWholesaler view
        public ActionResult IndexWholesaler()
        {
            string userId = _userManager.GetUserId(User);
            var user = _db.Users.Find(userId);
            if (user.UserType != UserType.Wholesaler)
            {
                return Unauthorized();
            }

            // Include related entities to avoid null reference exceptions
            var wholesalerProducts = _db.WholesalerProducts
                .Include(wp => wp.Product)
                .ThenInclude(p => p.Category)
                .Where(wp => wp.WholesalerID == userId)
                .ToList();

            return View(wholesalerProducts);
        }
        // GET: Products/MyInventory
        [Authorize]
        public ActionResult MyInventory()
        {
            string userId = _userManager.GetUserId(User);
            var user = _db.Users.Find(userId);

            if (user.UserType != UserType.Wholesaler)
            {
                return Unauthorized();
            }

            // Get wholesaler's products with included related data
            var wholesalerProducts = _db.WholesalerProducts
                .Include(wp => wp.Product)
                .ThenInclude(p => p.Category)
                .Where(wp => wp.WholesalerID == userId)
                .ToList();

            return View(wholesalerProducts);
        }
        // GET: Products/Details/5
        //public ActionResult Details(int? id)
        //{
        //    if (id == null)
        //    {
        //        return BadRequest();
        //    }
        //    Product product = _db.Products.Find(id);
        //    if (product == null)
        //    {
        //        return NotFound();
        //    }
        //    return View(product);
        //}
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            // Find the WholesalerProduct instead of just Product
            var wholesalerProduct = _db.WholesalerProducts
                .Include(wp => wp.Product)
                    .ThenInclude(p => p.Category)
                .Include(wp => wp.Wholesaler)
                .FirstOrDefault(wp => wp.ProductID == id);

            if (wholesalerProduct == null)
            {
                return NotFound();
            }

            return View(wholesalerProduct);
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

        [HttpGet]
        [Authorize]
        public ActionResult AddToWholesaler()
        {
            string userId = _userManager.GetUserId(User);
            var user = _db.Users.Find(userId);
            if (user.UserType != UserType.Wholesaler)
            {
                return Unauthorized();
            }

            var availableProducts = _db.Products.ToList();
            ViewBag.Products = new SelectList(availableProducts, "ProductID", "Name");

            // Add this line to pass the user ID to the view
            ViewBag.UserId = userId;

            return View(new WholesalerProduct());
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

            // Add debugging 
            System.Diagnostics.Debug.WriteLine($"Adding product {wholesalerProduct.ProductID} with quantity {wholesalerProduct.AvailableQuantity}");

            // Set the WholesalerID here
            wholesalerProduct.WholesalerID = userId;

            // Remove these validation errors specifically
            ModelState.Remove("Product");
            ModelState.Remove("Wholesaler");

            if (ModelState.IsValid)
            {
                try
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
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error saving product: {ex.Message}");
                    ModelState.AddModelError("", "Error saving to database: " + ex.Message);
                }
            }
            else
            {
                // Log validation errors
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error in {state.Key}: {error.ErrorMessage}");
                    }
                }
            }

            // If we got this far, something failed - repopulate the dropdown
            var availableProducts = _db.Products.ToList();
            ViewBag.Products = new SelectList(availableProducts, "ProductID", "Name", wholesalerProduct.ProductID);
            ViewBag.UserId = userId;
            return View(wholesalerProduct);
        }
        // GET: Product/EditWholesalerProduct/5
        [HttpGet]
        [Authorize]
        public ActionResult EditWholesalerProduct(int id)
        {
            string userId = _userManager.GetUserId(User);
            var user = _db.Users.Find(userId);
            if (user.UserType != UserType.Wholesaler)
            {
                return Unauthorized();
            }

            // Get the wholesaler product with related product data
            var wholesalerProduct = _db.WholesalerProducts
                .Include(wp => wp.Product)
                .FirstOrDefault(wp => wp.WholesalerProductID == id && wp.WholesalerID == userId);

            if (wholesalerProduct == null)
            {
                return NotFound();
            }

            // Pass available products to view for dropdown
            var availableProducts = _db.Products.ToList();
            ViewBag.Products = new SelectList(availableProducts, "ProductID", "Name", wholesalerProduct.ProductID);
            ViewBag.UserId = userId;

            return View(wholesalerProduct);
        }

        // POST: Product/EditWholesalerProduct/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult EditWholesalerProduct(WholesalerProduct wholesalerProduct)
        {
            string userId = _userManager.GetUserId(User);
            var user = _db.Users.Find(userId);
            if (user.UserType != UserType.Wholesaler)
            {
                return Unauthorized();
            }

            // Ensure the product belongs to this user
            if (wholesalerProduct.WholesalerID != userId)
            {
                return Unauthorized();
            }

            // Remove navigation property validation errors
            ModelState.Remove("Product");
            ModelState.Remove("Wholesaler");

            if (ModelState.IsValid)
            {
                try
                {
                    // Get existing entity and update its values
                    var existingProduct = _db.WholesalerProducts.Find(wholesalerProduct.WholesalerProductID);

                    if (existingProduct == null)
                    {
                        return NotFound();
                    }

                    // Update properties
                    existingProduct.Price = wholesalerProduct.Price;
                    existingProduct.AvailableQuantity = wholesalerProduct.AvailableQuantity;
                    existingProduct.MinimumOrderQuantity = wholesalerProduct.MinimumOrderQuantity;

                    _db.SaveChanges();

                    return RedirectToAction("IndexWholesaler");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error saving product: {ex.Message}");
                    ModelState.AddModelError("", "Error saving to database: " + ex.Message);
                }
            }
            else
            {
                // Log validation errors
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error in {state.Key}: {error.ErrorMessage}");
                    }
                }
            }

            // If we got this far, something failed - repopulate the dropdown
            var availableProducts = _db.Products.ToList();
            ViewBag.Products = new SelectList(availableProducts, "ProductID", "Name", wholesalerProduct.ProductID);
            ViewBag.UserId = userId;

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

        // Add these methods to your ProductController or modify the existing ones

        // GET: Products/BrowseWholesalerProducts
        [Authorize]
        public ActionResult BrowseWholesalerProducts(string searchQuery, int? categoryId, string sortOrder)
        {
            string userId = _userManager.GetUserId(User);
            var user = _db.Users.Find(userId);

            if (user.UserType != UserType.Retailer)
            {
                return RedirectToAction("Index");
            }

            // Get all available wholesaler products with quantity > 0
            var query = _db.WholesalerProducts
                .Include(wp => wp.Product)
                    .ThenInclude(p => p.Category)
                .Include(wp => wp.Wholesaler)
                .Where(wp => wp.AvailableQuantity > 0);

            // Apply search filter
            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(wp =>
                    wp.Product.Name.Contains(searchQuery) ||
                    wp.Product.Description.Contains(searchQuery));

                ViewBag.CurrentSearchQuery = searchQuery;
            }

            // Apply category filter
            if (categoryId.HasValue)
            {
                query = query.Where(wp => wp.Product.CategoryID == categoryId.Value);
                ViewBag.CurrentCategoryId = categoryId.Value;
            }

            // Apply sorting
            ViewBag.CurrentSortOrder = sortOrder;
            switch (sortOrder)
            {
                case "nameDesc":
                    query = query.OrderByDescending(wp => wp.Product.Name);
                    break;
                case "priceAsc":
                    query = query.OrderBy(wp => wp.Price);
                    break;
                case "priceDesc":
                    query = query.OrderByDescending(wp => wp.Price);
                    break;
                case "quantityAsc":
                    query = query.OrderBy(wp => wp.AvailableQuantity);
                    break;
                case "quantityDesc":
                    query = query.OrderByDescending(wp => wp.AvailableQuantity);
                    break;
                default: // nameAsc
                    query = query.OrderBy(wp => wp.Product.Name);
                    break;
            }

            var wholesalerProducts = query.ToList();

            // Get all categories for filter dropdown
            ViewBag.Categories = _db.Categories.ToList();

            // Get wholesalers that have products available
            ViewBag.Wholesalers = _db.Users
                .Where(u => u.UserType == UserType.Wholesaler &&
                       _db.WholesalerProducts.Any(wp => wp.WholesalerID == u.Id && wp.AvailableQuantity > 0))
                .ToList();

            try
            {
                // Get cart count for the UI
                ViewBag.CartItemCount = _db.CartItems
                    .Where(ci => ci.RetailerID == userId)
                    .Sum(ci => ci.Quantity);
            }
            catch (SqlException ex)
            {
                // Handle the exception (e.g., log it and set a default value)
                System.Diagnostics.Debug.WriteLine($"Error accessing CartItems: {ex.Message}");
                ViewBag.CartItemCount = 0;
            }

            return View(wholesalerProducts);
        }


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


        // Add to ProductController
        [Authorize]
        public ActionResult RetailerInventory()
        {
            string userId = _userManager.GetUserId(User);
            var user = _db.Users.Find(userId);

            if (user.UserType != UserType.Retailer)
            {
                return Unauthorized();
            }

            // Get retailer's products with included related data
            var retailerProducts = _db.RetailerProducts
                .Include(rp => rp.Product)
                .ThenInclude(p => p.Category)
                .Where(rp => rp.RetailerID == userId)
                .ToList();

            return View(retailerProducts);
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
                .ThenInclude(p => p.Category)
                .FirstOrDefault(rp => rp.RetailerProductID == id && rp.RetailerID == userId);

            if (retailerProduct == null)
            {
                return NotFound();
            }
            ViewData["Title"] = "Edit Inventory Item";
            return View(retailerProduct);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult EditRetailerProduct(int RetailerProductID, decimal Price, int StockQuantity)
        {
            string userId = _userManager.GetUserId(User);
            var user = _db.Users.Find(userId);
            if (user.UserType != UserType.Retailer)
            {
                return Unauthorized();
            }

            // Find the existing product
            var existingProduct = _db.RetailerProducts
                .FirstOrDefault(rp => rp.RetailerProductID == RetailerProductID && rp.RetailerID == userId);

            if (existingProduct == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Update only the fields that should be modified
                existingProduct.Price = Price;
                existingProduct.StockQuantity = StockQuantity;

                _db.SaveChanges();
                return RedirectToAction("RetailerInventory");
            }

            // If validation fails, reload the complete entity
            var completeRetailerProduct = _db.RetailerProducts
                .Include(rp => rp.Product)
                .ThenInclude(p => p.Category)
                .FirstOrDefault(rp => rp.RetailerProductID == RetailerProductID && rp.RetailerID == userId);

            ViewData["Title"] = "Edit Inventory Item";
            return View(completeRetailerProduct);
        }        // GET: Products/DeleteRetailerProduct/5
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

        // Add these methods to your ProductController or create a new OrderController

        // GET: Orders
        [Authorize]
        public ActionResult RetailerOrders()
        {
            string userId = _userManager.GetUserId(User);
            var user = _db.Users.Find(userId);

            if (user.UserType != UserType.Retailer)
            {
                return Unauthorized();
            }

            var orders = _db.Orders
                .Include(o => o.Wholesaler)
                .Include(o => o.OrderItems)
                .Where(o => o.RetailerID == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            return View(orders);
        }

        // GET: Orders/Details/5
        [Authorize]
        public ActionResult OrderDetails(int? id)
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

            var order = _db.Orders
                .Include(o => o.Wholesaler)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefault(o => o.OrderID == id && o.RetailerID == userId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: Orders/Create/5 (5 is the wholesalerID)
        [Authorize]
        public ActionResult CreateOrder(string wholesalerId)
        {
            string userId = _userManager.GetUserId(User);
            var user = _db.Users.Find(userId);

            if (user.UserType != UserType.Retailer)
            {
                return Unauthorized();
            }

            // Get wholesaler products for this specific wholesaler
            var wholesalerProducts = _db.WholesalerProducts
                .Include(wp => wp.Product)
                .Where(wp => wp.WholesalerID == wholesalerId && wp.AvailableQuantity > 0)
                .ToList();

            var wholesaler = _db.Users.Find(wholesalerId);
            ViewBag.WholesalerName = wholesaler?.BusinessName;
            ViewBag.WholesalerID = wholesalerId;

            return View(wholesalerProducts);
        }

        // POST: Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult PlaceOrder(OrderViewModel model)
        {
            string userId = _userManager.GetUserId(User);
            var user = _db.Users.Find(userId);

            if (user.UserType != UserType.Retailer)
            {
                return Unauthorized();
            }

            if (ModelState.IsValid)
            {
                // Create new order
                var order = new Order
                {
                    RetailerID = userId,
                    WholesalerID = model.WholesalerID,
                    OrderDate = DateTime.Now,
                    Status = OrderStatus.Pending,
                    OrderItems = new List<OrderItem>()
                };

                // Add ordered items
                foreach (var item in model.OrderItems.Where(i => i.Quantity > 0))
                {
                    var wholesalerProduct = _db.WholesalerProducts.Find(item.WholesalerProductID);

                    if (wholesalerProduct == null || wholesalerProduct.AvailableQuantity < item.Quantity)
                    {
                        ModelState.AddModelError("", $"Product {item.ProductName} is no longer available in requested quantity.");
                        return RedirectToAction("CreateOrder", new { wholesalerId = model.WholesalerID });
                    }

                    order.OrderItems.Add(new OrderItem
                    {
                        ProductID = wholesalerProduct.ProductID,
                        WholesalerProductID = wholesalerProduct.WholesalerProductID,
                        Quantity = item.Quantity,
                        Price = wholesalerProduct.Price
                    });

                    // Reduce available quantity from wholesaler
                    wholesalerProduct.AvailableQuantity -= item.Quantity;
                }

                _db.Orders.Add(order);
                _db.SaveChanges();

                return RedirectToAction("RetailerOrders");
            }

            return RedirectToAction("CreateOrder", new { wholesalerId = model.WholesalerID });
        }

        // GET: Orders/Cancel/5
        [Authorize]
        public ActionResult CancelOrder(int? id)
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

            var order = _db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefault(o => o.OrderID == id && o.RetailerID == userId && o.Status == OrderStatus.Pending);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Orders/Cancel/5
        [HttpPost, ActionName("CancelOrder")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult CancelOrderConfirmed(int id)
        {
            string userId = _userManager.GetUserId(User);
            var user = _db.Users.Find(userId);

            if (user.UserType != UserType.Retailer)
            {
                return Unauthorized();
            }

            var order = _db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefault(o => o.OrderID == id && o.RetailerID == userId && o.Status == OrderStatus.Pending);

            if (order == null)
            {
                return NotFound();
            }

            // Return items to wholesaler inventory
            foreach (var item in order.OrderItems)
            {
                var wholesalerProduct = _db.WholesalerProducts.Find(item.WholesalerProductID);
                if (wholesalerProduct != null)
                {
                    wholesalerProduct.AvailableQuantity += item.Quantity;
                }
            }

            order.Status = OrderStatus.Cancelled;
            _db.SaveChanges();

            return RedirectToAction("RetailerOrders");
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