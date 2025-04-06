using WebsiteBaby3.Helpers;
using WebsiteBaby3.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace WebsiteBaby3.ViewComponents
{
    public class CartQuickViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var cart = HttpContext.Session.Get<List<CartItem>>(MySetting.CART_KEY) ?? new List<CartItem>();

            return View(cart); 
        }
    }
}
