using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaiTHbuoi1.Models
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                // Create roles if they don't exist
                string[] roleNames = { "Admin", "Manager", "Customer" };

                foreach (var roleName in roleNames)
                {
                    var roleExist = await roleManager.RoleExistsAsync(roleName);
                    if (!roleExist)
                    {
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                    }
                }

                // Create admin user if it doesn't exist
                var adminUser = await userManager.FindByEmailAsync("admin@example.com");
                if (adminUser == null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = "admin@example.com",
                        Email = "admin@example.com",
                        FirstName = "Admin",
                        LastName = "User",
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(user, "Admin@123");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, "Admin");
                    }
                }

                // Seed categories if none exist
                if (!context.Categories.Any())
                {
                    context.Categories.AddRange(
                        new Category { Name = "Electronics", Description = "Electronic devices and gadgets", DisplayOrder = 1 },
                        new Category { Name = "Clothing", Description = "Apparel and fashion items", DisplayOrder = 2 },
                        new Category { Name = "Books", Description = "Books and publications", DisplayOrder = 3 },
                        new Category { Name = "Home & Kitchen", Description = "Home and kitchen appliances", DisplayOrder = 4 },
                        new Category { Name = "Sports", Description = "Sports equipment and accessories", DisplayOrder = 5 }
                    );
                    await context.SaveChangesAsync();
                }

                // Seed suppliers if none exist
                if (!context.Suppliers.Any())
                {
                    context.Suppliers.AddRange(
                        new Supplier { Name = "Tech Supplies Inc.", ContactName = "John Doe", Email = "contact@techsupplies.com", Phone = "123-456-7890" },
                        new Supplier { Name = "Fashion World", ContactName = "Jane Smith", Email = "info@fashionworld.com", Phone = "987-654-3210" },
                        new Supplier { Name = "Book Publishers Ltd.", ContactName = "Robert Johnson", Email = "sales@bookpublishers.com", Phone = "555-123-4567" }
                    );
                    await context.SaveChangesAsync();
                }

                // Seed sliders if none exist
                if (!context.Sliders.Any())
                {
                    context.Sliders.AddRange(
                        new Slider { Title = "New Arrivals", Subtitle = "Check out our latest products", ImageUrl = "/images/sliders/slider1.jpg", DisplayOrder = 1 },
                        new Slider { Title = "Special Offers", Subtitle = "Up to 50% off on selected items", ImageUrl = "/images/sliders/slider2.jpg", DisplayOrder = 2 },
                        new Slider { Title = "Free Shipping", Subtitle = "On orders over $50", ImageUrl = "/images/sliders/slider3.jpg", DisplayOrder = 3 }
                    );
                    await context.SaveChangesAsync();
                }

                // Seed sample products if none exist
                if (!context.Products.Any())
                {
                    var categories = await context.Categories.ToListAsync();
                    var suppliers = await context.Suppliers.ToListAsync();

                    if (categories.Any() && suppliers.Any())
                    {
                        var electronicsCat = categories.FirstOrDefault(c => c.Name == "Electronics");
                        var clothingCat = categories.FirstOrDefault(c => c.Name == "Clothing");
                        var booksCat = categories.FirstOrDefault(c => c.Name == "Books");

                        var techSupplier = suppliers.FirstOrDefault(s => s.Name == "Tech Supplies Inc.");
                        var fashionSupplier = suppliers.FirstOrDefault(s => s.Name == "Fashion World");
                        var bookSupplier = suppliers.FirstOrDefault(s => s.Name == "Book Publishers Ltd.");

                        if (electronicsCat != null && techSupplier != null)
                        {
                            context.Products.Add(new Product
                            {
                                Name = "Smartphone X",
                                Description = "Latest smartphone with advanced features",
                                Price = 799.99m,
                                Stock = 50,
                                CategoryId = electronicsCat.Id,
                                SupplierId = techSupplier.Id,
                                ImageUrl = "/images/products/smartphone.jpg",
                                IsFeatured = true,
                                IsNewArrival = true
                            });

                            context.Products.Add(new Product
                            {
                                Name = "Laptop Pro",
                                Description = "High-performance laptop for professionals",
                                Price = 1299.99m,
                                Stock = 30,
                                CategoryId = electronicsCat.Id,
                                SupplierId = techSupplier.Id,
                                ImageUrl = "/images/products/laptop.jpg",
                                IsBestSeller = true
                            });
                        }

                        if (clothingCat != null && fashionSupplier != null)
                        {
                            context.Products.Add(new Product
                            {
                                Name = "Men's T-Shirt",
                                Description = "Comfortable cotton t-shirt",
                                Price = 29.99m,
                                DiscountPrice = 19.99m,
                                Stock = 100,
                                CategoryId = clothingCat.Id,
                                SupplierId = fashionSupplier.Id,
                                ImageUrl = "/images/products/tshirt.jpg",
                                IsOnSale = true
                            });
                        }

                        if (booksCat != null && bookSupplier != null)
                        {
                            context.Products.Add(new Product
                            {
                                Name = "Programming Guide",
                                Description = "Comprehensive programming guide for beginners",
                                Price = 49.99m,
                                Stock = 75,
                                CategoryId = booksCat.Id,
                                SupplierId = bookSupplier.Id,
                                ImageUrl = "/images/products/book.jpg",
                                IsBestSeller = true
                            });
                        }

                        await context.SaveChangesAsync();
                    }
                }
            }
        }
    }
}