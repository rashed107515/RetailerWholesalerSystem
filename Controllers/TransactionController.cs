using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailerWholesalerSystem.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
namespace RetailerWholesalerSystem.Controllers
{
    [Authorize]
    public class TransactionController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Transaction
        public ActionResult Index()
        {
            string userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var transactions = db.Transactions
                .Include(t => t.Wholesaler)
                .Where(t => t.RetailerID == userId)
                .OrderByDescending(t => t.Date)
                .ToList();
            return View(transactions);
        }

        // GET: Transaction/Create
        public ActionResult SelectWholesaler()
        {
            // Show list of wholesalers to choose from
            var wholesalers = db.Users
                .Where(u => u.UserType == UserType.Wholesaler)
                .ToList();
            return View(wholesalers);
        }

        // GET: Transaction/Create/5 (5 is wholesaler ID)
        // GET: Transaction/Create/5
        // GET: Transaction/Create/5
        public ActionResult Create(string id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            var wholesaler = db.Users.Find(id);
            if (wholesaler == null)
            {
                return NotFound();
            }

            try
            {
                // Get products from this wholesaler
                var wholesalerProducts = db.WholesalerProducts
                    .Include(wp => wp.Product)
                    .Where(wp => wp.WholesalerID == id && wp.AvailableQuantity > 0)
                    .ToList();

                ViewBag.WholesalerProducts = wholesalerProducts;
                ViewBag.Wholesaler = wholesaler;

                // Create new transaction with wholesaler ID
                var transaction = new Transaction
                {
                    WholesalerID = id,
                    RetailerID = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    Date = DateTime.Now,
                    Status = TransactionStatus.Pending
                };

                return View(transaction);
            }
            catch (Exception ex)
            {
                // Log the exception
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");

                // Return a simple error
                ViewBag.ErrorMessage = "An error occurred while processing your request.";
                return View("Error");
            }
        }        // POST: Transaction/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Transaction transaction, int[] productIds, int[] quantities)
        {
            if (ModelState.IsValid)
            {
                transaction.RetailerID = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                transaction.Date = DateTime.Now;
                transaction.Status = TransactionStatus.Pending;

                decimal totalAmount = 0;
                transaction.TransactionDetails = new List<TransactionDetail>();

                // Process each product in the order
                for (int i = 0; i < productIds.Length; i++)
                {
                    if (quantities[i] <= 0) continue;

                    var wholesalerProduct = db.WholesalerProducts
                        .Include(wp => wp.Product)
                        .FirstOrDefault(wp => wp.ProductID == productIds[i] && wp.WholesalerID == transaction.WholesalerID);

                    if (wholesalerProduct != null && quantities[i] <= wholesalerProduct.AvailableQuantity)
                    {
                        decimal subtotal = wholesalerProduct.Price * quantities[i];

                        // Create transaction detail
                        var detail = new TransactionDetail
                        {
                            ProductID = productIds[i],
                            Quantity = quantities[i],
                            UnitPrice = wholesalerProduct.Price,
                            Subtotal = subtotal
                        };

                        transaction.TransactionDetails.Add(detail);
                        totalAmount += subtotal;

                        // Update available quantity
                        wholesalerProduct.AvailableQuantity -= quantities[i];
                    }
                }

                // If no products were selected or all had quantity 0
                if (transaction.TransactionDetails.Count == 0)
                {
                    ModelState.AddModelError("", "Please select at least one product with quantity greater than 0.");

                    // Repopulate required data for the view
                    var wholesaler = db.Users.Find(transaction.WholesalerID);
                    var wholesalerProducts = db.WholesalerProducts
                        .Include(wp => wp.Product)
                        .Where(wp => wp.WholesalerID == transaction.WholesalerID && wp.AvailableQuantity > 0)
                        .ToList();

                    ViewBag.WholesalerProducts = wholesalerProducts;
                    ViewBag.Wholesaler = wholesaler;

                    return View(transaction);
                }

                transaction.TotalAmount = totalAmount;

                db.Transactions.Add(transaction);
                db.SaveChanges();

                return RedirectToAction("Confirmation", new { id = transaction.TransactionID });
            }

            // If we got this far, something failed, redisplay form
            var wholesalerForError = db.Users.Find(transaction.WholesalerID);
            var productsForError = db.WholesalerProducts
                .Include(wp => wp.Product)
                .Where(wp => wp.WholesalerID == transaction.WholesalerID && wp.AvailableQuantity > 0)
                .ToList();

            ViewBag.WholesalerProducts = productsForError;
            ViewBag.Wholesaler = wholesalerForError;

            return View(transaction);
        }

