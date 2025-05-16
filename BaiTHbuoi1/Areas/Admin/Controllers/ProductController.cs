using BaiTHbuoi1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BaiTHbuoi1.Areas.Admin.Controllers
{
    /// <summary>
    /// Controller quản lý sản phẩm trong khu vực Admin
    /// </summary>
    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly ILogger<ProductController> _logger;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
        private const int PageSize = 10;

        public ProductController(
            ApplicationDbContext context,
            IWebHostEnvironment hostEnvironment,
            ILogger<ProductController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _hostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Hiển thị danh sách sản phẩm có phân trang và lọc
        /// </summary>
        /// <param name="page">Số trang hiện tại</param>
        /// <param name="categoryId">Lọc theo ID danh mục</param>
        /// <param name="supplierId">Lọc theo ID nhà cung cấp</param>
        /// <param name="searchTerm">Từ khóa tìm kiếm</param>
        /// <param name="sortOrder">Thứ tự sắp xếp (tên, giá, ngày)</param>
        /// <param name="showInactive">Hiển thị cả sản phẩm không hoạt động</param>
        /// <returns>View với danh sách sản phẩm đã phân trang</returns>
        public async Task<IActionResult> Index(
            int page = 1,
            int? categoryId = null,
            int? supplierId = null,
            string searchTerm = null,
            string sortOrder = null,
            bool showInactive = false)
        {
            _logger.LogInformation("Đang lấy danh sách sản phẩm với bộ lọc: Danh mục={CategoryId}, Nhà cung cấp={SupplierId}, Tìm kiếm={SearchTerm}",
                categoryId, supplierId, searchTerm);

            // Bắt đầu với tất cả sản phẩm
            var productsQuery = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .AsQueryable();

            // Áp dụng bộ lọc
            if (categoryId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);
            }

            if (supplierId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.SupplierId == supplierId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                productsQuery = productsQuery.Where(p => p.Name.Contains(searchTerm) ||
                                                        (p.Description != null && p.Description.Contains(searchTerm)));
            }

            if (!showInactive)
            {
                productsQuery = productsQuery.Where(p => p.IsActive);
            }

            // Áp dụng sắp xếp
            productsQuery = sortOrder switch
            {
                "name_desc" => productsQuery.OrderByDescending(p => p.Name),
                "price" => productsQuery.OrderBy(p => p.Price),
                "price_desc" => productsQuery.OrderByDescending(p => p.Price),
                "date" => productsQuery.OrderBy(p => p.CreatedAt),
                "date_desc" => productsQuery.OrderByDescending(p => p.CreatedAt),
                _ => productsQuery.OrderBy(p => p.Name), // Mặc định sắp xếp theo tên tăng dần
            };

            // Tính toán phân trang
            var totalItems = await productsQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)PageSize);

            // Đảm bảo trang nằm trong phạm vi hợp lệ
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            // Lấy kết quả đã phân trang
            var products = await productsQuery
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Chuẩn bị dữ liệu cho view
            ViewData["Categories"] = await _context.Categories.ToListAsync();
            ViewData["Suppliers"] = await _context.Suppliers.ToListAsync();
            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = totalPages;
            ViewData["CategoryId"] = categoryId;
            ViewData["SupplierId"] = supplierId;
            ViewData["SearchTerm"] = searchTerm;
            ViewData["SortOrder"] = sortOrder;
            ViewData["ShowInactive"] = showInactive;

            return View(products);
        }

        /// <summary>
        /// Hiển thị chi tiết của một sản phẩm cụ thể
        /// </summary>
        /// <param name="id">ID của sản phẩm cần xem</param>
        /// <returns>View với thông tin chi tiết sản phẩm</returns>
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Yêu cầu xem chi tiết sản phẩm với ID null");
                return BadRequest("ID sản phẩm là bắt buộc");
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Include(p => p.OrderItems)
                .ThenInclude(oi => oi.Order)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
            {
                _logger.LogWarning("Không tìm thấy sản phẩm với ID {ProductId}", id);
                return NotFound($"Không tìm thấy sản phẩm với ID {id}");
            }

            _logger.LogInformation("Đã lấy chi tiết sản phẩm {ProductId}: {ProductName}", id, product.Name);
            return View(product);
        }

        /// <summary>
        /// Hiển thị form tạo sản phẩm mới
        /// </summary>
        /// <returns>View với form tạo sản phẩm</returns>
        public async Task<IActionResult> Create()
        {
            await PrepareProductFormViewData();
            return View(new Product { IsActive = true });
        }

        /// <summary>
        /// Xử lý việc tạo sản phẩm mới từ dữ liệu form
        /// </summary>
        /// <param name="product">Dữ liệu sản phẩm từ form</param>
        /// <returns>Chuyển hướng đến Index khi thành công, hoặc hiển thị lại form với lỗi</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            // Xác thực danh mục tồn tại
            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == product.CategoryId);
            if (!categoryExists)
            {
                ModelState.AddModelError("CategoryId", "Danh mục đã chọn không tồn tại");
            }

            // Xác thực nhà cung cấp tồn tại nếu được cung cấp
            if (product.SupplierId.HasValue)
            {
                var supplierExists = await _context.Suppliers.AnyAsync(s => s.Id == product.SupplierId.Value);
                if (!supplierExists)
                {
                    ModelState.AddModelError("SupplierId", "Nhà cung cấp đã chọn không tồn tại");
                }
            }

            // Xác thực giá
            if (product.Price <= 0)
            {
                ModelState.AddModelError("Price", "Giá phải lớn hơn không");
            }

            // Xác thực giá khuyến mãi nếu được cung cấp
            if (product.DiscountPrice.HasValue && product.DiscountPrice.Value >= product.Price)
            {
                ModelState.AddModelError("DiscountPrice", "Giá khuyến mãi phải nhỏ hơn giá gốc");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Xử lý tải lên hình ảnh
                    product.ImageUrl = await ProcessImageUpload(null);

                    // Đặt thời gian tạo
                    product.CreatedAt = DateTime.Now;

                    // Đặt IsOnSale dựa trên giá khuyến mãi
                    product.IsOnSale = product.DiscountPrice.HasValue && product.DiscountPrice.Value > 0;

                    _context.Add(product);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Đã tạo sản phẩm mới: {ProductName} (ID: {ProductId})", product.Name, product.Id);
                    TempData["SuccessMessage"] = $"Sản phẩm '{product.Name}' đã được tạo thành công.";

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi tạo sản phẩm {ProductName}", product.Name);
                    ModelState.AddModelError("", "Đã xảy ra lỗi khi tạo sản phẩm. Vui lòng thử lại.");
                }
            }

            await PrepareProductFormViewData(product.CategoryId, product.SupplierId);
            return View(product);
        }

        /// <summary>
        /// Hiển thị form chỉnh sửa sản phẩm
        /// </summary>
        /// <param name="id">ID của sản phẩm cần chỉnh sửa</param>
        /// <returns>View với form chỉnh sửa sản phẩm</returns>
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Yêu cầu chỉnh sửa sản phẩm với ID null");
                return BadRequest("ID sản phẩm là bắt buộc");
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                _logger.LogWarning("Không tìm thấy sản phẩm với ID {ProductId} để chỉnh sửa", id);
                return NotFound($"Không tìm thấy sản phẩm với ID {id}");
            }

            await PrepareProductFormViewData(product.CategoryId, product.SupplierId);
            return View(product);
        }

        /// <summary>
        /// Xử lý việc cập nhật sản phẩm từ dữ liệu form
        /// </summary>
        /// <param name="id">ID của sản phẩm cần cập nhật</param>
        /// <param name="product">Dữ liệu sản phẩm đã cập nhật</param>
        /// <returns>Chuyển hướng đến Index khi thành công, hoặc hiển thị lại form với lỗi</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.Id)
            {
                _logger.LogWarning("ID sản phẩm không khớp: ID URL {UrlId} vs ID Form {FormId}", id, product.Id);
                return BadRequest("ID sản phẩm không khớp");
            }

            // Xác thực danh mục tồn tại
            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == product.CategoryId);
            if (!categoryExists)
            {
                ModelState.AddModelError("CategoryId", "Danh mục đã chọn không tồn tại");
            }

            // Xác thực nhà cung cấp tồn tại nếu được cung cấp
            if (product.SupplierId.HasValue)
            {
                var supplierExists = await _context.Suppliers.AnyAsync(s => s.Id == product.SupplierId.Value);
                if (!supplierExists)
                {
                    ModelState.AddModelError("SupplierId", "Nhà cung cấp đã chọn không tồn tại");
                }
            }

            // Xác thực giá
            if (product.Price <= 0)
            {
                ModelState.AddModelError("Price", "Giá phải lớn hơn không");
            }

            // Xác thực giá khuyến mãi nếu được cung cấp
            if (product.DiscountPrice.HasValue && product.DiscountPrice.Value >= product.Price)
            {
                ModelState.AddModelError("DiscountPrice", "Giá khuyến mãi phải nhỏ hơn giá gốc");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Lấy sản phẩm hiện có để kiểm tra thay đổi
                    var existingProduct = await _context.Products
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.Id == id);

                    if (existingProduct == null)
                    {
                        _logger.LogWarning("Không tìm thấy sản phẩm với ID {ProductId} trong quá trình cập nhật", id);
                        return NotFound($"Không tìm thấy sản phẩm với ID {id}");
                    }

                    // Xử lý tải lên hình ảnh
                    var newImageUrl = await ProcessImageUpload(existingProduct.ImageUrl);
                    if (newImageUrl != null)
                    {
                        product.ImageUrl = newImageUrl;
                    }
                    else if (Request.Form.Files.Count == 0)
                    {
                        // Giữ hình ảnh hiện có nếu không có hình ảnh mới được tải lên
                        product.ImageUrl = existingProduct.ImageUrl;
                    }

                    // Đặt thời gian cập nhật
                    product.UpdatedAt = DateTime.Now;
                    product.CreatedAt = existingProduct.CreatedAt; // Giữ ngày tạo ban đầu

                    // Đặt IsOnSale dựa trên giá khuyến mãi
                    product.IsOnSale = product.DiscountPrice.HasValue && product.DiscountPrice.Value > 0;

                    _context.Update(product);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Đã cập nhật sản phẩm: {ProductName} (ID: {ProductId})", product.Name, product.Id);
                    TempData["SuccessMessage"] = $"Sản phẩm '{product.Name}' đã được cập nhật thành công.";

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!ProductExists(product.Id))
                    {
                        _logger.LogWarning("Không tìm thấy sản phẩm với ID {ProductId} trong quá trình kiểm tra đồng thời", id);
                        return NotFound($"Không tìm thấy sản phẩm với ID {id}");
                    }
                    else
                    {
                        _logger.LogError(ex, "Lỗi đồng thời khi cập nhật sản phẩm {ProductId}", id);
                        ModelState.AddModelError("", "Sản phẩm này đã được sửa đổi bởi người dùng khác. Vui lòng làm mới và thử lại.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi cập nhật sản phẩm {ProductId}", id);
                    ModelState.AddModelError("", "Đã xảy ra lỗi khi cập nhật sản phẩm. Vui lòng thử lại.");
                }
            }

            await PrepareProductFormViewData(product.CategoryId, product.SupplierId);
            return View(product);
        }

        /// <summary>
        /// Hiển thị trang xác nhận xóa sản phẩm
        /// </summary>
        /// <param name="id">ID của sản phẩm cần xóa</param>
        /// <returns>View với xác nhận xóa sản phẩm</returns>
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Yêu cầu xóa sản phẩm với ID null");
                return BadRequest("ID sản phẩm là bắt buộc");
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Include(p => p.OrderItems)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
            {
                _logger.LogWarning("Không tìm thấy sản phẩm với ID {ProductId} để xóa", id);
                return NotFound($"Không tìm thấy sản phẩm với ID {id}");
            }

            // Kiểm tra xem sản phẩm có đơn hàng không
            if (product.OrderItems.Any())
            {
                ViewData["HasOrders"] = true;
                ViewData["OrderCount"] = product.OrderItems.Count;
            }

            return View(product);
        }

        /// <summary>
        /// Xử lý việc xóa sản phẩm
        /// </summary>
        /// <param name="id">ID của sản phẩm cần xóa</param>
        /// <param name="forceDelete">Có xóa cưỡng chế ngay cả khi sản phẩm có đơn hàng</param>
        /// <returns>Chuyển hướng đến Index khi thành công</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, bool forceDelete = false)
        {
            var product = await _context.Products
                .Include(p => p.OrderItems)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                _logger.LogWarning("Không tìm thấy sản phẩm với ID {ProductId} trong quá trình xác nhận xóa", id);
                return NotFound($"Không tìm thấy sản phẩm với ID {id}");
            }

            // Kiểm tra xem sản phẩm có đơn hàng và chúng ta không cưỡng chế xóa
            if (product.OrderItems.Any() && !forceDelete)
            {
                TempData["ErrorMessage"] = $"Sản phẩm '{product.Name}' có {product.OrderItems.Count} đơn hàng và không thể xóa. Sử dụng xóa cưỡng chế nếu bạn thực sự muốn xóa nó.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            try
            {
                // Xóa file hình ảnh nếu tồn tại
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    await DeleteProductImage(product.ImageUrl);
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Đã xóa sản phẩm: {ProductName} (ID: {ProductId})", product.Name, id);
                TempData["SuccessMessage"] = $"Sản phẩm '{product.Name}' đã được xóa thành công.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa sản phẩm {ProductId}", id);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi xóa sản phẩm. Vui lòng thử lại.";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        /// <summary>
        /// Chuyển đổi trạng thái hoạt động của sản phẩm
        /// </summary>
        /// <param name="id">ID của sản phẩm cần chuyển đổi</param>
        /// <returns>Chuyển hướng đến Index</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                _logger.LogWarning("Không tìm thấy sản phẩm với ID {ProductId} để chuyển đổi trạng thái hoạt động", id);
                return NotFound($"Không tìm thấy sản phẩm với ID {id}");
            }

            try
            {
                product.IsActive = !product.IsActive;
                product.UpdatedAt = DateTime.Now;

                _context.Update(product);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Đã chuyển đổi trạng thái hoạt động cho sản phẩm {ProductId} thành {IsActive}", id, product.IsActive);
                TempData["SuccessMessage"] = $"Sản phẩm '{product.Name}' hiện đang {(product.IsActive ? "hoạt động" : "không hoạt động")}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi chuyển đổi trạng thái hoạt động cho sản phẩm {ProductId}", id);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi cập nhật trạng thái sản phẩm. Vui lòng thử lại.";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Chuyển đổi thuộc tính cờ boolean của sản phẩm
        /// </summary>
        /// <param name="id">ID của sản phẩm</param>
        /// <param name="property">Tên thuộc tính cần chuyển đổi</param>
        /// <returns>Chuyển hướng đến Index</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFlag(int id, string property)
        {
            // Xác thực tên thuộc tính để ngăn chặn vấn đề bảo mật
            var allowedProperties = new[] { "IsFeatured", "IsNewArrival", "IsOnSale", "IsBestSeller" };
            if (!allowedProperties.Contains(property))
            {
                _logger.LogWarning("Tên thuộc tính không hợp lệ {PropertyName} trong ToggleFlag", property);
                return BadRequest("Tên thuộc tính không hợp lệ");
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                _logger.LogWarning("Không tìm thấy sản phẩm với ID {ProductId} để chuyển đổi cờ {PropertyName}", id, property);
                return NotFound($"Không tìm thấy sản phẩm với ID {id}");
            }

            try
            {
                // Sử dụng reflection để chuyển đổi thuộc tính
                var prop = typeof(Product).GetProperty(property);
                if (prop != null && prop.PropertyType == typeof(bool))
                {
                    bool currentValue = (bool)prop.GetValue(product);
                    prop.SetValue(product, !currentValue);

                    // Trường hợp đặc biệt cho IsOnSale
                    if (property == "IsOnSale")
                    {
                        // Nếu bật IsOnSale, đảm bảo có giá khuyến mãi
                        if (!currentValue && (!product.DiscountPrice.HasValue || product.DiscountPrice.Value <= 0))
                        {
                            product.DiscountPrice = product.Price * 0.9m; // Mặc định giảm giá 10%
                        }
                        // Nếu tắt IsOnSale, xóa giá khuyến mãi
                        else if (currentValue)
                        {
                            product.DiscountPrice = null;
                        }
                    }

                    product.UpdatedAt = DateTime.Now;
                    _context.Update(product);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Đã chuyển đổi {PropertyName} cho sản phẩm {ProductId} thành {Value}",
                        property, id, !currentValue);

                    TempData["SuccessMessage"] = $"Cờ {property} của sản phẩm '{product.Name}' đã được cập nhật thành công.";
                }
                else
                {
                    _logger.LogWarning("Không tìm thấy thuộc tính {PropertyName} hoặc không phải kiểu boolean", property);
                    TempData["ErrorMessage"] = "Thuộc tính không hợp lệ.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi chuyển đổi cờ {PropertyName} cho sản phẩm {ProductId}", property, id);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi cập nhật sản phẩm. Vui lòng thử lại.";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Xuất sản phẩm ra định dạng CSV
        /// </summary>
        /// <param name="categoryId">Lọc theo ID danh mục</param>
        /// <param name="supplierId">Lọc theo ID nhà cung cấp</param>
        /// <param name="searchTerm">Từ khóa tìm kiếm</param>
        /// <returns>Tải xuống file CSV</returns>
        public async Task<IActionResult> ExportToCsv(
            int? categoryId = null,
            int? supplierId = null,
            string searchTerm = null)
        {
            // Bắt đầu với tất cả sản phẩm
            var productsQuery = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .AsQueryable();

            // Áp dụng bộ lọc
            if (categoryId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);
            }

            if (supplierId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.SupplierId == supplierId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                productsQuery = productsQuery.Where(p => p.Name.Contains(searchTerm) ||
                                                        (p.Description != null && p.Description.Contains(searchTerm)));
            }

            var products = await productsQuery.ToListAsync();

            // Tạo nội dung CSV
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Id,Tên,Mô tả,Giá,Giá khuyến mãi,Tồn kho,Danh mục,Nhà cung cấp,Hoạt động,Nổi bật,Mới về,Đang giảm giá,Bán chạy,Ngày tạo");

            foreach (var product in products)
            {
                csv.AppendLine($"{product.Id}," +
                              $"\"{EscapeCsvField(product.Name)}\"," +
                              $"\"{EscapeCsvField(product.Description)}\"," +
                              $"{product.Price}," +
                              $"{product.DiscountPrice}," +
                              $"{product.Stock}," +
                              $"\"{EscapeCsvField(product.Category?.Name)}\"," +
                              $"\"{EscapeCsvField(product.Supplier?.Name)}\"," +
                              $"{product.IsActive}," +
                              $"{product.IsFeatured}," +
                              $"{product.IsNewArrival}," +
                              $"{product.IsOnSale}," +
                              $"{product.IsBestSeller}," +
                              $"{product.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            }

            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"danh_sach_san_pham_{timestamp}.csv");
        }

        #region Helper Methods

        /// <summary>
        /// Kiểm tra xem sản phẩm có tồn tại không
        /// </summary>
        /// <param name="id">ID của sản phẩm cần kiểm tra</param>
        /// <returns>True nếu sản phẩm tồn tại, ngược lại là False</returns>
        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }

        /// <summary>
        /// Chuẩn bị dữ liệu cho form sản phẩm
        /// </summary>
        /// <param name="selectedCategoryId">ID danh mục đã chọn</param>
        /// <param name="selectedSupplierId">ID nhà cung cấp đã chọn</param>
        private async Task PrepareProductFormViewData(int? selectedCategoryId = null, int? selectedSupplierId = null)
        {
            ViewData["CategoryId"] = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", selectedCategoryId);
            ViewData["SupplierId"] = new SelectList(await _context.Suppliers.OrderBy(s => s.Name).ToListAsync(), "Id", "Name", selectedSupplierId);
        }

        /// <summary>
        /// Xử lý tải lên hình ảnh cho sản phẩm
        /// </summary>
        /// <param name="existingImageUrl">URL hình ảnh hiện có để thay thế</param>
        /// <returns>URL hình ảnh mới hoặc null nếu không có hình ảnh được tải lên</returns>
        private async Task<string> ProcessImageUpload(string existingImageUrl)
        {
            if (Request.Form.Files.Count == 0)
            {
                return null; // Không có file được tải lên
            }

            var file = Request.Form.Files.FirstOrDefault();
            if (file == null || file.Length == 0)
            {
                return null; // Không có file hoặc file rỗng
            }

            // Xác thực phần mở rộng file
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(fileExtension))
            {
                throw new InvalidOperationException($"Loại file không hợp lệ. Các loại được phép: {string.Join(", ", _allowedExtensions)}");
            }

            // Xác thực kích thước file (tối đa 5MB)
            if (file.Length > 5 * 1024 * 1024)
            {
                throw new InvalidOperationException("Kích thước file vượt quá giới hạn tối đa 5MB.");
            }

            // Tạo cấu trúc thư mục
            var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "products");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Tạo tên file duy nhất
            var uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Lưu file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // Xóa hình ảnh cũ nếu tồn tại
            if (!string.IsNullOrEmpty(existingImageUrl))
            {
                await DeleteProductImage(existingImageUrl);
            }

            return "/images/products/" + uniqueFileName;
        }

        /// <summary>
        /// Xóa file hình ảnh sản phẩm
        /// </summary>
        /// <param name="imageUrl">URL hình ảnh cần xóa</param>
        private async Task DeleteProductImage(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
            {
                return;
            }

            try
            {
                var imagePath = Path.Combine(_hostEnvironment.WebRootPath, imageUrl.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    // Xóa file bất đồng bộ
                    await Task.Run(() => System.IO.File.Delete(imagePath));
                    _logger.LogInformation("Đã xóa hình ảnh sản phẩm: {ImagePath}", imagePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa hình ảnh sản phẩm: {ImageUrl}", imageUrl);
                // Tiếp tục thực thi ngay cả khi xóa hình ảnh thất bại
            }
        }

        /// <summary>
        /// Thoát ký tự đặc biệt trong trường CSV
        /// </summary>
        /// <param name="field">Trường cần thoát</param>
        /// <returns>Trường đã thoát</returns>
        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
            {
                return string.Empty;
            }

            // Thay thế dấu ngoặc kép bằng hai dấu ngoặc kép
            return field.Replace("\"", "\"\"");
        }

        #endregion
    }
}
