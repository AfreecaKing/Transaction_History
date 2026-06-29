using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class FundTransactionController : Controller
    {
        private readonly AppDbContext _context;

        public FundTransactionController(AppDbContext context)
        {
            _context = context;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        // 顯示新增交易頁面 (GET)
        public async Task<IActionResult> Create(int id)
        {
            int userId = GetUserId();
            var fundPool = await _context.FundPools
                .FirstOrDefaultAsync(f => f.FundPoolId == id && f.UserId == userId);

            if (fundPool == null) return NotFound();

            ViewBag.FundPoolId = id;
            ViewBag.FundPoolName = fundPool.PoolName;
            return View();
        }

        // 處理新增交易 (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int fundPoolId,
            TransactionType type,
            DateTime transactionTime,
            decimal? amount,
            string? stockCode,
            int? shares,
            decimal? pricePerShare)
        {
            int userId = GetUserId();
            var fundPool = await _context.FundPools
                .FirstOrDefaultAsync(f => f.FundPoolId == fundPoolId && f.UserId == userId);

            if (fundPool == null) return NotFound();

            var transaction = new FundTransaction
            {
                FundPoolId = fundPoolId,
                FundPool = fundPool,
                Type = type,
                TransactionTime = transactionTime,
                Amount = amount,
                StockCode = stockCode,
                Shares = shares,
                PricePerShare = pricePerShare
            };

            _context.FundTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            return RedirectToAction("Detail", "FundPool", new { id = fundPoolId });
        }

        // 顯示編輯頁面 (GET)
        public async Task<IActionResult> Edit(int id)
        {
            int userId = GetUserId();
            var transaction = await _context.FundTransactions
                .Include(t => t.FundPool)
                .FirstOrDefaultAsync(t => t.TransactionId == id && t.FundPool.UserId == userId);

            if (transaction == null) return NotFound();

            ViewBag.FundPoolId = transaction.FundPoolId;
            ViewBag.FundPoolName = transaction.FundPool.PoolName;
            return View(transaction);
        }

        // 處理編輯 (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int transactionId,
            TransactionType type,
            DateTime transactionTime,
            decimal? amount,
            string? stockCode,
            int? shares,
            decimal? pricePerShare)
        {
            int userId = GetUserId();
            var transaction = await _context.FundTransactions
                .Include(t => t.FundPool)
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId && t.FundPool.UserId == userId);

            if (transaction == null) return NotFound();

            transaction.Type = type;
            transaction.TransactionTime = transactionTime;
            transaction.Amount = amount;
            transaction.StockCode = stockCode;
            transaction.Shares = shares;
            transaction.PricePerShare = pricePerShare;

            await _context.SaveChangesAsync();

            return RedirectToAction("Detail", "FundPool", new { id = transaction.FundPoolId });
        }

        // 刪除 (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            int userId = GetUserId();
            var transaction = await _context.FundTransactions
                .Include(t => t.FundPool)
                .FirstOrDefaultAsync(t => t.TransactionId == id && t.FundPool.UserId == userId);

            if (transaction == null) return NotFound();

            int fundPoolId = transaction.FundPoolId;
            _context.FundTransactions.Remove(transaction);
            await _context.SaveChangesAsync();

            return RedirectToAction("Detail", "FundPool", new { id = fundPoolId });
        }
    }
}