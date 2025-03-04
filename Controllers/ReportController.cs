using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailerWholesalerSystem.Models;
using RetailerWholesalerSystem.ViewModels;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
namespace RetailerWholesalerSystem.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Report
        public ActionResult Index()
        {
            return View();
        }

        // GET: Report/TransactionHistory
        public ActionResult TransactionHistory(DateTime? startDate, DateTime? endDate)
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = db.Users.Find(userId);

            if (startDate == null)
                startDate = DateTime.Now.AddMonths(-3);

            if (endDate == null)
                endDate = DateTime.Now;

            IQueryable<Transaction> transactions;

            if (user.UserType == UserType.Retailer)
            {
                transactions = db.Transactions
                    .Include("Wholesaler")
                    .Include("TransactionDetails")
                    .Include("TransactionDetails.Product")
                    .Where(t => t.RetailerID == userId &&
                        t.Date >= startDate && t.Date <= endDate)
                    .OrderByDescending(t => t.Date);
            }
            else
            {
                transactions = db.Transactions
                    .Include("Retailer")
                    .Include("TransactionDetails")
                    .Include("TransactionDetails.Product")
                    .Where(t => t.WholesalerID == userId &&
                        t.Date >= startDate && t.Date <= endDate)
                    .OrderByDescending(t => t.Date);
            }

            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.UserType = user.UserType;

            return View(transactions.ToList());
        }

        // GET: Report/ProductSales
        [Authorize(Roles = "Wholesaler")]
        public ActionResult ProductSales(DateTime? startDate, DateTime? endDate)
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (startDate == null)
                startDate = DateTime.Now.AddMonths(-3);

            if (endDate == null)
                endDate = DateTime.Now;

            var transactions = db.Transactions
                .Include("TransactionDetails")
                .Include("TransactionDetails.Product")
                .Where(t => t.WholesalerID == userId &&
                    t.Date >= startDate && t.Date <= endDate)
                .ToList();

            var productSales = transactions
                .SelectMany(t => t.TransactionDetails)
                .GroupBy(td => td.ProductID)
                .Select(g => new ProductSalesViewModel
                {
                    Product = db.Products.Find(g.Key),
                    TotalQuantity = g.Sum(td => td.Quantity),
                    TotalAmount = g.Sum(td => td.Subtotal)
                })
                .OrderByDescending(ps => ps.TotalAmount)
                .ToList();

            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;

            return View(productSales);
        }

        // GET: Report/CustomerSales
        [Authorize(Roles = "Wholesaler")]
        public ActionResult CustomerSales(DateTime? startDate, DateTime? endDate)
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (startDate == null)
                startDate = DateTime.Now.AddMonths(-3);

            if (endDate == null)
                endDate = DateTime.Now;

            var transactions = db.Transactions
                .Include("Retailer")
                .Where(t => t.WholesalerID == userId &&
                    t.Date >= startDate && t.Date <= endDate)
                .ToList();

            var customerSales = transactions
                .GroupBy(t => t.RetailerID)
                .Select(g => new CustomerSalesViewModel
                {
                    Customer = db.Users.Find(g.Key),
                    TransactionCount = g.Count(),
                    TotalAmount = g.Sum(t => t.TotalAmount)
                })
                .OrderByDescending(cs => cs.TotalAmount)
                .ToList();

            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;

            return View(customerSales);
        }
    }
}
