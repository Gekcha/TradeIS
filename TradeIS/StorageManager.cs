using System;
using System.IO;
using System.Text.Json;
using TradeIS.Models;

namespace TradeIS
{
    public static class StorageManager
    {
        private static string file = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "store.json"
        );

        public static void Save(DataStorage store)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(store, options);
            File.WriteAllText(file, json);
        }

        public static DataStorage Load()
        {
            if (!File.Exists(file))
            {
                var store = CreateDefaultData();
                Save(store);
                return store;
            }

            string json = File.ReadAllText(file);
            return JsonSerializer.Deserialize<DataStorage>(json) ?? new DataStorage();
        }

        public static DataStorage CreateDefaultData()
        {
            var store = new DataStorage();

            // --- TradePoints ---
            store.TradePoints.Add(new DepartmentStore { Id = 1, Name = "Универмаг Центр", Size = 1200, Rent = 300000, Utilities = 50000, Counters = 20 });
            store.TradePoints.Add(new DepartmentStore { Id = 2, Name = "Универмаг Север", Size = 900, Rent = 250000, Utilities = 42000, Counters = 15 });
            store.TradePoints.Add(new Shop { Id = 3, Name = "Магазин Электроника", Size = 200, Rent = 80000, Utilities = 15000, Counters = 4 });
            store.TradePoints.Add(new Shop { Id = 4, Name = "Магазин Одежда", Size = 150, Rent = 70000, Utilities = 12000, Counters = 3 });
            store.TradePoints.Add(new Shop { Id = 5, Name = "Магазин Продукты", Size = 180, Rent = 75000, Utilities = 14000, Counters = 4 });
            store.TradePoints.Add(new Kiosk { Id = 6, Name = "Киоск Газеты", Size = 20, Rent = 15000, Utilities = 2000, Counters = 1 });
            store.TradePoints.Add(new Kiosk { Id = 7, Name = "Киоск Снэки", Size = 25, Rent = 17000, Utilities = 2500, Counters = 1 });
            store.TradePoints.Add(new Stall { Id = 8, Name = "Лоток Фрукты", Size = 10, Rent = 10000, Utilities = 1000, Counters = 1 });
            store.TradePoints.Add(new Stall { Id = 9, Name = "Лоток Овощи", Size = 12, Rent = 11000, Utilities = 1000, Counters = 1 });
            store.TradePoints.Add(new Shop { Id = 10, Name = "Магазин Бытовая техника", Size = 220, Rent = 90000, Utilities = 16000, Counters = 5 });

            // --- Products ---
            store.Products.Add(new Product { Id = 1, Name = "Телевизор" });
            store.Products.Add(new Product { Id = 2, Name = "Ноутбук" });
            store.Products.Add(new Product { Id = 3, Name = "Холодильник" });
            store.Products.Add(new Product { Id = 4, Name = "Куртка" });
            store.Products.Add(new Product { Id = 5, Name = "Джинсы" });
            store.Products.Add(new Product { Id = 6, Name = "Хлеб" });
            store.Products.Add(new Product { Id = 7, Name = "Молоко" });
            store.Products.Add(new Product { Id = 8, Name = "Газета" });
            store.Products.Add(new Product { Id = 9, Name = "Чипсы" });
            store.Products.Add(new Product { Id = 10, Name = "Яблоки" });

            // --- Suppliers ---
            store.Suppliers.Add(new Supplier { Id = 1, Name = "TechSupply" });
            store.Suppliers.Add(new Supplier { Id = 2, Name = "MegaElectro" });
            store.Suppliers.Add(new Supplier { Id = 3, Name = "FashionGroup" });
            store.Suppliers.Add(new Supplier { Id = 4, Name = "FoodMarket" });
            store.Suppliers.Add(new Supplier { Id = 5, Name = "FreshFarm" });
            store.Suppliers.Add(new Supplier { Id = 6, Name = "NewsDistribution" });
            store.Suppliers.Add(new Supplier { Id = 7, Name = "SnackImport" });
            store.Suppliers.Add(new Supplier { Id = 8, Name = "ApplianceWorld" });
            store.Suppliers.Add(new Supplier { Id = 9, Name = "GlobalTrade" });
            store.Suppliers.Add(new Supplier { Id = 10, Name = "LocalProducts" });

            // --- Supplies (СТРОКИ) ---
            store.Supplies.Add(new Supply { Id = 1, Supplier = "TechSupply", Product = "Телевизор", Quantity = 15, Price = 30000, Date = new DateTime(2025, 2, 1) });
            store.Supplies.Add(new Supply { Id = 2, Supplier = "MegaElectro", Product = "Ноутбук", Quantity = 10, Price = 50000, Date = new DateTime(2025, 2, 2) });
            store.Supplies.Add(new Supply { Id = 3, Supplier = "ApplianceWorld", Product = "Холодильник", Quantity = 5, Price = 45000, Date = new DateTime(2025, 2, 5) });
            store.Supplies.Add(new Supply { Id = 4, Supplier = "FashionGroup", Product = "Куртка", Quantity = 20, Price = 6000, Date = new DateTime(2025, 2, 6) });
            store.Supplies.Add(new Supply { Id = 5, Supplier = "FashionGroup", Product = "Джинсы", Quantity = 25, Price = 4000, Date = new DateTime(2025, 2, 6) });
            store.Supplies.Add(new Supply { Id = 6, Supplier = "FoodMarket", Product = "Хлеб", Quantity = 100, Price = 50, Date = new DateTime(2025, 2, 7) });
            store.Supplies.Add(new Supply { Id = 7, Supplier = "FoodMarket", Product = "Молоко", Quantity = 80, Price = 70, Date = new DateTime(2025, 2, 7) });
            store.Supplies.Add(new Supply { Id = 8, Supplier = "NewsDistribution", Product = "Газета", Quantity = 200, Price = 30, Date = new DateTime(2025, 2, 8) });
            store.Supplies.Add(new Supply { Id = 9, Supplier = "SnackImport", Product = "Чипсы", Quantity = 60, Price = 120, Date = new DateTime(2025, 2, 9) });
            store.Supplies.Add(new Supply { Id = 10, Supplier = "FreshFarm", Product = "Яблоки", Quantity = 120, Price = 90, Date = new DateTime(2025, 2, 10) });

            // --- Sellers ---
            store.Sellers.Add(new Seller { Id = 1, Name = "Иван Петров", TradePoint = "Универмаг Центр", Salary = 45000 });
            store.Sellers.Add(new Seller { Id = 2, Name = "Анна Сидорова", TradePoint = "Универмаг Север", Salary = 43000 });
            store.Sellers.Add(new Seller { Id = 3, Name = "Олег Кузнецов", TradePoint = "Магазин Электроника", Salary = 42000 });
            store.Sellers.Add(new Seller { Id = 4, Name = "Мария Орлова", TradePoint = "Магазин Одежда", Salary = 40000 });
            store.Sellers.Add(new Seller { Id = 5, Name = "Дмитрий Волков", TradePoint = "Магазин Продукты", Salary = 38000 });
            store.Sellers.Add(new Seller { Id = 6, Name = "Елена Павлова", TradePoint = "Киоск Газеты", Salary = 30000 });
            store.Sellers.Add(new Seller { Id = 7, Name = "Сергей Лебедев", TradePoint = "Киоск Снэки", Salary = 30000 });
            store.Sellers.Add(new Seller { Id = 8, Name = "Ольга Федорова", TradePoint = "Лоток Фрукты", Salary = 25000 });
            store.Sellers.Add(new Seller { Id = 9, Name = "Андрей Смирнов", TradePoint = "Лоток Овощи", Salary = 25000 });
            store.Sellers.Add(new Seller { Id = 10, Name = "Николай Иванов", TradePoint = "Магазин Бытовая техника", Salary = 42000 });

            // --- Customers ---
            store.Customers.Add(new Customer { Id = 1, Name = "Алексей" });
            store.Customers.Add(new Customer { Id = 2, Name = "Марина" });
            store.Customers.Add(new Customer { Id = 3, Name = "Виктор" });
            store.Customers.Add(new Customer { Id = 4, Name = "Татьяна" });
            store.Customers.Add(new Customer { Id = 5, Name = "Игорь" });
            store.Customers.Add(new Customer { Id = 6, Name = "Наталья" });
            store.Customers.Add(new Customer { Id = 7, Name = "Светлана" });
            store.Customers.Add(new Customer { Id = 8, Name = "Роман" });
            store.Customers.Add(new Customer { Id = 9, Name = "Екатерина" });
            store.Customers.Add(new Customer { Id = 10, Name = "Павел" });

            // --- Sales (СТРОКИ) ---
            store.Sales.Add(new Sale { Id = 1, Product = "Телевизор", TradePoint = "Магазин Электроника", Seller = "Олег Кузнецов", Customer = "Алексей", Quantity = 1, Price = 35000, Date = new DateTime(2025, 2, 11) });
            store.Sales.Add(new Sale { Id = 2, Product = "Ноутбук", TradePoint = "Магазин Электроника", Seller = "Олег Кузнецов", Customer = "Марина", Quantity = 1, Price = 60000, Date = new DateTime(2025, 2, 11) });
            store.Sales.Add(new Sale { Id = 3, Product = "Куртка", TradePoint = "Магазин Одежда", Seller = "Мария Орлова", Customer = "Виктор", Quantity = 1, Price = 9000, Date = new DateTime(2025, 2, 12) });
            store.Sales.Add(new Sale { Id = 4, Product = "Джинсы", TradePoint = "Магазин Одежда", Seller = "Мария Орлова", Customer = "Татьяна", Quantity = 2, Price = 6000, Date = new DateTime(2025, 2, 12) });
            store.Sales.Add(new Sale { Id = 5, Product = "Хлеб", TradePoint = "Магазин Продукты", Seller = "Дмитрий Волков", Customer = "", Quantity = 5, Price = 60, Date = new DateTime(2025, 2, 13) });
            store.Sales.Add(new Sale { Id = 6, Product = "Молоко", TradePoint = "Магазин Продукты", Seller = "Дмитрий Волков", Customer = "", Quantity = 3, Price = 80, Date = new DateTime(2025, 2, 13) });
            store.Sales.Add(new Sale { Id = 7, Product = "Газета", TradePoint = "Киоск Газеты", Seller = "Елена Павлова", Customer = "", Quantity = 10, Price = 40, Date = new DateTime(2025, 2, 14) });
            store.Sales.Add(new Sale { Id = 8, Product = "Чипсы", TradePoint = "Киоск Снэки", Seller = "Сергей Лебедев", Customer = "", Quantity = 4, Price = 150, Date = new DateTime(2025, 2, 14) });
            store.Sales.Add(new Sale { Id = 9, Product = "Яблоки", TradePoint = "Лоток Фрукты", Seller = "Ольга Федорова", Customer = "", Quantity = 6, Price = 120, Date = new DateTime(2025, 2, 15) });
            store.Sales.Add(new Sale { Id = 10, Product = "Холодильник", TradePoint = "Магазин Бытовая техника", Seller = "Николай Иванов", Customer = "Павел", Quantity = 1, Price = 52000, Date = new DateTime(2025, 2, 16) });

            // --- Requests (ID) ---
            store.Requests.Add(new Request { Id = 1, TradePointId = 3, ProductId = 1, Quantity = 2, Date = new DateTime(2025, 2, 17) });
            store.Requests.Add(new Request { Id = 2, TradePointId = 3, ProductId = 2, Quantity = 3, Date = new DateTime(2025, 2, 17) });
            store.Requests.Add(new Request { Id = 3, TradePointId = 4, ProductId = 4, Quantity = 5, Date = new DateTime(2025, 2, 18) });
            store.Requests.Add(new Request { Id = 4, TradePointId = 4, ProductId = 5, Quantity = 6, Date = new DateTime(2025, 2, 18) });
            store.Requests.Add(new Request { Id = 5, TradePointId = 5, ProductId = 6, Quantity = 40, Date = new DateTime(2025, 2, 19) });
            store.Requests.Add(new Request { Id = 6, TradePointId = 5, ProductId = 7, Quantity = 30, Date = new DateTime(2025, 2, 19) });
            store.Requests.Add(new Request { Id = 7, TradePointId = 6, ProductId = 8, Quantity = 100, Date = new DateTime(2025, 2, 20) });
            store.Requests.Add(new Request { Id = 8, TradePointId = 7, ProductId = 9, Quantity = 40, Date = new DateTime(2025, 2, 20) });
            store.Requests.Add(new Request { Id = 9, TradePointId = 8, ProductId = 10, Quantity = 50, Date = new DateTime(2025, 2, 21) });
            store.Requests.Add(new Request { Id = 10, TradePointId = 10, ProductId = 3, Quantity = 2, Date = new DateTime(2025, 2, 22) });

            // --- SupplierOrders (ID) ---
            store.SupplierOrders.Add(new SupplierOrder { Id = 1, SupplierId = 1, ProductId = 1, Quantity = 5, Price = 30000, Date = new DateTime(2025, 2, 23) });
            store.SupplierOrders.Add(new SupplierOrder { Id = 2, SupplierId = 2, ProductId = 2, Quantity = 4, Price = 50000, Date = new DateTime(2025, 2, 23) });
            store.SupplierOrders.Add(new SupplierOrder { Id = 3, SupplierId = 3, ProductId = 4, Quantity = 10, Price = 6000, Date = new DateTime(2025, 2, 24) });
            store.SupplierOrders.Add(new SupplierOrder { Id = 4, SupplierId = 3, ProductId = 5, Quantity = 10, Price = 4000, Date = new DateTime(2025, 2, 24) });
            store.SupplierOrders.Add(new SupplierOrder { Id = 5, SupplierId = 4, ProductId = 6, Quantity = 80, Price = 50, Date = new DateTime(2025, 2, 25) });
            store.SupplierOrders.Add(new SupplierOrder { Id = 6, SupplierId = 4, ProductId = 7, Quantity = 60, Price = 70, Date = new DateTime(2025, 2, 25) });
            store.SupplierOrders.Add(new SupplierOrder { Id = 7, SupplierId = 6, ProductId = 8, Quantity = 200, Price = 30, Date = new DateTime(2025, 2, 26) });
            store.SupplierOrders.Add(new SupplierOrder { Id = 8, SupplierId = 7, ProductId = 9, Quantity = 70, Price = 120, Date = new DateTime(2025, 2, 26) });
            store.SupplierOrders.Add(new SupplierOrder { Id = 9, SupplierId = 5, ProductId = 10, Quantity = 150, Price = 90, Date = new DateTime(2025, 2, 27) });
            store.SupplierOrders.Add(new SupplierOrder { Id = 10, SupplierId = 8, ProductId = 3, Quantity = 3, Price = 45000, Date = new DateTime(2025, 2, 27) });

            return store;
        }
    }
}