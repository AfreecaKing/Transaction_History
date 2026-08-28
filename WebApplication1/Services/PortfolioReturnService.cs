using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Services;

public class PortfolioReturnService
{
    private readonly AppDbContext _context;
    private readonly StockService _stockService;

    public PortfolioReturnService(AppDbContext context, StockService stockService)
    {
        _context = context;
        _stockService = stockService;
    }

    public async Task<ReturnViewModel> CalculateAsync(FundPool fundPool, CancellationToken cancellationToken = default)
    {
        var transactions = await _context.FundTransactions
            .Where(t => t.FundPoolId == fundPool.FundPoolId)
            .OrderBy(t => t.TransactionTime)
            .ToListAsync(cancellationToken);

        decimal cash = 0;
        decimal deposits = 0;
        decimal withdrawals = 0;
        var positions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var transaction in transactions)
        {
            switch (transaction.Type)
            {
                case TransactionType.入金:
                    cash += transaction.Amount ?? 0;
                    deposits += transaction.Amount ?? 0;
                    break;
                case TransactionType.出金:
                    cash -= transaction.Amount ?? 0;
                    withdrawals += transaction.Amount ?? 0;
                    break;
                case TransactionType.買入 when IsValidStockTransaction(transaction):
                    cash -= transaction.PricePerShare!.Value * transaction.Shares!.Value;
                    AddShares(positions, transaction.StockCode!, transaction.Shares.Value);
                    break;
                case TransactionType.賣出 when IsValidStockTransaction(transaction):
                    cash += transaction.PricePerShare!.Value * transaction.Shares!.Value;
                    AddShares(positions, transaction.StockCode!, -transaction.Shares.Value);
                    break;
            }
        }

        var activePositions = positions.Where(x => x.Value > 0).ToList();
        var latestPrices = await LoadAndStoreLatestPricesAsync(activePositions.Select(x => x.Key), cancellationToken);
        var holdings = new List<StockHolding>();

        foreach (var position in activePositions)
        {
            if (!latestPrices.TryGetValue(position.Key, out var price)) continue;
            holdings.Add(new StockHolding
            {
                StockCode = position.Key,
                Shares = position.Value,
                TodayPrice = price.Close,
                MarketValue = price.Close * position.Value
            });
        }

        var marketValue = holdings.Sum(x => x.MarketValue);
        var missingPriceCodes = activePositions
            .Where(x => !latestPrices.ContainsKey(x.Key))
            .Select(x => x.Key)
            .ToList();
        var hasCompletePrices = missingPriceCodes.Count == 0;
        var totalAssets = cash + marketValue;
        var netInvested = deposits - withdrawals;
        var totalReturn = totalAssets - netInvested;

        var newestPriceDate = latestPrices.Count == 0
            ? string.Empty
            : latestPrices.Values.Max(x => x.PriceDate).ToString("yyyy-MM-dd");

        return new ReturnViewModel
        {
            FundPoolId = fundPool.FundPoolId,
            PoolName = fundPool.PoolName,
            TotalInvested = netInvested,
            CashBalance = cash,
            TotalMarketValue = marketValue,
            TotalAssets = totalAssets,
            TotalReturn = totalReturn,
            ReturnRate = netInvested > 0 ? Math.Round(totalReturn / netInvested * 100, 2) : 0,
            Holdings = holdings,
            PriceDate = newestPriceDate,
            HasCompletePrices = hasCompletePrices,
            MissingPriceCodes = missingPriceCodes
        };
    }

    private async Task<Dictionary<string, StockPrice>> LoadAndStoreLatestPricesAsync(IEnumerable<string> stockCodes, CancellationToken cancellationToken)
    {
        var codes = stockCodes.Select(NormalizeCode).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (codes.Count == 0) return new(StringComparer.OrdinalIgnoreCase);

        TodayBatchResult? response = null;
        try
        {
            response = await _stockService.GetTodayBatchPricesAsync(codes, cancellationToken);
        }
        catch (HttpRequestException)
        {
            // 股價來源暫時無法連線時，改用資料庫中最後一筆成功儲存的價格。
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // 遠端逾時時同樣使用快取價格。
        }
        if (response != null)
        {
            foreach (var item in response.Prices.Where(x => x.Close.HasValue && DateOnly.TryParse(x.Date, out _)))
            {
                var code = NormalizeCode(item.StockCode);
                var date = DateOnly.Parse(item.Date!);
                var stored = await _context.StockPrices.SingleOrDefaultAsync(
                    x => x.StockCode == code && x.PriceDate == date, cancellationToken);
                if (stored == null)
                {
                    _context.StockPrices.Add(new StockPrice
                    {
                        StockCode = code,
                        Ticker = item.Ticker,
                        PriceDate = date,
                        Close = item.Close!.Value
                    });
                }
                else
                {
                    stored.Close = item.Close!.Value;
                    stored.Ticker = item.Ticker;
                    stored.UpdatedAt = DateTime.UtcNow;
                }
            }
            await _context.SaveChangesAsync(cancellationToken);
        }

        var prices = await _context.StockPrices
            .Where(x => codes.Contains(x.StockCode))
            .ToListAsync(cancellationToken);
        return prices.GroupBy(x => x.StockCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(p => p.PriceDate).First(), StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsValidStockTransaction(FundTransaction transaction) =>
        !string.IsNullOrWhiteSpace(transaction.StockCode) && transaction.Shares > 0 && transaction.PricePerShare >= 0;

    private static void AddShares(Dictionary<string, int> positions, string stockCode, int shares)
    {
        var code = NormalizeCode(stockCode);
        positions[code] = positions.GetValueOrDefault(code) + shares;
    }

    private static string NormalizeCode(string stockCode) => stockCode.Trim().ToUpperInvariant();
}
