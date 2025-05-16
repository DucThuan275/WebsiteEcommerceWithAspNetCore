using BaiTHbuoi1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BaiTHbuoi1.Areas.Admin.Controllers
{
    /// <summary>
    /// Controller quản lý tin tức trong khu vực Admin
    /// </summary>
    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    public class NewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public NewsController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        /// <summary>
        /// Hiển thị danh sách tất cả tin tức
        /// </summary>
        public async Task<IActionResult> Index()
        {
            return View(await _context.News.OrderByDescending(n => n.PublishDate).ToListAsync());
        }

        /// <summary>
        /// Hiển thị chi tiết của một tin tức cụ thể
        /// </summary>
        /// <param name="id">ID của tin tức cần xem</param>
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var news = await _context.News
                .FirstOrDefaultAsync(m => m.Id == id);

            if (news == null)
            {
                return NotFound();
            }

            return View(news);
        }

        /// <summary>
        /// Hiển thị form tạo tin tức mới
        /// </summary>
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Xử lý việc tạo tin tức mới từ dữ liệu form
        /// </summary>
        /// <param name="news">Dữ liệu tin tức từ form</param>
        /// <param name="imageFile">File hình ảnh đại diện cho tin tức</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(News news, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                // Xử lý tải lên hình ảnh
                if (imageFile != null && imageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "news");
                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // Tạo thư mục nếu chưa tồn tại
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }

                    news.ImageUrl = "/images/news/" + uniqueFileName;
                }

                // Đặt ngày xuất bản là ngày hiện tại nếu không được cung cấp
                if (news.PublishDate == default)
                {
                    news.PublishDate = DateTime.Now;
                }

                // Đặt tác giả nếu không được cung cấp
                if (string.IsNullOrEmpty(news.Author))
                {
                    news.Author = User.Identity?.Name ?? "Admin";
                }

                _context.Add(news);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Tin tức đã được tạo thành công.";
                return RedirectToAction(nameof(Index));
            }
            return View(news);
        }

        /// <summary>
        /// Hiển thị form chỉnh sửa tin tức
        /// </summary>
        /// <param name="id">ID của tin tức cần chỉnh sửa</param>
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var news = await _context.News.FindAsync(id);
            if (news == null)
            {
                return NotFound();
            }
            return View(news);
        }

        /// <summary>
        /// Xử lý việc cập nhật tin tức từ dữ liệu form
        /// </summary>
        /// <param name="id">ID của tin tức cần cập nhật</param>
        /// <param name="news">Dữ liệu tin tức từ form</param>
        /// <param name="imageFile">File hình ảnh mới (nếu có)</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, News news, IFormFile? imageFile)
        {
            if (id != news.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Xử lý tải lên hình ảnh
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "news");
                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        // Tạo thư mục nếu chưa tồn tại
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(fileStream);
                        }

                        // Xóa hình ảnh cũ nếu tồn tại
                        if (!string.IsNullOrEmpty(news.ImageUrl))
                        {
                            var oldImagePath = Path.Combine(_hostEnvironment.WebRootPath, news.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        news.ImageUrl = "/images/news/" + uniqueFileName;
                    }

                    _context.Update(news);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Tin tức đã được cập nhật thành công.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NewsExists(news.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(news);
        }

        /// <summary>
        /// Hiển thị trang xác nhận xóa tin tức
        /// </summary>
        /// <param name="id">ID của tin tức cần xóa</param>
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var news = await _context.News
                .FirstOrDefaultAsync(m => m.Id == id);

            if (news == null)
            {
                return NotFound();
            }

            return View(news);
        }

        /// <summary>
        /// Xử lý việc xóa tin tức
        /// </summary>
        /// <param name="id">ID của tin tức cần xóa</param>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var news = await _context.News.FindAsync(id);
            if (news == null)
            {
                return NotFound();
            }

            // Xóa file hình ảnh nếu tồn tại
            if (!string.IsNullOrEmpty(news.ImageUrl))
            {
                var imagePath = Path.Combine(_hostEnvironment.WebRootPath, news.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            _context.News.Remove(news);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Tin tức đã được xóa thành công.";
            return RedirectToAction(nameof(Index));
        }

        private bool NewsExists(int id)
        {
            return _context.News.Any(e => e.Id == id);
        }
    }
}
