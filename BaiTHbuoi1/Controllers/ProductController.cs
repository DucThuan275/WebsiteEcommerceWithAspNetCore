using BaiTHbuoi1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BaiTHbuoi1.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Product
        public async Task<IActionResult> Index(int? categoryId, string searchString, string sort = "newest", int page = 1)
        {
            var pageSize = 12;
            // Start with all products
            var productsQuery = _context.Products
                .Where(p => p.IsActive)
                .Include(p => p.Category)
                .AsQueryable();

            // Filter by category if provided
            if (categoryId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId);
                ViewBag.CategoryId = categoryId;
                var category = await _context.Categories.FindAsync(categoryId);
                if (category != null)
                {
                    ViewBag.CategoryName = category.Name;
                }
            }

            // Filter by search string if provided
            if (!string.IsNullOrEmpty(searchString))
            {
                productsQuery = productsQuery.Where(p =>
                    p.Name.Contains(searchString) ||
                    (p.Description != null && p.Description.Contains(searchString)));
                ViewBag.SearchString = searchString;
            }

            // Order by selected sort option
            switch (sort)
            {
                case "price-asc":
                    productsQuery = productsQuery.OrderBy(p => p.DiscountPrice ?? p.Price);
                    break;
                case "price-desc":
                    productsQuery = productsQuery.OrderByDescending(p => p.DiscountPrice ?? p.Price);
                    break;
                case "name-asc":
                    productsQuery = productsQuery.OrderBy(p => p.Name);
                    break;
                case "name-desc":
                    productsQuery = productsQuery.OrderByDescending(p => p.Name);
                    break;
                default: // "newest"
                    productsQuery = productsQuery.OrderByDescending(p => p.CreatedAt);
                    break;
            }

            ViewBag.Sort = sort;

            // Get total count for pagination
            var totalItems = await productsQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Ensure page is within valid range
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            // Get paginated products
            var products = await productsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Set up pagination info
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            // Get all categories for sidebar
            ViewBag.Categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            return View(products);
        }

        // GET: Product/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            // Get related products from same category
            var relatedProducts = await _context.Products
                .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id && p.IsActive)
                .Take(4)
                .ToListAsync();

            ViewBag.RelatedProducts = relatedProducts;

            return View(product);
        }
    }
}
