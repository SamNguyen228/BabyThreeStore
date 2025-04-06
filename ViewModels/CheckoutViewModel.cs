namespace WebsiteBaby3.ViewModels
{
    public class CheckoutViewModel
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public int? PromotionId { get; set; }
        public List<CartItem> CartItems { get; set; }
        public string PromotionCode { get; set; }
        public double DiscountAmount { get; set; } // Nhận từ Controller
        public double TotalAmount { get; set; }    // Đã tính giảm giá
    }
}
