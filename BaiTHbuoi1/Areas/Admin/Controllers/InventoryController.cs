using BaiTHbuoi1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BaiTHbuoi1.Areas.Admin.Controllers
{
    /// <summary>
    /// Controller quản lý nhập kho trong khu vực Admin
    /// </summary>
    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    public class InventoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InventoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Hiển thị danh sách tất cả các bản ghi nhập kho
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var inventories = await _context.Inventories
                .Include(i => i.Product)
                .Include(i => i.Supplier)
                .OrderByDescending(i => i.ReceivedDate)
                .ToListAsync();

            return View(inventories);
        }

        /// <summary>
        /// Hiển thị chi tiết của một bản ghi nhập kho cụ thể
        /// </summary>
        /// <param name="id">ID của bản ghi nhập kho cần xem</param>
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventory = await _context.Inventories
                .Include(i => i.Product)
                .Include(i => i.Supplier)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (inventory == null)
            {
                return NotFound();
            }

            return View(inventory);
        }

        /// <summary>
        /// Hiển thị form tạo bản ghi nhập kho mới
        /// </summary>
        public IActionResult Create()
        {
            ViewData["ProductId"] = new SelectList(_context.Products, "Id", "Name");
            ViewData["SupplierId"] = new SelectList(_context.Suppliers, "Id", "Name");
            return View();
        }

        /// <summary>
        /// Xử lý việc tạo bản ghi nhập kho mới từ dữ liệu form
        /// </summary>
        /// <param name="inventory">Dữ liệu nhập kho từ form</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Inventory inventory)
        {
            if (ModelState.IsValid)
            {
                // Đặt ngày nhận là ngày hiện tại nếu không được cung cấp
                if (inventory.ReceivedDate == default)
                {
                    inventory.ReceivedDate = DateTime.Now;
                }

                _context.Add(inventory);

                // Cập nhật số lượng tồn kho của sản phẩm
                var product = await _context.Products.FindAsync(inventory.ProductId);
                if (product != null)
                {
                    product.Stock += inventory.Quantity;
                    _context.Update(product);
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Bản ghi nhập kho đã được tạo thành công.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["ProductId"] = new SelectList(_context.Products, "Id", "Name", inventory.ProductId);
            ViewData["SupplierId"] = new SelectList(_context.Suppliers, "Id", "Name", inventory.SupplierId);
            return View(inventory);
        }

        /// <summary>
        /// Hiển thị form chỉnh sửa bản ghi nhập kho
        /// </summary>
        /// <param name="id">ID của bản ghi nhập kho cần chỉnh sửa</param>
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventory = await _context.Inventories.FindAsync(id);
            if (inventory == null)
            {
                return NotFound();
            }

            ViewData["ProductId"] = new SelectList(_context.Products, "Id", "Name", inventory.ProductId);
            ViewData["SupplierId"] = new SelectList(_context.Suppliers, "Id", "Name", inventory.SupplierId);
            return View(inventory);
        }

        /// <summary>
        /// Xử lý việc cập nhật bản ghi nhập kho từ dữ liệu form
        /// </summary>
        /// <param name="id">ID của bản ghi nhập kho cần cập nhật</param>
        /// <param name="inventory">Dữ liệu nhập kho từ form</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Inventory inventory)
        {
            if (id != inventory.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Lấy bản ghi nhập kho gốc
                    var originalInventory = await _context.Inventories.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
                    if (originalInventory == null)
                    {
                        return NotFound();
                    }

                    // Tính toán sự chênh lệch số lượng
                    var quantityDifference = inventory.Quantity - originalInventory.Quantity;

                    // Cập nhật bản ghi nhập kho
                    _context.Update(inventory);

                    // Cập nhật số lượng tồn kho của sản phẩm nếu số lượng thay đổi
                    if (quantityDifference != 0)
                    {
                        var product = await _context.Products.FindAsync(inventory.ProductId);
                        if (product != null)
                        {
                            product.Stock += quantityDifference;
                            _context.Update(product);
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Bản ghi nhập kho đã được cập nhật thành công.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InventoryExists(inventory.Id))
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

            ViewData["ProductId"] = new SelectList(_context.Products, "Id", "Name", inventory.ProductId);
            ViewData["SupplierId"] = new SelectList(_context.Suppliers, "Id", "Name", inventory.SupplierId);
            return View(inventory);
        }

        /// <summary>
        /// Hiển thị trang xác nhận xóa bản ghi nhập kho
        /// </summary>
        /// <param name="id">ID của bản ghi nhập kho cần xóa</param>
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventory = await _context.Inventories
                .Include(i => i.Product)
                .Include(i => i.Supplier)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (inventory == null)
            {
                return NotFound();
            }

            return View(inventory);
        }

        /// <summary>
        /// Xử lý việc xóa bản ghi nhập kho
        /// </summary>
        /// <param name="id">ID của bản ghi nhập kho cần xóa</param>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var inventory = await _context.Inventories.FindAsync(id);
            if (inventory == null)
            {
                return NotFound();
            }

            // Cập nhật số lượng tồn kho của sản phẩm
            var product = await _context.Products.FindAsync(inventory.ProductId);
            if (product != null)
            {
                product.Stock -= inventory.Quantity;
                if (product.Stock < 0)
                {
                    product.Stock = 0;
                }
                _context.Update(product);
            }

            _context.Inventories.Remove(inventory);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Bản ghi nhập kho đã được xóa thành công.";
            return RedirectToAction(nameof(Index));
        }

        private bool InventoryExists(int id)
        {
            return _context.Inventories.Any(e => e.Id == id);
        }
    }
}
