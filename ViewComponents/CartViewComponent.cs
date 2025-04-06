using WebsiteBaby3.Helpers;
using WebsiteBaby3.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using WebsiteBaby3.Models;

namespace WebsiteBaby3.ViewComponents
{
    public class CartViewComponent : ViewComponent
    {
        private readonly WebBaby3Context _context;

        public CartViewComponent(WebBaby3Context context)
        {
            _context = context;
        }

        public IViewComponentResult Invoke()
        {
            var userId = GetUserId();
            var cartCount = _context.Carts
                                    .Where(c => c.UserId == userId)
                                    .Sum(c => c.Quantity);

            return View(cartCount);
        }

        private int GetUserId()
        {
            var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
        }
    }

}
