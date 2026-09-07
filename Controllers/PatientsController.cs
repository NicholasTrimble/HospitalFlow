using HospitalFlow.Data;
using HospitalFlow.Hubs;
using HospitalFlow.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HospitalFlow.Controllers
{
    [Authorize]
    public class PatientsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<HospitalHub> _hubContext;

        public PatientsController(ApplicationDbContext context, IHubContext<HospitalHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // GET: Patients/Create
        public async Task<IActionResult> Create()
        {
            // Fetch only available rooms for assignment
            var availableRooms = await _context.Rooms
                .Where(r => r.Status == RoomStatus.Available)
                .ToListAsync();

            ViewData["RoomId"] = new SelectList(availableRooms, "Id", "Name");
            return View();
        }


        // Delete patient and flag room for cleaning if necessary
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var patient = await _context.Patients
                .Include(p => p.Room)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (patient != null)
            {
                // If the patient was in a room, flag it for cleaning
                if (patient.Room != null)
                {
                    patient.Room.Status = RoomStatus.CleaningRequired;
                }

                _context.Patients.Remove(patient);
                await _context.SaveChangesAsync();

                await _hubContext.Clients.All.SendAsync("RefreshData");
            }

            return RedirectToAction("Index", "Rooms");
        }




        // POST: Patients/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FullName,MedicalRecordNumber,AcuityLevel,Notes,RoomId")] Patient patient)
        {
            if (ModelState.IsValid)
            {
                patient.AdmissionDate = DateTime.UtcNow;

                // If an available room was chosen, flip that room's status to Occupied
                if (patient.RoomId.HasValue)
                {
                    var room = await _context.Rooms.FindAsync(patient.RoomId.Value);
                    if (room != null)
                    {
                        room.Status = RoomStatus.Occupied;
                    }
                }

                _context.Add(patient);
                await _context.SaveChangesAsync();

                await _hubContext.Clients.All.SendAsync("RefreshData");

                // Redirect back to the main Rooms dashboard
                return RedirectToAction("Index", "Rooms");
            }


            // Reload dropdown if validation fails
            var availableRooms = await _context.Rooms
                .Where(r => r.Status == RoomStatus.Available)
                .ToListAsync();

            ViewData["RoomId"] = new SelectList(availableRooms, "Id", "Name", patient.RoomId);
            return View(patient);
        }
    }
}
