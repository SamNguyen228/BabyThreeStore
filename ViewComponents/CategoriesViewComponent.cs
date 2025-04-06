using Microsoft.AspNetCore.Mvc;
using WebsiteBaby3.Models;
using WebsiteBaby3.ViewModels;

namespace WebsiteTMDT.ViewComponents
{
    public class CategoriesViewComponent : ViewComponent 
    {
        private readonly WebBaby3Context db;

        public CategoriesViewComponent(WebBaby3Context context) => db = context;

        public IViewComponentResult Invoke()
        {
            var data = db.Categories.Select(lo => new CategoriesVM
            {
                MaLoai = lo.CategoryId,
                TenLoai = lo.CategoryName,
                SoLuong = lo.Products.Count
            }).OrderBy(p => p.TenLoai);
            return View(data); //Default.cshtml
        }
    }
}
