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
using Microsoft.Extensions.Logging;

namespace RetailerWholesalerSystem.Controllers
{
    [Authorize]
    public class TransactionController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly ILogger<TransactionController> _logger;

        public TransactionController(ApplicationDbContext context, ILogger<TransactionController> logger)
        {
            db = context;
            _logger = logger;
        }

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
                _logger.LogError($"Error in Create GET: {ex.Message}");
                ViewBag.ErrorMessage = "An error occurred while processing your request.";
                return View("Error");
            }
        }

        // POST: Transaction/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Transaction transaction, int[] productIds, int[] quantities)
        {
            try
            {
                _logger.LogInformation("Create POST started");
                _logger.LogInformation($"Transaction: {transaction?.TransactionID}");
                _logger.LogInformation($"ProductIds received: {(productIds != null ? string.Join(",", productIds) : "null")}");
                _logger.LogInformation($"Quantities received: {(quantities != null ? string.Join(",", quantities) : "null")}");

                // Check for required product data
                if (productIds == null || quantities == null || productIds.Length == 0 || quantities.Length == 0)
                {
                    _logger.LogWarning("Create POST: No products were selected");
                    ModelState.AddModelError("", "Please select at least one product.");
                    PrepareCreateViewData(transaction.WholesalerID);
                    return View(transaction);
                }

                // Clear validation for navigation properties - we'll set these manually
                ModelState.Remove("Retailer");
                ModelState.Remove("Wholesaler");
                ModelState.Remove("TransactionDetails");

                // If ModelState is still invalid, return to the view
                if (!ModelState.IsValid)
                {
                    // Log the remaining validation errors
                    foreach (var modelStateKey in ModelState.Keys)
                    {
                        var modelStateVal = ModelState[modelStateKey];
                        if (modelStateVal.Errors.Count > 0)
                        {
                            _logger.LogWarning($"Error for {modelStateKey}: {modelStateVal.Errors[0].ErrorMessage}");
                        }
                    }

                    PrepareCreateViewData(transaction.WholesalerID);
                    return View(transaction);
                }

                // Set up relationships manually
                transaction.Date = DateTime.Now;
                transaction.Status = TransactionStatus.Pending;
                transaction.PaymentMethod = transaction.PaymentMethod ?? "Pending";
                transaction.Notes = transaction.Notes ?? "Order placed online";

                // Fetch the actual entities from the database instead of trying to bind complex objects
                var retailer = db.Users.Find(transaction.RetailerID);
                var wholesaler = db.Users.Find(transaction.WholesalerID);

                if (retailer == null || wholesaler == null)
                {
                    ModelState.AddModelError("", "Invalid retailer or wholesaler information.");
                    PrepareCreateViewData(transaction.WholesalerID);
                    return View(transaction);
                }

                // Set navigation properties
                transaction.Retailer = retailer;
                transaction.Wholesaler = wholesaler;

                // Initialize TransactionDetails collection manually
                decimal totalAmount = 0;
                transaction.TransactionDetails = new List<TransactionDetail>();

                // Process selected products
                bool atLeastOneProductAdded = false;
                for (int i = 0; i < Math.Min(productIds.Length, quantities.Length); i++)
                {
                    if (quantities[i] <= 0) continue;

                    var wholesalerProduct = db.WholesalerProducts
                        .Include(wp => wp.Product)
                        .FirstOrDefault(wp => wp.ProductID == productIds[i] &&
                                              wp.WholesalerID == transaction.WholesalerID);

                    if (wholesalerProduct != null)
                    {
                        _logger.LogInformation($"Adding product {productIds[i]} with quantity {quantities[i]}");

                        // Check available quantity
                        if (quantities[i] > wholesalerProduct.AvailableQuantity)
                        {
                            _logger.LogWarning($"Requested quantity {quantities[i]} exceeds available {wholesalerProduct.AvailableQuantity} for product {productIds[i]}");
                            ModelState.AddModelError("", $"Product '{wholesalerProduct.Product.Name}' only has {wholesalerProduct.AvailableQuantity} available.");
                            continue;
                        }

                        decimal subtotal = wholesalerProduct.Price * quantities[i];

                        // Create transaction detail
                        var detail = new TransactionDetail
                        {
                            ProductID = productIds[i],
                            Quantity = quantities[i],
                            UnitPrice = wholesalerProduct.Price,
                            Product = wholesalerProduct.Product
                        };

                        transaction.TransactionDetails.Add(detail);
                        totalAmount += subtotal;

                        // Update available quantity
                        wholesalerProduct.AvailableQuantity -= quantities[i];

                        atLeastOneProductAdded = true;
                    }
                    else
                    {
                        _logger.LogWarning($"Product {productIds[i]} not found for wholesaler {transaction.WholesalerID}");
                    }
                }

                // If no products were successfully added
                if (!atLeastOneProductAdded)
                {
                    ModelState.AddModelError("", "No valid products were added to the order.");
                    _logger.LogWarning("Create POST: No valid products added");
                    PrepareCreateViewData(transaction.WholesalerID);
                    return View(transaction);
                }

                transaction.TotalAmount = totalAmount;

                try
                {
                    _logger.LogInformation("Adding transaction to database");
                    db.Transactions.Add(transaction);
                    db.SaveChanges();
                    _logger.LogInformation($"Transaction {transaction.TransactionID} created successfully");

                    TempData["SuccessMessage"] = "Order created successfully!";
                    return RedirectToAction(nameof(Confirmation), new { id = transaction.TransactionID });
                }
                catch (Exception dbEx)
                {
                    _logger.LogError($"Database error in Create POST: {dbEx.Message}");
                    if (dbEx.InnerException != null)
                    {
                        _logger.LogError($"Inner exception: {dbEx.InnerException.Message}");
                    }
                    ModelState.AddModelError("", "A database error occurred. Please try again.");
                    PrepareCreateViewData(transaction.WholesalerID);
                    return View(transaction);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Create POST: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                }
                _logger.LogError($"Stack trace: {ex.StackTrace}");

                ModelState.AddModelError("", "An error occurred while processing the transaction. Please try again.");
                PrepareCreateViewData(transaction.WholesalerID);
                return View(transaction);
            }
        }
        // Helper method to repopulate the Create view data
        private void PrepareCreateViewData(string wholesalerId)
        {
            var wholesaler = db.Users.Find(wholesalerId);
            var wholesalerProducts = db.WholesalerProducts
                .Include(wp => wp.Product)
                .Where(wp => wp.WholesalerID == wholesalerId && wp.AvailableQuantity > 0)
                .ToList();

            ViewBag.WholesalerProducts = wholesalerProducts;
            ViewBag.Wholesaler = wholesaler;
        }

        // GET: Transaction/Confirmation/5
        //public ActionResult Confirmation(int? id)
        //{
        //    if (id == null)
        //    {
        //        _logger.LogWarning("Confirmation: id is null");
        //        return BadRequest();
        //    }

        //    try
        //    {
        //        var transaction = db.Transactions
        //            .Include(t => t.Retailer)
        //            .Include(t => t.Wholesaler)
        //            .Include(t => t.TransactionDetails)
        //            .ThenInclude(td => td.Product)
        //            .FirstOrDefault(t => t.TransactionID == id);

        //        if (transaction == null)
        //        {
        //            _logger.LogWarning($"Confirmation: Transaction {id} not found");
        //            return NotFound();
        //        }

        //        // Security check - make sure the current user is either the retailer or wholesaler
        //        string currentUserId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        //        if (transaction.RetailerID != currentUserId && transaction.WholesalerID != currentUserId)
        //        {
        //            _logger.LogWarning($"Confirmation: Unauthorized access to transaction {id} by user {currentUserId}");
        //            return Unauthorized();
        //        }

        //        _logger.LogInformation($"Showing confirmation for transaction {id}");
        //        return View(transaction);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"Error in Confirmation: {ex.Message}");
        //        ViewBag.ErrorMessage = "An error occurred while retrieving the transaction details.";
        //        return View("Error");
        //    }
        //}

        // Other actions remain the same...

        // GET: Transaction/Details/5
        // GET: Transaction/Confirmation/5
        //public ActionResult Confirmation(string id)
        //{
        //    if (string.IsNullOrEmpty(id))
        //    {
        //        _logger.LogWarning("Confirmation: id is null or empty");
        //        return BadRequest();
        //    }

        //    try
        //    {
        //        // Parse the ID - assuming TransactionID is an int
        //        if (!int.TryParse(id, out int transactionId))
        //        {
        //            _logger.LogWarning($"Confirmation: Invalid transaction ID format: {id}");
        //            return BadRequest("Invalid transaction ID format");
        //        }

        //        var transaction = db.Transactions
        //            .Include(t => t.Retailer)
        //            .Include(t => t.Wholesaler)
        //            .Include(t => t.TransactionDetails)
        //                .ThenInclude(td => td.Product)
        //            .FirstOrDefault(t => t.TransactionID == transactionId);

        //        if (transaction == null)
        //        {
        //            _logger.LogWarning($"Confirmation: Transaction {id} not found");
        //            return NotFound();
        //        }

        //        // Security check - make sure the current user is either the retailer or wholesaler
        //        string currentUserId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        //        if (transaction.RetailerID != currentUserId && transaction.WholesalerID != currentUserId)
        //        {
        //            _logger.LogWarning($"Confirmation: Unauthorized access to transaction {id} by user {currentUserId}");
        //            return Unauthorized();
        //        }

        //        _logger.LogInformation($"Showing confirmation for transaction {id}");
        //        return View(transaction);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"Error in Confirmation: {ex.Message}");
        //        ViewBag.ErrorMessage = "An error occurred while retrieving the transaction details.";
        //        return View("Error");
        //    }
        //}

        public ActionResult Confirmation(int id)
        {
            if (id <= 0)
            {
                _logger.LogWarning("Confirmation: Invalid id");
                return BadRequest();
            }

            try
            {
                var transaction = db.Transactions
                    .Include(t => t.Retailer)
                    .Include(t => t.Wholesaler)
                    .Include(t => t.TransactionDetails)
                        .ThenInclude(td => td.Product)
                    .FirstOrDefault(t => t.TransactionID == id);

                if (transaction == null)
                {
                    _logger.LogWarning($"Confirmation: Transaction {id} not found");
                    return NotFound();
                }

                // Security check - make sure current user is either retailer or wholesaler
                string currentUserId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (transaction.RetailerID != currentUserId && transaction.WholesalerID != currentUserId)
                {
                    _logger.LogWarning($"Unauthorized access to transaction {id} by user {currentUserId}");
                    return Unauthorized();
                }

                _logger.LogInformation($"Showing confirmation for transaction {id}");
                return View(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Confirmation: {ex.Message}");
                ViewBag.ErrorMessage = "An error occurred while retrieving the transaction details.";
                return View("Error");
            }
        }

      
        // POST: Transaction/ConfirmOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmOrder(Transaction transaction, int[] productIds, int[] quantities)
        {
            try
            {
                _logger.LogInformation("ConfirmOrder started");

                // Validation - Check if any products were selected
                if (productIds == null || quantities == null || productIds.Length == 0)
                {
                    ModelState.AddModelError("", "Please select at least one product.");
                    PrepareCreateViewData(transaction.WholesalerID);
                    return View("Create", transaction);
                }

                // Validate quantities
                bool anyProductSelected = false;
                for (int i = 0; i < quantities.Length; i++)
                {
                    if (quantities[i] > 0)
                    {
                        anyProductSelected = true;
                        break;
                    }
                }

                if (!anyProductSelected)
                {
                    ModelState.AddModelError("", "Please select at least one product with quantity greater than 0.");
                    PrepareCreateViewData(transaction.WholesalerID);
                    return View("Create", transaction);
                }

                // Get the retailer ID from the current user
                string retailerId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(retailerId))
                {
                    _logger.LogWarning("ConfirmOrder: Unable to determine current user");
                    return RedirectToAction("SelectWholesaler");
                }

                // Get the wholesaler ID from the transaction object or form input
                string wholesalerId = transaction.WholesalerID;
                if (string.IsNullOrEmpty(wholesalerId))
                {
                    _logger.LogWarning("ConfirmOrder: Missing wholesaler ID");
                    return RedirectToAction("SelectWholesaler");
                }

                // Check if the form was submitted with a "confirm" parameter
                // This indicates that the user has confirmed the order and we should process it
                bool isConfirmed = Request.Form.ContainsKey("confirm") && Request.Form["confirm"] == "true";

                // If not confirmed, just show the confirmation view
                if (!isConfirmed)
                {
                    // Fetch retailer and wholesaler entities
                    var retailer = db.Users.Find(retailerId);
                    var wholesaler = db.Users.Find(wholesalerId);

                    if (retailer == null || wholesaler == null)
                    {
                        ModelState.AddModelError("", "Invalid retailer or wholesaler information.");
                        PrepareCreateViewData(transaction.WholesalerID);
                        return View("Create", transaction);
                    }

                    // Set up the transaction for preview
                    transaction.Date = DateTime.Now;
                    transaction.Status = TransactionStatus.Pending;
                    transaction.PaymentMethod = transaction.PaymentMethod ?? "Pending";
                    transaction.Notes = transaction.Notes ?? "Order placed online";
                    transaction.Retailer = retailer;
                    transaction.Wholesaler = wholesaler;

                    // Calculate details for preview
                    decimal totalAmount = 0;
                    var detailsList = new List<TransactionDetail>();

                    for (int i = 0; i < Math.Min(productIds.Length, quantities.Length); i++)
                    {
                        if (quantities[i] <= 0) continue;

                        var wholesalerProduct = db.WholesalerProducts
                            .Include(wp => wp.Product)
                            .FirstOrDefault(wp => wp.ProductID == productIds[i] &&
                                                wp.WholesalerID == transaction.WholesalerID);

                        if (wholesalerProduct != null)
                        {
                            // Check available quantity
                            if (quantities[i] > wholesalerProduct.AvailableQuantity)
                            {
                                ModelState.AddModelError("", $"Product '{wholesalerProduct.Product.Name}' only has {wholesalerProduct.AvailableQuantity} available.");
                                PrepareCreateViewData(transaction.WholesalerID);
                                return View("Create", transaction);
                            }

                            decimal subtotal = wholesalerProduct.Price * quantities[i];

                            var detail = new TransactionDetail
                            {
                                ProductID = productIds[i],
                                Quantity = quantities[i],
                                UnitPrice = wholesalerProduct.Price,
                                Product = wholesalerProduct.Product,
                                Subtotal = subtotal // Make sure this property exists
                            };

                            detailsList.Add(detail);
                            totalAmount += subtotal;
                        }
                    }

                    if (detailsList.Count == 0)
                    {
                        ModelState.AddModelError("", "No valid products were added to the order.");
                        PrepareCreateViewData(transaction.WholesalerID);
                        return View("Create", transaction);
                    }

                    transaction.TransactionDetails = detailsList;
                    transaction.TotalAmount = totalAmount;

                    // Store product IDs and quantities in ViewBag
                    ViewBag.ProductIds = productIds;
                    ViewBag.Quantities = quantities;

                    return View(transaction);
                }
                else
                {
                    // Process the order (this is executed when the user clicks "Confirm Order")

                    // Create a new transaction with the essential data
                    var newTransaction = new Transaction
                    {
                        WholesalerID = wholesalerId,
                        RetailerID = retailerId,
                        Date = DateTime.Now,
                        Status = TransactionStatus.Pending,
                        PaymentMethod = transaction.PaymentMethod ?? "Pending",
                        Notes = transaction.Notes ?? "Order placed online",
                        TransactionDetails = new List<TransactionDetail>()
                    };

                    // Calculate details and add to transaction
                    decimal totalAmount = 0;
                    bool atLeastOneProductAdded = false;

                    for (int i = 0; i < Math.Min(productIds.Length, quantities.Length); i++)
                    {
                        if (quantities[i] <= 0) continue;

                        var wholesalerProduct = db.WholesalerProducts
                            .Include(wp => wp.Product)
                            .FirstOrDefault(wp => wp.ProductID == productIds[i] && wp.WholesalerID == wholesalerId);

                        if (wholesalerProduct != null && wholesalerProduct.AvailableQuantity >= quantities[i])
                        {
                            decimal subtotal = wholesalerProduct.Price * quantities[i];

                            var detail = new TransactionDetail
                            {
                                ProductID = productIds[i],
                                Quantity = quantities[i],
                                UnitPrice = wholesalerProduct.Price,
                                Product = wholesalerProduct.Product,
                                Subtotal = subtotal // Ensure this property exists
                            };

                            newTransaction.TransactionDetails.Add(detail);
                            totalAmount += subtotal;

                            // Update available quantity
                            wholesalerProduct.AvailableQuantity -= quantities[i];
                            db.Entry(wholesalerProduct).State = EntityState.Modified;

                            atLeastOneProductAdded = true;
                        }
                    }

                    if (!atLeastOneProductAdded)
                    {
                        _logger.LogWarning("ProcessOrder: No valid products added");
                        TempData["ErrorMessage"] = "No valid products were added to the order.";
                        return RedirectToAction("Create", new { id = wholesalerId });
                    }

                    newTransaction.TotalAmount = totalAmount;

                    try
                    {
                        _logger.LogInformation("Adding transaction to database");
                        db.Transactions.Add(newTransaction);
                        db.SaveChanges();

                        int transactionId = newTransaction.TransactionID;
                        _logger.LogInformation($"Transaction {transactionId} created successfully");

                        TempData["SuccessMessage"] = "Order created successfully!";
                        return RedirectToAction("Confirmation", new { id = transactionId });
                    }
                    catch (Exception dbEx)
                    {
                        _logger.LogError($"Database error in ProcessOrder: {dbEx.Message}");
                        TempData["ErrorMessage"] = "A database error occurred. Please try again.";
                        return RedirectToAction("Create", new { id = wholesalerId });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in ConfirmOrder: {ex.Message}");
                ModelState.AddModelError("", "An error occurred while processing your order. Please try again.");
                PrepareCreateViewData(transaction.WholesalerID);
                return View("Create", transaction);
            }
        }
      
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
                .ThenInclude(td => td.Product)
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
                .ThenInclude(td => td.Product)
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
                transaction.PaymentMethod = paymentMethod ?? transaction.PaymentMethod; // Keep existing if null
                transaction.Notes = notes ?? string.Empty; // Never allow null notes
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
                .ThenInclude(td => td.Product)
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


        // GET: Transaction/PrintReceipt/5
        public ActionResult PrintReceipt(string id)
        {
            try
            {
                _logger.LogInformation($"PrintReceipt started for transaction ID: {id}");

                // If TransactionID in the database is an int
                if (!int.TryParse(id, out int transactionIdInt))
                {
                    _logger.LogWarning($"PrintReceipt: Invalid transaction ID format: {id}");
                    return BadRequest("Invalid transaction ID format");
                }

                var transaction = db.Transactions
                    .Include(t => t.Retailer)
                    .Include(t => t.Wholesaler)
                    .Include(t => t.TransactionDetails)
                        .ThenInclude(td => td.Product)
                    .FirstOrDefault(t => t.TransactionID == transactionIdInt);

                if (transaction == null)
                {
                    _logger.LogWarning($"PrintReceipt: Transaction {id} not found");
                    return NotFound();
                }

                // Check if current user has access to this receipt
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (transaction.RetailerID != currentUserId && transaction.WholesalerID != currentUserId && !User.IsInRole("Admin"))
                {
                    _logger.LogWarning($"PrintReceipt: Unauthorized access attempt to transaction {id} by user {currentUserId}");
                    return Forbid();
                }

                _logger.LogInformation($"PrintReceipt: Rendering receipt for transaction {id}");
                return View(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in PrintReceipt: {ex.Message}");
                return RedirectToAction(nameof(Index));
            }
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