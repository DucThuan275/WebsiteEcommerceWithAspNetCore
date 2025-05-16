using BaiTHbuoi1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace BaiTHbuoi1.Areas.Admin.Controllers
{
    /// <summary>
    /// Controller quản lý trang chủ Admin, hiển thị tổng quan về hệ thống
    /// </summary>
    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Hiển thị trang tổng quan với các thống kê và dữ liệu quan trọng
        /// </summary>
        public async Task<IActionResult> Index()
        {
            // Lấy số lượng cho dashboard
            ViewBag.TotalProducts = await _context.Products.CountAsync();
            ViewBag.TotalCategories = await _context.Categories.CountAsync();
            ViewBag.TotalOrders = await _context.Orders.CountAsync();
            ViewBag.TotalCustomers = await _context.Users.CountAsync();

            // Lấy đơn hàng gần đây
            ViewBag.RecentOrders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync();

            // Lấy sản phẩm sắp hết hàng
            ViewBag.LowStockProducts = await _context.Products
                .Where(p => p.Stock <= 10)
                .Take(5)
                .ToListAsync();

            // Lấy số lượng liên hệ chưa đọc
            ViewBag.UnreadContacts = await _context.Contacts
                .Where(c => !c.IsRead)
                .CountAsync();

            return View();
        }
    }
}
