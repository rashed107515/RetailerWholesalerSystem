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

                // Create an execution strategy for the transaction
                var strategy = _db.Database.CreateExecutionStrategy();

                var orderIds = new List<int>();

                // Execute everything in a retriable transaction
                await strategy.ExecuteAsync(async () =>
                {
                    // Start a database transaction
                    using var transaction = await _db.Database.BeginTransactionAsync();

                    try
                    {
                        // Group cart items by wholesaler
                        var wholesalerGroups = cartItems.GroupBy(ci => ci.WholesalerProduct.WholesalerID);

                        foreach (var group in wholesalerGroups)
                        {
                            // Create an order for each wholesaler
                            var wholesalerId = group.Key;
                            var orderItems = group.ToList();

                            var order = new Order
                            {
                                OrderDate = DateTime.Now,
                                RetailerID = userId,
                                WholesalerID = wholesalerId,
                                Status = OrderStatus.Pending,
                                TrackingNumber = "Pending",
                                DeliveryAddress = model.DeliveryAddress,
                                ContactPhone = model.ContactPhone,
                                PreferredDeliveryDate = model.PreferredDeliveryDate,
                                DeliveryInstructions = model.DeliveryInstructions,
                                PaymentMethod = model.PaymentMethod
                            };

                            // Add wholesaler-specific notes if available
                            string wholesalerIdStr = wholesalerId.ToString();
                            if (model.WholesalerNotes != null && model.WholesalerNotes.ContainsKey(wholesalerIdStr))
                            {
                                order.WholesalerNotes = model.WholesalerNotes[wholesalerIdStr];
                            }

                            // Add order to database and save to get OrderID
                            _db.Orders.Add(order);
                            await _db.SaveChangesAsync();

                            // Create order items after getting OrderID
                            foreach (var item in orderItems)
                            {
                                var wholesalerProduct = await _db.WholesalerProducts
                                    .Include(wp => wp.Product)
                                    .FirstOrDefaultAsync(wp => wp.WholesalerProductID == item.WholesalerProductID);

                                if (wholesalerProduct.AvailableQuantity < item.Quantity)
                                {
                                    throw new InvalidOperationException($"Product '{item.WholesalerProduct.Product.Name}' is no longer available in the requested quantity. Only {wholesalerProduct.AvailableQuantity} units available.");
                                }

                                // Create order item with OrderID
                                var orderItem = new OrderItem
                                {
                                    OrderID = order.OrderID,
                                    ProductID = item.WholesalerProduct.ProductID,
                                    WholesalerProductID = item.WholesalerProductID,
                                    Quantity = item.Quantity,
                                    Price = item.WholesalerProduct.Price
                                };

                                // Add order item to database
                                _db.OrderItems.Add(orderItem);

                                // Update available quantity for wholesaler
                                wholesalerProduct.AvailableQuantity -= item.Quantity;

                                // Add to retailer's inventory automatically
                                var existingRetailerProduct = await _db.RetailerProducts
                                    .FirstOrDefaultAsync(rp => rp.RetailerID == userId &&
                                                              rp.ProductID == item.WholesalerProduct.ProductID);

                                if (existingRetailerProduct != null)
                                {
                                    // Update existing inventory
                                    existingRetailerProduct.StockQuantity += item.Quantity;
                                    // Optionally recalculate price average or keep existing price
                                }
                                else
                                {
                                    // Add new product to retailer's inventory with default markup
                                    decimal suggestedRetailPrice = item.WholesalerProduct.Price * 1.2m; // 20% markup

                                    var retailerProduct = new RetailerProduct
                                    {
                                        RetailerID = userId,
                                        ProductID = item.WholesalerProduct.ProductID,
                                        Price = suggestedRetailPrice,
                                        StockQuantity = item.Quantity
                                    };

                                    _db.RetailerProducts.Add(retailerProduct);
                                }
                            }

                            // Save order items and quantity updates
                            await _db.SaveChangesAsync();

                            // Store order ID for confirmation
                            orderIds.Add(order.OrderID);
                        }

                        // Remove all cart items
                        _db.CartItems.RemoveRange(cartItems);
                        await _db.SaveChangesAsync();

                        // Commit transaction
                        await transaction.CommitAsync();
                    }
                    catch (Exception)
                    {
                        // Roll back transaction on error
                        await transaction.RollbackAsync();
                        throw; // Re-throw to be caught by the outer try-catch
                    }
                });

                // Store order IDs in TempData for confirmation page
                TempData["OrderIds"] = string.Join(",", orderIds);
                TempData["SuccessMessage"] = "Your order has been placed successfully! Products have been added to your inventory.";

                // Redirect to confirmation page
                return RedirectToAction("OrderConfirmation");
            }
            catch (Exception ex)
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine($"Exception in PlaceOrder: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                ModelState.AddModelError("", $"An error occurred while processing your order: {ex.Message}");

                // Reload cart items
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
        }

        // POST: Cart/PlaceOrder
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        //{
        //    string userId = _userManager.GetUserId(User);
        //    var user = await _db.Users.FindAsync(userId);

        //    if (user.UserType != UserType.Retailer)
        //    {
        //        return RedirectToAction("Index", "Home");
        //    }

        //    if (!ModelState.IsValid)
        //    {
        //        // Reload cart items if model validation fails
        //        model.CartItems = await _db.CartItems
        //            .Include(ci => ci.WholesalerProduct)
        //                .ThenInclude(wp => wp.Product)
        //                    .ThenInclude(p => p.Category)
        //            .Include(ci => ci.WholesalerProduct)
        //                .ThenInclude(wp => wp.Wholesaler)
        //            .Where(ci => ci.RetailerID == userId)
        //            .ToListAsync();

        //        return View("Checkout", model);
        //    }

        //    try
        //    {
        //        // Get cart items
        //        var cartItems = await _db.CartItems
        //            .Include(ci => ci.WholesalerProduct)
        //                .ThenInclude(wp => wp.Product)
        //            .Include(ci => ci.WholesalerProduct)
        //                .ThenInclude(wp => wp.Wholesaler)
        //            .Where(ci => ci.RetailerID == userId)
        //            .ToListAsync();

        //        if (!cartItems.Any())
        //        {
        //            TempData["InfoMessage"] = "Your cart is empty.";
        //            return RedirectToAction(nameof(Index));
        //        }

        //        // Create an execution strategy for the transaction
        //        var strategy = _db.Database.CreateExecutionStrategy();

        //        var orderIds = new List<int>();

        //        // Execute everything in a retriable transaction
        //        await strategy.ExecuteAsync(async () =>
        //        {
        //            // Start a database transaction
        //            using var transaction = await _db.Database.BeginTransactionAsync();

        //            try
        //            {
        //                // Group cart items by wholesaler
        //                var wholesalerGroups = cartItems.GroupBy(ci => ci.WholesalerProduct.WholesalerID);

        //                foreach (var group in wholesalerGroups)
        //                {
        //                    // Create an order for each wholesaler
        //                    var wholesalerId = group.Key;
        //                    var orderItems = group.ToList();

        //                    var order = new Order
        //                    {
        //                        OrderDate = DateTime.Now,
        //                        RetailerID = userId,
        //                        WholesalerID = wholesalerId,
        //                        Status = OrderStatus.Pending,
        //                        TrackingNumber = "Pending",
        //                        DeliveryAddress = model.DeliveryAddress,
        //                        ContactPhone = model.ContactPhone,
        //                        PreferredDeliveryDate = model.PreferredDeliveryDate,
        //                        DeliveryInstructions = model.DeliveryInstructions,
        //                        PaymentMethod = model.PaymentMethod
        //                    };

        //                    // Add wholesaler-specific notes if available
        //                    string wholesalerIdStr = wholesalerId.ToString();
        //                    if (model.WholesalerNotes != null && model.WholesalerNotes.ContainsKey(wholesalerIdStr))
        //                    {
        //                        order.WholesalerNotes = model.WholesalerNotes[wholesalerIdStr];
        //                    }

        //                    // Add order to database and save to get OrderID
        //                    _db.Orders.Add(order);
        //                    await _db.SaveChangesAsync();

        //                    // Create order items after getting OrderID
        //                    foreach (var item in orderItems)
        //                    {
        //                        var wholesalerProduct = await _db.WholesalerProducts
        //                            .Include(wp => wp.Product)
        //                            .FirstOrDefaultAsync(wp => wp.WholesalerProductID == item.WholesalerProductID);

        //                        if (wholesalerProduct.AvailableQuantity < item.Quantity)
        //                        {
        //                            throw new InvalidOperationException($"Product '{item.WholesalerProduct.Product.Name}' is no longer available in the requested quantity. Only {wholesalerProduct.AvailableQuantity} units available.");
        //                        }

        //                        // Create order item with OrderID
        //                        var orderItem = new OrderItem
        //                        {
        //                            OrderID = order.OrderID,
        //                            ProductID = item.WholesalerProduct.ProductID,
        //                            WholesalerProductID = item.WholesalerProductID,
        //                            Quantity = item.Quantity,
        //                            Price = item.WholesalerProduct.Price
        //                        };

        //                        // Add order item to database
        //                        _db.OrderItems.Add(orderItem);

        //                        // Update available quantity for wholesaler
        //                        wholesalerProduct.AvailableQuantity -= item.Quantity;

        //                        // Add to retailer's inventory automatically
        //                        var existingRetailerProduct = await _db.RetailerProducts
        //                            .FirstOrDefaultAsync(rp => rp.RetailerID == userId &&
        //                                                      rp.ProductID == item.WholesalerProduct.ProductID);

        //                        if (existingRetailerProduct != null)
        //                        {
        //                            // Update existing inventory
        //                            existingRetailerProduct.StockQuantity += item.Quantity;
        //                            // Optionally recalculate price average or keep existing price
        //                        }
        //                        else
        //                        {
        //                            // Add new product to retailer's inventory with default markup
        //                            decimal suggestedRetailPrice = item.WholesalerProduct.Price * 1.2m; // 20% markup

        //                            var retailerProduct = new RetailerProduct
        //                            {
        //                                RetailerID = userId,
        //                                ProductID = item.WholesalerProduct.ProductID,
        //                                Price = suggestedRetailPrice,
        //                                StockQuantity = item.Quantity
        //                            };

        //                            _db.RetailerProducts.Add(retailerProduct);
        //                        }
        //                    }

        //                    // Save order items and quantity updates
        //                    await _db.SaveChangesAsync();

        //                    // Store order ID for confirmation
        //                    orderIds.Add(order.OrderID);
        //                }

        //                // Remove all cart items
        //                _db.CartItems.RemoveRange(cartItems);
        //                await _db.SaveChangesAsync();

        //                // Commit transaction
        //                await transaction.CommitAsync();
        //            }
        //            catch (Exception)
        //            {
        //                // Roll back transaction on error
        //                await transaction.RollbackAsync();
        //                throw; // Re-throw to be caught by the outer try-catch
        //            }
        //        });

        //        // Store order IDs in TempData for confirmation page
        //        TempData["OrderIds"] = string.Join(",", orderIds);
        //        TempData["SuccessMessage"] = "Your order has been placed successfully! Products have been added to your inventory.";

        //        // Redirect to confirmation page
        //        return RedirectToAction("OrderConfirmation");
        //    }
        //    catch (Exception ex)
        //    {
        //        // Log the error
        //        System.Diagnostics.Debug.WriteLine($"Exception in PlaceOrder: {ex.Message}");
        //        System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

        //        ModelState.AddModelError("", $"An error occurred while processing your order: {ex.Message}");

        //        // Reload cart items
        //        model.CartItems = await _db.CartItems
        //            .Include(ci => ci.WholesalerProduct)
        //                .ThenInclude(wp => wp.Product)
        //                    .ThenInclude(p => p.Category)
        //            .Include(ci => ci.WholesalerProduct)
        //                .ThenInclude(wp => wp.Wholesaler)
        //            .Where(ci => ci.RetailerID == userId)
        //            .ToListAsync();

        //        return View("Checkout", model);
        //    }
        //}

        //// POST: Cart/PlaceOrder
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        //{
        //    string userId = _userManager.GetUserId(User);
        //    var user = await _db.Users.FindAsync(userId);

        //    if (user.UserType != UserType.Retailer)
        //    {
        //        return RedirectToAction("Index", "Home");
        //    }

        //    if (!ModelState.IsValid)
        //    {
        //        // Reload cart items if model validation fails
        //        model.CartItems = await _db.CartItems
        //            .Include(ci => ci.WholesalerProduct)
        //                .ThenInclude(wp => wp.Product)
        //                    .ThenInclude(p => p.Category)
        //            .Include(ci => ci.WholesalerProduct)
        //                .ThenInclude(wp => wp.Wholesaler)
        //            .Where(ci => ci.RetailerID == userId)
        //            .ToListAsync();

        //        return View("Checkout", model);
        //    }

        //    try
        //    {
        //        // Get cart items
        //        var cartItems = await _db.CartItems
        //            .Include(ci => ci.WholesalerProduct)
        //                .ThenInclude(wp => wp.Product)
        //            .Include(ci => ci.WholesalerProduct)
        //                .ThenInclude(wp => wp.Wholesaler)
        //            .Where(ci => ci.RetailerID == userId)
        //            .ToListAsync();

        //        if (!cartItems.Any())
        //        {
        //            TempData["InfoMessage"] = "Your cart is empty.";
        //            return RedirectToAction(nameof(Index));
        //        }

        //        // Create an execution strategy for the transaction
        //        var strategy = _db.Database.CreateExecutionStrategy();

        //        var orderIds = new List<int>();

        //        // Execute everything in a retriable transaction
        //        await strategy.ExecuteAsync(async () =>
        //        {
        //            // Start a database transaction
        //            using var transaction = await _db.Database.BeginTransactionAsync();

        //            try
        //            {
        //                // Group cart items by wholesaler
        //                var wholesalerGroups = cartItems.GroupBy(ci => ci.WholesalerProduct.WholesalerID);

        //                foreach (var group in wholesalerGroups)
        //                {
        //                    // Create an order for each wholesaler
        //                    var wholesalerId = group.Key;
        //                    var orderItems = group.ToList();

        //                    var order = new Order
        //                    {
        //                        OrderDate = DateTime.Now,
        //                        RetailerID = userId,
        //                        WholesalerID = wholesalerId,
        //                        Status = OrderStatus.Pending,
        //                        TrackingNumber = "Pending", // Add this line
        //                        DeliveryAddress = model.DeliveryAddress,
        //                        ContactPhone = model.ContactPhone,
        //                        PreferredDeliveryDate = model.PreferredDeliveryDate,
        //                        DeliveryInstructions = model.DeliveryInstructions,
        //                        PaymentMethod = model.PaymentMethod
        //                    };

        //                    // Add wholesaler-specific notes if available
        //                    string wholesalerIdStr = wholesalerId.ToString();
        //                    if (model.WholesalerNotes != null && model.WholesalerNotes.ContainsKey(wholesalerIdStr))
        //                    {
        //                        order.WholesalerNotes = model.WholesalerNotes[wholesalerIdStr];
        //                    }

        //                    // Add order to database and save to get OrderID
        //                    _db.Orders.Add(order);
        //                    await _db.SaveChangesAsync();

        //                    // Create order items after getting OrderID
        //                    foreach (var item in orderItems)
        //                    {
        //                        var wholesalerProduct = await _db.WholesalerProducts
        //                            .Include(wp => wp.Product)
        //                            .FirstOrDefaultAsync(wp => wp.WholesalerProductID == item.WholesalerProductID);
        //                        if (wholesalerProduct.AvailableQuantity < item.Quantity)
        //                        {
        //                            throw new InvalidOperationException($"Product '{item.WholesalerProduct.Product.Name}' is no longer available in the requested quantity. Only {wholesalerProduct.AvailableQuantity} units available.");
        //                        }

        //                        // Create order item with OrderID
        //                        var orderItem = new OrderItem
        //                        {
        //                            OrderID = order.OrderID,
        //                            ProductID = item.WholesalerProduct.ProductID,
        //                            WholesalerProductID = item.WholesalerProductID,
        //                            Quantity = item.Quantity,
        //                            Price = item.WholesalerProduct.Price
        //                        };

        //                        // Add order item to database
        //                        _db.OrderItems.Add(orderItem);

        //                        // Update available quantity
        //                        wholesalerProduct.AvailableQuantity -= item.Quantity;
        //                    }

        //                    // Save order items and quantity updates
        //                    await _db.SaveChangesAsync();                            // Store order ID for confirmation
        //                    orderIds.Add(order.OrderID);
        //                }

        //                // Remove all cart items
        //                _db.CartItems.RemoveRange(cartItems);
        //                await _db.SaveChangesAsync();

        //                // Commit transaction
        //                await transaction.CommitAsync();
        //            }
        //            catch (Exception)
        //            {
        //                // Roll back transaction on error
        //                await transaction.RollbackAsync();
        //                throw; // Re-throw to be caught by the outer try-catch
        //            }
        //        });

        //        // Store order IDs in TempData for confirmation page
        //        TempData["OrderIds"] = string.Join(",", orderIds);
        //        TempData["SuccessMessage"] = "Your order has been placed successfully!";

        //        // Redirect to confirmation page
        //        return RedirectToAction("OrderConfirmation");
        //    }
        //    catch (Exception ex)
        //    {
        //        // Log the error
        //        System.Diagnostics.Debug.WriteLine($"Exception in PlaceOrder: {ex.Message}");
        //        System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

        //        ModelState.AddModelError("", $"An error occurred while processing your order: {ex.Message}");

        //        // Reload cart items
        //        model.CartItems = await _db.CartItems
        //            .Include(ci => ci.WholesalerProduct)
        //                .ThenInclude(wp => wp.Product)
        //                    .ThenInclude(p => p.Category)
        //            .Include(ci => ci.WholesalerProduct)
        //                .ThenInclude(wp => wp.Wholesaler)
        //            .Where(ci => ci.RetailerID == userId)
        //            .ToListAsync();

        //        return View("Checkout", model);
        //    }
        //}

        // GET: Cart/OrderConfirmation
        public IActionResult OrderConfirmation()
        {
            // Display order confirmation page
            // Order IDs are available in TempData["OrderIds"] if needed
            return View();
        }

    }
}