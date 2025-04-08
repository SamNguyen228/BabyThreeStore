using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.EntityFrameworkCore;
using System.Configuration;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
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
                options.ClientId = builder.Configuration["Authentication_Google_ClientId"];
                options.ClientSecret = builder.Configuration["Authentication_Google_ClientSecret"];
                options.CallbackPath = "/signin-google";
            })
            .AddFacebook(options =>
            {
                options.AppId = builder.Configuration["Authentication_Facebook_AppId"];
                options.AppSecret = builder.Configuration["Authentication_Facebook_AppSecret"];
                options.CallbackPath = "/signin-facebook";
                options.SaveTokens = true;
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

             builder.Services.Configure<RequestLocalizationOptions>(options =>
             {
                 // Tạo danh sách các CultureInfo từ mảng string
                 var supportedCultures = new[] { "en-US", "en-GB" }
                     .Select(culture => new CultureInfo(culture))
                     .ToList();
            
                 options.DefaultRequestCulture = new RequestCulture("en-US");
                 options.SupportedCultures = supportedCultures;
                 options.SupportedUICultures = supportedCultures;
             });

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

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

             // Cấu hình Localization Middleware
             var localizationOptions = new RequestLocalizationOptions()
                 .SetDefaultCulture("en-US")
                 .AddSupportedCultures("en-US", "en-GB")
                 .AddSupportedUICultures("en-US", "en-GB");
            
             app.UseRequestLocalization(localizationOptions);

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
