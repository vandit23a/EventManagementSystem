using EventManagementSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Admin
        public async Task<IActionResult> Index()
        {
            // Total number of events
            ViewBag.TotalEvents = await _context.Events.CountAsync();

            // Total number of registrations/participants
            ViewBag.TotalParticipants =
                await _context.EventRegistrations.CountAsync();

            // Event-wise participant count
            var eventParticipation = await _context.Events
                .Select(e => new
                {
                    EventId = e.EventId,
                    EventName = e.Name,
                    MaxParticipants = e.MaxParticipants,
                    Participants = e.Registrations.Count
                })
                .OrderBy(e => e.EventName)
                .ToListAsync();

            ViewBag.EventParticipation = eventParticipation;

            return View();
        }
    }
}