using Microsoft.AspNetCore.Mvc;
using WebApplication1.Services;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class TestController : Controller
    {
        private readonly StockService _stockService;
        private readonly AppDbContext _content;
        public TestController(StockService stockService,AppDbContext content)
        {
            _stockService = stockService;
            _content = content;
        }


        public IActionResult Index()
        {
            var users = _content.Users.ToList();
            return View(users);
        }
        
    }
}
