using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using HospitalFlow.Data;
using HospitalFlow.Hubs;
using HospitalFlow.Models;
using Microsoft.AspNetCore.Authorization;

namespace HospitalFlow.Controllers
{
    [Authorize]
    public class RoomsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<HospitalHub> _hubContext;

        public RoomsController(ApplicationDbContext context, IHubContext<HospitalHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // GET: Rooms
        public async Task<IActionResult> Index()
        {
            var rooms = await _context.Rooms
                .Include(r => r.Equipments)
                .Include(r => r.Patients)
                .ToListAsync();

            // Fetch unassigned patients for the admission dropdown
            ViewBag.UnassignedPatients = await _context.Patients
                .Where(p => p.RoomId == null)
                .OrderBy(p => p.FullName)
                .ToListAsync();

            return View(rooms);
        }

        // POST: Rooms/MarkSanitized/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkSanitized(int roomId)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null) return NotFound();

            room.Status = RoomStatus.Available;
            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("RefreshData");

            return RedirectToAction(nameof(Index));
        }

        // POST: Rooms/DischargePatient/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DischargePatient(int patientId)
        {
            var patient = await _context.Patients
                .Include(p => p.Room)
                .FirstOrDefaultAsync(p => p.Id == patientId);

            if (patient == null) return NotFound();

            if (patient.Room != null)
            {
                // Unassign patient and mark the room for cleaning
                patient.Room.Status = RoomStatus.CleaningRequired;
                patient.RoomId = null;
            }

            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("RefreshData");

            return RedirectToAction(nameof(Index));
        }

        // POST: Rooms/AssignPatient
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignPatient(int roomId, int patientId)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            var patient = await _context.Patients.FindAsync(patientId);

            if (room == null || patient == null)
            {
                return NotFound();
            }

            // Room must not be in cleaning or maintenance
            if (room.Status == RoomStatus.CleaningRequired || room.Status == RoomStatus.Maintenance)
            {
                TempData["Error"] = "Cannot admit patient to a room requiring cleaning or maintenance.";
                return RedirectToAction(nameof(Index));
            }

            patient.RoomId = roomId;
            room.Status = RoomStatus.Occupied;

            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("RefreshData");

            return RedirectToAction(nameof(Index));
        }

        // GET: Rooms/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var room = await _context.Rooms
                .Include(r => r.Equipments)
                .Include(r => r.Patients)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (room == null) return NotFound();

            return View(room);
        }

        // GET: Rooms/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Rooms/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Department,MaxCapacity")] Room room)
        {
            if (ModelState.IsValid)
            {
                _context.Add(room);
                await _context.SaveChangesAsync();

                await _hubContext.Clients.All.SendAsync("RefreshData");

                return RedirectToAction(nameof(Index));
            }
            return View(room);
        }

        // GET: Rooms/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();

            return View(room);
        }

        // POST: Rooms/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Department,MaxCapacity,Status")] Room room)
        {
            if (id != room.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(room);
                    await _context.SaveChangesAsync();

                    await _hubContext.Clients.All.SendAsync("RefreshData");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RoomExists(room.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(room);
        }

        // GET: Rooms/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var room = await _context.Rooms.FirstOrDefaultAsync(m => m.Id == id);
            if (room == null) return NotFound();

            return View(room);
        }

        // POST: Rooms/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room != null)
            {
                _context.Rooms.Remove(room);
            }

            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("RefreshData");

            return RedirectToAction(nameof(Index));
        }

        private bool RoomExists(int id)
        {
            return _context.Rooms.Any(e => e.Id == id);
        }
    }
}