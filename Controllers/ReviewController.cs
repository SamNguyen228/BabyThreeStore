using Microsoft.AspNetCore.Mvc;
using WebsiteBaby3.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace WebsiteTMDT.Controllers
{
    [Route("api/reviews")]
    [ApiController]
    public class ReviewsController : ControllerBase
    {
        private readonly WebBaby3Context _context;

        public ReviewsController(WebBaby3Context context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> PostReview([FromBody] ReviewModel model)
        {
            if (model == null || model.danhGia < 1 || model.danhGia > 5)
                return BadRequest("Dữ liệu đánh giá không hợp lệ.");

            var user = _context.Users.FirstOrDefault(u => u.Email == model.email);
            if (user == null)
                return BadRequest("Người dùng không tồn tại.");

            // Kiểm tra xem user đã mua sản phẩm và đơn hàng ở trạng thái 'Completed' chưa
            var hasCompletedOrder = _context.Orders
                .Where(o => o.UserId == user.UserId && o.Status == "Completed")
                .Join(_context.OrderDetails,
                      o => o.OrderId,
                      od => od.OrderId,
                      (o, od) => od)
                .Any(od => od.ProductId == model.maSanPham);

            if (!hasCompletedOrder)
            {
                return BadRequest("Bạn chưa trải nghiệm sản phẩm nên không thể đánh giá.");
            }

            var review = new Review
            {
                UserId = user.UserId,
                ProductId = model.maSanPham,
                ReviewText = model.noiDung,
                Rating = model.danhGia,
                CreatedAt = DateTime.Now
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đánh giá đã được thêm thành công!" });
        }
    }
    public class ReviewModel
    {
        public string tenKhachHang { get; set; }
        public string email { get; set; }
        public string noiDung { get; set; }
        public int danhGia { get; set; }
        public int maSanPham { get; set; }
    }
}
