using Microsoft.AspNetCore.Mvc;
using WebsiteBaby3.Models;
using WebsiteBaby3.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace WebsiteBaby3.Controllers
{
    public class CartController : Controller
    {
        private readonly WebBaby3Context db;

        public CartController(WebBaby3Context context)
        {
            db = context;
        }

        public List<CartItem> Cart
        {
            get
            {
                var userId = GetUserId(); 
                return db.Carts
                         .Where(c => c.UserId == userId)
                         .Select(c => new CartItem
                         {
                             MaSP = c.Product.ProductId,
                             TenSP = c.Product.ProductName,
                             Gia = (double)c.Product.Price,
                             HinhAnh = c.Product.ImageUrl,
                             SoLuong = c.Quantity
                         }).ToList();
            }
        }

        [Authorize(Roles = "Customer")]
        public IActionResult Index()
        {
            var userId = GetUserId();
            var cartItems = db.Carts
                              .Where(c => c.UserId == userId)
                              .Select(c => new CartItem
                              {
                                  MaSP = c.Product.ProductId,
                                  TenSP = c.Product.ProductName,
                                  Gia = (double)c.Product.Price,
                                  HinhAnh = c.Product.ImageUrl,
                                  SoLuong = c.Quantity
                              }).ToList();

            ViewBag.CartCount = cartItems.Sum(c => c.SoLuong);
            return View(cartItems);
        }

        [Authorize(Roles = "Customer")]
        [HttpPost]
        public IActionResult AddToCart(int id, int quantity = 1)
        {
            var userId = GetUserId();
            var product = db.Products.SingleOrDefault(p => p.ProductId == id);

            if (product == null)
            {
                TempData["Error"] = "Sản phẩm không tồn tại";
                return Redirect(Request.Headers["Referer"].ToString());
            }

            // Kiểm tra số lượng tồn kho
            if (product.StockQuantity < quantity)
            {
                TempData["AddError"] = "Sản phẩm đã hết hàng hoặc số lượng không đủ!";
                return Redirect(Request.Headers["Referer"].ToString());
            }

            var cartItem = db.Carts.SingleOrDefault(c => c.UserId == userId && c.ProductId == id);

            if (cartItem == null)
            {
                cartItem = new Cart
                {
                    UserId = userId,
                    ProductId = product.ProductId,
                    Quantity = quantity
                };
                db.Carts.Add(cartItem);
            }
            else
            {
                // Kiểm tra số lượng tồn kho trước khi tăng số lượng
                if (cartItem.Quantity + quantity > product.StockQuantity)
                {
                    TempData["AddError"] = "Số lượng sản phẩm trong giỏ hàng vượt quá tồn kho!";
                    return Redirect(Request.Headers["Referer"].ToString());
                }
                cartItem.Quantity += quantity;
            }

            db.SaveChanges();
            TempData["AddSuccess"] = "Sản phẩm đã được thêm vào giỏ hàng!";
            return Redirect(Request.Headers["Referer"].ToString());
        }

        [Authorize(Roles = "Customer")]
        public IActionResult RemoveCart(int id)
        {
            var userId = GetUserId();
            var cartItem = db.Carts.SingleOrDefault(c => c.UserId == userId && c.ProductId == id);

            if (cartItem != null)
            {
                db.Carts.Remove(cartItem);
                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult UpdateQuantity(int id, int quantity)
        {
            var userId = GetUserId();
            var cartItem = db.Carts.SingleOrDefault(c => c.UserId == userId && c.ProductId == id);

            if (cartItem != null)
            {
                cartItem.Quantity = quantity;
                db.SaveChanges();
            }

            // Tính tổng tiền và tổng số lượng sản phẩm
            var total = db.Carts.Where(c => c.UserId == userId).Sum(c => c.Product.Price * c.Quantity);
            var totalQuantity = db.Carts.Where(c => c.UserId == userId).Sum(c => c.Quantity);

            return Json(new
            {
                success = true,
                total = total.ToString("0.00"),
                quantity = totalQuantity
            });
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
        }
    }
}
