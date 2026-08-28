using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApplication1.Models;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Services;

public class ReturnController : Controller
{
    private readonly AppDbContext _context;
    private readonly PortfolioReturnService _returnService;

    public ReturnController(AppDbContext context, PortfolioReturnService returnService)
    {
        _context = context;
        _returnService = returnService;
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

        return Json(await _returnService.CalculateAsync(fundPool, HttpContext.RequestAborted));
    }
}
