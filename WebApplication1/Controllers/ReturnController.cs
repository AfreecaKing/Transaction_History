using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApplication1.Models;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Services;

public class ReturnController : Controller
{
    private readonly AppDbContext _context;
    private readonly StockService _stockService;

    public ReturnController(AppDbContext context, StockService stockService)
    {
        _context = context;
        _stockService = stockService;
    }

    private int GetUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    [HttpGet]
    public async Task<IActionResult> Calculate(int fundPoolId)
    {
        int userId = GetUserId();

        var fundPool = await _context.FundPools
            .FirstOrDefaultAsync(f => f.FundPoolId == fundPoolId && f.UserId == userId);

        if (fundPool == null) return NotFound();

        var transactions = await _context.FundTransactions
            .Where(t => t.FundPoolId == fundPoolId)
            .OrderBy(t => t.TransactionTime)
            .ToListAsync();

        decimal cash = 0;
        decimal totalInvested = 0;
        var stocks = new Dictionary<string, int>();

        foreach (var t in transactions)
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

        var activeStocks = stocks.Where(s => s.Value > 0).ToList();
        var holdings = new List<object>();
        decimal totalMarketValue = 0;
        string priceDate = "";

        if (activeStocks.Any())
        {
            var stockCodes = activeStocks.Select(s => s.Key).ToList();
            var todayBatch = await _stockService.GetTodayBatchPricesAsync(stockCodes);

            if (todayBatch != null)
            {
                var priceMap = todayBatch.Prices
                    .Where(p => p.Close.HasValue)
                    .ToDictionary(p => p.StockCode, p => p);

                priceDate = todayBatch.Prices
                    .Where(p => p.Date != null)
                    .Select(p => p.Date!)
                    .FirstOrDefault() ?? "";

                foreach (var s in activeStocks)
                {
                    if (priceMap.TryGetValue(s.Key, out var priceInfo) && priceInfo.Close.HasValue)
                    {
                        decimal marketValue = priceInfo.Close.Value * s.Value;
                        totalMarketValue += marketValue;
                        holdings.Add(new
                        {
                            stockCode = s.Key,
                            shares = s.Value,
                            todayPrice = priceInfo.Close.Value,
                            marketValue
                        });
                    }
                }
            }
        }

        decimal totalReturn = cash + totalMarketValue - totalInvested;
        decimal returnRate = totalInvested > 0
            ? Math.Round(totalReturn / totalInvested * 100, 2)
            : 0;

        // 回傳 JSON 給前端
        return Json(new
        {
            cashBalance = cash,
            totalInvested,
            totalMarketValue,
            totalAssets = cash + totalMarketValue,
            totalReturn,
            returnRate,
            priceDate,
            holdings
        });
    }
}