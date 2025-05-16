using BaiTHbuoi1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace BaiTHbuoi1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var students = await _context.asp_mssv.ToListAsync();
            return View(students);
            // Get sliders
            var sliders = await _context.Sliders
                .Where(s => s.IsActive)
                .OrderBy(s => s.DisplayOrder)
                .ToListAsync();

            ViewBag.Sliders = sliders;

            // Get categories
            var categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .Take(6)
                .ToListAsync();

            ViewBag.Categories = categories;

            // Get featured products
            var featuredProducts = await _context.Products
                .Where(p => p.IsActive && p.IsFeatured)
                .Include(p => p.Category)
                .Take(8)
                .ToListAsync();

            ViewBag.FeaturedProducts = featuredProducts;

            // Get new arrivals
            var newArrivals = await _context.Products
                .Where(p => p.IsActive && p.IsNewArrival)
                .Include(p => p.Category)
                .OrderByDescending(p => p.CreatedAt)
                .Take(8)
                .ToListAsync();

            ViewBag.NewArrivals = newArrivals;

            // Get best sellers
            var bestSellers = await _context.Products
                .Where(p => p.IsActive && p.IsBestSeller)
                .Include(p => p.Category)
                .Take(8)
                .ToListAsync();

            ViewBag.BestSellers = bestSellers;

            // Get on sale products
            var onSaleProducts = await _context.Products
                .Where(p => p.IsActive && p.IsOnSale && p.DiscountPrice.HasValue)
                .Include(p => p.Category)
                .Take(8)
                .ToListAsync();

            ViewBag.OnSaleProducts = onSaleProducts;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}