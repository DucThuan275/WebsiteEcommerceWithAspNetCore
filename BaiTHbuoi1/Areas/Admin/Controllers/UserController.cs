using BaiTHbuoi1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaiTHbuoi1.Areas.Admin.Controllers
{
    /// <summary>
    /// Controller quản lý người dùng trong khu vực Admin
    /// Chỉ có Admin mới có quyền truy cập
    /// </summary>
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        /// <summary>
        /// Hiển thị danh sách tất cả người dùng và vai trò của họ
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userViewModels = new List<UserViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                userViewModels.Add(new UserViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    Roles = string.Join(", ", roles)
                });
            }

            return View(userViewModels);
        }

        /// <summary>
        /// Hiển thị chi tiết của một người dùng cụ thể
        /// </summary>
        /// <param name="id">ID của người dùng cần xem</param>
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);

            var userViewModel = new UserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                City = user.City,
                PostalCode = user.PostalCode,
                Country = user.Country,
                Roles = string.Join(", ", roles)
            };

            return View(userViewModel);
        }

        /// <summary>
        /// Hiển thị form chỉnh sửa thông tin người dùng và vai trò
        /// </summary>
        /// <param name="id">ID của người dùng cần chỉnh sửa</param>
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var allRoles = await _roleManager.Roles.ToListAsync();

            var userEditViewModel = new UserEditViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                City = user.City,
                PostalCode = user.PostalCode,
                Country = user.Country,
                Roles = roles.ToList(),
                AllRoles = allRoles.Select(r => new SelectListItem
                {
                    Text = r.Name,
                    Value = r.Name,
                    Selected = roles.Contains(r.Name)
                }).ToList()
            };

            return View(userEditViewModel);
        }

        /// <summary>
        /// Xử lý việc cập nhật thông tin người dùng và vai trò
        /// </summary>
        /// <param name="id">ID của người dùng cần cập nhật</param>
        /// <param name="model">Dữ liệu người dùng từ form</param>
        /// <param name="selectedRoles">Danh sách vai trò được chọn</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UserEditViewModel model, List<string> selectedRoles)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                // Cập nhật thông tin người dùng
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.PhoneNumber = model.PhoneNumber;
                user.Address = model.Address;
                user.City = model.City;
                user.PostalCode = model.PostalCode;
                user.Country = model.Country;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }

                // Cập nhật vai trò
                var userRoles = await _userManager.GetRolesAsync(user);

                // Xóa các vai trò không được chọn
                foreach (var role in userRoles)
                {
                    if (!selectedRoles.Contains(role))
                    {
                        await _userManager.RemoveFromRoleAsync(user, role);
                    }
                }

                // Thêm các vai trò được chọn mà người dùng chưa có
                foreach (var role in selectedRoles)
                {
                    if (!userRoles.Contains(role))
                    {
                        await _userManager.AddToRoleAsync(user, role);
                    }
                }

                TempData["Success"] = "Thông tin người dùng đã được cập nhật thành công.";
                return RedirectToAction(nameof(Index));
            }

            // Nếu có lỗi, hiển thị lại form
            var allRoles = await _roleManager.Roles.ToListAsync();
            model.AllRoles = allRoles.Select(r => new SelectListItem
            {
                Text = r.Name,
                Value = r.Name,
                Selected = selectedRoles.Contains(r.Name)
            }).ToList();

            return View(model);
        }
    }

    // View models cho quản lý người dùng
    public class UserViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public string Roles { get; set; } = string.Empty;
    }

    public class UserEditViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public List<SelectListItem> AllRoles { get; set; } = new List<SelectListItem>();
    }
}
