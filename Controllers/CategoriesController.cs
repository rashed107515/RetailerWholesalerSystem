using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RetailerWholesalerSystem.Models;
using System.Linq;
using System.Threading.Tasks;

namespace RetailerWholesalerSystem.Controllers
{
    [Authorize]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public CategoriesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _db = context;
            _userManager = userManager;
        }

        // Add this action method to your CategoriesController.cs file

        public async Task<IActionResult> ManageCategories()
        {
            // Fetch all categories
            var categories = await _db.Categories.ToListAsync();
            return View(categories);
        }

        // Add these two action methods to handle the AJAX calls from your view

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory([Bind("Name,Description")] Category category)
        {
            if (ModelState.IsValid)
            {
                string userId = _userManager.GetUserId(User);
                category.CreatedByUserID = userId;

                // Only admins can make categories global
                if (!User.IsInRole("Admin"))
                {
                    category.IsGlobal = false;
                }

                _db.Categories.Add(category);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(ManageCategories));
            }

            // If we got this far, something failed; redisplay form
            return RedirectToAction(nameof(ManageCategories));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(int CategoryID, string Name, string Description)
        {
            var category = await _db.Categories.FindAsync(CategoryID);

            if (category == null)
            {
                return NotFound();
            }

            string userId = _userManager.GetUserId(User);

            // Users can only edit their own categories or admins can edit any
            if (category.CreatedByUserID != userId && !User.IsInRole("Admin"))
            {
                return Unauthorized();
            }

            // Update the category
            category.Name = Name;
            category.Description = Description;

            _db.Update(category);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(ManageCategories));
        }

        // GET: Categories
        public async Task<IActionResult> Index()
        {
            string userId = _userManager.GetUserId(User);
            var user = await _db.Users.FindAsync(userId);

            // Get global categories and user-specific categories
            var categories = await _db.Categories
                .Where(c => c.IsGlobal || c.CreatedByUserID == userId)
                .ToListAsync();

            return View(categories);
        }

        // GET: Categories/Create
        public IActionResult Create()
        {
            ViewBag.Categories = new SelectList(_db.Categories, "CategoryID", "Name");
            return View();
        }

        // POST: Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,IsGlobal")] Category category)
        {
            string userId = _userManager.GetUserId(User);
            category.CreatedByUserID = userId;

            // Debug: Check if ModelState is valid and log errors
            if (!ModelState.IsValid)
            {
                // Log validation errors
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        // You can use your logging system here, or temporarily use Debug.WriteLine
                        System.Diagnostics.Debug.WriteLine($"Validation error: {error.ErrorMessage}");
                    }
                }
                return View(category);
            }

            try
            {

                // Only admins can create global categories
                if (!User.IsInRole("Admin"))
                {
                    category.IsGlobal = false;
                }

                // Debug: Print category details before saving
                System.Diagnostics.Debug.WriteLine($"Adding category: {category.Name}, Description: {category.Description}, IsGlobal: {category.IsGlobal}, UserId: {userId}");

                _db.Categories.Add(category);
                await _db.SaveChangesAsync();

                // Debug: Confirm the save was successful
                System.Diagnostics.Debug.WriteLine("Category successfully saved to database");

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Log the exception
                System.Diagnostics.Debug.WriteLine($"Error creating category: {ex.Message}");
                ModelState.AddModelError(string.Empty, "An error occurred while saving the category. Please try again.");
                return View(category);
            }
        }

        // GET: Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            string userId = _userManager.GetUserId(User);
            var category = await _db.Categories.FindAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            // Users can only edit their own categories or admins can edit any
            if (category.CreatedByUserID != userId && !User.IsInRole("Admin"))
            {
                return Unauthorized();
            }

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CategoryID,Name,Description,IsGlobal,CreatedByUserID")] Category category)
        {
            System.Diagnostics.Debug.WriteLine($"Edit action called with ID: {id}, Name: {category.Name}");

            if (id != category.CategoryID)
            {
                System.Diagnostics.Debug.WriteLine("IDs don't match");
                return BadRequest();
            }

            string userId = _userManager.GetUserId(User);
            var existingCategory = await _db.Categories.FindAsync(id);

            if (existingCategory == null)
            {
                System.Diagnostics.Debug.WriteLine("Category not found");
                return NotFound();
            }

            System.Diagnostics.Debug.WriteLine($"Found existing category: {existingCategory.Name}");

            // Users can only edit their own categories or admins can edit any
            if (existingCategory.CreatedByUserID != userId && !User.IsInRole("Admin"))
            {
                System.Diagnostics.Debug.WriteLine("Unauthorized");
                return Unauthorized();
            }

            System.Diagnostics.Debug.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");

            if (ModelState.IsValid)
            {
                try
                {
                    // Only update certain fields and preserve the original CreatedByUserID
                    existingCategory.Name = category.Name;
                    existingCategory.Description = category.Description;

                    // Only admins can make a category global
                    if (User.IsInRole("Admin"))
                    {
                        existingCategory.IsGlobal = category.IsGlobal;
                    }

                    System.Diagnostics.Debug.WriteLine("Updating category");
                    _db.Update(existingCategory);
                    await _db.SaveChangesAsync();
                    System.Diagnostics.Debug.WriteLine("Category updated successfully");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error updating category: {ex.Message}");
                    if (ex is DbUpdateConcurrencyException && !CategoryExists(category.CategoryID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            System.Diagnostics.Debug.WriteLine("ModelState is invalid");
            foreach (var modelState in ModelState.Values)
            {
                foreach (var error in modelState.Errors)
                {
                    System.Diagnostics.Debug.WriteLine($"Validation error: {error.ErrorMessage}");
                }
            }

            return View(category);
        }

        // GET: Categories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            string userId = _userManager.GetUserId(User);
            var category = await _db.Categories.FindAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            // Users can only delete their own categories or admins can delete any
            if (category.CreatedByUserID != userId && !User.IsInRole("Admin"))
            {
                return Unauthorized();
            }

            return View(category);
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            string userId = _userManager.GetUserId(User);
            var category = await _db.Categories.FindAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            // Users can only delete their own categories or admins can delete any
            if (category.CreatedByUserID != userId && !User.IsInRole("Admin"))
            {
                return Unauthorized();
            }

            // Check if category is in use
            var productsUsingCategory = await _db.Products.AnyAsync(p => p.CategoryID == id);
            if (productsUsingCategory)
            {
                ModelState.AddModelError(string.Empty, "Cannot delete category because it is used by one or more products.");
                return View(category);
            }

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CategoryExists(int id)
        {
            return _db.Categories.Any(e => e.CategoryID == id);
        }
    }
}