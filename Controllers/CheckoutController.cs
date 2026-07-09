using FreshMart.Helpers;
using FreshMart.Models;
using FreshMart.Services;
using Microsoft.AspNetCore.Mvc;

namespace FreshMart.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CheckoutController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // SHOW CHECKOUT PAGE
        // =========================
        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "User");

            var user = _context.Users.Find(userId);
            if (user == null)
                return RedirectToAction("Login", "User");

            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new();

            ViewBag.Cart = cart;
            ViewBag.CartTotal = cart.Sum(c => c.Price * c.Quantity);

            var model = new CheckoutViewModel
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
            };

            return View(model);
        }

        // =========================
        // PLACE ORDER (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PlaceOrder(CheckoutViewModel model)
        {
            var sessionUserId = HttpContext.Session.GetInt32("UserId");
            if (sessionUserId == null)
                return RedirectToAction("Login", "User");

            model.UserId = sessionUserId.Value;

            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new();
            if (!cart.Any())
                return RedirectToAction("Index", "Cart");

            if (!ModelState.IsValid)
            {
                ViewBag.Cart = cart;
                ViewBag.CartTotal = cart.Sum(c => c.Price * c.Quantity);
                return View("Index", model);
            }

            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var order = new Order
                {
                    UserId = model.UserId,
                    OrderDate = DateTime.Now,
                    PaymentMethod = model.PaymentMethod,
                    TotalAmount = cart.Sum(c => c.Price * c.Quantity),
                    Status = "Pending",
                    FullName = model.FullName,
                    Email = model.Email,
                    Address = model.Address,
                    Phone = model.Phone
                };

                _context.Orders.Add(order);
                _context.SaveChanges();

                foreach (var item in cart)
                {
                    // RE-VALIDATE STOCK AT CHECKOUT TIME
                    var product = _context.Products.Find(item.ProductId);
                    if (product == null || product.Stock < item.Quantity)
                    {
                        transaction.Rollback();
                        TempData["Error"] = $"Sản phẩm '{item.ProductName}' không đủ hàng trong kho (Còn {product?.Stock ?? 0}). Vui lòng cập nhật lại giỏ hàng.";
                        return RedirectToAction("Index", "Cart");
                    }

                    // DEDUCT STOCK
                    product.Stock -= item.Quantity;
                    _context.Products.Update(product);

                    _context.OrderItems.Add(new OrderItem
                    {
                        OrderId = order.OrderId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Price = item.Price
                    });
                }

                _context.SaveChanges();
                transaction.Commit();

                HttpContext.Session.Remove("Cart");
                return RedirectToAction("Success", new { id = order.OrderId });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                // Log exception
                TempData["Error"] = "Đã xảy ra lỗi khi tạo đơn hàng. Vui lòng thử lại.";
                return RedirectToAction("Index", "Cart");
            }
        }

        // =========================
        // SUCCESS PAGE
        // =========================
        public IActionResult Success(int id)
        {
            var order = _context.Orders.Find(id);
            if (order == null)
                return NotFound();

            return View(order);
        }

        // =========================
        // RECEIPT (PDF)
        // =========================
        public IActionResult Receipt(int id)
        {
            var order = _context.Orders.Find(id);
            if (order == null)
                return NotFound();

            var items = _context.OrderItems.Where(i => i.OrderId == id).ToList();
            var products = _context.Products.ToList();

            var service = new ReceiptService();
            var pdfBytes = service.GenerateReceipt(order, items, products);

            return File(pdfBytes, "application/pdf", $"Receipt_{order.OrderId}.pdf");
        }

        // =========================
        // MY ORDERS (Order History)
        // =========================
        public IActionResult MyOrders()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "User");

            var orders = _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            return View(orders);
        }

        // =========================
        // ORDER DETAILS
        // =========================
        public IActionResult OrderDetails(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "User");

            var order = _context.Orders.FirstOrDefault(o => o.OrderId == id && o.UserId == userId);
            if (order == null)
                return NotFound();

            ViewBag.Items = _context.OrderItems.Where(i => i.OrderId == id).ToList();
            ViewBag.Products = _context.Products.ToList();

            return View(order);
        }

        // =========================
        // CANCEL ORDER
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelOrder(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "User");

            var order = _context.Orders.FirstOrDefault(o => o.OrderId == id && o.UserId == userId);
            if (order == null)
                return NotFound();

            if (order.Status != "Pending")
            {
                TempData["Error"] = "Chỉ có thể hủy đơn hàng đang chờ xử lý.";
                return RedirectToAction("OrderDetails", new { id });
            }

            using var transaction = _context.Database.BeginTransaction();
            try
            {
                // RESTORE STOCK
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

                order.Status = "Cancelled";
                order.UpdatedAt = DateTime.Now;
                _context.SaveChanges();
                transaction.Commit();

                TempData["Success"] = "Đã hủy đơn hàng thành công và hoàn trả số lượng vào kho.";
                return RedirectToAction("MyOrders");
            }
            catch (Exception)
            {
                transaction.Rollback();
                TempData["Error"] = "Đã xảy ra lỗi khi hủy đơn hàng.";
                return RedirectToAction("OrderDetails", new { id });
            }
        }

        // =========================
        // UPDATE ORDER STATUS (Admin)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateStatus(int id, string status)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
                return RedirectToAction("Login", "User");

            var order = _context.Orders.Find(id);
            if (order == null)
                return NotFound();

            if (status == "Pending" || status == "Completed" || status == "Cancelled")
            {
                var oldStatus = order.Status;
                
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
                        // Re-deduct stock
                        var items = _context.OrderItems.Where(i => i.OrderId == id).ToList();
                        foreach (var item in items)
                        {
                            var product = _context.Products.Find(item.ProductId);
                            if (product == null || product.Stock < item.Quantity)
                            {
                                transaction.Rollback();
                                TempData["Error"] = $"Không đủ hàng trong kho để thay đổi trạng thái đơn hàng này (Còn {product?.Stock ?? 0}).";
                                return RedirectToAction("OrderDetails", new { id });
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
                    TempData["Success"] = $"Đã cập nhật trạng thái đơn hàng #{id} thành '{statusVn}' và đồng bộ kho.";
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    TempData["Error"] = "Đã xảy ra lỗi khi cập nhật trạng thái đơn hàng.";
                }
            }

            return RedirectToAction("OrderDetails", new { id });
        }
    }
}
