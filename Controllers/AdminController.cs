
using FreshMart.Models;
using Microsoft.AspNetCore.Mvc;

namespace FreshMart.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("UserRole") == "Admin";
        }

        public IActionResult Dashboard()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "User");

            // Basic Stats
            ViewBag.TotalProducts = _context.Products.Count();
            ViewBag.TotalUsers = _context.Users.Count();
            ViewBag.TotalCategories = _context.Categories.Count();

            ViewBag.TotalOrders = _context.Orders.Count();
            ViewBag.TotalRevenue = _context.Orders.Sum(o => o.TotalAmount);

            ViewBag.TodaysOrders = _context.Orders
                .Where(o => o.OrderDate.Date == DateTime.Today)
                .Count();

            ViewBag.TodaysRevenue = _context.Orders
                .Where(o => o.OrderDate.Date == DateTime.Today)
                .Sum(o => o.TotalAmount);

            ViewBag.AverageOrder = _context.Orders.Any()
                ? _context.Orders.Average(o => o.TotalAmount)
                : 0;

            // --------------------------------------
            // ?? DAILY REVENUE (last 7 days)
            // --------------------------------------
            var today = DateTime.Today;
            var last7 = Enumerable.Range(0, 7)
                .Select(i => today.AddDays(-i))
                .OrderBy(d => d)
                .ToList();

            ViewBag.DailyLabels = last7.Select(d => d.ToString("dd/MM")).ToList();

            ViewBag.DailyRevenue = last7
                .Select(d => _context.Orders
                    .Where(o => o.OrderDate.Date == d)
                    .Sum(o => o.TotalAmount))
                .ToList();

            // --------------------------------------
            // ?? CATEGORY-WISE SALES
            // --------------------------------------
            var categoryLabels = _context.Categories
                .Select(c => c.CategoryName)
                .ToList();

            var categoryIds = _context.Categories
                .Select(c => c.CategoryId)
                .ToList();

            var categorySales = categoryIds
                .Select(id =>
                    _context.OrderItems
                        .Where(oi => oi.Product.CategoryId == id)
                        .Sum(oi => oi.Price * oi.Quantity)
                )
                .ToList();

            ViewBag.CategoryLabels = categoryLabels;
            ViewBag.CategorySales = categorySales;

            // --------------------------------------
            // ?? PAYMENT METHOD DISTRIBUTION
            // --------------------------------------
            ViewBag.PaymentLabels = _context.Orders
                .Select(o => o.PaymentMethod)
                .Distinct()
                .ToList();

            ViewBag.PaymentValues = _context.Orders
                .GroupBy(o => o.PaymentMethod)
                .Select(g => g.Count())
                .ToList();

            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "User");
        }
    }
}

