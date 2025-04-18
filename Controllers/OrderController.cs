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
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrderController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _db = context;
            _userManager = userManager;
        }

        // GET: Orders for Retailer
        public async Task<IActionResult> MyOrders()
        {
            string userId = _userManager.GetUserId(User);
            var user = await _db.Users.FindAsync(userId);

            if (user.UserType != UserType.Retailer)
            {
                return RedirectToAction("Index", "Home");
            }

            var orders = await _db.Orders
                .Include(o => o.Wholesaler)
                .Include(o => o.OrderItems)
                .Where(o => o.RetailerID == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // GET: Orders for Wholesaler
        public async Task<IActionResult> ReceivedOrders()
        {
            string userId = _userManager.GetUserId(User);
            var user = await _db.Users.FindAsync(userId);

            if (user.UserType != UserType.Wholesaler)
            {
                return RedirectToAction("Index", "Home");
            }

            var orders = await _db.Orders
                .Include(o => o.Retailer)
                .Include(o => o.OrderItems)
                .Where(o => o.WholesalerID == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // GET: Order Details
        public async Task<IActionResult> Details(int id)
        {
            string userId = _userManager.GetUserId(User);
            var user = await _db.Users.FindAsync(userId);

            var order = await _db.Orders
                .Include(o => o.Retailer)
                .Include(o => o.Wholesaler)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderID == id);

            if (order == null)
            {
                return NotFound();
            }

            // Check if the user is authorized to view this order
            if (user.UserType == UserType.Retailer && order.RetailerID != userId ||
                user.UserType == UserType.Wholesaler && order.WholesalerID != userId)
            {
                return Forbid();
            }

            return View(order);
        }

        // POST: Update Order Status (for Wholesalers)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int orderId, OrderStatus newStatus)
        {
            string userId = _userManager.GetUserId(User);
            var user = await _db.Users.FindAsync(userId);

            if (user.UserType != UserType.Wholesaler)
            {
                return Forbid();
            }

            var order = await _db.Orders
                .FirstOrDefaultAsync(o => o.OrderID == orderId && o.WholesalerID == userId);

            if (order == null)
            {
                return NotFound();
            }

            // Update status
            order.Status = newStatus;

            // If status is shipped, set shipped date
            if (newStatus == OrderStatus.Shipped)
            {
                order.ShippedDate = DateTime.Now;
            }

            // If status is delivered, set delivered date
            if (newStatus == OrderStatus.Delivered)
            {
                order.DeliveredDate = DateTime.Now;
            }

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        // POST: Cancel Order (for Retailers, only if order is still pending)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            string userId = _userManager.GetUserId(User);
            var user = await _db.Users.FindAsync(userId);

            if (user.UserType != UserType.Retailer)
            {
                return Forbid();
            }

            var order = await _db.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.WholesalerProduct)
                .FirstOrDefaultAsync(o => o.OrderID == orderId && o.RetailerID == userId);

            if (order == null)
            {
                return NotFound();
            }

            // Can only cancel if order is pending
            if (order.Status != OrderStatus.Pending)
            {
                TempData["ErrorMessage"] = "Only pending orders can be cancelled.";
                return RedirectToAction(nameof(Details), new { id = orderId });
            }

            // Update order status
            order.Status = OrderStatus.Cancelled;

            // Return items to inventory
            foreach (var item in order.OrderItems)
            {
                item.WholesalerProduct.AvailableQuantity += item.Quantity;
            }

            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your order has been cancelled.";
            return RedirectToAction(nameof(MyOrders));
        }

        // Add this method to your OrderController
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, OrderStatus newStatus)
        {
            string userId = _userManager.GetUserId(User);
            var user = await _db.Users.FindAsync(userId);

            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderID == orderId && o.WholesalerID == userId);

            if (order == null)
            {
                return NotFound();
            }

            // Update order status
            order.Status = newStatus;

            // If the order is being marked as "Shipped" or "Delivered"
            if (newStatus == OrderStatus.Shipped || newStatus == OrderStatus.Delivered)
            {
                // Order items should already be in retailer's inventory from the initial order
                // But if you want to handle fulfillment separately, you could add inventory here
            }

            await _db.SaveChangesAsync();
            return RedirectToAction("WholesalerOrders");
        }
    }
}