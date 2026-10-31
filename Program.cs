using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Online_Store.DataAccess;
using Online_Store.Enums;
using Online_Store.Models;

namespace Online_Store
{
    internal class Program
    {
        static void Main(string[] args)
        {
            using var db = new ApplicationDbContext();

            // ==========================================
            // LEVEL 1: Warm-up
            // ==========================================

            // 1. For every delivered order, return OrderId, CustomerName, and Total, ordered by Total descending.
            var q1 = db.Orders
                .Where(o => o.Status == OrderStatus.Delivered)
                .Select(o => new {
                    OrderId = o.Id,
                    CustomerName = o.Customer.FullName,
                    Total = o.Items.Sum(i => i.Quantity * i.UnitPrice * (1 - i.Discount))
                })
                .OrderByDescending(x => x.Total)
                .ToList();

            // 2. Count how many orders exist per status, with the percentage of the whole.
            int totalOrders = db.Orders.Count();
            var q2 = db.Orders
                .GroupBy(o => o.Status)
                .Select(g => new {
                    Status = g.Key,
                    Count = g.Count(),
                    Percentage = totalOrders == 0 ? 0 : (double)g.Count() / totalOrders * 100
                })
                .ToList();

            // 3. Return products whose price is above the average price of their own category.
            var q3 = db.Products
                .Where(p => p.Price > db.Products
                    .Where(p2 => p2.CategoryId == p.CategoryId)
                    .Average(p2 => p2.Price))
                .ToList();

            // 4. Return products that were never ordered.
            var q4 = db.Products
                .Where(p => !p.OrderItems.Any())
                .ToList();

            // 5. Return customers who registered in the last 30 days and have at least one order.
            var thirtyDaysAgo = DateTime.Now.AddDays(-30);
            var q5 = db.Customers
                .Where(c => c.RegisteredAt >= thirtyDaysAgo && c.Orders.Any())
                .ToList();

            // 6. Return customers who cancelled more than 2 orders, with the number of cancelled orders.
            var q6 = db.Customers
                .Select(c => new {
                    Customer = c,
                    CancelledOrdersCount = c.Orders.Count(o => o.Status == OrderStatus.Cancelled)
                })
                .Where(x => x.CancelledOrdersCount > 2)
                .ToList();


            // ==========================================
            // LEVEL 2: Grouping and aggregation
            // ==========================================

            // 7. Top 5 customers by total spending: name, order count, total spent, average order value.
            var q7 = db.Customers
                .Select(c => new {
                    Name = c.FullName,
                    OrderCount = c.Orders.Count(o => o.Status == OrderStatus.Delivered),
                    TotalSpent = c.Orders
                        .Where(o => o.Status == OrderStatus.Delivered)
                        .SelectMany(o => o.Items)
                        .Sum(i => i.Quantity * i.UnitPrice * (1 - i.Discount)),
                    AvgOrderValue = c.Orders
                        .Where(o => o.Status == OrderStatus.Delivered)
                        .Average(o => (decimal?)o.Items.Sum(i => i.Quantity * i.UnitPrice * (1 - i.Discount))) ?? 0m
                })
                .OrderByDescending(x => x.TotalSpent)
                .Take(5)
                .ToList();

            // 8. Monthly report for 2025: Year, Month, OrderCount, Revenue, ordered by month.
            var q8 = db.Orders
                .Where(o => o.Status == OrderStatus.Delivered && o.OrderDate.Year == 2025)
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.Items.Sum(i => i.Quantity * i.UnitPrice * (1 - i.Discount)))
                })
                .OrderBy(x => x.Month)
                .ToList();

            // 9. Revenue per category, ordered by revenue descending.
            var q9 = db.OrderItems
                .Where(oi => oi.Order.Status == OrderStatus.Delivered)
                .GroupBy(oi => oi.Product.Category.Name)
                .Select(g => new {
                    CategoryName = g.Key,
                    Revenue = g.Sum(i => i.Quantity * i.UnitPrice * (1 - i.Discount))
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            // 10. Count customers who never placed an order, grouped by city, ordered by count descending.
            var q10 = db.Customers
                .Where(c => !c.Orders.Any())
                .GroupBy(c => c.City)
                .Select(g => new {
                    City = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            // 11. Products with at least 10 reviews and an average rating of 4.5 or more.
            var q11 = db.Products
                .Where(p => p.Reviews.Count >= 10 && p.Reviews.Average(r => (double?)r.Rating) >= 4.5)
                .Select(p => new {
                    ProductName = p.Name,
                    Category = p.Category.Name,
                    ReviewCount = p.Reviews.Count,
                    AverageRating = p.Reviews.Average(r => (double)r.Rating)
                })
                .ToList();

            // 12. For each category, return the top 2 most expensive products.
            var q12 = db.Categories
                .Select(c => new {
                    CategoryName = c.Name,
                    TopProducts = c.Products
                        .OrderByDescending(p => p.Price)
                        .Take(2)
                        .Select(p => new { p.Name, p.Price })
                        .ToList()
                })
                .ToList();

            // 13. Products whose stock is lower than total quantity in Pending orders.
            var q13 = db.Products
                .Select(p => new {
                    Name = p.Name,
                    Stock = p.Stock,
                    PendingQuantity = p.OrderItems
                        .Where(oi => oi.Order.Status == OrderStatus.Pending)
                        .Sum(oi => (int?)oi.Quantity) ?? 0
                })
                .Where(x => x.Stock < x.PendingQuantity)
                .ToList();

            // 14. Customers whose total spending is above the average spending of all customers.
            var avgSpending = db.Customers
                                .Select(c => c.Orders
                                    .Where(o => o.Status == OrderStatus.Delivered)
                                    .SelectMany(o => o.Items)
                                    .Sum(i => (decimal?)(i.Quantity * i.UnitPrice * (1 - i.Discount))) ?? 0m)
                                .Average();

            var q14 = db.Customers
                .Select(c => new {
                    Customer = c,
                    TotalSpent = c.Orders
                        .Where(o => o.Status == OrderStatus.Delivered)
                        .SelectMany(o => o.Items)
                        .Sum(i => (decimal?)(i.Quantity * i.UnitPrice * (1 - i.Discount))) ?? 0m
                })
                .Where(x => x.TotalSpent > avgSpending)
                .ToList();


            // ==========================================
            // LEVEL 3: Joins, subqueries, set logic
            // ==========================================

            // 15. For each category, return its best-selling product by quantity (categories with no sales included).
            var q15 = db.Categories
                .Select(c => new {
                    CategoryName = c.Name,
                    BestSellingProduct = c.Products
                        .Select(p => new {
                            Product = p,
                            TotalQty = p.OrderItems
                                .Where(oi => oi.Order.Status == OrderStatus.Delivered)
                                .Sum(oi => (int?)oi.Quantity) ?? 0
                        })
                        .OrderByDescending(x => x.TotalQty)
                        .Select(x => x.Product.Name)
                        .FirstOrDefault()
                })
                .ToList();

            // 16. Customers who bought every product in the category "Laptops".
            var laptopProductIds = db.Products
                .Where(p => p.Category.Name == "Laptops")
                .Select(p => p.Id)
                .ToList();

            var q16 = db.Customers
                .Where(c => laptopProductIds.All(laptopId =>
                    c.Orders
                     .Where(o => o.Status == OrderStatus.Delivered)
                     .SelectMany(o => o.Items)
                     .Any(i => i.ProductId == laptopId)
                ))
                .ToList();

            // 17. Reviews written by a customer for a product they never bought.
            var q17 = db.Reviews
                .Where(r => !r.Customer.Orders
                    .Where(o => o.Status == OrderStatus.Delivered)
                    .SelectMany(o => o.Items)
                    .Any(i => i.ProductId == r.ProductId))
                .ToList();

            // 18. Return all customers with their order count, including zero orders (GroupJoin).
            var q18 = db.Customers
                .GroupJoin(
                    db.Orders,
                    c => c.Id,
                    o => o.CustomerId,
                    (c, customerOrders) => new {
                        CustomerName = c.FullName,
                        OrderCount = customerOrders.Count()
                    }
                )
                .ToList();

            // 19. The top 5 product pairs that appear together in the same order most often.
            var q19 = db.OrderItems
                .Where(oi => oi.Order.Status == OrderStatus.Delivered)
                .Select(oi => new { oi.OrderId, oi.ProductId })
                .Join(
                    db.OrderItems.Where(oi => oi.Order.Status == OrderStatus.Delivered),
                    i1 => i1.OrderId,
                    i2 => i2.OrderId,
                    (i1, i2) => new { Product1 = i1.ProductId, Product2 = i2.ProductId }
                )
                .Where(p => p.Product1 < p.Product2)
                .GroupBy(p => new { p.Product1, p.Product2 })
                .Select(g => new {
                    Product1Id = g.Key.Product1,
                    Product2Id = g.Key.Product2,
                    PairCount = g.Count()
                })
                .OrderByDescending(x => x.PairCount)
                .Take(5)
                .ToList();

            // 20. For each customer, return their name and most recent order with items.
            var q20 = db.Customers
                .Select(c => new {
                    CustomerName = c.FullName,
                    MostRecentOrder = c.Orders
                        .OrderByDescending(o => o.OrderDate)
                        .Select(o => new {
                            OrderId = o.Id,
                            OrderDate = o.OrderDate,
                            Items = o.Items.Select(i => new {
                                ProductName = i.Product.Name,
                                Quantity = i.Quantity
                            }).ToList()
                        })
                        .FirstOrDefault()
                })
                .ToList();

            // 21. For each customer, return their favorite category (highest total quantity bought).
            var q21 = db.Customers
                .Select(c => new {
                    CustomerName = c.FullName,
                    FavoriteCategory = c.Orders
                        .Where(o => o.Status == OrderStatus.Delivered)
                        .SelectMany(o => o.Items)
                        .GroupBy(i => i.Product.Category.Name)
                        .Select(g => new {
                            CategoryName = g.Key,
                            TotalQty = g.Sum(i => i.Quantity)
                        })
                        .OrderByDescending(x => x.TotalQty)
                        .Select(x => x.CategoryName)
                        .FirstOrDefault()
                })
                .ToList();

            // 22. Rank products by revenue (1, 2, 3 ...)
            var q22 = db.Products
                .Select(p => new {
                    Product = p.Name,
                    Revenue = p.OrderItems
                        .Where(oi => oi.Order.Status == OrderStatus.Delivered)
                        .Sum(i => (decimal?)(i.Quantity * i.UnitPrice * (1 - i.Discount))) ?? 0m
                })
                .OrderByDescending(x => x.Revenue)
                .ToList()
                .Select((x, index) => new {
                    Rank = index + 1,
                    x.Product,
                    x.Revenue
                })
                .ToList();


            // ==========================================
            // LEVEL 4: Real-world methods
            // ==========================================

            // 23. Search Products Method Execution Test
            var filter = new ProductFilter { SearchText = "Phone", PageSize = 5 };
            var searchResult = SearchProducts(db, filter);

            // 24. Increase price by 10% for "Accessories" with stock < 5 without loading into memory
            db.Products
                .Where(p => p.Category.Name == "Accessories" && p.Stock < 5)
                .ExecuteUpdate(s => s.SetProperty(p => p.Price, p => p.Price * 1.10m));

            // 25. Delete cancelled orders older than 1 year without loading into memory
            var oneYearAgo = DateTime.Now.AddYears(-1);
            db.Orders
                .Where(o => o.Status == OrderStatus.Cancelled && o.OrderDate < oneYearAgo)
                .ExecuteDelete();

            Console.WriteLine("All queries executed successfully!");
        }

        // ==========================================
        // Supporting Helper Classes & Methods for Level 4
        // ==========================================

        public class ProductFilter
        {
            public string SearchText { get; set; }
            public int? CategoryId { get; set; }
            public decimal? MinPrice { get; set; }
            public decimal? MaxPrice { get; set; }
            public bool InStockOnly { get; set; }
            public string SortBy { get; set; } // "name", "price", "rating"
            public int PageNumber { get; set; } = 1;
            public int PageSize { get; set; } = 10;
        }

        public class PagedResult<T>
        {
            public List<T> Items { get; set; }
            public int TotalCount { get; set; }
        }

        public static PagedResult<object> SearchProducts(ApplicationDbContext db, ProductFilter f)
        {
            var query = db.Products.AsQueryable();

            if (!string.IsNullOrEmpty(f.SearchText))
                query = query.Where(p => p.Name.Contains(f.SearchText));

            if (f.CategoryId.HasValue)
                query = query.Where(p => p.CategoryId == f.CategoryId.Value);

            if (f.MinPrice.HasValue)
                query = query.Where(p => p.Price >= f.MinPrice.Value);

            if (f.MaxPrice.HasValue)
                query = query.Where(p => p.Price <= f.MaxPrice.Value);

            if (f.InStockOnly)
                query = query.Where(p => p.Stock > 0);

            int totalCount = query.Count();

            query = f.SortBy?.ToLower() switch
            {
                "price" => query.OrderBy(p => p.Price),
                "rating" => query.OrderByDescending(p => p.Reviews.Average(r => (double?)r.Rating) ?? 0),
                _ => query.OrderBy(p => p.Name)
            };

            var items = query
                .Skip((f.PageNumber - 1) * f.PageSize)
                .Take(f.PageSize)
                .Select(p => new {
                    p.Id,
                    p.Name,
                    p.Price,
                    p.Stock,
                    CategoryName = p.Category.Name
                })
                .Cast<object>()
                .ToList();

            return new PagedResult<object> { Items = items, TotalCount = totalCount };
        }
    }
}