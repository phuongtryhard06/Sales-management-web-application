using FreshMart.Models;
using Microsoft.AspNetCore.Mvc;

namespace FreshMart.Controllers
{
    public class AdminUserController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminUserController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("UserRole") == "Admin";
        }

        // ---------- EDIT USER (GET) ----------
        public IActionResult Edit(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "User");

            var user = _context.Users.FirstOrDefault(u => u.UserId == id);

            if (user == null)
                return NotFound();

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(User model)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "User");

            // LOẠI BỎ VALIDATION PASSWORD KHI SỬA INFO
            ModelState.Remove("Password");
            ModelState.Remove("ConfirmPassword");
            ModelState.Remove("PasswordHash");

            if (!ModelState.IsValid)
                return View(model);

            // TÌM USER TỪ DB ĐỂ GÁN LẠI TỪNG FIELD (TRÁNH LỖI GHI ĐÈ PASSWORD/CREATEDAT)
            var user = _context.Users.FirstOrDefault(u => u.UserId == model.UserId);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy người dùng.";
                return RedirectToAction("Index");
            }

            // KIỂM TRA TRÙNG EMAIL
            if (_context.Users.Any(u => u.Email == model.Email && u.UserId != model.UserId))
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng bởi người dùng khác.");
                return View(model);
            }

            // CẬP NHẬT TỪNG TRƯỜNG CỤ THỂ
            user.FullName = model.FullName;
            user.Email = model.Email;
            user.Role = model.Role;

            _context.SaveChanges();
            TempData["Success"] = "Cập nhật người dùng thành công!";

            return RedirectToAction("Index");
        }



        // ---------- LIST USERS ----------
        public IActionResult Index()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "User");

            var users = _context.Users.ToList();
            return View(users);
        }

        // ---------- CREATE USER ----------
        public IActionResult Create()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "User");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(User model)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "User");

            if (!ModelState.IsValid)
                return View(model);

            // Hash password
            model.PasswordHash = HashPassword(model.Password);
    

            if (string.IsNullOrEmpty(model.Role))
                model.Role = "Customer";

            _context.Users.Add(model);
            _context.SaveChanges();
            TempData["Success"] = "Thêm người dùng thành công!";

            return RedirectToAction("Index");
        }

        // ---------- DELETE USER ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "User");

            var user = _context.Users.FirstOrDefault(u => u.UserId == id);

            if (user != null)
            {
                _context.Users.Remove(user);
                _context.SaveChanges();
                TempData["Success"] = "Xóa người dùng thành công!";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy người dùng.";
            }

            return RedirectToAction("Index");
        }

        // ---------- HASH ----------
        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
    }
}
