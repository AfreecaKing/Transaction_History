using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplication1.Models;
namespace WebApplication1.Controllers
{
    public class FundPoolController : Controller
    {
        private readonly AppDbContext _context;

        public FundPoolController(AppDbContext context)
        {
            _context = context;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            int userId = GetUserId();
            int pageSize = 6;
            var query = _context.FundPools.Where(f => f.UserId == userId);
            int totalCount = await query.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            var fundPools = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            return View(fundPools);
        }

        public IActionResult CreatePool()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string poolName, decimal currentValue)
        {
            int userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return RedirectToAction("Login", "Account");
            var fundPool = new FundPool
            {
                UserId = userId,
                User = user,
                PoolName = poolName,
                CurrentValue = currentValue
            };
            _context.FundPools.Add(fundPool);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Detail(int id)
        {
            int userId = GetUserId();
            var fundPool = await _context.FundPools
                .Include(f => f.Transactions)
                .FirstOrDefaultAsync(f => f.FundPoolId == id && f.UserId == userId);
            if (fundPool == null) return NotFound();
            return View(fundPool);
        }

        // 刪除資金池 (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            int userId = GetUserId();
            var fundPool = await _context.FundPools
                .FirstOrDefaultAsync(f => f.FundPoolId == id && f.UserId == userId);
            if (fundPool == null) return NotFound();
            _context.FundPools.Remove(fundPool);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
