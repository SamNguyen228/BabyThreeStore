using WebsiteBaby3.Models;

namespace WebsiteBaby3.ViewModels
{
    public class ProductsVM
    {
        public int MaSP { get; set; }
        public string TenSP { get; set; }
        public string MoTa { get; set; }
        public string HinhAnh { get; set; }
        public double Gia { get; set; }
        public int SoLuong { get; set; }
        public int MaLoai { get; set; }
        public string TenLoai { get; set; }
    }

    public class ProductsDetailVM
    {
        public int MaSP { get; set; }
        public string TenSP { get; set; }
        public string MoTa { get; set; }
        public string HinhAnh { get; set; }
        public double Gia { get; set; }
        public int SoLuong { get; set; }
        public int MaLoai { get; set; }
        public string TenLoai { get; set; }
        public string ChiTiet { get; set; }
        public int DiemDanhGia { get; set; }
        public int SoLuongTon { get; set; }
        public string MauSac { get; set; }
        public string DungLuong { get; set; }
        public List<Product> Products { get; set; }
        // Danh sách các đánh giá
        public List<ReviewVM> Reviews { get; set; } = new List<ReviewVM>();
        public bool DaMuaHang { get; set; }
        public bool ChoPhepDanhGia { get; set; }
    }
}
