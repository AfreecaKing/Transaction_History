using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplication1.Models;
using WebApplication1.Services;
namespace WebApplication1.Controllers
{
    public class FundPoolController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PortfolioReturnService _returnService;

        public FundPoolController(AppDbContext context, PortfolioReturnService returnService)
        {
            _context = context;
            _returnService = returnService;
        }

        private int GetUserId() //取得存在cookie中的UserId
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        public async Task<IActionResult> Index(int page = 1)    // 根據分頁顯示資金池列表
        {
            int userId = GetUserId();
            int pageSize = 6;
            var query = _context.FundPools.Where(f => f.UserId == userId);// 取得該使用者的資金池
            int totalCount = await query.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            var fundPools = await query
                .Skip((page - 1) * pageSize)    // 跳過前面頁數的資金池
                .Take(pageSize) 
                .ToListAsync();
            ViewBag.CurrentPage = page; // 將當前頁數傳遞給view
            ViewBag.TotalPages = totalPages; // 將總頁數傳遞給view
            return View(fundPools);
        }

        public IActionResult CreatePool()   // 顯示新增資金池頁面 (GET)
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]  //POST: 新增資金池
        public async Task<IActionResult> Create(string poolName, decimal currentValue)
        {
            int userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);  // 取得使用者資訊
            if (user == null) return RedirectToAction("Login", "Account");
            var fundPool = new FundPool // 建立新的資金池實例
            {
                UserId = userId,
                User = user,
                PoolName = poolName,
                CurrentValue = currentValue
            };
            _context.FundPools.Add(fundPool);   // 將新的資金池加入資料庫上下文
            await _context.SaveChangesAsync();  // 將變更保存到資料庫
            return RedirectToAction(nameof(Index)); // 新增完成後重新導向到資金池列表頁面
        }

        public async Task<IActionResult> Detail(int id, int transactionPage = 1)
        {
            int userId = GetUserId();

            var fundPool = await _context.FundPools
                .FirstOrDefaultAsync(f => f.FundPoolId == id && f.UserId == userId);

            if (fundPool == null) return NotFound();

            ViewBag.Return = await _returnService.CalculateAsync(fundPool, HttpContext.RequestAborted);

            // 分頁部分不變
            int pageSize = 10;
            var transactionQuery = _context.FundTransactions
                .Where(t => t.FundPoolId == id)
                .OrderByDescending(t => t.TransactionTime);

            int totalCount = await transactionQuery.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var transactions = await transactionQuery
                .Skip((transactionPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Transactions = transactions;
            ViewBag.CurrentPage = transactionPage;
            ViewBag.TotalPages = totalPages;

            return View(fundPool);
        }

        // 刪除資金池 (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            int userId = GetUserId();
            var fundPool = await _context.FundPools // 取得該使用者的指定資金池
                .FirstOrDefaultAsync(f => f.FundPoolId == id && f.UserId == userId);
            if (fundPool == null) return NotFound();
            _context.FundPools.Remove(fundPool);    // 從資料庫上下文中移除該資金池
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
