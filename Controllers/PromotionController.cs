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
    public class PromotionController : Controller
    {
        private readonly WebBaby3Context _context;

        public PromotionController(WebBaby3Context context)
        {
            _context = context;
        }

        // GET: PromotionManage
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(string searchTerm, DateTime? startDate, DateTime? endDate, int pageNumber = 1, int pageSize = 5)
        {
            var query = _context.Promotions.AsQueryable();

            // Tìm kiếm theo tên khuyến mãi
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p => p.PromotionName.Contains(searchTerm));
            }

            // Lọc theo ngày bắt đầu và ngày kết thúc
            if (startDate.HasValue)
            {
                query = query.Where(p => p.StartDate >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                query = query.Where(p => p.EndDate <= endDate.Value);
            }

            // Đếm tổng số bản ghi
            int totalRecords = await query.CountAsync();

            // Tính tổng số trang
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            // Lấy dữ liệu theo trang
            var promotions = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Truyền dữ liệu vào ViewBag
            ViewBag.SearchTerm = searchTerm;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.CurrentPage = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;

            return View(promotions);
        }

        // GET: PromotionManage/Details/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var promotion = await _context.Promotions
                .FirstOrDefaultAsync(m => m.PromotionId == id);
            if (promotion == null)
            {
                return NotFound();
            }

            return View(promotion);
        }

        // GET: PromotionManage/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            TempData["CreatePromotion"] = "Thêm mã khuyến mãi thành công!";
            return View();
        }

        // POST: PromotionManage/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PromotionId,PromotionName,DiscountPercentage,DiscountAmount,StartDate,EndDate,MinOrderValue,MaxDiscount,CreatedAt")] Promotion promotion)
        {
            if (ModelState.IsValid)
            {
                _context.Add(promotion);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(promotion);
        }

        // GET: PromotionManage/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null)
            {
                return NotFound();
            }
            TempData["EditPromotion"] = "Cập nhật thông tin khuyến mãi thành công!";
            return View(promotion);
        }

        // POST: PromotionManage/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PromotionId,PromotionName,DiscountPercentage,DiscountAmount,StartDate,EndDate,MinOrderValue,MaxDiscount,CreatedAt")] Promotion promotion)
        {
            if (id != promotion.PromotionId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(promotion);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PromotionExists(promotion.PromotionId))
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
            return View(promotion);
        }

        // GET: PromotionManage/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var promotion = await _context.Promotions
                .FirstOrDefaultAsync(m => m.PromotionId == id);
            if (promotion == null)
            {
                return NotFound();
            }

            return View(promotion);
        }

        // POST: PromotionManage/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion != null)
            {
                _context.Promotions.Remove(promotion);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PromotionExists(int id)
        {
            return _context.Promotions.Any(e => e.PromotionId == id);
        }
    }
}
