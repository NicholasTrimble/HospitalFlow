using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HospitalFlow.Data;
using HospitalFlow.Models;
using Microsoft.AspNetCore.Authorization;
using HospitalFlow.Hubs;         
using Microsoft.AspNetCore.SignalR; 

namespace HospitalFlow.Controllers
{
    [Authorize]
    public class EquipmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<HospitalHub> _hubContext;

        public EquipmentsController(ApplicationDbContext context, IHubContext<HospitalHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // GET: Equipments
        public async Task<IActionResult> Index(string searchString)
        {
            var equipmentQuery = _context.Equipments.Include(e => e.Room).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                equipmentQuery = equipmentQuery.Where(s => s.SerialNumber.Contains(searchString)
                                                         || s.Type.Contains(searchString));
            }

            ViewData["CurrentFilter"] = searchString;
            return View(await equipmentQuery.ToListAsync());
        }

        // GET: Equipments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var equipment = await _context.Equipments
                .Include(e => e.Room)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (equipment == null) return NotFound();

            return View(equipment);
        }

        // GET: Equipments/Create
        public IActionResult Create()
        {
            ViewData["RoomId"] = new SelectList(_context.Rooms, "Id", "Name");
            return View();
        }

        // POST: Equipments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,SerialNumber,Type,Status,RoomId")] Equipment equipment)
        {
            if (ModelState.IsValid)
            {
                _context.Add(equipment);
                await _context.SaveChangesAsync();

                await _hubContext.Clients.All.SendAsync("RefreshData");

                return RedirectToAction(nameof(Index));
            }
            ViewData["RoomId"] = new SelectList(_context.Rooms, "Id", "Name", equipment.RoomId);
            return View(equipment);
        }

        // GET: Equipments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var equipment = await _context.Equipments.FindAsync(id);
            if (equipment == null) return NotFound();

            ViewData["RoomId"] = new SelectList(_context.Rooms, "Id", "Name", equipment.RoomId);
            return View(equipment);
        }

        // POST: Equipments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,SerialNumber,Type,Status,RoomId")] Equipment equipment)
        {
            if (id != equipment.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(equipment);
                    await _context.SaveChangesAsync();

                    await _hubContext.Clients.All.SendAsync("RefreshData");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EquipmentExists(equipment.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["RoomId"] = new SelectList(_context.Rooms, "Id", "Name", equipment.RoomId);
            return View(equipment);
        }

        // GET: Equipments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var equipment = await _context.Equipments
                .Include(e => e.Room)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (equipment == null) return NotFound();

            return View(equipment);
        }

        // POST: Equipments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var equipment = await _context.Equipments.FindAsync(id);
            if (equipment != null)
            {
                _context.Equipments.Remove(equipment);
            }

            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("RefreshData");

            return RedirectToAction(nameof(Index));
        }

        private bool EquipmentExists(int id)
        {
            return _context.Equipments.Any(e => e.Id == id);
        }
    }
}