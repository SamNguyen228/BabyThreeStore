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
    public class ProductManageController : Controller
    {
        private readonly WebBaby3Context _context;

        public ProductManageController(WebBaby3Context context)
        {
            _context = context;
        }

        // GET: ProductManage
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(string search, string sortOrder, int page = 1, int pageSize = 8)
        {
            var products = _context.Products.Include(p => p.Category).AsQueryable();

            // Tìm kiếm theo tên sản phẩm
            if (!string.IsNullOrEmpty(search))
            {
                products = products.Where(p => p.ProductName.Contains(search));
            }

            // Xác định trạng thái sắp xếp (nếu ASC thì đổi thành DESC, và ngược lại)
            ViewBag.CurrentSort = sortOrder;
            ViewBag.NameSort = sortOrder == "name_asc" ? "name_desc" : "name_asc";
            ViewBag.PriceSort = sortOrder == "price_asc" ? "price_desc" : "price_asc";
            ViewBag.CategorySort = sortOrder == "category_asc" ? "category_desc" : "category_asc";

            // Sắp xếp theo điều kiện được chọn
            switch (sortOrder)
            {
                case "name_desc":
                    products = products.OrderByDescending(p => p.ProductName);
                    break;
                case "price_asc":
                    products = products.OrderBy(p => p.Price);
                    break;
                case "price_desc":
                    products = products.OrderByDescending(p => p.Price);
                    break;
                case "category_asc":
                    products = products.OrderBy(p => p.Category.CategoryName);
                    break;
                case "category_desc":
                    products = products.OrderByDescending(p => p.Category.CategoryName);
                    break;
                default:
                    products = products.OrderBy(p => p.ProductName); // Mặc định sắp xếp theo tên tăng dần
                    break;
            }

            // Phân trang
            int totalItems = await products.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var pagedProducts = await products.Skip((page - 1) * pageSize)
                                              .Take(pageSize)
                                              .ToListAsync();

            // Truyền dữ liệu vào ViewBag
            ViewBag.Search = search;
            ViewBag.SortOrder = sortOrder;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;

            return View(pagedProducts);
        }

        // GET: ProductManage/Details/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: ProductManage/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductId,ProductName,CategoryId,Price,StockQuantity,ImageUrl,Description,CreatedAt")] Product product)
        {
            if (!ModelState.IsValid)
            {
                // Log lỗi để debug
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine(error.ErrorMessage);
                }

                ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName", product.CategoryId);
                return View(product);
            }

            _context.Add(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        // GET: ProductManage/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName", product.CategoryId);
            return View(product);
        }

        // POST: ProductManage/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductId,ProductName,CategoryId,Price,StockQuantity,ImageUrl,Description,CreatedAt")] Product product)
        {
            if (id != product.ProductId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.ProductId))
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
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName", product.CategoryId);
            return View(product);
        }

        // GET: ProductManage/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: ProductManage/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }
    }
}
