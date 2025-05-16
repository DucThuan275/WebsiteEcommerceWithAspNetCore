using BaiTHbuoi1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace BaiTHbuoi1.Controllers
{
    public class NewsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NewsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: News
        public async Task<IActionResult> Index(int page = 1)
        {
            var pageSize = 5;

            var newsQuery = _context.News
                .Where(n => n.IsPublished)
                .OrderByDescending(n => n.PublishDate)
                .AsQueryable();

            var totalItems = await newsQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            var news = await newsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(news);
        }

        // GET: News/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var news = await _context.News
                .FirstOrDefaultAsync(m => m.Id == id && m.IsPublished);

            if (news == null)
            {
                return NotFound();
            }

            // Get related news
            var relatedNews = await _context.News
                .Where(n => n.Id != news.Id && n.IsPublished)
                .OrderByDescending(n => n.PublishDate)
                .Take(3)
                .ToListAsync();

            ViewBag.RelatedNews = relatedNews;

            return View(news);
        }
    }
}