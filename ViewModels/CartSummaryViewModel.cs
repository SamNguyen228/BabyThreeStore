using WebsiteBaby3.ViewModels;

namespace WebsiteBaby3.ViewModels
{

    namespace WebsiteBaby3.ViewModels
    {
        public class CartSummaryViewModel
        {
            public int TotalQuantity { get; set; }
            public double TotalPrice { get; set; }
            public List<CartItem> Items { get; set; }
        }
    }
}
