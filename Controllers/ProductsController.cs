//using FreshMart.Models;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;

//namespace FreshMart.Controllers
//{
//    public class ProductsController : Controller
//    {
//        private readonly ApplicationDbContext _context;

//        public ProductsController(ApplicationDbContext context)
//        {
//            _context = context;
//        }

//        public IActionResult Index()
//        {
//            var products = _context.Products.Include(x => x.Category).ToList();
//            return View(products);
//        }

//        public IActionResult Details(int id)
//        {
//            var product = _context.Products
//                .Include(p => p.Category)
//                .FirstOrDefault(p => p.ProductId == id);

//            if (product == null)
//                return NotFound();

//            return View(product);
//        }


//        // CATEGORY FILTER
//        public IActionResult Category(int id)
//        {
//            var category = _context.Categories.FirstOrDefault(c => c.CategoryId == id);
//            if (category == null) return NotFound();

//            ViewBag.CategoryName = category.CategoryName;

//            var products = _context.Products
//                .Include(p => p.Category)
//                .Where(p => p.CategoryId == id)
//                .ToList();

//            return View(products);
//        }
//    }
//}

using FreshMart.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreshMart.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(string? keyword, int? categoryId, decimal? minPrice, decimal? maxPrice, int page = 1)
        {
            ViewBag.Categories = _context.Categories.ToList();

            var query = _context.Products.Include(p => p.Category).AsQueryable();

            // Lưu state bộ lọc ra ViewBag để hiển thị lại trên Form
            ViewBag.Keyword = keyword;
            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;

            // Debug logic (tắt khi release)
            // TempData["Debug"] = $"Keyword: {keyword}, Cat: {categoryId}, Min: {minPrice}, Max: {maxPrice}";

            // 1. Lọc theo từ khóa (tên sản phẩm)
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var search = keyword.ToLower().Trim();
                query = query.Where(p => p.Name != null && p.Name.ToLower().Contains(search));
            }

            // 2. Lọc theo danh mục
            if (categoryId.HasValue && categoryId > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId);
                var cat = _context.Categories.Find(categoryId);
                if (cat != null) ViewBag.CategoryName = cat.CategoryName;
            }

            // 3. Lọc theo khoảng giá
            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            // Phân trang
            int pageSize = 12;
            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            var products = query.OrderByDescending(p => p.CreatedAt)
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToList();

            return View("Index", products);
        }

        public IActionResult Details(int id)
        {
            ViewBag.Categories = _context.Categories.ToList();

            var product = _context.Products
                .Include(p => p.Category)
                .FirstOrDefault(p => p.ProductId == id);

            if (product == null)
                return NotFound();

            ViewBag.RelatedProducts = _context.Products
                .Where(p => p.CategoryId == product.CategoryId && p.ProductId != id)
                .Take(4)
                .ToList();

            return View(product);
        }

        // CATEGORY REDIRECT (Gom về Index để quản lý filter tập trung)
        public IActionResult Category(int id, int page = 1)
        {
            return RedirectToAction("Index", new { categoryId = id, page = page });
        }

        // AJAX SEARCH
        [HttpGet]
        public IActionResult SearchAjax(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return Json(new { success = false });
            }

            var keywords = term.ToLower().Trim();
            var results = _context.Products
                .Include(p => p.Category)
                .Where(p => p.Name!.ToLower().Contains(keywords) || p.Category!.CategoryName!.ToLower().Contains(keywords))
                .Select(p => new
                {
                    id = p.ProductId,
                    name = p.Name,
                    price = p.Price,
                    image = p.ImagePath
                })
                .Take(8)
                .ToList();

            return Json(new { success = true, data = results });
        }
    }
}

