using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using RetailerWholesalerSystem.Models;
using RetailerWholesalerSystem.ViewModels;
using Microsoft.Extensions.Logging;
using RetailerWholesalerSystem;

namespace RetailerWholesalerSystem.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        // GET: /Account/Login
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Added debugging
            _logger.LogInformation($"Login attempt for {model.Email}");
            Console.WriteLine($"Login attempt for {model.Email}");

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in.");
                Console.WriteLine($"Login successful for {model.Email}");
                return RedirectToLocal(returnUrl);
            }
            if (result.RequiresTwoFactor)
            {
                return RedirectToAction(nameof(LoginWith2fa), new { returnUrl, model.RememberMe });
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out.");
                Console.WriteLine($"Account locked out: {model.Email}");
                return RedirectToAction(nameof(Lockout));
            }
            else
            {
                // Added debugging
                _logger.LogWarning($"Invalid login attempt for {model.Email}");
                Console.WriteLine($"Login failed for {model.Email}");

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }
        }

        // GET: /Account/LoginWith2fa
        [AllowAnonymous]
        public async Task<IActionResult> LoginWith2fa(bool rememberMe, string returnUrl = null)
        {
            // Ensure the user has gone through the username & password screen first
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();

            if (user == null)
            {
                throw new InvalidOperationException("Unable to load two-factor authentication user.");
            }

            var model = new LoginViewModel { RememberMe = rememberMe };
            ViewData["ReturnUrl"] = returnUrl;

            return View(model);
        }

        // GET: /Account/Lockout
        [AllowAnonymous]
        public IActionResult Lockout()
        {
            return View();
        }

        // GET: /Account/Register
        [AllowAnonymous]
        public IActionResult Register(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            // Added debugging
            _logger.LogInformation("Register action hit");
            Console.WriteLine($"Register action hit for {model.Email}, role: {model.Role}");
            Console.WriteLine($"BusinessName: {model.BusinessName}, Address: {model.Address}, ContactInfo: {model.ContactInfo}");

            if (ModelState.IsValid)
            {
                _logger.LogInformation("Model state is valid");
                Console.WriteLine("Model state is valid");

                // Determine UserType based on the Role selection
                UserType userType;

                // Check what the user selected in the Role dropdown
                if (model.Role.Equals("Retailer", StringComparison.OrdinalIgnoreCase))
                {
                    userType = UserType.Retailer;
                }
                else if (model.Role.Equals("Wholesaler", StringComparison.OrdinalIgnoreCase))
                {
                    userType = UserType.Wholesaler;
                }
                else
                {
                    // Default or handle invalid selection
                    ModelState.AddModelError(string.Empty, "Invalid role selected.");
                    _logger.LogWarning("Invalid role selected: {Role}", model.Role);
                    Console.WriteLine($"Invalid role selected: {model.Role}");
                    return View(model);
                }

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    BusinessName = model.BusinessName,
                    Address = model.Address,
                    ContactInfo = model.ContactInfo,
                    UserType = userType // Using the enum value we determined above
                };

                Console.WriteLine($"Created user object with BusinessName: {user.BusinessName}");

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // Also assign the user to the appropriate role in the Identity system
                    var roleResult = await _userManager.AddToRoleAsync(user, model.Role);

                    if (!roleResult.Succeeded)
                    {
                        Console.WriteLine($"Failed to add user to role {model.Role}");
                        foreach (var error in roleResult.Errors)
                        {
                            Console.WriteLine($"Role Error: {error.Description}");
                            ModelState.AddModelError(string.Empty, $"Role Error: {error.Description}");
                        }

                        // Delete the user since we couldn't assign the role
                        await _userManager.DeleteAsync(user);
                        return View(model);
                    }

                    _logger.LogInformation("User created a new account with password.");
                    Console.WriteLine($"User {model.Email} created successfully and added to role {model.Role}");

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToLocal(returnUrl);
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                    _logger.LogError("Error creating user: {ErrorDescription}", error.Description);
                    Console.WriteLine($"Error creating user: {error.Description}");
                }
            }
            else
            {
                _logger.LogWarning("Model state is invalid");
                Console.WriteLine("Model state is invalid. Errors:");

                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        Console.WriteLine($"- {error.ErrorMessage}");
                    }
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }
    }
}