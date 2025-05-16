using BaiTHbuoi1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BaiTHbuoi1.Areas.Admin.Controllers
{
    /// <summary>
    /// Controller quản lý danh mục sản phẩm trong khu vực Admin
    /// </summary>
    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public CategoryController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        /// <summary>
        /// Hiển thị danh sách tất cả các danh mục sản phẩm
        /// </summary>
        public async Task<IActionResult> Index()
        {
            return View(await _context.Categories.OrderBy(c => c.DisplayOrder).ToListAsync());
        }

        /// <summary>
        /// Hiển thị chi tiết của một danh mục cụ thể
        /// </summary>
        /// <param name="id">ID của danh mục cần xem</param>
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(m => m.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        /// <summary>
        /// Hiển thị form tạo danh mục mới
        /// </summary>
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Xử lý việc tạo danh mục mới từ dữ liệu form
        /// </summary>
        /// <param name="category">Dữ liệu danh mục từ form</param>
        /// <param name="imageFile">File hình ảnh đại diện cho danh mục</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                // Xử lý tải lên hình ảnh
                if (imageFile != null && imageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "categories");
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

                    category.ImageUrl = "/images/categories/" + uniqueFileName;
                }

                _context.Add(category);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Danh mục đã được tạo thành công.";
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        /// <summary>
        /// Hiển thị form chỉnh sửa danh mục
        /// </summary>
        /// <param name="id">ID của danh mục cần chỉnh sửa</param>
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        /// <summary>
        /// Xử lý việc cập nhật danh mục từ dữ liệu form
        /// </summary>
        /// <param name="id">ID của danh mục cần cập nhật</param>
        /// <param name="category">Dữ liệu danh mục từ form</param>
        /// <param name="imageFile">File hình ảnh mới (nếu có)</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category, IFormFile? imageFile)
        {
            if (id != category.Id)
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
                        var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "categories");
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
                        if (!string.IsNullOrEmpty(category.ImageUrl))
                        {
                            var oldImagePath = Path.Combine(_hostEnvironment.WebRootPath, category.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        category.ImageUrl = "/images/categories/" + uniqueFileName;
                    }

                    _context.Update(category);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Danh mục đã được cập nhật thành công.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.Id))
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
            return View(category);
        }

        /// <summary>
        /// Hiển thị trang xác nhận xóa danh mục
        /// </summary>
        /// <param name="id">ID của danh mục cần xóa</param>
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(m => m.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            return View(category); // Trả về một danh mục, không phải danh sách
        }

        /// <summary>
        /// Xử lý việc xóa danh mục
        /// </summary>
        /// <param name="id">ID của danh mục cần xóa</param>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            // Kiểm tra xem danh mục có sản phẩm không
            var hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == id);
            if (hasProducts)
            {
                TempData["Error"] = "Không thể xóa danh mục vì có sản phẩm liên quan.";
                return RedirectToAction(nameof(Index));
            }

            // Xóa file hình ảnh nếu tồn tại
            if (!string.IsNullOrEmpty(category.ImageUrl))
            {
                var imagePath = Path.Combine(_hostEnvironment.WebRootPath, category.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Danh mục đã được xóa thành công.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Kiểm tra xem danh mục có tồn tại không
        /// </summary>
        /// <param name="id">ID của danh mục cần kiểm tra</param>
        /// <returns>True nếu danh mục tồn tại, ngược lại là False</returns>
        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }
    }
}
