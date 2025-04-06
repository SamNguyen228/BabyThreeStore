using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.EntityFrameworkCore;
using System.Configuration;
using WebsiteBaby3.Models;

namespace WebsiteBaby3
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            //// Thêm kết nối Database vào DI
            //builder.Services.AddDbContext<WebBaby3Context>(options =>
            //    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddDbContext<WebBaby3Context>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
            sqlOptions => sqlOptions.EnableRetryOnFailure()));

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
             .AddCookie(options =>
             {
                 var config = builder.Configuration.GetSection("Authentication:Cookie");
                 options.LoginPath = config["LoginPath"];
                 options.LogoutPath = config["LogoutPath"];
                 options.AccessDeniedPath = config["AccessDeniedPath"];
             })
             .AddGoogle(options =>
             {
                 var googleConfig = builder.Configuration.GetSection("Authentication:Google");
                 options.ClientId = googleConfig["ClientId"];
                 options.ClientSecret = googleConfig["ClientSecret"];
                 options.CallbackPath = "/signin-google";
             })
             .AddFacebook(options =>
             {
                 var facebookConfig = builder.Configuration.GetSection("Authentication:Facebook");
                 options.AppId = facebookConfig["AppId"];
                 options.AppSecret = facebookConfig["AppSecret"];
                 options.CallbackPath = "/signin-facebook";
                 options.SaveTokens = true;
             });

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.SameSite = SameSiteMode.None;
            });

            // Cấu hình Authorization
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
                options.AddPolicy("CustomerPolicy", policy => policy.RequireRole("Customer"));
            });

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.None;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
            });

            builder.Services.AddAuthorization();
            // Add services to the container.
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<WebBaby3Context>();

                var users = context.Users.Where(u => !u.PasswordHash.StartsWith("$2a$")).ToList();
                foreach (var user in users)
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash, 10);
                }

                if (users.Any())
                {
                    context.SaveChanges();
                    Console.WriteLine("✅ Đã cập nhật mật khẩu cũ thành mã hóa BCrypt.");
                }
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseCors("AllowAll");
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
