using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailerWholesalerSystem.Models;

namespace RetailerWholesalerSystem.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _db = context;
            _userManager = userManager;
        }

        // GET: Cart
        public async Task<IActionResult> Index()
        {
            string userId = _userManager.GetUserId(User);
            var user = await _db.Users.FindAsync(userId);

            if (user.UserType != UserType.Retailer)
            {
                return RedirectToAction("Index", "Home");
            }

            var cartItems = await _db.CartItems
                .Include(ci => ci.WholesalerProduct)
                    .ThenInclude(wp => wp.Product)
                .Include(ci => ci.WholesalerProduct)
                    .ThenInclude(wp => wp.Wholesaler)
                .Where(ci => ci.RetailerID == userId)
                .ToListAsync();

            // Group by wholesaler for better display
            var groupedItems = cartItems
                .GroupBy(ci => ci.WholesalerProduct.WholesalerID)
                .Select(group => new CartItemGroupViewModel
                {
                    WholesalerId = group.Key,
                    WholesalerName = group.First().WholesalerProduct.Wholesaler.BusinessName,
                    Items = group.ToList(),
                    SubTotal = group.Sum(ci => ci.Quantity * ci.WholesalerProduct.Price)
                })
                .ToList();

            var cartViewModel = new CartViewModel
            {
                CartGroups = groupedItems,
                TotalAmount = groupedItems.Sum(g => g.SubTotal)
            };

            return View(cartViewModel);
        }

        // POST: Cart/AddToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int wholesalerProductId, int quantity)
        {
            string userId = _userManager.GetUserId(User);
            var user = await _db.Users.FindAsync(userId);

            if (user.UserType != UserType.Retailer)
            {
                return Json(new { success = false, message = "Only retailers can add products to cart" });
            }

            // Get the wholesaler product
            var wholesalerProduct = await _db.WholesalerProducts
                .FirstOrDefaultAsync(wp => wp.WholesalerProductID == wholesalerProductId);

            if (wholesalerProduct == null)
            {
                return Json(new { success = false, message = "Product not found" });
            }

            // Validate quantity
            if (quantity < wholesalerProduct.MinimumOrderQuantity)
            {
                return Json(new { success = false, message = $"Minimum order quantity is {wholesalerProduct.MinimumOrderQuantity}" });
            }

            if (quantity > wholesalerProduct.AvailableQuantity)
            {
                return Json(new { success = false, message = $"Only {wholesalerProduct.AvailableQuantity} units available" });
            }

            // Check if item already exists in cart
            var existingItem = await _db.CartItems
                .FirstOrDefaultAsync(ci => ci.RetailerID == userId && ci.WholesalerProductID == wholesalerProductId);

            if (existingItem != null)
            {
                // Update quantity if already in cart
                existingItem.Quantity += quantity;

                // Ensure quantity doesn't exceed available stock
                if (existingItem.Quantity > wholesalerProduct.AvailableQuantity)
                {
                    existingItem.Quantity = wholesalerProduct.AvailableQuantity;
                }
            }
            else
            {
                // Add new cart item
                var newCartItem = new CartItem
                {
                    RetailerID = userId,
                    WholesalerProductID = wholesalerProductId,
                    Quantity = quantity,
                    DateAdded = DateTime.Now
                };

                _db.CartItems.Add(newCartItem);
            }

            await _db.SaveChangesAsync();

            // Get updated cart count for the response
            int cartCount = await _db.CartItems
                .Where(ci => ci.RetailerID == userId)
                .SumAsync(ci => ci.Quantity);

            return Json(new { success = true, cartCount = cartCount });
        }

        // POST: Cart/UpdateQuantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            string userId = _userManager.GetUserId(User);
            var user = await _db.Users.FindAsync(userId);

            if (user.UserType != UserType.Retailer)
            {
                return Json(new { success = false, message = "Access denied" });
            }

            // Get cart item
            var cartItem = await _db.CartItems
                .Include(ci => ci.WholesalerProduct)
                .FirstOrDefaultAsync(ci => ci.CartItemID == cartItemId && ci.RetailerID == userId);

            if (cartItem == null)
            {
                return Json(new { success = false, message = "Item not found in your cart" });
            }

            // Validate quantity
            if (quantity < cartItem.WholesalerProduct.MinimumOrderQuantity)
            {
                return Json(new
                {
                    success = false,
                    message = $"Minimum order quantity is {cartItem.WholesalerProduct.MinimumOrderQuantity}"
                });
            }

            if (quantity > cartItem.WholesalerProduct.AvailableQuantity)
            {
                return Json(new
                {
                    success = false,
                    message = $"Only {cartItem.WholesalerProduct.AvailableQuantity} units available"
                });
            }

            // Update quantity
            cartItem.Quantity = quantity;
            await _db.SaveChangesAsync();

            // Calculate new item total
            decimal itemTotal = cartItem.Quantity * cartItem.WholesalerProduct.Price;

            return Json(new
            {
                success = true,
                itemTotal = itemTotal,
                formattedItemTotal = string.Format("{0:C}", itemTotal)
            });
        }

        // POST: Cart/RemoveItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItem(int cartItemId)
        {
            string userId = _userManager.GetUserId(User);
            var user = await _db.Users.FindAsync(userId);

            if (user.UserType != UserType.Retailer)
            {
                return Json(new { success = false, message = "Access denied" });
            }

            // Get cart item
            var cartItem = await _db.CartItems
                .FirstOrDefaultAsync(ci => ci.CartItemID == cartItemId && ci.RetailerID == userId);

            if (cartItem == null)
            {
                return Json(new { success = false, message = "Item not found in your cart" });
            }

            // Remove item
            _db.CartItems.Remove(cartItem);
            await _db.SaveChangesAsync();

            return Json(new { success = true });
        }

        // POST: Cart/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout()
        {
            string userId = _userManager.GetUserId(User);
            var user = await _db.Users.FindAsync(userId);

            if (user.UserType != UserType.Retailer)
            {
                return RedirectToAction("Index", "Home");
            }

            // Get all cart items grouped by wholesaler
            var cartItems = await _db.CartItems
                .Include(ci => ci.WholesalerProduct)
                    .ThenInclude(wp => wp.Product)
                .Where(ci => ci.RetailerID == userId)
                .ToListAsync();

            if (!cartItems.Any())
            {
                return RedirectToAction("Index");
            }

            // Group by wholesaler and create separate orders for each
            var wholesalerGroups = cartItems
                .GroupBy(ci => ci.WholesalerProduct.WholesalerID)
                .ToList();

            foreach (var group in wholesalerGroups)
            {
                // Create new order
                var order = new Order
                {
                    OrderDate = DateTime.Now,
                    RetailerID = userId,
                    WholesalerID = group.Key,
                    Status = OrderStatus.Pending,
                    OrderItems = new List<OrderItem>()
                };

                // Add order items
                foreach (var cartItem in group)
                {
                    // Check if product is still available in requested quantity
                    var wholesalerProduct = await _db.WholesalerProducts
                        .FindAsync(cartItem.WholesalerProductID);

                    if (wholesalerProduct.AvailableQuantity < cartItem.Quantity)
                    {
                        // If not available, show error and return to cart
                        TempData["ErrorMessage"] = $"Product '{cartItem.WholesalerProduct.Product.Name}' is no longer available in the requested quantity.";
                        return RedirectToAction("Index");
                    }

                    // Add order item
                    order.OrderItems.Add(new OrderItem
                    {
                        ProductID = cartItem.WholesalerProduct.ProductID,
                        WholesalerProductID = cartItem.WholesalerProductID,
                        Quantity = cartItem.Quantity,
                        Price = cartItem.WholesalerProduct.Price
                    });

                    // Update available quantity
                    wholesalerProduct.AvailableQuantity -= cartItem.Quantity;
                }

                // Add order to database
                _db.Orders.Add(order);
            }

            // Remove all cart items
            _db.CartItems.RemoveRange(cartItems);

            // Save all changes
            await _db.SaveChangesAsync();

            // Redirect to orders page with success message
            TempData["SuccessMessage"] = "Your order has been placed successfully.";
            return RedirectToAction("MyOrders", "Order");
        }
    }
}