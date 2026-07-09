using FreshMart.Models;
using Microsoft.AspNetCore.Mvc;

namespace FreshMart.Controllers
{
    public class AdminCategoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminCategoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool AdminCheck()
        {
            return HttpContext.Session.GetString("UserRole") == "Admin";
        }


        public IActionResult Index()
        {
            if (!AdminCheck()) return RedirectToAction("Login", "User");

            var categories = _context.Categories.ToList();
            return View(categories);
        }

        public IActionResult Create()
        {
            if (!AdminCheck()) return RedirectToAction("Login", "User");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Category category)
        {
            if (!AdminCheck()) return RedirectToAction("Login", "User");

            if (ModelState.IsValid)
            {
                _context.Categories.Add(category);
                _context.SaveChanges();
                TempData["Success"] = "Thêm danh mục thành công!";
                return RedirectToAction("Index");
            }

            return View(category);
        }

        public IActionResult Edit(int id)
        {
            if (!AdminCheck()) return RedirectToAction("Login", "User");

            var category = _context.Categories.FirstOrDefault(c => c.CategoryId == id);

            if (category == null) return NotFound();

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Category category)
        {
            if (!AdminCheck()) return RedirectToAction("Login", "User");

            if (ModelState.IsValid)
            {
                _context.Categories.Update(category);
                _context.SaveChanges();
                TempData["Success"] = "Cập nhật danh mục thành công!";
                return RedirectToAction("Index");
            }

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            if (!AdminCheck()) return RedirectToAction("Login", "User");

            var category = _context.Categories.FirstOrDefault(c => c.CategoryId == id);

            if (category != null)
            {
                // Check if any products are linked
                bool hasProducts = _context.Products.Any(p => p.CategoryId == id);
                if (hasProducts)
                {
                    TempData["Error"] = "Không thể xóa danh mục này vì đang có sản phẩm liên kết.";
                    return RedirectToAction("Index");
                }

                _context.Categories.Remove(category);
                _context.SaveChanges();
                TempData["Success"] = "Xóa danh mục thành công!";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy danh mục.";
            }

            return RedirectToAction("Index");
        }
    }
}
