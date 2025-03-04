using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using RetailerWholesalerSystem.Models;
using RetailerWholesalerSystem.ViewModels;
using System.Security.Claims;

namespace RetailerWholesalerSystem.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Dashboard
        public ActionResult Index()
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = db.Users.Find(userId);

            var dashboardViewModel = new DashboardViewModel
            {
                RecentTransactions = new List<Transaction>(),
                MonthlySummary = new Dictionary<string, decimal>()
            };

            // Get data from the last 6 months
            var startDate = DateTime.Now.AddMonths(-5).StartOfMonth();
            var endDate = DateTime.Now.EndOfMonth();

            // Generate monthly labels
            for (var date = startDate; date <= endDate; date = date.AddMonths(1))
            {
                dashboardViewModel.MonthlySummary[date.ToString("MMM yyyy")] = 0;
            }

            if (user.UserType == UserType.Retailer)
            {
                // For Retailer: Get all transactions where this user is the retailer
                var retailerTransactions = db.Transactions
                    .Where(t => t.RetailerID == userId)
                    .OrderByDescending(t => t.Date)
                    .ToList();

                dashboardViewModel.TotalSpent = retailerTransactions.Sum(t => t.TotalAmount);
                dashboardViewModel.OutstandingAmount = retailerTransactions
                    .Where(t => t.Status == TransactionStatus.Pending)
                    .Sum(t => t.TotalAmount);
                dashboardViewModel.PendingTransactions = retailerTransactions
                    .Count(t => t.Status == TransactionStatus.Pending);
                dashboardViewModel.RecentTransactions = retailerTransactions.Take(5).ToList();

                // Get monthly spending
                var monthlySummary = retailerTransactions
                    .Where(t => t.Date >= startDate && t.Date <= endDate)
                    .GroupBy(t => new { Month = t.Date.Month, Year = t.Date.Year })
                    .Select(g => new
                    {
                        Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                        Amount = g.Sum(t => t.TotalAmount)
                    })
                    .ToList();

                foreach (var summary in monthlySummary)
                {
                    dashboardViewModel.MonthlySummary[summary.Date.ToString("MMM yyyy")] = summary.Amount;
                }
            }
            else if (user.UserType == UserType.Wholesaler)
            {
                // For Wholesaler: Get all transactions where this user is the wholesaler
                var wholesalerTransactions = db.Transactions
                    .Where(t => t.WholesalerID == userId)
                    .OrderByDescending(t => t.Date)
                    .ToList();

                dashboardViewModel.TotalEarned = wholesalerTransactions.Sum(t => t.TotalAmount);
                dashboardViewModel.OutstandingAmount = wholesalerTransactions
                    .Where(t => t.Status == TransactionStatus.Pending)
                    .Sum(t => t.TotalAmount);
                dashboardViewModel.PendingTransactions = wholesalerTransactions
                    .Count(t => t.Status == TransactionStatus.Pending);
                dashboardViewModel.RecentTransactions = wholesalerTransactions.Take(5).ToList();

                // Get monthly earnings
                var monthlySummary = wholesalerTransactions
                    .Where(t => t.Date >= startDate && t.Date <= endDate)
                    .GroupBy(t => new { Month = t.Date.Month, Year = t.Date.Year })
                    .Select(g => new
                    {
                        Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                        Amount = g.Sum(t => t.TotalAmount)
                    })
                    .ToList();

                foreach (var summary in monthlySummary)
                {
                    dashboardViewModel.MonthlySummary[summary.Date.ToString("MMM yyyy")] = summary.Amount;
                }
            }

            return View(dashboardViewModel);
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

    // Extension method for DateTime to get start/end of month
    public static class DateTimeExtensions
    {
        public static DateTime StartOfMonth(this DateTime date)
        {
            return new DateTime(date.Year, date.Month, 1);
        }

        public static DateTime EndOfMonth(this DateTime date)
        {
            return new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
        }
    }
}