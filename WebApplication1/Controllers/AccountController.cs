using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        //  透過建構函式注入剛才牽好線的 MySQL DbContext
        public AccountController(AppDbContext context)
        {
            _context = context;
        }
        // ==================== 1. 註冊功能 ====================

        // 顯示註冊頁面 (GET)
        public IActionResult Register() => View();
        // 處理註冊資料 (POST)
        [HttpPost]
        public IActionResult Register(User user)
        {
            // 用Name檢查是否已經有相同的使用者名稱存在資料庫中
            var isExist = _context.Users.Any(u => u.Name == user.Name);
            if (isExist)
            {
                ViewBag.Error = "該使用者名稱已被註冊！";
                return View();
            }
            // 如果沒有重複，則將新使用者資料加入資料庫
            _context.Users.Add(user);
            _context.SaveChanges();
            return RedirectToAction("Login");
        }
        // 當瀏覽器輸入 /Account/Login 時，負責把 Login.cshtml 畫面丟出來
        public IActionResult Login() => View();
        // 處理登入資料 (POST)
        [HttpPost]
        public IActionResult Login(string name, string password) { 
            var user = _context.Users.FirstOrDefault(u => u.Name == name && u.Password == password);
            if (user != null)
            {
                HttpContext.Session.SetString("UserName", user.Name); // 將使用者name存入Session
                return RedirectToAction("Index","Home");
            }
            ViewBag.Error = "帳號或密碼錯誤！";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // 移除Session中的data
            return RedirectToAction("Index","Home");    //return to home page
        }
       
    }
}