        // GET: Transaction/Confirmation/5
        public ActionResult Confirmation(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            var transaction = db.Transactions
                .Include(t => t.Retailer)
                .Include(t => t.Wholesaler)
                .Include(t => t.TransactionDetails)
                .Include("TransactionDetails.Product")
                .FirstOrDefault(t => t.TransactionID == id);

            if (transaction == null)
            {
                return NotFound();
            }

            // Security check - make sure the current user is either the retailer or wholesaler
            string currentUserId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (transaction.RetailerID != currentUserId && transaction.WholesalerID != currentUserId)
            {
                return Unauthorized();
            }

            return View(transaction);
        }

        // GET: Transaction/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            var transaction = db.Transactions
                .Include(t => t.Retailer)
                .Include(t => t.Wholesaler)
                .Include(t => t.TransactionDetails)
                .Include("TransactionDetails.Product")
                .FirstOrDefault(t => t.TransactionID == id);

            if (transaction == null)
            {
                return NotFound();
            }

            // Security check - make sure the current user is either the retailer or wholesaler
            string currentUserId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (transaction.RetailerID != currentUserId && transaction.WholesalerID != currentUserId)
            {
                return Unauthorized();
            }

            return View(transaction);
        }

        // GET: Transaction/UpdateStatus/5
        public ActionResult UpdateStatus(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            var transaction = db.Transactions
                .Include(t => t.Retailer)
                .Include(t => t.Wholesaler)
                .Include(t => t.TransactionDetails)
                .Include("TransactionDetails.Product")
                .FirstOrDefault(t => t.TransactionID == id);

            if (transaction == null)
            {
                return NotFound();
            }

            // Security check - make sure the current user is either the retailer or wholesaler
            string currentUserId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (transaction.RetailerID != currentUserId && transaction.WholesalerID != currentUserId)
            {
                return Unauthorized();
            }

            if (transaction.Status != TransactionStatus.Pending)
            {
                TempData["ErrorMessage"] = "This transaction cannot be updated because it is no longer in Pending status.";
                return RedirectToAction("Details", new { id = transaction.TransactionID });
            }

            return View(transaction);
        }

        // POST: Transaction/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateStatus(int id, TransactionStatus status, string paymentMethod, string notes)
        {
            var transaction = db.Transactions.Find(id);
            if (transaction == null)
            {
                return NotFound();
            }

            // Security check - make sure the current user is either the retailer or wholesaler
            string currentUserId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (transaction.RetailerID != currentUserId && transaction.WholesalerID != currentUserId)
            {
                return Unauthorized();
            }

            if (transaction.Status == TransactionStatus.Pending)
            {
                transaction.Status = status;
                transaction.PaymentMethod = paymentMethod;
                transaction.Notes = notes;
                db.SaveChanges();

                TempData["SuccessMessage"] = "Transaction status has been updated successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "This transaction cannot be updated because it is no longer in Pending status.";
            }

            return RedirectToAction("Details", new { id = transaction.TransactionID });
        }

        // GET: Transaction/Receipt/5
        public ActionResult Receipt(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            var transaction = db.Transactions
                .Include(t => t.Retailer)
                .Include(t => t.Wholesaler)
                .Include(t => t.TransactionDetails)
                .Include("TransactionDetails.Product")
                .FirstOrDefault(t => t.TransactionID == id);

            if (transaction == null)
            {
                return NotFound();
            }

            // Security check - make sure the current user is either the retailer or wholesaler
            string currentUserId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (transaction.RetailerID != currentUserId && transaction.WholesalerID != currentUserId)
            {
                return Unauthorized();
            }

            return View(transaction);
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
