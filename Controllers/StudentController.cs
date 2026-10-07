using EventManagementSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventManagementSystem.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Student
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var today = DateTime.Today;

            // All events registered by the logged-in student
            var registeredEvents = await _context.EventRegistrations
                .Where(r => r.UserId == userId)
                .Include(r => r.Event)
                .OrderByDescending(r => r.RegistrationDate)
                .ToListAsync();

            // Upcoming events registered by the student
            var upcomingRegisteredEvents = registeredEvents
                .Where(r => r.Event.Date >= today)
                .OrderBy(r => r.Event.Date)
                .ThenBy(r => r.Event.Time)
                .ToList();

            // Upcoming events available in the system
            var upcomingEvents = await _context.Events
                .Where(e => e.Date >= today)
                .OrderBy(e => e.Date)
                .ThenBy(e => e.Time)
                .Take(5)
                .ToListAsync();

            ViewBag.TotalRegistrations = registeredEvents.Count;
            ViewBag.UpcomingRegistrations = upcomingRegisteredEvents.Count;
            ViewBag.UpcomingEvents = upcomingEvents;
            ViewBag.RegisteredEvents = upcomingRegisteredEvents;

            return View();
        }

        public async Task<IActionResult> ParticipatedEvents()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var participatedEvents = await _context.EventRegistrations
                .Where(r => r.UserId == userId)
                .Include(r => r.Event)
                .Where(r => r.Event.Date < DateTime.Today)
                .OrderByDescending(r => r.Event.Date)
                .ThenByDescending(r => r.Event.Time)
                .ToListAsync();

            return View(participatedEvents);
        }
    }
}