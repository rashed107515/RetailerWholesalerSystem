using System;
using System.Linq;
using System.Security.Claims;
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
        private readonly object _logger;

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
        // Add/update this action in your CartController.cs file

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int wholesalerProductId, int quantity)
        {
            try
            {
                // Get current user ID
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Validate the wholesaler product exists
                var wholesalerProduct = await _db.WholesalerProducts
                    .Include(wp => wp.Product)
                    .FirstOrDefaultAsync(wp => wp.WholesalerProductID == wholesalerProductId);

                if (wholesalerProduct == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                // Validate quantity
                if (quantity < wholesalerProduct.MinimumOrderQuantity)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Minimum order quantity is {wholesalerProduct.MinimumOrderQuantity}"
                    });
                }

                if (quantity > wholesalerProduct.AvailableQuantity)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Only {wholesalerProduct.AvailableQuantity} units available"
                    });
                }

                // Check if item already exists in cart
                var cartItem = await _db.CartItems
                    .FirstOrDefaultAsync(ci => ci.RetailerID == userId &&
                                              ci.WholesalerProductID == wholesalerProductId);

                if (cartItem != null)
                {
                    // Update existing cart item
                    cartItem.Quantity = quantity;
                }
                else
                {
                    // Add new cart item
                    cartItem = new CartItem
                    {
                        RetailerID = userId,
                        WholesalerProductID = wholesalerProductId,
                        Quantity = quantity,
                        DateAdded = DateTime.Now
                    };
                    _db.CartItems.Add(cartItem);
                }

                await _db.SaveChangesAsync();

                // Get updated cart count
                var cartCount = await _db.CartItems
                    .Where(ci => ci.RetailerID == userId)
                    .CountAsync();

                return Json(new
                {
                    success = true,
                    message = "Product added to cart successfully",
                    cartCount = cartCount
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
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

        // Add these methods to your CartController.cs file

        // Add these methods to your CartController.cs file

        // GET: Cart/Checkout
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            string userId = _userManager.GetUserId(User);
            var user = await _db.Users.FindAsync(userId);

            if (user.UserType != UserType.Retailer)
            {
                return RedirectToAction("Index", "Home");
            }

            // Get cart items with all related data
            var cartItems = await _db.CartItems
                .Include(ci => ci.WholesalerProduct)
                    .ThenInclude(wp => wp.Product)
                        .ThenInclude(p => p.Category)
                .Include(ci => ci.WholesalerProduct)
                    .ThenInclude(wp => wp.Wholesaler)
                .Where(ci => ci.RetailerID == userId)
                .ToListAsync();

            if (!cartItems.Any())
            {
                // Redirect to cart if empty
                TempData["InfoMessage"] = "Your cart is empty. Add products before proceeding to checkout.";
                return RedirectToAction(nameof(Index));
            }

            // Get user profile data to pre-fill checkout form (if you have a retailer profile table)
            // Assuming you have a RetailerProfile model - adjust as needed
            var retailerProfile = await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.Address, u.PhoneNumber })
                .FirstOrDefaultAsync();

            var viewModel = new CheckoutViewModel
            {
                CartItems = cartItems,
                DeliveryAddress = retailerProfile?.Address ?? "",
                ContactPhone = retailerProfile?.PhoneNumber ?? ""
            };

            return View(viewModel);
        }

        // POST: Cart/PlaceOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            string userId = _userManager.GetUserId(User);
            var user = await _db.Users.FindAsync(userId);

            if (user.UserType != UserType.Retailer)
            {
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                // Reload cart items if model validation fails
                model.CartItems = await _db.CartItems
                    .Include(ci => ci.WholesalerProduct)
                        .ThenInclude(wp => wp.Product)
                            .ThenInclude(p => p.Category)
                    .Include(ci => ci.WholesalerProduct)
                        .ThenInclude(wp => wp.Wholesaler)
                    .Where(ci => ci.RetailerID == userId)
                    .ToListAsync();

                return View("Checkout", model);
            }

            try
            {
                // Get cart items
                var cartItems = await _db.CartItems
                    .Include(ci => ci.WholesalerProduct)
                        .ThenInclude(wp => wp.Product)
                    .Include(ci => ci.WholesalerProduct)
                        .ThenInclude(wp => wp.Wholesaler)
                    .Where(ci => ci.RetailerID == userId)
                    .ToListAsync();

                if (!cartItems.Any())
                {
                    TempData["InfoMessage"] = "Your cart is empty.";
                    return RedirectToAction(nameof(Index));
                }

                // Start a database transaction
                using var transaction = await _db.Database.BeginTransactionAsync();

                try
                {
                    // Group cart items by wholesaler
                    var wholesalerGroups = cartItems.GroupBy(ci => ci.WholesalerProduct.WholesalerID);
                    var orderIds = new List<int>();

                    foreach (var group in wholesalerGroups)
                    {
                        // Create an order for each wholesaler
                        var wholesalerId = group.Key;
                        var orderItems = group.ToList();

                        // Create order
                        var order = new Order
                        {
                            OrderDate = DateTime.Now,
                            RetailerID = userId,
                            WholesalerID = wholesalerId,
                            Status = OrderStatus.Pending,
                            // If you want to store these additional fields, you'll need to add them to your Order model
                            // Or create an OrderDetails table for this extra information
                        };

                        // Create order items collection
                        var newOrderItems = new List<OrderItem>();

                        foreach (var item in orderItems)
                        {
                            // Check if product is still available in requested quantity
                            var wholesalerProduct = await _db.WholesalerProducts
                                .FindAsync(item.WholesalerProductID);

                            if (wholesalerProduct.AvailableQuantity < item.Quantity)
                            {
                                ModelState.AddModelError("", $"Product '{item.WholesalerProduct.Product.Name}' is no longer available in the requested quantity. Only {wholesalerProduct.AvailableQuantity} units available.");

                                // Reload cart items
                                model.CartItems = cartItems;
                                return View("Checkout", model);
                            }

                            // Create order item
                            var orderItem = new OrderItem
                            {
                                ProductID = item.WholesalerProduct.ProductID,
                                WholesalerProductID = item.WholesalerProductID,
                                Quantity = item.Quantity,
                                Price = item.WholesalerProduct.Price
                            };

                            newOrderItems.Add(orderItem);

                            // Update available quantity
                            wholesalerProduct.AvailableQuantity -= item.Quantity;
                        }

                        // Assign order items
                        order.OrderItems = newOrderItems;

                        // Add order to database
                        _db.Orders.Add(order);
                        await _db.SaveChangesAsync();

                        // Store order ID for confirmation
                        orderIds.Add(order.OrderID);
                    }

                    // Remove all cart items
                    _db.CartItems.RemoveRange(cartItems);
                    await _db.SaveChangesAsync();

                    // Commit transaction
                    await transaction.CommitAsync();

                    // Store order IDs in TempData for confirmation page
                    TempData["OrderIds"] = string.Join(",", orderIds);
                    TempData["SuccessMessage"] = "Your order has been placed successfully!";

                    // Redirect to confirmation page
                    return RedirectToAction("OrderConfirmation");
                }
                catch (Exception ex)
                {
                    // Roll back transaction on error
                    await transaction.RollbackAsync();

                    // Log the error
                    // _logger.LogError(ex, "Error placing order"); // Uncomment if you have logging configured

                    ModelState.AddModelError("", $"An error occurred while processing your order: {ex.Message}");

                    // Reload cart items
                    model.CartItems = cartItems;
                    return View("Checkout", model);
                }
            }
            catch (Exception ex)
            {
                // Log the error
                // _logger.LogError(ex, "Error accessing cart data"); // Uncomment if you have logging configured

                ModelState.AddModelError("", "An error occurred while accessing your cart data. Please try again.");
                return View("Checkout", model);
            }
        }

        // GET: Cart/OrderConfirmation
        public IActionResult OrderConfirmation()
        {
            // Display order confirmation page
            // Order IDs are available in TempData["OrderIds"] if needed
            return View();
        }

        // POST: Cart/Checkout
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Checkout()
        //{
        //    string userId = _userManager.GetUserId(User);
        //    var user = await _db.Users.FindAsync(userId);

        //    if (user.UserType != UserType.Retailer)
        //    {
        //        return RedirectToAction("Index", "Home");
        //    }

        //    // Get all cart items grouped by wholesaler
        //    var cartItems = await _db.CartItems
        //        .Include(ci => ci.WholesalerProduct)
        //            .ThenInclude(wp => wp.Product)
        //        .Where(ci => ci.RetailerID == userId)
        //        .ToListAsync();

        //    if (!cartItems.Any())
        //    {
        //        return RedirectToAction("Index");
        //    }

        //    // Group by wholesaler and create separate orders for each
        //    var wholesalerGroups = cartItems
        //        .GroupBy(ci => ci.WholesalerProduct.WholesalerID)
        //        .ToList();

        //    foreach (var group in wholesalerGroups)
        //    {
        //        // Create new order
        //        var order = new Order
        //        {
        //            OrderDate = DateTime.Now,
        //            RetailerID = userId,
        //            WholesalerID = group.Key,
        //            Status = OrderStatus.Pending,
        //            OrderItems = new List<OrderItem>()
        //        };

        //        // Add order items
        //        foreach (var cartItem in group)
        //        {
        //            // Check if product is still available in requested quantity
        //            var wholesalerProduct = await _db.WholesalerProducts
        //                .FindAsync(cartItem.WholesalerProductID);

        //            if (wholesalerProduct.AvailableQuantity < cartItem.Quantity)
        //            {
        //                // If not available, show error and return to cart
        //                TempData["ErrorMessage"] = $"Product '{cartItem.WholesalerProduct.Product.Name}' is no longer available in the requested quantity.";
        //                return RedirectToAction("Index");
        //            }

        //            // Add order item
        //            order.OrderItems.Add(new OrderItem
        //            {
        //                ProductID = cartItem.WholesalerProduct.ProductID,
        //                WholesalerProductID = cartItem.WholesalerProductID,
        //                Quantity = cartItem.Quantity,
        //                Price = cartItem.WholesalerProduct.Price
        //            });

        //            // Update available quantity
        //            wholesalerProduct.AvailableQuantity -= cartItem.Quantity;
        //        }

        //        // Add order to database
        //        _db.Orders.Add(order);
        //    }

        //    // Remove all cart items
        //    _db.CartItems.RemoveRange(cartItems);

        //    // Save all changes
        //    await _db.SaveChangesAsync();

        //    // Redirect to orders page with success message
        //    TempData["SuccessMessage"] = "Your order has been placed successfully.";
        //    return RedirectToAction("MyOrders", "Order");
        //}
    }
}