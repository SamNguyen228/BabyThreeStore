using ClosedXML.Excel;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebsiteBaby3.Models;

namespace WebsiteBaby3.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportController : Controller
    {
        private readonly WebBaby3Context _context;

        public ReportController(WebBaby3Context context)
        {
            _context = context;
        }

        // Trang báo cáo hiển thị biểu đồ
        [Authorize(Roles = "Admin")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("api/sales-report")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetSalesReport([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            var query = _context.Orders
                .Where(o => o.Status == "Completed");

            if (fromDate.HasValue)
                query = query.Where(o => o.CreatedAt >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(o => o.CreatedAt <= toDate.Value);

            var totalRevenue = query.Sum(o => o.TotalAmount);
            var totalCustomers = query.Select(o => o.UserId).Distinct().Count();
            var totalProductsSold = _context.OrderDetails
                .Where(od => query.Select(o => o.OrderId).Contains(od.OrderId))
                .Sum(od => od.Quantity);

            var salesOverTime = query
                .GroupBy(o => new { o.CreatedAt.Value.Year, o.CreatedAt.Value.Month })
                .Select(g => new { year = g.Key.Year, month = g.Key.Month, revenue = g.Sum(o => o.TotalAmount) })
                .ToList();

            var salesByCategory = _context.OrderDetails
                .Where(od => query.Select(o => o.OrderId).Contains(od.OrderId))
                .GroupBy(od => od.Product.Category.CategoryName)
                .Select(g => new { category = g.Key, count = g.Sum(od => od.Quantity) })
                .ToDictionary(x => x.category, x => x.count);

            // Lấy thông tin chi tiết khách hàng
            var customerDetails = query
                .GroupBy(o => new { o.UserId, o.User.FullName, o.User.Email })
                .Select(g => new
                {
                    userId = g.Key.UserId,
                    name = g.Key.FullName,
                    email = g.Key.Email,
                    orderCount = g.Count(),
                    totalSpent = g.Sum(o => o.TotalAmount)
                })
                .ToList();

            // Lấy thông tin chi tiết sản phẩm bán ra
            var productDetails = _context.OrderDetails
                .Where(od => query.Select(o => o.OrderId).Contains(od.OrderId))
                .GroupBy(od => new { od.ProductId, od.Product.ProductName, od.Product.Category.CategoryName })
                .Select(g => new
                {
                    productId = g.Key.ProductId,
                    productName = g.Key.ProductName,
                    category = g.Key.CategoryName,
                    quantitySold = g.Sum(od => od.Quantity),
                    totalRevenue = g.Sum(od => od.Quantity * od.Price)
                })
                .ToList();

            return Ok(new
            {
                totalRevenue,
                totalCustomers,
                totalProductsSold,
                salesOverTime,
                salesByCategory,
                customerDetails,
                productDetails
            });
        }

        // Xuất báo cáo ra Excel
        [Authorize(Roles = "Admin")]
        public IActionResult ExportToExcel(DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Orders
                .Where(o => o.Status == "Completed")
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(o => o.CreatedAt >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(o => o.CreatedAt <= toDate.Value);

            var completedOrders = query.ToList();

            var totalRevenue = completedOrders.Sum(o => o.TotalAmount);
            var totalCustomers = completedOrders.Select(o => o.UserId).Distinct().Count();
            var totalProductsSold = completedOrders.SelectMany(o => o.OrderDetails).Sum(od => od.Quantity);

            // Thống kê chi tiết sản phẩm
            var productDetails = completedOrders
                .SelectMany(o => o.OrderDetails)
                .GroupBy(od => new { od.ProductId, od.Product.ProductName })
                .Select(g => new
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    QuantitySold = g.Sum(od => od.Quantity),
                    TotalRevenue = g.Sum(od => od.Quantity * od.Price)
                })
                .ToList();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Sales Report");

            worksheet.Cell(1, 1).Value = "Thống kê doanh thu";
            worksheet.Cell(2, 1).Value = "Tổng doanh thu:";
            worksheet.Cell(2, 2).Value = totalRevenue;

            worksheet.Cell(3, 1).Value = "Tổng số khách hàng:";
            worksheet.Cell(3, 2).Value = totalCustomers;

            worksheet.Cell(4, 1).Value = "Tổng số sản phẩm đã bán:";
            worksheet.Cell(4, 2).Value = totalProductsSold;

            // Thêm bảng chi tiết sản phẩm
            var startRow = 6;
            worksheet.Cell(startRow, 1).Value = "Mã SP";
            worksheet.Cell(startRow, 2).Value = "Tên sản phẩm";
            worksheet.Cell(startRow, 3).Value = "Số lượng bán";
            worksheet.Cell(startRow, 4).Value = "Doanh thu";

            int row = startRow + 1;
            foreach (var product in productDetails)
            {
                worksheet.Cell(row, 1).Value = product.ProductId;
                worksheet.Cell(row, 2).Value = product.ProductName;
                worksheet.Cell(row, 3).Value = product.QuantitySold;
                worksheet.Cell(row, 4).Value = product.TotalRevenue;
                row++;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "SalesReport.xlsx");
        }

        // Xuất báo cáo ra Csv
        [Authorize(Roles = "Admin")]
        public IActionResult ExportToCsv()
        {
            var completedOrders = _context.Orders
                .Where(o => o.Status == "Completed")
                .Include(o => o.OrderDetails)
                .ToList();

            var totalRevenue = completedOrders.Sum(o => o.TotalAmount);
            var totalCustomers = completedOrders.Select(o => o.UserId).Distinct().Count();
            var totalProductsSold = completedOrders
                .SelectMany(o => o.OrderDetails)
                .Sum(od => od.Quantity);

            var csv = new StringBuilder();
            csv.AppendLine("\uFEFFThống kê doanh thu");
            csv.AppendLine($"Tổng doanh thu;{totalRevenue}");
            csv.AppendLine($"Tổng số khách hàng;{totalCustomers}");
            csv.AppendLine($"Tổng số sản phẩm đã bán;{totalProductsSold}");

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv; charset=utf-8", "SalesReport.csv");
        }

    }
}
