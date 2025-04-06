using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBaby3.Models;
using WebsiteBaby3.ViewModels;

namespace WebsiteBaby3.Controllers
{
    public class ProductController : Controller
    {
        private readonly WebBaby3Context db;

        public ProductController(WebBaby3Context context)
        {
            db = context;
        }

        public IActionResult Index(int? loai, string search, string sortOrder, int page = 1, int pageSize = 8)
        {
            var products = db.Products.AsQueryable();

            if (loai.HasValue)
            {
                products = products.Where(p => p.CategoryId == loai.Value);
            }

            if (!string.IsNullOrEmpty(search))
            {
                products = products.Where(p => p.ProductName.Contains(search));
            }

            // Xử lý sắp xếp theo yêu cầu
            switch (sortOrder)
            {
                case "price_asc":
                    products = products.OrderBy(p => p.Price);
                    break;
                case "price_desc":
                    products = products.OrderByDescending(p => p.Price);
                    break;
                case "category":
                    products = products.OrderBy(p => p.Category.CategoryName);
                    break;
                default:
                    products = products.OrderBy(p => p.ProductName);
                    break;
            }

            int totalItems = products.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var result = products.Skip((page - 1) * pageSize).Take(pageSize)
                .Select(p => new ProductsVM
                {
                    MaSP = p.ProductId,
                    TenSP = p.ProductName,
                    HinhAnh = p.ImageUrl,
                    Gia = (double)p.Price,
                    MoTa = p.Description,
                    TenLoai = p.Category.CategoryName
                }).ToList();

            ViewBag.Search = search;
            ViewBag.SortOrder = sortOrder;
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;
            ViewBag.Loai = loai;
            ViewBag.PageSize = pageSize;

            return View(result);
        }

        public IActionResult Search(string? query)
        {
            var products = db.Products.AsQueryable();
            if (query != null)
            {
                products = products.Where(p => p.ProductName.Contains(query));
            }

            var result = products.Select(p => new ProductsVM
            {
                MaSP = p.ProductId,
                TenSP = p.ProductName,
                HinhAnh = p.ImageUrl,
                Gia = (double)p.Price,
                MoTa = p.Description,
                TenLoai = p.Category.CategoryName
            });
            return View(result);
        }

        public IActionResult Detail(int id)
        {
            var data = db.Products
                .Include(p => p.Category)
                .SingleOrDefault(p => p.ProductId == id);

            if (data == null)
            {
                TempData["Message"] = $"Không tìm thấy sản phẩm có mã {id}";
                return RedirectToAction("/404");
            }

            var reviews = db.Reviews
                .Where(r => r.ProductId == id)
                .Select(r => new ReviewVM
                {
                    tenKhachHang = r.User.FullName,
                    danhGia = r.Rating,
                    ngayDang = r.CreatedAt,
                    noiDung = r.ReviewText
                })
                .ToList();

            bool daMuaHang = false;
            bool choPhepDanhGia = false;

            try
            {
                int userId = GetUserId();
                int soLanMua = db.OrderDetails
                    .Include(od => od.Order)
                    .Where(od => od.ProductId == id
                              && od.Order.UserId == userId
                              && od.Order.Status.Trim().ToLower() == "completed")
                    .Count();

                int soLanDanhGia = db.Reviews
                    .Where(r => r.ProductId == id && r.UserId == userId)
                    .Count();

                if (soLanMua > 0)
                {
                    daMuaHang = true;
                    if (soLanMua > soLanDanhGia)
                    {
                        choPhepDanhGia = true;
                    }
                }
            }
            catch
            {
                daMuaHang = false;
                choPhepDanhGia = false;
            }

            var result = new ProductsDetailVM
            {
                MaLoai = data.CategoryId,
                MaSP = data.ProductId,
                TenSP = data.ProductName,
                HinhAnh = data.ImageUrl,
                Gia = (double)data.Price,
                MoTa = data.Description,
                TenLoai = data.Category.CategoryName,
                ChiTiet = data.Description ?? string.Empty,
                DiemDanhGia = 5, 
                SoLuongTon = data.StockQuantity,
                SoLuong = data.StockQuantity,
                Reviews = reviews,
                DaMuaHang = daMuaHang,
                ChoPhepDanhGia = choPhepDanhGia
            };

            return View(result);
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
        }
    }
}
