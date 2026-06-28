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
        // 顯示使用者的資金池列表
        public async Task<IActionResult> Index()
        {
            int userId = GetUserId();

            var fundPools = await _context.FundPools
                .Where(f => f.UserId == userId)
                .ToListAsync();

            return View(fundPools);
        }
        // 顯示新增頁面 (GET)
        public IActionResult CreatePool()
        {
            return View();
        }

        // 新增
        [HttpPost]
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
    }
}
