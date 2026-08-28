using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Services;

public record StockCandleDto(string Time, decimal Open, decimal High, decimal Low, decimal Close, long Volume);

public class StockPriceHistoryService
{
    private static readonly IReadOnlyDictionary<string, int> PeriodDays = new Dictionary<string, int>
    {
        ["1mo"] = 35,
        ["3mo"] = 100,
        ["6mo"] = 200,
        ["1y"] = 370
    };

    private readonly AppDbContext _context;
    private readonly StockService _stockService;

    public StockPriceHistoryService(AppDbContext context, StockService stockService)
    {
        _context = context;
        _stockService = stockService;
    }

    public async Task<IReadOnlyList<StockCandleDto>> GetAsync(string stockCode, string period, CancellationToken cancellationToken = default)
    {
        if (!PeriodDays.TryGetValue(period, out var days))
            throw new ArgumentException("不支援的K線區間", nameof(period));

        var code = stockCode.Trim().ToUpperInvariant();
        try
        {
            var response = await _stockService.GetCandlesAsync(code, period, cancellationToken);
            if (response != null)
            {
                await StoreAsync(code, response, cancellationToken);
            }
        }
        catch (HttpRequestException)
        {
            // 遠端服務不可用時讀取已儲存的歷史股價。
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // 遠端逾時時讀取已儲存的歷史股價。
        }

        var firstDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-days));
        var prices = await _context.StockPrices
            .Where(x => x.StockCode == code && x.PriceDate >= firstDate &&
                        x.Open.HasValue && x.High.HasValue && x.Low.HasValue)
            .OrderBy(x => x.PriceDate)
            .ToListAsync(cancellationToken);

        return prices.Select(x => new StockCandleDto(
                x.PriceDate.ToString("yyyy-MM-dd"),
                x.Open!.Value,
                x.High!.Value,
                x.Low!.Value,
                x.Close,
                x.Volume ?? 0))
            .ToList();
    }

    private async Task StoreAsync(string stockCode, StockCandleResult result, CancellationToken cancellationToken)
    {
        var incoming = result.Candles
            .Where(x => DateOnly.TryParse(x.Date, out _))
            .ToList();
        if (incoming.Count == 0) return;

        var dates = incoming.Select(x => DateOnly.Parse(x.Date)).ToList();
        var firstDate = dates.Min();
        var lastDate = dates.Max();
        var stored = await _context.StockPrices
            .Where(x => x.StockCode == stockCode && x.PriceDate >= firstDate && x.PriceDate <= lastDate)
            .ToDictionaryAsync(x => x.PriceDate, cancellationToken);

        foreach (var candle in incoming)
        {
            var date = DateOnly.Parse(candle.Date);
            if (!stored.TryGetValue(date, out var price))
            {
                price = new StockPrice
                {
                    StockCode = stockCode,
                    Ticker = result.Ticker,
                    PriceDate = date
                };
                _context.StockPrices.Add(price);
            }

            price.Ticker = result.Ticker;
            price.Open = candle.Open;
            price.High = candle.High;
            price.Low = candle.Low;
            price.Close = candle.Close;
            price.Volume = candle.Volume;
            price.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
