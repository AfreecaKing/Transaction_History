using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Controllers;

[Authorize]
public class StockChartController : Controller
{
    private readonly AppDbContext _context;
    private readonly StockPriceHistoryService _historyService;

    public StockChartController(AppDbContext context, StockPriceHistoryService historyService)
    {
        _context = context;
        _historyService = historyService;
    }

    [HttpGet]
    public async Task<IActionResult> Candles(int fundPoolId, string stockCode, string period = "6mo")
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        var code = stockCode.Trim().ToUpperInvariant();
        var canAccess = await _context.FundTransactions.AnyAsync(t =>
            t.FundPoolId == fundPoolId &&
            t.FundPool.UserId == userId &&
            t.StockCode != null &&
            t.StockCode.ToUpper() == code);
        if (!canAccess) return NotFound();

        try
        {
            var candles = await _historyService.GetAsync(code, period, HttpContext.RequestAborted);
            if (candles.Count == 0)
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = $"目前沒有 {code} 的K線資料" });

            return Json(new { stockCode = code, period, candles });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }
}
