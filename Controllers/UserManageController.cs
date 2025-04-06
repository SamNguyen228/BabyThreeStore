using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebsiteBaby3.Models;

namespace WebsiteBaby3.Controllers
{
    public class UserManageController : Controller
    {
        private readonly WebBaby3Context _context;

        public UserManageController(WebBaby3Context context)
        {
            _context = context;
        }

        // GET: UserManage
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(string search, string sortOrder, int page = 1, int pageSize = 10)
        {
            var users = _context.Users.Include(u => u.Role).AsQueryable();

            // Tìm kiếm theo tên hoặc email
            if (!string.IsNullOrEmpty(search))
            {
                users = users.Where(u => u.FullName.Contains(search) || u.Email.Contains(search));
                ViewBag.Search = search;
            }

            // Xử lý sắp xếp
            ViewBag.CurrentSort = sortOrder;
            ViewBag.NameSort = sortOrder == "name_asc" ? "name_desc" : "name_asc";
            ViewBag.RoleSort = sortOrder == "role_asc" ? "role_desc" : "role_asc";

            users = sortOrder switch
            {
                "name_desc" => users.OrderByDescending(u => u.FullName),
                "role_asc" => users.OrderBy(u => u.Role.RoleName),
                "role_desc" => users.OrderByDescending(u => u.Role.RoleName),
                _ => users.OrderBy(u => u.FullName) 
            };

            // Phân trang
            int totalUsers = await users.CountAsync();
            int totalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);

            users = users.Skip((page - 1) * pageSize).Take(pageSize);

            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;

            return View(await users.ToListAsync());
        }

        // GET: UserManage/Details/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: UserManage/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleName");
            return View();
        }

        // POST: UserManage/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,FullName,Email,PasswordHash,Phone,Address,CreatedAt,RoleId")] User user)
        {
            if (ModelState.IsValid)
            {
                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleName", user.RoleId);
            return View(user);
        }

        // GET: UserManage/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleName", user.RoleId);
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("UserId,FullName,Email,PasswordHash,Phone,Address,CreatedAt,RoleId")] User user)
        {
            if (id != user.UserId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == id);
                    if (existingUser == null)
                    {
                        return NotFound();
                    }

                    if (string.IsNullOrEmpty(user.PasswordHash))
                    {
                        user.PasswordHash = existingUser.PasswordHash;
                    }
                    else
                    {
                        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash, 10);
                    }

                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.UserId))
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
            ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleName", user.RoleId);
            return View(user);
        }

        // GET: UserManage/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: UserManage/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }
    }
}
