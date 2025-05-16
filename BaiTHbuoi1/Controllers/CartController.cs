using BaiTHbuoi1.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BaiTHbuoi1.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager; // Add this field
        // Helper methods for cart session management

        public CartController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager) // Update constructor
        {
            _context = context;
            _userManager = userManager; // Initialize the field
        }

        // GET: Cart
        public async Task<IActionResult> Index()
        {
            var cart = GetCartFromSession();
            var cartItems = new List<CartItem>();

            foreach (var item in cart)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    cartItems.Add(new CartItem
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Price = product.DiscountPrice ?? product.Price,
                        Quantity = item.Quantity,
                        ImageUrl = product.ImageUrl
                    });
                }
            }

            return View(cartItems);
        }

        // POST: Cart/AddToCart
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return NotFound();
            }

            // Check if product is in stock
            if (product.Stock < quantity)
            {
                TempData["Error"] = "Không đủ số lượng sản phẩm trong kho.";
                return RedirectToAction("Details", "Product", new { id = productId });
            }

            var cart = GetCartFromSession();

            // Check if product already in cart
            var existingItem = cart.FirstOrDefault(i => i.ProductId == productId);
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = productId,
                    ProductName = product.Name,
                    Price = product.DiscountPrice ?? product.Price,
                    Quantity = quantity,
                    ImageUrl = product.ImageUrl
                });
            }

            SaveCartToSession(cart);

            TempData["Success"] = "Đã thêm sản phẩm vào giỏ hàng thành công.";
            return RedirectToAction("Index");
        }
        // POST: Cart/UpdateCart
        [HttpPost]
        public IActionResult UpdateCart(int[] productId, int[] quantity)
        {
            if (productId == null || quantity == null || productId.Length != quantity.Length)
            {
                TempData["Error"] = "Dữ liệu không hợp lệ. Vui lòng thử lại.";
                return RedirectToAction("Index");
            }

            var cart = GetCartFromSession();
            bool cartChanged = false;

            for (int i = 0; i < productId.Length; i++)
            {
                var item = cart.FirstOrDefault(c => c.ProductId == productId[i]);
                if (item != null)
                {
                    if (quantity[i] <= 0)
                    {
                        cart.Remove(item);
                        cartChanged = true;
                    }
                    else if (item.Quantity != quantity[i])
                    {
                        item.Quantity = quantity[i];
                        cartChanged = true;
                    }
                }
            }

            if (cartChanged)
            {
                SaveCartToSession(cart);
                TempData["Success"] = "Đã cập nhật giỏ hàng thành công.";
            }

            return RedirectToAction("Index");
        }

        // POST: Cart/RemoveFromCart
        [HttpPost]
        public IActionResult RemoveFromCart(int productId)
        {
            try
            {
                // Debug information
                Console.WriteLine($"Đã gọi RemoveFromCart với productId: {productId}");

                var cart = GetCartFromSession();
                Console.WriteLine($"Giỏ hàng trước khi xóa: {cart.Count} sản phẩm");

                var item = cart.FirstOrDefault(c => c.ProductId == productId);
                if (item != null)
                {
                    cart.Remove(item);
                    SaveCartToSession(cart);
                    Console.WriteLine($"Đã xóa sản phẩm. Giỏ hàng hiện có {cart.Count} sản phẩm");
                    TempData["Success"] = "Đã xóa sản phẩm khỏi giỏ hàng.";
                }
                else
                {
                    Console.WriteLine($"Không tìm thấy sản phẩm với productId {productId} trong giỏ hàng");
                    TempData["Error"] = "Không tìm thấy sản phẩm trong giỏ hàng.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi trong RemoveFromCart: {ex.Message}");
                TempData["Error"] = "Đã xảy ra lỗi khi xóa sản phẩm.";
            }

            return RedirectToAction("Index");
        }

        // GET: Cart/Checkout
        [HttpGet]
        public IActionResult Checkout()
        {
            // Check if user is authenticated
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                TempData["Error"] = "Vui lòng đăng nhập để thanh toán.";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Checkout", "Cart") });
            }

            var cart = GetCartFromSession();
            if (cart.Count == 0)
            {
                TempData["Error"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index");
            }

            return View();
        }

        // POST: Cart/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(Order order)
        {
            // Check if user is authenticated
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                TempData["Error"] = "Vui lòng đăng nhập để thanh toán.";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Checkout", "Cart") });
            }

            var cart = GetCartFromSession();
            if (cart.Count == 0)
            {
                TempData["Error"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                // Get current user
                var userId = _userManager.GetUserId(User);
                if (userId == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                // Create order
                order.UserId = userId;
                order.OrderDate = DateTime.Now;
                order.OrderStatus = "Pending";
                order.PaymentStatus = "Pending";

                // Calculate total amount
                decimal totalAmount = 0;
                var orderItems = new List<OrderItem>();

                foreach (var item in cart)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        // Check if product is in stock
                        if (product.Stock < item.Quantity)
                        {
                            TempData["Error"] = $"Không đủ số lượng trong kho cho sản phẩm {product.Name}.";
                            return RedirectToAction("Index");
                        }

                        var price = product.DiscountPrice ?? product.Price;
                        var subtotal = price * item.Quantity;
                        totalAmount += subtotal;

                        var orderItem = new OrderItem
                        {
                            ProductId = product.Id,
                            Quantity = item.Quantity,
                            UnitPrice = price,
                            Subtotal = subtotal
                        };

                        orderItems.Add(orderItem);

                        // Update product stock
                        product.Stock -= item.Quantity;
                        _context.Update(product);
                    }
                }

                order.TotalAmount = totalAmount;
                order.OrderItems = orderItems;

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Clear cart
                HttpContext.Session.Remove("Cart");

                TempData["Success"] = "Đơn hàng của bạn đã được đặt thành công.";
                return RedirectToAction("OrderConfirmation", new { id = order.Id });
            }

            return View(order);
        }

        // GET: Cart/OrderConfirmation/5
        public async Task<IActionResult> OrderConfirmation(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // Helper methods for cart session management
        private List<CartItem> GetCartFromSession()
        {
            var session = HttpContext.Session;
            var cartJson = session.GetString("Cart");

            if (string.IsNullOrEmpty(cartJson))
            {
                return new List<CartItem>();
            }

            return JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
        }

        private void SaveCartToSession(List<CartItem> cart)
        {
            var session = HttpContext.Session;
            var cartJson = JsonSerializer.Serialize(cart);
            session.SetString("Cart", cartJson);
        }
    }

    // Helper class for cart items
    public class CartItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string? ImageUrl { get; set; }
        public decimal Subtotal => Price * Quantity;
    }
}
