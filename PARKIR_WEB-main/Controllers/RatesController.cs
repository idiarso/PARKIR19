using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkIRC.Data;
using ParkIRC.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace ParkIRC.Controllers
{
    [Authorize]
    public class RatesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RatesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Display list of parking rates
        public async Task<IActionResult> Index()
        {
            var rates = await _context.Set<ParkingRateConfiguration>().ToListAsync();
            return View(rates);
        }

        // Create new rate
        public IActionResult Create()
        {
            var rate = new ParkingRateConfiguration
            {
                VehicleType = "car", // Default value, will be changed by user
                CreatedAt = DateTime.Now,
                EffectiveFrom = DateTime.Now,
                IsActive = true,
                CreatedBy = User.Identity?.Name ?? "System",
                LastModifiedBy = User.Identity?.Name ?? "System"
            };
            return View(rate);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ParkingRateConfiguration rate)
        {
            if (ModelState.IsValid)
            {
                rate.CreatedAt = DateTime.Now;
                rate.LastModifiedAt = DateTime.Now;
                rate.CreatedBy = User.Identity?.Name ?? "System";
                rate.LastModifiedBy = User.Identity?.Name ?? "System";
                
                _context.Add(rate);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(rate);
        }

        // Edit rate
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var rate = await _context.Set<ParkingRateConfiguration>().FindAsync(id);
            if (rate == null)
            {
                return NotFound();
            }
            return View(rate);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ParkingRateConfiguration rate)
        {
            if (id != rate.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    rate.LastModifiedAt = DateTime.Now;
                    rate.LastModifiedBy = User.Identity?.Name ?? "System";
                    
                    _context.Update(rate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RateExists(rate.Id))
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
            return View(rate);
        }

        // Delete rate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var rate = await _context.Set<ParkingRateConfiguration>().FindAsync(id);
            if (rate != null)
            {
                _context.Set<ParkingRateConfiguration>().Remove(rate);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool RateExists(int id)
        {
            return _context.Set<ParkingRateConfiguration>().Any(e => e.Id == id);
        }
    }
} 