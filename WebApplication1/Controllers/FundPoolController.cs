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

            // 計算現金餘額和市值
            var allTransactions = await _context.FundTransactions
                .Where(t => t.FundPoolId == id)
                .OrderBy(t => t.TransactionTime)
                .ToListAsync();

            decimal cash = 0;
            decimal totalInvested = 0;
            var stocks = new Dictionary<string, int>();

            foreach (var t in allTransactions)
            {
                switch (t.Type)
                {
                    case TransactionType.入金:
                        cash += t.Amount ?? 0;
                        totalInvested += t.Amount ?? 0;
                        break;
                    case TransactionType.出金:
                        cash -= t.Amount ?? 0;
                        break;
                    case TransactionType.買入:
                        if (t.StockCode != null && t.Shares.HasValue && t.PricePerShare.HasValue)
                        {
                            cash -= t.PricePerShare.Value * t.Shares.Value;
                            if (!stocks.ContainsKey(t.StockCode))
                                stocks[t.StockCode] = 0;
                            stocks[t.StockCode] += t.Shares.Value;
                        }
                        break;
                    case TransactionType.賣出:
                        if (t.StockCode != null && t.Shares.HasValue && t.PricePerShare.HasValue)
                        {
                            cash += t.PricePerShare.Value * t.Shares.Value;
                            if (stocks.ContainsKey(t.StockCode))
                                stocks[t.StockCode] -= t.Shares.Value;
                        }
                        break;
                }
            }

            ViewBag.CashBalance = cash;
            ViewBag.TotalInvested = totalInvested;

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
