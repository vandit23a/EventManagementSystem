using EventManagementSystem.Data;
using EventManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventManagementSystem.Controllers
{
    [Authorize]
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EventsController(
          ApplicationDbContext context,
          UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Events
        public async Task<IActionResult> Index(
    string? searchName,
    string? category,
    DateTime? date,
    bool upcomingOnly = false)

        {            // Automatically update past events to Completed
            var currentDateTime = DateTime.Now;

            var pastEvents = await _context.Events
                .Where(e => e.Status == "Upcoming")
                .ToListAsync();

            foreach (var eventItem in pastEvents)
            {
                var eventDateTime = eventItem.Date.Date.Add(eventItem.Time);

                if (eventDateTime <= currentDateTime)
                {
                    eventItem.Status = "Completed";
                }
            }

            if (pastEvents.Any(e => e.Status == "Completed"))
            {
                await _context.SaveChangesAsync();
            }

            {
                var query = _context.Events.AsQueryable();

                // Search by event name
                if (!string.IsNullOrWhiteSpace(searchName))
                {
                    query = query.Where(e =>
                        e.Name.Contains(searchName));
                }

                // Filter by category
                if (!string.IsNullOrWhiteSpace(category))
                {
                    query = query.Where(e =>
                        e.Category == category);
                }

                // Filter by date
                if (date.HasValue)
                {
                    query = query.Where(e =>
                        e.Date.Date == date.Value.Date);
                }

                // Show only upcoming events
                if (upcomingOnly)
                {
                    query = query.Where(e =>
                        e.Status == "Upcoming" &&
                        e.Date >= DateTime.Today);
                }

                var events = await query
                    .OrderBy(e => e.Date)
                    .ThenBy(e => e.Time)
                    .ToListAsync();

                ViewBag.SearchName = searchName;
                ViewBag.Category = category;
                ViewBag.Date = date?.ToString("yyyy-MM-dd");
                ViewBag.UpcomingOnly = upcomingOnly;

                ViewBag.Categories = await _context.Events
                    .Select(e => e.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                return View(events);
            }
        }
        // GET: Events/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _context.Events
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (@event == null)
            {
                return NotFound();
            }

            // Automatically mark event as Completed when its date and time have passed
            if (@event.Status == "Upcoming")
            {
                var eventDateTime = @event.Date.Date.Add(@event.Time);

                if (eventDateTime <= DateTime.Now)
                {
                    @event.Status = "Completed";
                    await _context.SaveChangesAsync();
                }
            }

            ViewBag.Participants = @event.Registrations
            .OrderBy(r => r.StudentName)
            .ToList();

            ViewBag.ParticipatedSeats = @event.Registrations.Count;

            ViewBag.VacantSeats =
                Math.Max(0, @event.MaxParticipants - @event.Registrations.Count);

            // Check whether the logged-in student is already registered
            ViewBag.IsRegistered = false;

            if (User.IsInRole("Student"))
            {
                var userId = _userManager.GetUserId(User);

                if (userId != null)
                {
                    ViewBag.IsRegistered = await _context.EventRegistrations
                        .AnyAsync(r =>
                            r.EventId == id &&
                            r.UserId == userId);
                }
            }

            return View(@event);
        }

        // GET: Events/Participants/5
        [Authorize(Roles = "Student,Admin")]
        public async Task<IActionResult> Participants(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var eventItem = await _context.Events
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (eventItem == null)
            {
                return NotFound();
            }

            var participantUserIds = eventItem.Registrations
                .Select(r => r.UserId)
                .ToList();

            var users = await _userManager.Users
                .Where(u => participantUserIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Name);

            foreach (var registration in eventItem.Registrations)
            {
                if (users.TryGetValue(registration.UserId, out var studentName))
                {
                    registration.StudentName = studentName;
                }
            }

            var participants = eventItem.Registrations
                .OrderBy(r => r.StudentName)
                .ToList();

            ViewBag.EventId = eventItem.EventId;
            ViewBag.EventName = eventItem.Name;
            ViewBag.ParticipantCount = participants.Count;

            return View(participants);
        }


        // GET: Events/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Events/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Event eventItem)
        {
            if (ModelState.IsValid)
            {
                // Every newly created event starts as Upcoming
                eventItem.Status = "Upcoming";

                _context.Events.Add(eventItem);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(eventItem);
        }

        // GET: Events/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var eventItem = await _context.Events.FindAsync(id);

            if (eventItem == null)
            {
                return NotFound();
            }

            return View(eventItem);
        }

        // POST: Events/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Event eventItem)
        {
            if (id != eventItem.EventId)
            {
                return NotFound();
            }

            var existingEvent = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (existingEvent == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Completed events cannot be changed back to Upcoming/Cancelled
                if (existingEvent.Status == "Completed")
                {
                    eventItem.Status = "Completed";
                }
                else
                {
                    // Admin can keep Upcoming or change it to Cancelled
                    if (eventItem.Status != "Cancelled")
                    {
                        eventItem.Status = "Upcoming";
                    }
                }

                existingEvent.Name = eventItem.Name;
                existingEvent.Description = eventItem.Description;
                existingEvent.Category = eventItem.Category;
                existingEvent.Date = eventItem.Date;
                existingEvent.Time = eventItem.Time;
                existingEvent.Venue = eventItem.Venue;
                existingEvent.MaxParticipants = eventItem.MaxParticipants;
                existingEvent.Status = eventItem.Status;

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(eventItem);
        }

        // GET: Events/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (eventItem == null)
            {
                return NotFound();
            }

            return View(eventItem);
        }

        // POST: Events/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var eventItem = await _context.Events.FindAsync(id);

            if (eventItem != null)
            {
                _context.Events.Remove(eventItem);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Events/Register/5
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Register(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (eventItem == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            // Check if student is already registered
            var alreadyRegistered = await _context.EventRegistrations
                .AnyAsync(r => r.EventId == id && r.UserId == userId);

            if (alreadyRegistered)
            {
                TempData["ErrorMessage"] = "You are already registered for this event.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Check event capacity
            var participantCount = await _context.EventRegistrations
                .CountAsync(r => r.EventId == id);

            if (participantCount >= eventItem.MaxParticipants)
            {
                TempData["ErrorMessage"] = "This event is already full.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Check event status
            if (eventItem.Status == "Completed" ||
                eventItem.Status == "Cancelled")
            {
                TempData["ErrorMessage"] =
                    "Registration is not available for this event.";

                return RedirectToAction(nameof(Details), new { id });
            }

            var registration = new EventRegistration
            {
                EventId = eventItem.EventId,
                UserId = userId,
                StudentName = User.Identity?.Name ?? "Unknown",
                RegistrationDate = DateTime.Now
            };

            _context.EventRegistrations.Add(registration);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "You have successfully registered for the event.";

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Events/CancelRegistration/5
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> CancelRegistration(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var registration = await _context.EventRegistrations
                .FirstOrDefaultAsync(r =>
                    r.EventId == id &&
                    r.UserId == userId);

            if (registration == null)
            {
                TempData["ErrorMessage"] =
                    "You are not registered for this event.";

                return RedirectToAction(nameof(Details), new { id });
            }

            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (eventItem == null)
            {
                return NotFound();
            }

            // Prevent cancellation for completed or cancelled events
            if (eventItem.Status == "Completed" ||
                eventItem.Status == "Cancelled")
            {
                TempData["ErrorMessage"] =
                    "Registration cannot be cancelled for this event.";

                return RedirectToAction(nameof(Details), new { id });
            }

            _context.EventRegistrations.Remove(registration);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Your event registration has been cancelled successfully.";

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Events/Upcoming
        [Authorize]
        public async Task<IActionResult> Upcoming()
        {
            var today = DateTime.Today;

            var upcomingEvents = await _context.Events
                .Where(e => e.Date >= today)
                .OrderBy(e => e.Date)
                .ThenBy(e => e.Time)
                .ToListAsync();

            return View(upcomingEvents);
        }

    }
}