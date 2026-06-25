using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace WebApplication1.Controllers
{
    [Authorize]
    public class MoneyController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
