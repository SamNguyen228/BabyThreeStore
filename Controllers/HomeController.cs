using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebsiteBaby3.Models;
using WebsiteBaby3.ViewModels;

namespace WebsiteBaby3.Controllers
{
    public class HomeController : Controller
    {
        private readonly WebBaby3Context db;
        public HomeController(WebBaby3Context context)
        {
            db = context;
        }

        public IActionResult Index(int? loai)
        {
            var products = db.Products.AsQueryable();
            if (loai.HasValue)
            {
                products = products.Where(p => p.CategoryId == loai.Value);
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

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult Contact()
        {
            return View();
        }

        [Authorize]
        public IActionResult DashBoard()
        {
            return View();
        }
    }
}
