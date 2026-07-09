using FreshMart.Models;
using FreshMart.Middleware;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
    logging.AddEventSourceLogger();
});

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Environment.EnvironmentName = "Development";

QuestPDF.Settings.License = LicenseType.Community;

// Register DbContext with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
// Always use custom global exception handler
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

// RBAC Middleware ensures Admin routes are secured
app.UseMiddleware<AdminAuthMiddleware>();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ---------------- Seed Data -----------------

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Clear existing products and categories to reseed with enhanced data
    // IMPORTANT: Comment out this section after first run to preserve data
    /*
    if (db.Products.Any())
    {
        db.Products.RemoveRange(db.Products);
        db.SaveChanges();
    }

    if (db.Categories.Any())
    {
        db.Categories.RemoveRange(db.Categories);
        db.SaveChanges();
    }
    */

    // Categories Seed
    if (!db.Categories.Any())
    {
        db.Categories.AddRange(
            new Category { CategoryName = "Trái cây", Description = "Trái cây tươi ngon" },
            new Category { CategoryName = "Rau củ", Description = "Rau củ tươi sạch" },
            new Category { CategoryName = "Sữa & Bơ", Description = "Sữa và các sản phẩm từ sữa" },
            new Category { CategoryName = "Bánh ngọt", Description = "Bánh mì và bánh ngọt" }
        );

        db.SaveChanges();
    }

    // Seed Admin Users
    if (!db.Users.Any())
    {
        // Hash function for passwords
        string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        db.Users.AddRange(
            new User
            {
                FullName = "Admin User",
                Email = "admin@freshmart.com",
                PasswordHash = HashPassword("Admin123"),
                Role = "Admin",
                CreatedAt = DateTime.Now
            },
            new User
            {
                FullName = "John Doe",
                Email = "john@example.com",
                PasswordHash = HashPassword("Test123"),
                Role = "Customer",
                CreatedAt = DateTime.Now
            }
        );

        db.SaveChanges();
    }

    // Products Seed
    if (!db.Products.Any())
    {
        var fruitsId = db.Categories.First(c => c.CategoryName == "Trái cây").CategoryId;
        var vegetablesId = db.Categories.First(c => c.CategoryName == "Rau củ").CategoryId;
        var dairyId = db.Categories.First(c => c.CategoryName == "Sữa & Bơ").CategoryId;
        var bakeryId = db.Categories.First(c => c.CategoryName == "Bánh ngọt").CategoryId;

        db.Products.AddRange(
            // TRÁI CÂY
            new Product
            {
                Name = "Táo tươi",
                Description = "Táo đỏ giòn ngọt, thích hợp ăn vặt hoặc làm bánh",
                Price = 3.99m,
                DiscountPrice = 2.99m,
                Weight = "1kg",
                Stock = 50,
                CategoryId = fruitsId,
                ImagePath = "apples.jpg"
            },
            new Product
            {
                Name = "Chuối hữu cơ",
                Description = "Chuối hữu cơ chín tự nhiên, giàu kali",
                Price = 2.49m,
                Weight = "1kg",
                Stock = 75,
                CategoryId = fruitsId,
                ImagePath = "bananas.jpg"
            },
            new Product
            {
                Name = "Dâu tây tươi",
                Description = "Dâu tây ngọt mọng nước, vừa thu hoạch",
                Price = 5.99m,
                DiscountPrice = 4.99m,
                Weight = "500g",
                Stock = 30,
                CategoryId = fruitsId,
                ImagePath = "strawberries.jpg"
            },
            new Product
            {
                Name = "Việt quất",
                Description = "Việt quất tươi giàu chất chống oxy hóa",
                Price = 6.99m,
                Weight = "250g",
                Stock = 40,
                CategoryId = fruitsId,
                ImagePath = "blueberries.jpg"
            },
            new Product
            {
                Name = "Cam ngọt",
                Description = "Cam Valencia mọng nước, giàu Vitamin C",
                Price = 4.49m,
                Weight = "1kg",
                Stock = 60,
                CategoryId = fruitsId,
                ImagePath = "oranges.jpg"
            },
            new Product
            {
                Name = "Nho đỏ",
                Description = "Nho đỏ không hạt, thích hợp ăn vặt",
                Price = 5.49m,
                Weight = "500g",
                Stock = 35,
                CategoryId = fruitsId,
                ImagePath = "grapes.jpg"
            },
            new Product
            {
                Name = "Xoài tươi",
                Description = "Xoài nhiệt đới ngọt lịm, ăn liền",
                Price = 3.99m,
                Weight = "1 trái",
                Stock = 25,
                CategoryId = fruitsId,
                ImagePath = "mango.jpg"
            },
            new Product
            {
                Name = "Dưa hấu",
                Description = "Dưa hấu to mọng nước, hoàn hảo cho mùa hè",
                Price = 7.99m,
                Weight = "1 trái",
                Stock = 15,
                CategoryId = fruitsId,
                ImagePath = "watermelon.jpg"
            },
            new Product
            {
                Name = "Dứa tươi",
                Description = "Dứa tươi chua ngọt thơm lừng",
                Price = 4.99m,
                Weight = "1 trái",
                Stock = 20,
                CategoryId = fruitsId,
                ImagePath = "pineapple.jpg"
            },

            // RAU CỦ
            new Product
            {
                Name = "Bông cải xanh",
                Description = "Bông cải xanh tươi giàu dinh dưỡng",
                Price = 2.99m,
                Weight = "500g",
                Stock = 45,
                CategoryId = vegetablesId,
                ImagePath = "broccoli.jpg"
            },
            new Product
            {
                Name = "Cà rốt hữu cơ",
                Description = "Cà rốt hữu cơ ngọt giòn",
                Price = 2.49m,
                Weight = "1kg",
                Stock = 55,
                CategoryId = vegetablesId,
                ImagePath = "carrots.jpg"
            },
            new Product
            {
                Name = "Cà chua tươi",
                Description = "Cà chua chín cây, hoàn hảo cho salad",
                Price = 3.49m,
                Weight = "500g",
                Stock = 50,
                CategoryId = vegetablesId,
                ImagePath = "tomatoes.jpg"
            },
            new Product
            {
                Name = "Rau chân vịt non",
                Description = "Rau chân vịt non tươi, đã rửa sạch",
                Price = 3.99m,
                Weight = "250g",
                Stock = 40,
                CategoryId = vegetablesId,
                ImagePath = "spinach.jpg"
            },
            new Product
            {
                Name = "Ớt chuông thập cẩm",
                Description = "Ớt chuông đỏ, vàng và xanh đa sắc",
                Price = 4.99m,
                Weight = "3 trái",
                Stock = 35,
                CategoryId = vegetablesId,
                ImagePath = "bellpeppers.jpg"
            },
            new Product
            {
                Name = "Khoai tây",
                Description = "Khoai tây thích hợp nướng, chiên hoặc nghiền",
                Price = 3.99m,
                Weight = "2kg",
                Stock = 60,
                CategoryId = vegetablesId,
                ImagePath = "potatoes.jpg"
            },
            new Product
            {
                Name = "Dưa leo tươi",
                Description = "Dưa leo giòn mát, thanh nhiệt",
                Price = 1.99m,
                Weight = "1 trái",
                Stock = 45,
                CategoryId = vegetablesId,
                ImagePath = "cucumber.jpg"
            },

            // SỮA & BƠ
            new Product
            {
                Name = "Sữa tươi nguyên kem",
                Description = "Sữa tươi nguyên kem, nguồn gốc địa phương",
                Price = 4.49m,
                Weight = "1L",
                Stock = 70,
                CategoryId = dairyId,
                ImagePath = "c2f98112-cb02-4ccb-af56-bb9241d1f03f_milk.jpg"
            },
            new Product
            {
                Name = "Sữa chua Hy Lạp",
                Description = "Sữa chua Hy Lạp béo mịn, giàu protein",
                Price = 5.99m,
                Weight = "500g",
                Stock = 40,
                CategoryId = dairyId,
                ImagePath = "yogurt.jpg"
            },
            new Product
            {
                Name = "Phô mai Cheddar",
                Description = "Phô mai Cheddar đậm đà, ủ hoàn hảo",
                Price = 7.99m,
                Weight = "400g",
                Stock = 30,
                CategoryId = dairyId,
                ImagePath = "cheddar.jpg"
            },
            new Product
            {
                Name = "Bơ hữu cơ",
                Description = "Bơ hữu cơ béo mịn, không muối",
                Price = 5.49m,
                Weight = "250g",
                Stock = 45,
                CategoryId = dairyId,
                ImagePath = "butter.jpg"
            },
            new Product
            {
                Name = "Phô mai kem",
                Description = "Phô mai kem mịn mượt, dễ phết",
                Price = 4.99m,
                Weight = "250g",
                Stock = 35,
                CategoryId = dairyId,
                ImagePath = "creamcheese.jpg"
            },

            // BÁNH NGỌT
            new Product
            {
                Name = "Bánh sừng bò",
                Description = "Bánh croissant Pháp bơ thơm giòn xốp",
                Price = 6.99m,
                Weight = "6 cái",
                Stock = 25,
                CategoryId = bakeryId,
                ImagePath = "croissant.jpg"
            },
            new Product
            {
                Name = "Bánh mì hamburger",
                Description = "Bánh mì hamburger mềm rắc mè",
                Price = 3.99m,
                Weight = "8 cái",
                Stock = 40,
                CategoryId = bakeryId,
                ImagePath = "burgerbuns.jpg"
            },
            new Product
            {
                Name = "Bánh muffin việt quất",
                Description = "Bánh muffin việt quất mới nướng",
                Price = 7.99m,
                Weight = "6 cái",
                Stock = 30,
                CategoryId = bakeryId,
                ImagePath = "muffins.jpg"
            },
            new Product
            {
                Name = "Bánh mì bơ tỏi",
                Description = "Bánh mì bơ tỏi giòn thơm",
                Price = 4.49m,
                Weight = "1 cái",
                Stock = 35,
                CategoryId = bakeryId,
                ImagePath = "garlicbread.jpg"
            }
        );

        db.SaveChanges();
    }

    // Convert existing USD prices to VND
    bool needsVndConversion = db.Products.Any(p => p.Price < 1000);
    if (needsVndConversion)
    {
        var productsToUpdate = db.Products.Where(p => p.Price < 1000).ToList();
        foreach (var p in productsToUpdate)
        {
            p.Price = Math.Round(p.Price * 25m) * 1000m;
            if (p.DiscountPrice.HasValue && p.DiscountPrice.Value > 0)
            {
                p.DiscountPrice = Math.Round(p.DiscountPrice.Value * 25m) * 1000m;
            }
        }
        db.SaveChanges();
    }
}

app.Run();
