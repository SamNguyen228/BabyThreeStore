namespace WebsiteBaby3.ViewModels
{
    public class CartItem
    {
        public int MaSP { get; set; }
        public string HinhAnh { get; set; }
        public string TenSP { get; set; }
        public double Gia { get; set; }
        public int SoLuong { get; set; }
        public double ThanhTien => Gia * SoLuong;
    }
}
