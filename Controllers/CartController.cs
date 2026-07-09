using FreshMart.Models;
using FreshMart.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FreshMart.Controllers
{
    /// <summary>
    /// Shopping cart controller with comprehensive validation
    /// </summary>
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CartController> _logger;
        private const int MAX_QUANTITY_PER_ITEM = 100;

        public CartController(ApplicationDbContext context, ILogger<CartController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Display shopping cart
        /// </summary>
        public IActionResult Index()
        {
            try
            {
                var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();
                _logger.LogInformation("Cart view accessed. Items in cart: {ItemCount}", cart.Count);
                return View(cart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accessing cart - {Message}", ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi tải giỏ hàng của bạn";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(int id)
        {
            try
            {
                // VALIDATE PRODUCT ID
                if (id <= 0)
                {
                    _logger.LogWarning("Cart Add: Invalid product ID - {ProductId}", id);
                    TempData["Error"] = "ID sản phẩm không hợp lệ";
                    return RedirectToAction("Index", "Home");
                }

                // RETRIEVE PRODUCT
                var product = _context.Products.FirstOrDefault(p => p.ProductId == id);
                if (product == null)
                {
                    _logger.LogWarning("Cart Add: Product not found - {ProductId}", id);
                    TempData["Error"] = "Không tìm thấy sản phẩm";
                    return NotFound();
                }

                // CHECK STOCK
                if (product.Stock <= 0)
                {
                    _logger.LogWarning("Cart Add: Product out of stock - {ProductId}", id);
                    string errMsg = "Sản phẩm này hiện đang hết hàng.";
                    if (IsAjax()) return Json(new { success = false, message = errMsg });
                    TempData["Error"] = errMsg;
                    return RedirectToAction("Details", "Products", new { id });
                }

                // GET OR CREATE CART
                var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();

                // CHECK IF ITEM ALREADY IN CART
                var existingItem = cart.FirstOrDefault(c => c.ProductId == id);

                if (existingItem != null)
                {
                    if (existingItem.Quantity + 1 > product.Stock)
                    {
                        string msg = $"Không đủ hàng trong kho. Chúng tôi chỉ còn {product.Stock} sản phẩm.";
                        if (IsAjax()) return Json(new { success = false, message = msg });
                        TempData["Error"] = msg;
                        return RedirectToAction("Index", "Cart");
                    }
                    existingItem.Quantity++;
                }
                else
                {
                    cart.Add(new CartItem
                    {
                        ProductId = product.ProductId,
                        ProductName = product.Name,
                        Name = product.Name,
                        Price = product.Price,
                        ImagePath = product.ImagePath,
                        Quantity = 1
                    });
                }

                // SAVE CART TO SESSION
                HttpContext.Session.SetObject("Cart", cart);
                
                string successMessage = "Đã thêm sản phẩm vào giỏ hàng thành công";
                
                if (IsAjax())
                {
                    var cartTotal = cart.Sum(i => i.Price * i.Quantity);
                    var itemCount = cart.Sum(i => i.Quantity);
                    return Json(new { 
                        success = true, 
                        message = successMessage,
                        itemCount = itemCount,
                        cartTotal = cartTotal.ToString("N0") + " ₫"
                    });
                }

                TempData["Success"] = successMessage;
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cart Add: Unexpected error - {Message}", ex.Message);
                string errorMessage = "Đã xảy ra lỗi khi thêm vào giỏ hàng";
                
                if (IsAjax()) return Json(new { success = false, message = errorMessage });
                
                TempData["Error"] = errorMessage;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BuyNow(int id)
        {
            try
            {
                // VALIDATE PRODUCT ID
                if (id <= 0)
                {
                    _logger.LogWarning("BuyNow: Invalid product ID - {ProductId}", id);
                    TempData["Error"] = "ID sản phẩm không hợp lệ";
                    return RedirectToAction("Index", "Home");
                }

                // RETRIEVE PRODUCT
                var product = _context.Products.FirstOrDefault(p => p.ProductId == id);
                if (product == null)
                {
                    _logger.LogWarning("BuyNow: Product not found - {ProductId}", id);
                    TempData["Error"] = "Không tìm thấy sản phẩm";
                    return NotFound();
                }

                // CHECK STOCK
                if (product.Stock <= 0)
                {
                    _logger.LogWarning("BuyNow: Product out of stock - {ProductId}", id);
                    string errMsg = "Sản phẩm này hiện đang hết hàng.";
                    if (IsAjax()) return Json(new { success = false, message = errMsg });
                    TempData["Error"] = errMsg;
                    return RedirectToAction("Details", "Products", new { id });
                }

                // GET OR CREATE CART
                var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();

                // CHECK IF ITEM ALREADY IN CART
                var existingItem = cart.FirstOrDefault(c => c.ProductId == id);

                if (existingItem != null)
                {
                    if (existingItem.Quantity + 1 > product.Stock)
                    {
                        string msg = $"Không đủ hàng trong kho. Chúng tôi chỉ còn {product.Stock} sản phẩm.";
                        if (IsAjax()) return Json(new { success = false, message = msg });
                        TempData["Error"] = msg;
                        return RedirectToAction("Index", "Cart");
                    }
                    existingItem.Quantity++;
                }
                else
                {
                    cart.Add(new CartItem
                    {
                        ProductId = product.ProductId,
                        ProductName = product.Name,
                        Name = product.Name,
                        Price = product.Price,
                        ImagePath = product.ImagePath,
                        Quantity = 1
                    });
                }

                // SAVE CART TO SESSION
                HttpContext.Session.SetObject("Cart", cart);
                
                if (IsAjax())
                {
                    return Json(new { 
                        success = true, 
                        redirectUrl = Url.Action("Index", "Checkout")
                    });
                }

                return RedirectToAction("Index", "Checkout");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BuyNow: Error - {Message}", ex.Message);
                if (IsAjax()) return Json(new { success = false, message = "Lỗi khi xử lý mua ngay" });
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Remove item from cart
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int id)
        {
            try
            {
                // VALIDATE PRODUCT ID
                if (id <= 0)
                {
                    _logger.LogWarning("Cart Remove: Invalid product ID - {ProductId}", id);
                    TempData["Error"] = "ID sản phẩm không hợp lệ";
                    return RedirectToAction("Index");
                }

                var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();
                var item = cart.FirstOrDefault(c => c.ProductId == id);

                if (item != null)
                {
                    cart.Remove(item);
                    HttpContext.Session.SetObject("Cart", cart);
                    _logger.LogInformation("Cart Remove: Item removed - ProductId: {ProductId}", id);
                    
                    string msg = "Đã xóa sản phẩm khỏi giỏ hàng";
                    if (IsAjax())
                    {
                        var cartTotal = cart.Sum(i => i.Price * i.Quantity);
                        var itemCount = cart.Sum(i => i.Quantity);
                        return Json(new { 
                            success = true, 
                            message = msg,
                            cartTotal = cartTotal.ToString("N0") + " ₫",
                            itemCount = itemCount
                        });
                    }
                    TempData["Success"] = msg;
                }
                
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cart Remove: Error - {Message}", ex.Message);
                if (IsAjax()) return Json(new { success = false, message = "Lỗi khi xóa sản phẩm" });
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Increase item quantity with validation
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Increase(int id)
        {
            try
            {
                // VALIDATE PRODUCT ID
                if (id <= 0)
                {
                    _logger.LogWarning("Cart Increase: Invalid product ID - {ProductId}", id);
                    TempData["Error"] = "ID sản phẩm không hợp lệ";
                    return RedirectToAction("Index");
                }

                var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();
                var item = cart.FirstOrDefault(c => c.ProductId == id);

                if (item != null)
                {
                    var product = _context.Products.Find(id);
                    if (product == null)
                    {
                        if (IsAjax()) return Json(new { success = false, message = "Sản phẩm không tồn tại." });
                        TempData["Error"] = "Sản phẩm không tồn tại.";
                        return RedirectToAction("Index");
                    }

                    if (item.Quantity + 1 > product.Stock)
                    {
                        string msg = $"Không đủ hàng trong kho. Chúng tôi chỉ còn {product.Stock} sản phẩm.";
                        if (IsAjax()) return Json(new { success = false, message = msg });
                        TempData["Error"] = msg;
                        return RedirectToAction("Index");
                    }

                    item.Quantity++;
                    HttpContext.Session.SetObject("Cart", cart);
                    
                    if (IsAjax())
                    {
                        var cartTotal = cart.Sum(i => i.Price * i.Quantity);
                        var itemCount = cart.Sum(i => i.Quantity);
                        return Json(new { 
                            success = true, 
                            quantity = item.Quantity,
                            lineTotal = (item.Price * item.Quantity).ToString("N0") + " ₫",
                            cartTotal = cartTotal.ToString("N0") + " ₫",
                            itemCount = itemCount
                        });
                    }
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                if (IsAjax()) return Json(new { success = false, message = "Lỗi cập nhật số lượng" });
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Decrease item quantity with validation
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Decrease(int id)
        {
            try
            {
                // VALIDATE PRODUCT ID
                if (id <= 0)
                {
                    _logger.LogWarning("Cart Decrease: Invalid product ID - {ProductId}", id);
                    TempData["Error"] = "ID sản phẩm không hợp lệ";
                    return RedirectToAction("Index");
                }

                var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();
                var item = cart.FirstOrDefault(c => c.ProductId == id);

                if (item != null)
                {
                    bool removed = false;
                    if (item.Quantity > 1)
                    {
                        item.Quantity--;
                    }
                    else
                    {
                        cart.Remove(item);
                        removed = true;
                    }

                    HttpContext.Session.SetObject("Cart", cart);
                    
                    if (IsAjax())
                    {
                        var cartTotal = cart.Sum(i => i.Price * i.Quantity);
                        var itemCount = cart.Sum(i => i.Quantity);
                        return Json(new { 
                            success = true, 
                            removed = removed,
                            quantity = item.Quantity,
                            lineTotal = (item.Price * item.Quantity).ToString("N0") + " ₫",
                            cartTotal = cartTotal.ToString("N0") + " ₫",
                            itemCount = itemCount
                        });
                    }
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                if (IsAjax()) return Json(new { success = false, message = "Lỗi cập nhật số lượng" });
                return RedirectToAction("Index");
            }
        }

        private bool IsAjax()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }
    }
}
