using Microsoft.AspNetCore.Mvc;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    public class TestController : Controller
    {
        private readonly StockService _stockService;
        public TestController(StockService stockService)
        {
            _stockService = stockService;
        }
        public async Task<IActionResult> Index()
        {
            // 測試今日股價
            var todayResult = await _stockService.GetTodayPriceAsync("2330");
            ViewBag.Today = todayResult != null
                ? $"股票:{todayResult.StockCode}, 日期:{todayResult.Date}, 收盤:{todayResult.Close}"
                : "今日股價失敗";

            // 測試歷史股價
            var historyResult = await _stockService.GetHistoryPriceAsync("2330", "2024-01-15");
            ViewBag.History = historyResult != null
                ? $"股票:{historyResult.StockCode}, 請求日期:{historyResult.RequestedDate}, 實際日期:{historyResult.ActualDate}, 收盤:{historyResult.Close}"
                : "歷史股價失敗";

            // 測試批次股價
            var batchResult = await _stockService.GetBatchPricesAsync("2330", new List<string> { "2024-01-15", "2024-06-01", "2024-12-01" });
            ViewBag.Batch = batchResult != null
                ? string.Join(" | ", batchResult.Prices.Select(p => $"{p.RequestedDate}→{p.Close}"))
                : "批次股價失敗";

            return View();
        }
    }
}
