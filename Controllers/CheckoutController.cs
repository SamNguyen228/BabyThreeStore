using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Threading.Tasks;
using WebsiteBaby3.Models;
using WebsiteBaby3.ViewModels;

namespace WebsiteBaby3.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly WebBaby3Context _context;

        public CheckoutController(WebBaby3Context context)
        {
            _context = context;
        }

        [Authorize(Roles = "Customer")]
        public IActionResult Index()
        {
            var userId = GetUserId();

            var cartItems = _context.Carts
                .Where(c => c.UserId == userId)
                .Select(c => new CartItem
                {
                    MaSP = c.Product.ProductId,
                    TenSP = c.Product.ProductName,
                    Gia = (double)c.Product.Price,
                    HinhAnh = c.Product.ImageUrl,
                    SoLuong = c.Quantity
                }).ToList();

            if (!cartItems.Any())
            {
                TempData["Error"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index", "Cart");
            }

            var user = _context.Users
                .Where(u => u.UserId == userId)
                .Select(u => new
                {
                    u.FullName,
                    u.Email,
                    u.Phone
                })
                .FirstOrDefault();

            var shippingAddress = _context.ShippingAddresses
                .Where(s => s.UserId == userId)
                .Select(s => s.Address)
                .FirstOrDefault();

            var checkoutModel = new CheckoutViewModel
            {
                FullName = user?.FullName ?? "Chưa cập nhật",
                Email = user?.Email ?? "Chưa cập nhật",
                Phone = user?.Phone ?? "Chưa cập nhật",
                Address = shippingAddress ?? "",
                CartItems = cartItems
            };

            return View(checkoutModel);
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> ProcessOrder(int? promotionId, User userForm, List<CartItem> cartItemsForm)
        {
            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                TempData["Error"] = "Người dùng không tồn tại.";
                return RedirectToAction("Index", "Cart");
            }

            // Lấy thông tin từ form nhưng không thay đổi database
            string fullName = !string.IsNullOrWhiteSpace(userForm.FullName) ? userForm.FullName : user.FullName;
            string email = !string.IsNullOrWhiteSpace(userForm.Email) ? userForm.Email : user.Email;
            string phone = !string.IsNullOrWhiteSpace(userForm.Phone) ? userForm.Phone : user.Phone;
            string address = !string.IsNullOrWhiteSpace(userForm.Address) ? userForm.Address : user.Address;

            // Lấy giỏ hàng từ form hoặc database
            var cartItems = cartItemsForm.Any() ? cartItemsForm : await _context.Carts
                .Where(c => c.UserId == userId)
                .Include(c => c.Product)
                .Select(c => new CartItem
                {
                    MaSP = c.Product.ProductId,
                    TenSP = c.Product.ProductName,
                    Gia = (double)c.Product.Price,
                    HinhAnh = c.Product.ImageUrl,
                    SoLuong = c.Quantity
                }).ToListAsync();

            if (!cartItems.Any())
            {
                TempData["Error"] = "Giỏ hàng trống hoặc không hợp lệ.";
                return RedirectToAction("Index", "Cart");
            }

            // Lấy danh sách sản phẩm để kiểm tra tồn kho
            var productIds = cartItems.Select(c => c.MaSP).ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.ProductId))
                .ToDictionaryAsync(p => p.ProductId);

            foreach (var cartItem in cartItems)
            {
                if (!products.ContainsKey(cartItem.MaSP) || products[cartItem.MaSP].StockQuantity < cartItem.SoLuong)
                {
                    TempData["Error"] = $"Sản phẩm '{cartItem.TenSP}' không đủ hàng trong kho.";
                    return RedirectToAction("Index", "Cart");
                }
            }

            // Tính tổng tiền
            decimal totalAmount = cartItems.Sum(c => (decimal)c.Gia * c.SoLuong);
            decimal discountAmount = 0;
            Promotion? promotion = null;

            if (promotionId.HasValue)
            {
                promotion = await _context.Promotions.FindAsync(promotionId.Value);
                if (promotion != null && DateTime.Now >= promotion.StartDate && DateTime.Now <= promotion.EndDate)
                {
                    if (!promotion.MinOrderValue.HasValue || totalAmount >= promotion.MinOrderValue.Value)
                    {
                        decimal discountByAmount = promotion.DiscountAmount ?? 0;
                        decimal discountByPercentage = (totalAmount * (promotion.DiscountPercentage ?? 0)) / 100;
                        discountAmount = promotion.DiscountAmount.HasValue ? discountByAmount : discountByPercentage;

                        if (promotion.MaxDiscount.HasValue)
                        {
                            discountAmount = Math.Min(discountAmount, promotion.MaxDiscount.Value);
                        }
                    }
                }
            }

            decimal finalAmount = totalAmount - discountAmount;

            // Tạo đơn hàng
            var order = new Order
            {
                UserId = userId,
                TotalAmount = finalAmount,
                DiscountAmount = discountAmount,
                Status = "Pending", // Không xử lý ngay
                CreatedAt = DateTime.Now,
                PromotionId = promotion?.PromotionId
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync(); // Lưu để có OrderId

            // Tạo chi tiết đơn hàng
            var orderDetails = cartItems.Select(item => new OrderDetail
            {
                OrderId = order.OrderId,
                ProductId = item.MaSP,
                Quantity = item.SoLuong,
                Price = (decimal)item.Gia
            }).ToList();

            _context.OrderDetails.AddRange(orderDetails);

            // Xoá giỏ hàng
            _context.Carts.RemoveRange(_context.Carts.Where(c => c.UserId == userId));

            // Tạo thông báo cho user
            _context.Notifications.Add(new Notification
            {
                Content = $"Đơn hàng đã được đặt thành công. Tổng tiền: {finalAmount:N0} $",
                CreatedAt = DateTime.Now,
                IsRead = false,
                TargetRole = "Customer",
                UserId = userId
            });

            // Tạo thông báo cho admin
            _context.Notifications.Add(new Notification
            {
                Content = $"Đơn hàng #{order.OrderId} của khách hàng {fullName} đã được đặt thành công. Tổng tiền: {finalAmount:N0} $",
                CreatedAt = DateTime.Now,
                IsRead = false,
                TargetRole = "Admin"
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đặt hàng thành công!";
            return RedirectToAction("OrderConfirmation", new { orderId = order.OrderId });
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> OrderConfirmation(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(order);
        }

        //[Authorize(Roles = "Customer")]
        //private async Task SendConfirmationEmail(string? userEmailForm, string? fullNameForm, string? phoneForm, string? addressForm, Order order)
        //{
        //    try
        //    {
        //        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == order.UserId);
        //        if (user == null) return;

        //        // Chỉ lấy thông tin từ form, KHÔNG cập nhật database
        //        string userEmail = !string.IsNullOrWhiteSpace(userEmailForm) ? userEmailForm : user.Email;
        //        string fullName = !string.IsNullOrWhiteSpace(fullNameForm) ? fullNameForm : user.FullName;
        //        string phoneNumber = !string.IsNullOrWhiteSpace(phoneForm) ? phoneForm : user.Phone;
        //        string address = !string.IsNullOrWhiteSpace(addressForm) ? addressForm : user.Address;

        //        if (string.IsNullOrEmpty(userEmail))
        //        {
        //            Console.WriteLine("Không có email để gửi.");
        //            return;
        //        }

        //        var smtpClient = new SmtpClient("smtp.gmail.com")
        //        {
        //            Port = 587,
        //            Credentials = new NetworkCredential("duonghoangsamet@gmail.com", "fuym cdrm oqep kgyh"),
        //            EnableSsl = true,
        //        };

        //        var orderDetails = await _context.OrderDetails
        //            .Where(od => od.OrderId == order.OrderId)
        //            .Include(od => od.Product)
        //            .ToListAsync();

        //        string productDetails = orderDetails.Aggregate("", (current, item) => current + $@"
        //        <tr>
        //            <td style='border:1px solid #ddd;padding:8px;'>{item.Product.ProductName}</td>
        //            <td style='border:1px solid #ddd;padding:8px;text-align:center;'>{item.Quantity}</td>
        //            <td style='border:1px solid #ddd;padding:8px;text-align:right;'>{item.Price:C}</td>
        //            <td style='border:1px solid #ddd;padding:8px;text-align:right;'>{(item.Price * item.Quantity):C}</td>
        //        </tr>");

        //        string subject = "Xác nhận đơn hàng - Apple Store";
        //        string body = $@"
        //            <h2 style='color:#2d89ef;'>Cảm ơn bạn đã đặt hàng tại Apple Store!</h2>
        //            <p style='font-size:16px;'>Đơn hàng của bạn đã được đặt thành công.</p>
        //            <p><strong>Mã đơn hàng:</strong> #{order.OrderId}</p>
        //            <p><strong>Ngày đặt hàng:</strong> {order.CreatedAt:dd/MM/yyyy HH:mm}</p>
        //            <p><strong>Tổng tiền:</strong> {order.TotalAmount:C}</p>
        //            <p><strong>Số tiền giảm giá:</strong> {order.DiscountAmount:C}</p>

        //            <h3>Thông tin khách hàng:</h3>
        //            <p><strong>Họ và tên:</strong> {fullName}</p>
        //            <p><strong>Số điện thoại:</strong> {phoneNumber}</p>
        //            <p><strong>Địa chỉ giao hàng:</strong> {address}</p>

        //            <h3>Chi tiết đơn hàng:</h3>
        //            <table style='border-collapse:collapse;width:100%;font-size:16px;'>
        //                <thead>
        //                    <tr style='background:#f4f4f4;'>
        //                        <th style='border:1px solid #ddd;padding:8px;'>Tên sản phẩm</th>
        //                        <th style='border:1px solid #ddd;padding:8px;'>Số lượng</th>
        //                        <th style='border:1px solid #ddd;padding:8px;'>Đơn giá</th>
        //                        <th style='border:1px solid #ddd;padding:8px;'>Thành tiền</th>
        //                    </tr>
        //                </thead>
        //                <tbody>
        //                    {productDetails}
        //                </tbody>
        //            </table>

        //            <p style='font-size:16px;'>Chúng tôi sẽ sớm giao hàng đến bạn. <br> Cảm ơn vì đã mua sắm tại <strong>Apple Store!</strong></p>
        //            <p style='font-size:14px;color:gray;'>Nếu có bất kỳ vấn đề gì, vui lòng liên hệ <a href='mailto:support@websitetmdt.com'>support@websitetmdt.com</a></p>
        //        ";

        //        var mailMessage = new MailMessage
        //        {
        //            From = new MailAddress("duonghoangsamet@gmail.com", "Apple Store"),
        //            Subject = subject,
        //            Body = body,
        //            IsBodyHtml = true,
        //        };

        //        mailMessage.To.Add(userEmail);

        //        await smtpClient.SendMailAsync(mailMessage);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Lỗi gửi email: {ex.Message}");
        //    }
        //}

        [HttpGet]
        public async Task<IActionResult> ApplyPromotion(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return Json(new { success = false, message = "Mã giảm giá không hợp lệ." });
            }

            // Chỉ lấy theo PromotionName
            var promotion = await _context.Promotions
                .FirstOrDefaultAsync(p => p.PromotionName == code
                                        && DateTime.Now >= p.StartDate
                                        && DateTime.Now <= p.EndDate);

            if (promotion == null)
            {
                return Json(new { success = false, message = "Mã giảm giá không tồn tại hoặc đã hết hạn." });
            }

            var userId = GetUserId();
            var cartItems = _context.Carts
                .Where(c => c.UserId == userId)
                .Include(c => c.Product)
                .ToList();

            if (!cartItems.Any())
            {
                return Json(new { success = false, message = "Giỏ hàng trống." });
            }

            decimal totalAmount = cartItems.Sum(c => (decimal)c.Product.Price * c.Quantity);
            decimal discountAmount = promotion.DiscountAmount.HasValue
                ? promotion.DiscountAmount.Value
                : (totalAmount * promotion.DiscountPercentage.GetValueOrDefault()) / 100;

            if (promotion.MaxDiscount.HasValue)
            {
                discountAmount = Math.Min(discountAmount, promotion.MaxDiscount.Value);
            }

            decimal finalTotal = totalAmount - discountAmount;

            return Json(new
            {
                success = true,
                promotionId = promotion.PromotionId,
                discount = discountAmount,
                totalAmount = finalTotal
            });
        }
        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        }
    }
}
