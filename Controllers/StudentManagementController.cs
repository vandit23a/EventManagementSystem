using EventManagementSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class StudentManagementController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public StudentManagementController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // GET: /StudentManagement
        public async Task<IActionResult> Index()
        {
            var students = await _userManager
                .GetUsersInRoleAsync("Student");

            return View(students);
        }

        // POST: /StudentManagement/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var student = await _userManager.FindByIdAsync(id);

            if (student == null)
            {
                return NotFound();
            }

            // Make sure this account is actually a Student
            var isStudent = await _userManager
                .IsInRoleAsync(student, "Student");

            if (!isStudent)
            {
                TempData["ErrorMessage"] =
                    "Only Student accounts can be deleted.";

                return RedirectToAction(nameof(Index));
            }

            // Delete all event registrations of this student
            var registrations = await _context.EventRegistrations
                .Where(r => r.UserId == student.Id)
                .ToListAsync();

            if (registrations.Any())
            {
                _context.EventRegistrations.RemoveRange(registrations);

                await _context.SaveChangesAsync();
            }

            // Delete the Identity account
            var result = await _userManager.DeleteAsync(student);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] =
                    "Student account deleted successfully.";
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    TempData["ErrorMessage"] +=
                        error.Description + " ";
                }
            }

            return RedirectToAction(nameof(Index));
        }
    }
}