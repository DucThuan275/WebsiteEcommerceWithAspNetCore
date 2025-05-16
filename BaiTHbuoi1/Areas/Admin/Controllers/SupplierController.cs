using BaiTHbuoi1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace BaiTHbuoi1.Areas.Admin.Controllers
{
    /// <summary>
    /// Controller quản lý nhà cung cấp trong khu vực Admin
    /// </summary>
    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    public class SupplierController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SupplierController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Hiển thị danh sách tất cả nhà cung cấp
        /// </summary>
        public async Task<IActionResult> Index()
        {
            return View(await _context.Suppliers.ToListAsync());
        }

        /// <summary>
        /// Hiển thị chi tiết của một nhà cung cấp cụ thể và các sản phẩm liên quan
        /// </summary>
        /// <param name="id">ID của nhà cung cấp cần xem</param>
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(m => m.Id == id);

            if (supplier == null)
            {
                return NotFound();
            }

            // Lấy sản phẩm từ nhà cung cấp này
            var products = await _context.Products
                .Where(p => p.SupplierId == id)
                .ToListAsync();

            ViewBag.Products = products;

            return View(supplier);
        }

        /// <summary>
        /// Hiển thị form tạo nhà cung cấp mới
        /// </summary>
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Xử lý việc tạo nhà cung cấp mới từ dữ liệu form
        /// </summary>
        /// <param name="supplier">Dữ liệu nhà cung cấp từ form</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                _context.Add(supplier);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Nhà cung cấp đã được tạo thành công.";
                return RedirectToAction(nameof(Index));
            }
            return View(supplier);
        }

        /// <summary>
        /// Hiển thị form chỉnh sửa nhà cung cấp
        /// </summary>
        /// <param name="id">ID của nhà cung cấp cần chỉnh sửa</param>
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                return NotFound();
            }
            return View(supplier);
        }

        /// <summary>
        /// Xử lý việc cập nhật nhà cung cấp từ dữ liệu form
        /// </summary>
        /// <param name="id">ID của nhà cung cấp cần cập nhật</param>
        /// <param name="supplier">Dữ liệu nhà cung cấp từ form</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Supplier supplier)
        {
            if (id != supplier.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(supplier);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Nhà cung cấp đã được cập nhật thành công.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SupplierExists(supplier.Id))
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
            return View(supplier);
        }

        /// <summary>
        /// Hiển thị trang xác nhận xóa nhà cung cấp
        /// </summary>
        /// <param name="id">ID của nhà cung cấp cần xóa</param>
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(m => m.Id == id);

            if (supplier == null)
            {
                return NotFound();
            }

            return View(supplier);
        }

        /// <summary>
        /// Xử lý việc xóa nhà cung cấp
        /// </summary>
        /// <param name="id">ID của nhà cung cấp cần xóa</param>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                return NotFound();
            }

            // Kiểm tra xem nhà cung cấp có sản phẩm không
            var hasProducts = await _context.Products.AnyAsync(p => p.SupplierId == id);
            if (hasProducts)
            {
                TempData["Error"] = "Không thể xóa nhà cung cấp vì có sản phẩm liên quan.";
                return RedirectToAction(nameof(Index));
            }

            // Kiểm tra xem nhà cung cấp có bản ghi nhập kho không
            var hasInventory = await _context.Inventories.AnyAsync(i => i.SupplierId == id);
            if (hasInventory)
            {
                TempData["Error"] = "Không thể xóa nhà cung cấp vì có bản ghi nhập kho liên quan.";
                return RedirectToAction(nameof(Index));
            }

            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Nhà cung cấp đã được xóa thành công.";
            return RedirectToAction(nameof(Index));
        }

        private bool SupplierExists(int id)
        {
            return _context.Suppliers.Any(e => e.Id == id);
        }
    }
}
