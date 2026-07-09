using FreshMart.Models;
using FreshMart.Helpers;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace FreshMart.Controllers
{
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserController> _logger;

        public UserController(ApplicationDbContext context, ILogger<UserController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ========== REGISTER ==========
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(User model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View(model);

                if (!ValidationHelper.IsValidEmail(model.Email))
                {
                    ModelState.AddModelError("Email", "Vui lòng nhập email hợp lệ.");
                    return View(model);
                }

                var (isValid, message) = ValidationHelper.ValidatePassword(model.Password);
                if (!isValid)
                {
                    ModelState.AddModelError("Password", message);
                    return View(model);
                }

                var existingUser = _context.Users.FirstOrDefault(u => u.Email == model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng.");
                    return View(model);
                }

                model.PasswordHash = HashPassword(model.Password);
                // SECURITY: Bắt buộc set cứng Role = "Customer" ở tầng backend
                // để ngăn chặn việc inject giá trị "Admin" qua DOM manipulation
                model.Role = "Customer";

                _context.Users.Add(model);
                _context.SaveChanges();

                TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Register error: {Message}", ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi. Vui lòng thử lại.";
                return RedirectToAction("Register");
            }
        }

        // ========== LOGIN ==========
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string Email, string PasswordHash)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(PasswordHash))
                {
                    TempData["Error"] = "Vui lòng nhập email và mật khẩu.";
                    return View();
                }

                var user = _context.Users.FirstOrDefault(u => u.Email == Email);
                if (user == null)
                {
                    TempData["Error"] = "Email hoặc mật khẩu không đúng.";
                    return View();
                }

                bool isPasswordValid = false;
                
                // 1. Try BCrypt verify
                try {
                    isPasswordValid = BCrypt.Net.BCrypt.Verify(PasswordHash, user.PasswordHash);
                } catch { } // Catch old hash format errors

                // 2. Fallback to Legacy SHA256
                if (!isPasswordValid)
                {
                    if (user.PasswordHash == LegacySHA256Hash(PasswordHash))
                    {
                        isPasswordValid = true;
                        // 3. Re-hash with BCrypt and update DB automatically
                        user.PasswordHash = HashPassword(PasswordHash);
                        _context.SaveChanges();
                    }
                }

                if (!isPasswordValid)
                {
                    TempData["Error"] = "Email hoặc mật khẩu không đúng.";
                    return View();
                }

                HttpContext.Session.SetInt32("UserId", user.UserId);
                HttpContext.Session.SetString("UserRole", user.Role);
                HttpContext.Session.SetString("UserName", user.FullName);
                HttpContext.Session.SetString("UserAvatar", user.AvatarPath ?? "");

                if (user.Role == "Admin")
                    return RedirectToAction("Dashboard", "Admin");

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error: {Message}", ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi đăng nhập.";
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateAvatar(IFormFile? avatarFile, string? presetName)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            var user = _context.Users.Find(userId);
            if (user == null) return RedirectToAction("Login");

            try
            {
                string? newPath = null;
                bool isNewCustom = false;

                if (!string.IsNullOrEmpty(presetName))
                {
                    // CASE 1: PRESET SELECTION
                    var presetsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars", "presets", presetName);
                    if (System.IO.File.Exists(presetsPath))
                    {
                        newPath = "presets/" + presetName;
                    }
                    else
                    {
                        TempData["Error"] = "Ảnh mẫu không tồn tại.";
                        return RedirectToAction("Profile");
                    }
                }
                else if (avatarFile != null && avatarFile.Length > 0)
                {
                    // CASE 2: CUSTOM UPLOAD
                    var extension = Path.GetExtension(avatarFile.FileName).ToLower();
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                    if (!allowedExtensions.Contains(extension))
                    {
                        TempData["Error"] = "Định dạng ảnh không hợp lệ (hỗ trợ JPG, PNG, WEBP).";
                        return RedirectToAction("Profile");
                    }

                    if (avatarFile.Length > 2 * 1024 * 1024)
                    {
                        TempData["Error"] = "Kích thước ảnh tối đa là 2MB.";
                        return RedirectToAction("Profile");
                    }

                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        avatarFile.CopyTo(stream);
                    }

                    newPath = uniqueFileName;
                    isNewCustom = true;
                }

                if (newPath != null)
                {
                    // DELETE OLD CUSTOM AVATAR (if not preset)
                    if (!string.IsNullOrEmpty(user.AvatarPath) && !user.AvatarPath.StartsWith("presets/"))
                    {
                        var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars", user.AvatarPath);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    user.AvatarPath = newPath;
                    _context.SaveChanges();

                    HttpContext.Session.SetString("UserAvatar", newPath);
                    TempData["Success"] = "Cập nhật ảnh đại diện thành công!";
                }
                else
                {
                    TempData["Error"] = "Vui lòng chọn ảnh hoặc tải lên file.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateAvatar error: {Message}", ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi cập nhật ảnh.";
            }

            return RedirectToAction("Profile");
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            foreach (var cookie in Request.Cookies.Keys)
            {
                if (cookie.Contains("Session"))
                {
                    Response.Cookies.Delete(cookie);
                }
            }
            TempData["Success"] = "Bạn đã đăng xuất thành công.";
            return RedirectToAction("Login");
        }

        // ========== PROFILE ==========
        public IActionResult Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login");

            var user = _context.Users.Find(userId);
            if (user == null)
                return RedirectToAction("Login");

            var orders = _context.Orders.Where(o => o.UserId == userId).ToList();

            // DYNAMIC PRESETS
            var presetsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars", "presets");
            var presetIcons = new List<string>();
            if (Directory.Exists(presetsPath))
            {
                presetIcons = Directory.GetFiles(presetsPath)
                    .Select(Path.GetFileName)
                    .Where(f => !string.IsNullOrEmpty(f))
                    .Cast<string>()
                    .ToList();
            }

            var model = new ProfileViewModel
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                TotalOrders = orders.Count,
                TotalSpent = orders.Sum(o => o.TotalAmount),
                AvatarPath = user.AvatarPath,
                PresetIcons = presetIcons
            };

            return View(model);
        }

        // ========== CHANGE PASSWORD ==========
        public IActionResult ChangePassword()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(ChangePasswordViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login");

            if (!ModelState.IsValid)
                return View(model);

            var user = _context.Users.Find(userId);
            if (user == null)
                return RedirectToAction("Login");

            // Verify current password
            bool isCurrentValid = false;
            try {
                isCurrentValid = BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash);
            } catch { }

            if (!isCurrentValid && user.PasswordHash == LegacySHA256Hash(model.CurrentPassword))
            {
                isCurrentValid = true;
            }

            if (!isCurrentValid)
            {
                ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng.");
                return View(model);
            }

            // Update to new password
            user.PasswordHash = HashPassword(model.NewPassword);
            _context.SaveChanges();

            TempData["Success"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Profile");
        }

        // ========== HASH PASSWORD ==========
        private string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be null or empty");

            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        private string LegacySHA256Hash(string password)
        {
            if (string.IsNullOrEmpty(password)) return string.Empty;
            using (var sha = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}
