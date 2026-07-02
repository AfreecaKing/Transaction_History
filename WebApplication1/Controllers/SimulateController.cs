using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    public class SimulateController : Controller
    {
        private readonly AppDbContext _context;
        private readonly StockService _stockService;

        public SimulateController(AppDbContext context, StockService stockService)
        {
            _context = context;
            _stockService = stockService;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        // 顯示試算頁面 (GET)
        public async Task<IActionResult> Index(int fundPoolId)
        {

            int userId = GetUserId();
            var fundPool = await _context.FundPools
                .FirstOrDefaultAsync(f => f.FundPoolId == fundPoolId && f.UserId == userId);

            if (fundPool == null) return NotFound();

            // 預設帶入資金池名稱和 ID，股票代號讓使用者自己填
            var vm = new StockSimulateViewModel
            {
                FundPoolId = fundPoolId,
                PoolName = fundPool.PoolName
            };

            return View(vm);
        }

        // 執行試算 (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(int fundPoolId, string stockCode)
        {

            int userId = GetUserId();

            var fundPool = await _context.FundPools
                .FirstOrDefaultAsync(f => f.FundPoolId == fundPoolId && f.UserId == userId);

            if (fundPool == null) return NotFound();

            try
            {
                var transactions = await _context.FundTransactions
                    .Where(t => t.FundPoolId == fundPoolId &&
                           (t.Type == TransactionType.入金 || t.Type == TransactionType.出金))
                    .OrderBy(t => t.TransactionTime)
                    .ToListAsync();

                if (!transactions.Any())
                {
                    ViewBag.Error = "此資金池沒有任何入金或出金紀錄";
                    return View(new StockSimulateViewModel
                    {
                        FundPoolId = fundPoolId,
                        PoolName = fundPool.PoolName,
                        StockCode = stockCode
                    });
                }

                var dates = transactions
                    .Select(t => t.TransactionTime.ToString("yyyy-MM-dd"))
                    .Distinct()
                    .ToList();

                var batchResult = await _stockService.GetBatchPricesAsync(stockCode, dates);

                if (batchResult == null)
                {
                    ViewBag.Error = $"無法取得 {stockCode} 的股價資料，請確認股票代號是否正確";
                    return View(new StockSimulateViewModel
                    {
                        FundPoolId = fundPoolId,
                        PoolName = fundPool.PoolName,
                        StockCode = stockCode
                    });
                }

                var priceMap = batchResult.Prices
                    .Where(p => p.Close.HasValue)
                    .GroupBy(p => p.RequestedDate)
                    .ToDictionary(g => g.Key, g => g.First().Close!.Value);

                int totalShares = 0;
                decimal totalInvested = 0;
                decimal totalWithdrawn = 0;

                foreach (var t in transactions)
                {
                    var dateKey = t.TransactionTime.ToString("yyyy-MM-dd");
                    if (!priceMap.TryGetValue(dateKey, out decimal price) || price <= 0)
                        continue;

                    if (t.Type == TransactionType.入金 && t.Amount.HasValue)
                    {
                        int sharesBought = (int)(t.Amount.Value / price);
                        totalShares += sharesBought;
                        totalInvested += t.Amount.Value;
                    }
                    else if (t.Type == TransactionType.出金 && t.Amount.HasValue)
                    {
                        int sharesToSell = (int)(t.Amount.Value / price);
                        sharesToSell = Math.Min(sharesToSell, totalShares);
                        totalShares -= sharesToSell;
                        totalWithdrawn += sharesToSell * price;
                    }
                }

                var todayResult = await _stockService.GetTodayPriceAsync(stockCode);
                if (todayResult == null)
                {
                    ViewBag.Error = "無法取得今日股價";
                    return View(new StockSimulateViewModel
                    {
                        FundPoolId = fundPoolId,
                        PoolName = fundPool.PoolName,
                        StockCode = stockCode
                    });
                }

                decimal todayPrice = todayResult.Close;
                decimal currentValue = totalShares * todayPrice;
                decimal totalReturn = currentValue + totalWithdrawn - totalInvested;
                decimal returnRate = totalInvested > 0
                    ? Math.Round(totalReturn / totalInvested * 100, 2)
                    : 0;

                var vm = new StockSimulateViewModel
                {
                    FundPoolId = fundPoolId,
                    PoolName = fundPool.PoolName,
                    StockCode = stockCode.ToUpper(),
                    TotalInvested = totalInvested,
                    TotalWithdrawn = totalWithdrawn,
                    TotalShares = totalShares,
                    TodayPrice = todayPrice,
                    CurrentValue = currentValue,
                    TotalReturn = totalReturn,
                    ReturnRate = returnRate,
                    TodayDate = todayResult.Date
                };
                //ViewBag.Error = $"TotalInvested={vm.TotalInvested}, TotalShares={vm.TotalShares}, TodayPrice={vm.TodayPrice}, ReturnRate={vm.ReturnRate}";
                return View(vm);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"發生錯誤：{ex.Message}";
                return View(new StockSimulateViewModel
                {
                    FundPoolId = fundPoolId,
                    PoolName = fundPool.PoolName,
                    StockCode = stockCode
                });
            }
        }
    }
}