using FreshMart.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FreshMart.Controllers
{
    /// <summary>
    /// Admin Order Management Controller
    /// Cho phép quản trị viên xem, lọc và cập nhật trạng thái đơn hàng
    /// </summary>
    public class AdminOrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminOrderController> _logger;

        // Danh sách trạng thái hợp lệ - whitelist để tránh inject status tùy ý
        private static readonly string[] ValidStatuses = { "Pending", "Completed", "Cancelled" };

        public AdminOrderController(ApplicationDbContext context, ILogger<AdminOrderController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("UserRole") == "Admin";
        }

        // ========== INDEX: Danh sách tất cả đơn hàng ==========
        public IActionResult Index(string? status, int page = 1)
        {
            if (!IsAdmin())
            {
                _logger.LogWarning("AdminOrderController Index: Truy cập trái phép.");
                return RedirectToAction("Login", "User");
            }

            try
            {
                var query = _context.Orders
                    .Include(o => o.User)
                    .AsQueryable();

                // Lọc theo trạng thái nếu có
                if (!string.IsNullOrEmpty(status) && ValidStatuses.Contains(status))
                {
                    query = query.Where(o => o.Status == status);
                    ViewBag.FilterStatus = status;
                }
                else
                {
                    ViewBag.FilterStatus = "";
                }

                // Phân trang
                int pageSize = 15;
                int totalItems = query.Count();
                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
                if (page < 1) page = 1;
                if (page > totalPages && totalPages > 0) page = totalPages;

                var orders = query
                    .OrderByDescending(o => o.OrderDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalItems = totalItems;
                ViewBag.ValidStatuses = ValidStatuses;

                // Thống kê nhanh
                ViewBag.PendingCount = _context.Orders.Count(o => o.Status == "Pending");
                ViewBag.CompletedCount = _context.Orders.Count(o => o.Status == "Completed");
                ViewBag.CancelledCount = _context.Orders.Count(o => o.Status == "Cancelled");

                _logger.LogInformation("AdminOrderController Index: Lấy {Count} đơn hàng (trang {Page}).", orders.Count, page);
                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AdminOrderController Index: Lỗi - {Message}", ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi tải danh sách đơn hàng.";
                return View(new List<Order>());
            }
        }

        // ========== DETAILS: Chi tiết đơn hàng ==========
        public IActionResult Details(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "User");

            if (id <= 0)
                return BadRequest("ID đơn hàng không hợp lệ.");

            try
            {
                var order = _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.Items)
                        .ThenInclude(i => i.Product)
                    .FirstOrDefault(o => o.OrderId == id);

                if (order == null)
                {
                    _logger.LogWarning("AdminOrderController Details: Không tìm thấy đơn hàng #{OrderId}", id);
                    TempData["Error"] = $"Không tìm thấy đơn hàng #{id}.";
                    return RedirectToAction("Index");
                }

                ViewBag.ValidStatuses = ValidStatuses;
                _logger.LogInformation("AdminOrderController Details: Xem đơn hàng #{OrderId}", id);
                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AdminOrderController Details: Lỗi - {Message}", ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi tải chi tiết đơn hàng.";
                return RedirectToAction("Index");
            }
        }

        // ========== UPDATE STATUS: Cập nhật trạng thái đơn hàng ==========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateStatus(int id, string status)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "User");

            // WHITELIST VALIDATION: Chỉ chấp nhận trạng thái hợp lệ
            if (!ValidStatuses.Contains(status))
            {
                _logger.LogWarning("AdminOrderController UpdateStatus: Trạng thái không hợp lệ '{Status}' cho đơn #{OrderId}", status, id);
                TempData["Error"] = $"Trạng thái '{status}' không hợp lệ.";
                return RedirectToAction("Details", new { id });
            }

            try
            {
                var order = _context.Orders.FirstOrDefault(o => o.OrderId == id);
                if (order == null)
                {
                    _logger.LogWarning("AdminOrderController UpdateStatus: Không tìm thấy đơn hàng #{OrderId}", id);
                    TempData["Error"] = $"Không tìm thấy đơn hàng #{id}.";
                    return RedirectToAction("Index");
                }

                var oldStatus = order.Status;

                // LOGIC: Sync stock only if status actually changes to/from Cancelled
                using var transaction = _context.Database.BeginTransaction();
                try
                {
                    if (oldStatus != "Cancelled" && status == "Cancelled")
                    {
                        // Restore stock
                        var items = _context.OrderItems.Where(i => i.OrderId == id).ToList();
                        foreach (var item in items)
                        {
                            var product = _context.Products.Find(item.ProductId);
                            if (product != null)
                            {
                                product.Stock += item.Quantity;
                                _context.Products.Update(product);
                            }
                        }
                    }
                    else if (oldStatus == "Cancelled" && status != "Cancelled")
                    {
                        // Re-deduct stock if moving away from Cancelled
                        var items = _context.OrderItems.Where(i => i.OrderId == id).ToList();
                        foreach (var item in items)
                        {
                            var product = _context.Products.Find(item.ProductId);
                            if (product == null || product.Stock < item.Quantity)
                            {
                                transaction.Rollback();
                                TempData["Error"] = $"Không đủ hàng trong kho để thay đổi trạng thái đơn hàng này (Sản phẩm '{item.Product?.Name ?? item.ProductId.ToString()}' chỉ còn {product?.Stock ?? 0}).";
                                return RedirectToAction("Details", new { id });
                            }
                            product.Stock -= item.Quantity;
                            _context.Products.Update(product);
                        }
                    }

                    order.Status = status;
                    order.UpdatedAt = DateTime.Now;
                    _context.SaveChanges();
                    transaction.Commit();

                    var statusVn = status switch {
                        "Pending" => "Chờ xử lý",
                        "Completed" => "Hoàn thành",
                        "Cancelled" => "Đã hủy",
                        _ => status
                    };
                    TempData["Success"] = $"Đơn hàng #{id} đã được cập nhật thành '{statusVn}' và kho đã được đồng bộ.";
                    return RedirectToAction("Details", new { id });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    _logger.LogError(ex, "AdminOrderController UpdateStatus: Lỗi - {Message}", ex.Message);
                    TempData["Error"] = "Đã xảy ra lỗi khi cập nhật trạng thái và đồng bộ kho.";
                    return RedirectToAction("Details", new { id });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AdminOrderController UpdateStatus: Lỗi - {Message}", ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi cập nhật trạng thái.";
                return RedirectToAction("Details", new { id });
            }
        }
        // ========== DELETE: Xóa đơn hàng hoàn toàn ==========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "User");

            try
            {
                var order = _context.Orders
                    .Include(o => o.Items)
                    .FirstOrDefault(o => o.OrderId == id);

                if (order == null)
                {
                    TempData["Error"] = $"Không tìm thấy đơn hàng #{id} để xóa.";
                    return RedirectToAction("Index");
                }

                // LOGIC TỒN KHO: 
                // Chỉ hoàn kho nếu đơn ở trạng thái "Pending" hoặc "Completed" (đã trừ kho trước đó).
                // Nếu đơn đã "Cancelled" thì kho đã được hoàn rồi, không làm gì thêm.
                if (order.Status != "Cancelled")
                {
                    foreach (var item in order.Items)
                    {
                        var product = _context.Products.Find(item.ProductId);
                        if (product != null)
                        {
                            product.Stock += item.Quantity;
                            _context.Products.Update(product);
                        }
                    }
                }

                // Xóa các mục hàng trước
                _context.OrderItems.RemoveRange(order.Items);
                // Sau đó xóa đơn hàng
                _context.Orders.Remove(order);
                
                _context.SaveChanges();

                _logger.LogInformation("AdminOrderController Delete: Đã xóa đơn hàng #{OrderId}", id);
                TempData["Success"] = $"Đã xóa đơn hàng #{id} thành công và đồng bộ kho.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AdminOrderController Delete: Lỗi - {Message}", ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi xóa đơn hàng.";
            }

            return RedirectToAction("Index");
        }
    }
}
