using BaiTHbuoi1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace BaiTHbuoi1.Areas.Admin.Controllers
{
    /// <summary>
    /// Controller quản lý đơn hàng trong khu vực Admin
    /// </summary>
    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Hiển thị danh sách đơn hàng với khả năng lọc theo trạng thái và tìm kiếm
        /// </summary>
        /// <param name="status">Trạng thái đơn hàng cần lọc</param>
        /// <param name="searchString">Chuỗi tìm kiếm</param>
        public async Task<IActionResult> Index(string status = null, string searchString = null)
        {
            var ordersQuery = _context.Orders
                .Include(o => o.User)
                .AsQueryable();

            // Lọc theo trạng thái nếu được cung cấp
            if (!string.IsNullOrEmpty(status))
            {
                ordersQuery = ordersQuery.Where(o => o.OrderStatus == status);
                ViewBag.CurrentStatus = status;
            }

            // Lọc theo chuỗi tìm kiếm nếu được cung cấp
            if (!string.IsNullOrEmpty(searchString))
            {
                ordersQuery = ordersQuery.Where(o =>
                    o.Id.ToString().Contains(searchString) ||
                    o.User.Email.Contains(searchString) ||
                    o.User.FirstName.Contains(searchString) ||
                    o.User.LastName.Contains(searchString) ||
                    o.ShippingName.Contains(searchString));

                ViewBag.SearchString = searchString;
            }

            var orders = await ordersQuery
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // Lấy danh sách trạng thái đơn hàng cho dropdown lọc
            ViewBag.OrderStatuses = new List<string> { "Đang chờ", "Đang xử lý", "Đã giao hàng", "Đã nhận hàng", "Đã hủy" };

            return View(orders);
        }

        /// <summary>
        /// Hiển thị chi tiết của một đơn hàng cụ thể
        /// </summary>
        /// <param name="id">ID của đơn hàng cần xem</param>
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        /// <summary>
        /// Cập nhật trạng thái của đơn hàng
        /// </summary>
        /// <param name="id">ID của đơn hàng cần cập nhật</param>
        /// <param name="status">Trạng thái mới</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            // Cập nhật trạng thái đơn hàng
            order.OrderStatus = status;

            // Nếu đơn hàng đã giao, cập nhật trạng thái thanh toán thành hoàn thành
            if (status == "Đã nhận hàng")
            {
                order.PaymentStatus = "Đã thanh toán";
            }

            _context.Update(order);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Trạng thái đơn hàng đã được cập nhật thành công.";
            return RedirectToAction(nameof(Details), new { id = id });
        }
    }
}
