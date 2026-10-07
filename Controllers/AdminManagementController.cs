using EventManagementSystem.Models;
using EventManagementSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EventManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminManagementController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminManagementController(
            UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // GET: /AdminManagement
        public async Task<IActionResult> Index()
        {
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");

            return View(adminUsers);
        }

        // GET: /AdminManagement/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: /AdminManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(CreateAdminViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var admin = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber
            };

            var result = await _userManager.CreateAsync(admin, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(admin, "Admin");

                TempData["AdminCreatedMessage"] =
                            "New Admin account created successfully.";

                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }
    }
}