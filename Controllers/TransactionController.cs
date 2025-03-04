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
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = db.Users.Find(userId);

            IQueryable<Transaction> transactions;

            if (user.UserType == UserType.Retailer)
            {
                transactions = db.Transactions
                    .Include(t => t.Wholesaler)
                    .Where(t => t.RetailerID == userId)
                    .OrderByDescending(t => t.Date);
            }
            else
            {
                transactions = db.Transactions
                    .Include(t => t.Retailer)
                    .Where(t => t.WholesalerID == userId)
                    .OrderByDescending(t => t.Date);
            }

            return View(transactions.ToList());
        }

        // GET:


        // GET: Transaction/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = db.Users.Find(userId);

            Transaction transaction = db.Transactions
                .Include(t => t.Retailer)
                .Include(t => t.Wholesaler)
                .Include(t => t.TransactionDetails)
                .Include("TransactionDetails.Product")
                .FirstOrDefault(t => t.TransactionID == id &&
                    (t.RetailerID == userId || t.WholesalerID == userId));

            if (transaction == null)
            {
                return NotFound();
            }

            return View(transaction);
        }

        // GET: Transaction/Create
        [Authorize]
        public ActionResult Create(string wholesalerId)
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = db.Users.Find(userId);

            if (user.UserType != UserType.Retailer)
            {
                return BadRequest();
            }

            if (string.IsNullOrEmpty(wholesalerId))
            {
                // Show list of wholesalers to choose from
                var wholesalers = db.Users
                    .Where(u => u.UserType == UserType.Wholesaler)
                    .ToList();

                return View("SelectWholesaler", wholesalers);
            }

            var wholesaler = db.Users.Find(wholesalerId);
            if (wholesaler == null || wholesaler.UserType != UserType.Wholesaler)
            {
                return NotFound();
            }

            // Get products from this wholesaler
            var wholesalerProducts = db.WholesalerProducts
                .Include(wp => wp.Product)
                .Where(wp => wp.WholesalerID == wholesalerId && wp.AvailableQuantity > 0)
                .ToList();

            ViewBag.WholesalerProducts = wholesalerProducts;
            ViewBag.Wholesaler = wholesaler;

            var transaction = new Transaction
            {
                RetailerID = userId,
                WholesalerID = wholesalerId,
                Date = DateTime.Now,
                Status = TransactionStatus.Pending,
                TotalAmount = 0,
                TransactionDetails = new List<TransactionDetail>()
            };

            return View(transaction);
        }

        // POST: Transaction/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult Create(Transaction transaction, int[] productIds, int[] quantities)
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = db.Users.Find(userId);

            if (user.UserType != UserType.Retailer)
            {
                return BadRequest();
            }

            if (ModelState.IsValid && productIds != null && quantities != null && productIds.Length == quantities.Length)
            {
                transaction.RetailerID = userId;
                transaction.Date = DateTime.Now;
                transaction.Status = TransactionStatus.Pending;
                transaction.TransactionDetails = new List<TransactionDetail>();

                decimal totalAmount = 0;

                for (int i = 0; i < productIds.Length; i++)
                {
                    if (quantities[i] <= 0)
                        continue;

                    var wholesalerProduct = db.WholesalerProducts
                        .Include(wp => wp.Product)
                        .FirstOrDefault(wp => wp.ProductID == productIds[i] &&
                            wp.WholesalerID == transaction.WholesalerID);

                    if (wholesalerProduct == null || wholesalerProduct.AvailableQuantity < quantities[i])
                        continue;

                    var detail = new TransactionDetail
                    {
                        ProductID = productIds[i],
                        Quantity = quantities[i],
                        UnitPrice = wholesalerProduct.Price,
                        Subtotal = wholesalerProduct.Price * quantities[i]
                    };

                    totalAmount += detail.Subtotal;
                    transaction.TransactionDetails.Add(detail);

                    // Update available quantity
                    wholesalerProduct.AvailableQuantity -= quantities[i];
                    db.Entry(wholesalerProduct).State = EntityState.Modified;
                }

                transaction.TotalAmount = totalAmount;

                if (transaction.TransactionDetails.Count > 0)
                {
                    db.Transactions.Add(transaction);
                    db.SaveChanges();
                    return RedirectToAction("Details", new { id = transaction.TransactionID });
                }
                else
                {
                    ModelState.AddModelError("", "Please select at least one product with valid quantity.");
                }
            }

            // If we got this far, something failed, redisplay form
            var wholesalerProducts = db.WholesalerProducts
                .Include(wp => wp.Product)
                .Where(wp => wp.WholesalerID == transaction.WholesalerID && wp.AvailableQuantity > 0)
                .ToList();

            ViewBag.WholesalerProducts = wholesalerProducts;
            ViewBag.Wholesaler = db.Users.Find(transaction.WholesalerID);

            return View(transaction);
        }

        // GET: Transaction/UpdateStatus/5
        [Authorize]
        public ActionResult UpdateStatus(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            Transaction transaction = db.Transactions
                .Include(t => t.Retailer)
                .Include(t => t.Wholesaler)
                .FirstOrDefault(t => t.TransactionID == id &&
                    (t.RetailerID == userId || t.WholesalerID == userId));

            if (transaction == null)
            {
                return NotFound();
            }

            return View(transaction);
        }

        // POST: Transaction/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult UpdateStatus(int id, TransactionStatus status, string paymentMethod)
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            Transaction transaction = db.Transactions
                .FirstOrDefault(t => t.TransactionID == id &&
                    (t.RetailerID == userId || t.WholesalerID == userId));

            if (transaction == null)
            {
                return NotFound();
            }

            transaction.Status = status;

            if (!string.IsNullOrEmpty(paymentMethod))
            {
                transaction.PaymentMethod = paymentMethod;
            }

            db.Entry(transaction).State = EntityState.Modified;
            db.SaveChanges();

            return RedirectToAction("Details", new { id = transaction.TransactionID });
        }
    }
}
