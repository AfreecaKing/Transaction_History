using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class ValidationController : Controller
    {
        private readonly AppDbContext _context;

        public ValidationController(AppDbContext context)
        {
            _context = context;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        public async Task<IActionResult> Index(int fundPoolId, string redirectTo)
        {
            int userId = GetUserId();

            var fundPool = await _context.FundPools
                .FirstOrDefaultAsync(f => f.FundPoolId == fundPoolId && f.UserId == userId);

            if (fundPool == null) return NotFound();

            var transactions = await _context.FundTransactions
                .Where(t => t.FundPoolId == fundPoolId)
                .OrderBy(t => t.TransactionTime)
                .ToListAsync();

            var vm = new ValidationResultViewModel
            {
                FundPoolId = fundPoolId,
                PoolName = fundPool.PoolName,
                IsValid = true,
                RedirectTo = redirectTo,  // 記住要跳去哪
                Errors = new List<ValidationError>()
            };

            if (!transactions.Any())
            {
                vm.IsValid = false;
                vm.Errors.Add(new ValidationError
                {
                    ErrorMessage = "此資金池沒有任何交易紀錄"
                });
                return View(vm);
            }

            decimal cash = 0;
            var stocks = new Dictionary<string, int>();

            foreach (var t in transactions)
            {
                switch (t.Type)
                {
                    case TransactionType.入金:
                        if (!t.Amount.HasValue || t.Amount <= 0)
                        {
                            vm.Errors.Add(new ValidationError
                            {
                                TransactionTime = t.TransactionTime,
                                Type = t.Type,
                                ErrorMessage = "入金金額必須大於 0"
                            });
                            break;
                        }
                        cash += t.Amount.Value;
                        break;

                    case TransactionType.出金:
                        if (!t.Amount.HasValue || t.Amount <= 0)
                        {
                            vm.Errors.Add(new ValidationError
                            {
                                TransactionTime = t.TransactionTime,
                                Type = t.Type,
                                ErrorMessage = "出金金額必須大於 0"
                            });
                            break;
                        }
                        if (t.Amount.Value > cash)
                        {
                            vm.Errors.Add(new ValidationError
                            {
                                TransactionTime = t.TransactionTime,
                                Type = t.Type,
                                ErrorMessage = $"出金 NT$ {t.Amount:N0} 超過現金餘額 NT$ {cash:N0}"
                            });
                            break;
                        }
                        cash -= t.Amount.Value;
                        break;

                    case TransactionType.買入:
                        if (string.IsNullOrEmpty(t.StockCode) || !t.Shares.HasValue || !t.PricePerShare.HasValue)
                        {
                            vm.Errors.Add(new ValidationError
                            {
                                TransactionTime = t.TransactionTime,
                                Type = t.Type,
                                ErrorMessage = "買入紀錄缺少股票代號、股數或每股價格"
                            });
                            break;
                        }
                        decimal buyCost = t.PricePerShare.Value * t.Shares.Value;
                        if (buyCost > cash)
                        {
                            vm.Errors.Add(new ValidationError
                            {
                                TransactionTime = t.TransactionTime,
                                Type = t.Type,
                                ErrorMessage = $"買入 {t.StockCode} {t.Shares}股，花費 NT$ {buyCost:N0} 超過現金餘額 NT$ {cash:N0}"
                            });
                            break;
                        }
                        cash -= buyCost;
                        if (!stocks.ContainsKey(t.StockCode))
                            stocks[t.StockCode] = 0;
                        stocks[t.StockCode] += t.Shares.Value;
                        break;

                    case TransactionType.賣出:
                        if (string.IsNullOrEmpty(t.StockCode) || !t.Shares.HasValue || !t.PricePerShare.HasValue)
                        {
                            vm.Errors.Add(new ValidationError
                            {
                                TransactionTime = t.TransactionTime,
                                Type = t.Type,
                                ErrorMessage = "賣出紀錄缺少股票代號、股數或每股價格"
                            });
                            break;
                        }
                        if (!stocks.ContainsKey(t.StockCode) || stocks[t.StockCode] < t.Shares.Value)
                        {
                            int currentShares = stocks.ContainsKey(t.StockCode) ? stocks[t.StockCode] : 0;
                            vm.Errors.Add(new ValidationError
                            {
                                TransactionTime = t.TransactionTime,
                                Type = t.Type,
                                ErrorMessage = $"賣出 {t.StockCode} {t.Shares}股，超過目前持股 {currentShares}股"
                            });
                            break;
                        }
                        cash += t.PricePerShare.Value * t.Shares.Value;
                        stocks[t.StockCode] -= t.Shares.Value;
                        break;
                }
            }

            vm.IsValid = !vm.Errors.Any();
            vm.FinalCash = cash;
            vm.FinalStocks = stocks;

            // 驗證通過 → 直接跳轉到對應功能
            if (vm.IsValid)
            {
                return redirectTo switch
                {
                    "Simulate" => RedirectToAction("Index", "Simulate", new { fundPoolId }),
                    "Return" => RedirectToAction("Index", "Return", new { fundPoolId }),
                    _ => RedirectToAction("Detail", "FundPool", new { id = fundPoolId })
                };
            }

            return View(vm);
        }
    }
}