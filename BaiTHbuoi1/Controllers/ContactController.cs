using BaiTHbuoi1.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace BaiTHbuoi1.Controllers
{
    public class ContactController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ContactController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Contact
        public IActionResult Index()
        {
            return View();
        }

        // POST: Contact/Send
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(Contact contact)
        {
            if (ModelState.IsValid)
            {
                contact.CreatedAt = DateTime.Now;
                contact.IsRead = false;

                _context.Add(contact);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Tin nhắn của bạn đã được gửi thành công. Chúng tôi sẽ liên hệ lại với bạn sớm.";
                return RedirectToAction(nameof(Index));
            }

            return View("Index", contact);
        }
    }
}
