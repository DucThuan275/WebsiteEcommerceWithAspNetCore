using BaiTHbuoi1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace BaiTHbuoi1.Areas.Admin.Controllers
{
    /// <summary>
    /// Controller quản lý các liên hệ từ khách hàng trong khu vực Admin
    /// </summary>
    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    public class ContactController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ContactController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Hiển thị danh sách các liên hệ, có thể lọc theo trạng thái đã đọc/chưa đọc
        /// </summary>
        /// <param name="unread">Lọc theo trạng thái chưa đọc</param>
        public async Task<IActionResult> Index(bool? unread)
        {
            var contactsQuery = _context.Contacts.AsQueryable();

            // Lọc theo trạng thái chưa đọc nếu được chỉ định
            if (unread.HasValue && unread.Value)
            {
                contactsQuery = contactsQuery.Where(c => !c.IsRead);
                ViewBag.UnreadOnly = true;
            }

            var contacts = await contactsQuery
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return View(contacts);
        }

        /// <summary>
        /// Hiển thị chi tiết của một liên hệ và đánh dấu là đã đọc
        /// </summary>
        /// <param name="id">ID của liên hệ cần xem</param>
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contact = await _context.Contacts
                .FirstOrDefaultAsync(m => m.Id == id);

            if (contact == null)
            {
                return NotFound();
            }

            // Đánh dấu là đã đọc nếu chưa
            if (!contact.IsRead)
            {
                contact.IsRead = true;
                _context.Update(contact);
                await _context.SaveChangesAsync();
            }

            return View(contact);
        }

        /// <summary>
        /// Đánh dấu một liên hệ là đã đọc
        /// </summary>
        /// <param name="id">ID của liên hệ cần đánh dấu</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var contact = await _context.Contacts.FindAsync(id);
            if (contact == null)
            {
                return NotFound();
            }

            contact.IsRead = true;
            _context.Update(contact);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã đánh dấu liên hệ là đã đọc.";

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Đánh dấu một liên hệ là chưa đọc
        /// </summary>
        /// <param name="id">ID của liên hệ cần đánh dấu</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsUnread(int id)
        {
            var contact = await _context.Contacts.FindAsync(id);
            if (contact == null)
            {
                return NotFound();
            }

            contact.IsRead = false;
            _context.Update(contact);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã đánh dấu liên hệ là chưa đọc.";

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Hiển thị trang xác nhận xóa liên hệ
        /// </summary>
        /// <param name="id">ID của liên hệ cần xóa</param>
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contact = await _context.Contacts
                .FirstOrDefaultAsync(m => m.Id == id);

            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }

        /// <summary>
        /// Xử lý việc xóa liên hệ
        /// </summary>
        /// <param name="id">ID của liên hệ cần xóa</param>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var contact = await _context.Contacts.FindAsync(id);
            if (contact == null)
            {
                return NotFound();
            }

            _context.Contacts.Remove(contact);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Liên hệ đã được xóa thành công.";
            return RedirectToAction(nameof(Index));
        }
    }
}
