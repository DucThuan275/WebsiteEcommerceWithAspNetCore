using BaiTHbuoi1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Threading.Tasks;

namespace BaiTHbuoi1.Controllers
{
    /// <summary>
    /// Controller quản lý slider trong khu vực Admin
    /// </summary>
    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    public class SliderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public SliderController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        /// <summary>
        /// Hiển thị danh sách tất cả slider
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var sliders = await _context.Sliders.OrderBy(s => s.DisplayOrder).ToListAsync();
            return View(sliders);
        }

        /// <summary>
        /// Hiển thị chi tiết của một slider cụ thể
        /// </summary>
        /// <param name="id">ID của slider cần xem</param>
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var slider = await _context.Sliders
                .FirstOrDefaultAsync(m => m.Id == id);
            if (slider == null)
            {
                return NotFound();
            }

            return View(slider);
        }

        /// <summary>
        /// Hiển thị form tạo slider mới
        /// </summary>
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Xử lý việc tạo slider mới từ dữ liệu form
        /// </summary>
        /// <param name="slider">Dữ liệu slider từ form</param>
        /// <param name="imageFile">File hình ảnh cho slider</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Subtitle,LinkUrl,DisplayOrder,IsActive")] Slider slider, IFormFile? imageFile)
        {
            // Kiểm tra xem file hình ảnh có được cung cấp không
            if (imageFile == null || imageFile.Length == 0)
            {
                ModelState.AddModelError("", "Hình ảnh là bắt buộc");
                return View(slider);
            }

            if (ModelState.IsValid)
            {
                // Xử lý tải lên hình ảnh
                string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "sliders");
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Đảm bảo thư mục tồn tại
                Directory.CreateDirectory(uploadsFolder);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }

                slider.ImageUrl = "/images/sliders/" + uniqueFileName;

                _context.Add(slider);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Slider đã được tạo thành công.";
                return RedirectToAction(nameof(Index));
            }
            return View(slider);
        }

        /// <summary>
        /// Hiển thị form chỉnh sửa slider
        /// </summary>
        /// <param name="id">ID của slider cần chỉnh sửa</param>
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var slider = await _context.Sliders.FindAsync(id);
            if (slider == null)
            {
                return NotFound();
            }
            return View(slider);
        }

        /// <summary>
        /// Xử lý việc cập nhật slider từ dữ liệu form
        /// </summary>
        /// <param name="id">ID của slider cần cập nhật</param>
        /// <param name="slider">Dữ liệu slider từ form</param>
        /// <param name="imageFile">File hình ảnh mới (nếu có)</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Subtitle,ImageUrl,LinkUrl,DisplayOrder,IsActive")] Slider slider, IFormFile? imageFile)
        {
            if (id != slider.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Xử lý tải lên hình ảnh nếu có hình ảnh mới
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        // Xóa hình ảnh cũ nếu tồn tại
                        if (!string.IsNullOrEmpty(slider.ImageUrl))
                        {
                            var oldImagePath = Path.Combine(_hostEnvironment.WebRootPath, slider.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "sliders");
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        // Đảm bảo thư mục tồn tại
                        Directory.CreateDirectory(uploadsFolder);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(fileStream);
                        }

                        slider.ImageUrl = "/images/sliders/" + uniqueFileName;
                    }

                    _context.Update(slider);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Slider đã được cập nhật thành công.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SliderExists(slider.Id))
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
            return View(slider);
        }

        /// <summary>
        /// Hiển thị trang xác nhận xóa slider
        /// </summary>
        /// <param name="id">ID của slider cần xóa</param>
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var slider = await _context.Sliders
                .FirstOrDefaultAsync(m => m.Id == id);
            if (slider == null)
            {
                return NotFound();
            }

            return View(slider);
        }

        /// <summary>
        /// Xử lý việc xóa slider
        /// </summary>
        /// <param name="id">ID của slider cần xóa</param>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var slider = await _context.Sliders.FindAsync(id);

            // Xóa file hình ảnh
            if (slider != null && !string.IsNullOrEmpty(slider.ImageUrl))
            {
                var imagePath = Path.Combine(_hostEnvironment.WebRootPath, slider.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            if (slider != null)
            {
                _context.Sliders.Remove(slider);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Slider đã được xóa thành công.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool SliderExists(int id)
        {
            return _context.Sliders.Any(e => e.Id == id);
        }

        /// <summary>
        /// Chuyển đổi trạng thái hoạt động của slider
        /// </summary>
        /// <param name="id">ID của slider cần chuyển đổi trạng thái</param>
        public async Task<IActionResult> ToggleActive(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var slider = await _context.Sliders.FindAsync(id);
            if (slider == null)
            {
                return NotFound();
            }

            slider.IsActive = !slider.IsActive;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Slider đã được {(slider.IsActive ? "kích hoạt" : "vô hiệu hóa")} thành công.";

            return RedirectToAction(nameof(Index));
        }
    }
}
