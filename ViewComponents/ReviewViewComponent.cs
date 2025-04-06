using Microsoft.AspNetCore.Mvc;
using WebsiteBaby3.ViewModels;
using System.Linq;
using WebsiteBaby3.Models;

namespace WebsiteBaby3.ViewComponents
{
    public class ReviewViewComponent : ViewComponent
    {
        private readonly WebBaby3Context db;

        public ReviewViewComponent(WebBaby3Context context) => db = context;

        public IViewComponentResult Invoke(int productId)
        {
            var data = db.Reviews
                .Where(rv => rv.ProductId == productId)
                .OrderByDescending(rv => rv.CreatedAt)
                .Select(rv => new ReviewVM
                {
                    maReview = rv.ReviewId,
                    maKhachHang = rv.UserId,
                    tenKhachHang = rv.User != null ? rv.User.FullName : "Ẩn danh",
                    maSanPham = rv.ProductId,
                    danhGia = rv.Rating,
                    noiDung = rv.ReviewText,
                    ngayDang = rv.CreatedAt
                })
                .ToList();

            return View(data);
        }
    }
}
