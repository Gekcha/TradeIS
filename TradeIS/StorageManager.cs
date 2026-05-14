using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using TradeIS.Models;

namespace TradeIS
{
    public static class StorageManager
    {
        private static string file = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "store.json"
        );

        private static JsonSerializerOptions GetOptions()
        {
            return new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                Converters =
                {
                    new TradePointConverter()
                }
            };
        }

        public static void Save(DataStorage store)
        {
            var json = JsonSerializer.Serialize(store, GetOptions());
            File.WriteAllText(file, json);
        }

        public static DataStorage Load()
        {
            if (!File.Exists(file))
            {
                var newStore = CreateDefaultData();
                Save(newStore);
                return newStore;
            }

            var json = File.ReadAllText(file);

            DataStorage loadedStore;

            try
            {
                loadedStore = JsonSerializer.Deserialize<DataStorage>(json, GetOptions())
                              ?? new DataStorage();
            }
            catch
            {
                // если JSON битый или структура изменилась
                loadedStore = new DataStorage();
            }

            loadedStore.ActualizeCounters();
            return loadedStore;
        }

        public static DataStorage CreateDefaultData()
        {
            var store = new DataStorage();

            // ---------------- TRADE POINTS ----------------
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

            // ---------------- PRODUCTS ----------------
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

            // ---------------- SUPPLIERS ----------------
            store.Suppliers.Add(new Supplier { Id = 1, Name = "TechSupply" });
            store.Suppliers.Add(new Supplier { Id = 2, Name = "MegaElectro" });
            store.Suppliers.Add(new Supplier { Id = 3, Name = "FashionGroup" });
            store.Suppliers.Add(new Supplier { Id = 4, Name = "FoodMarket" });
            store.Suppliers.Add(new Supplier { Id = 5, Name = "FreshFarm" });
            store.Suppliers.Add(new Supplier { Id = 6, Name = "NewsDistribution" });
            store.Suppliers.Add(new Supplier { Id = 7, Name = "SnackImport" });
            store.Suppliers.Add(new Supplier { Id = 8, Name = "ApplianceWorld" });

            // ---------------- SUPPLIES (ID VERSION) ----------------
            store.Supplies.Add(new Supply { Id = 1, SupplierId = 1, ProductId = 1, Quantity = 15, Price = 30000, Date = new DateTime(2025, 2, 1) });
            store.Supplies.Add(new Supply { Id = 2, SupplierId = 2, ProductId = 2, Quantity = 10, Price = 50000, Date = new DateTime(2025, 2, 2) });
            store.Supplies.Add(new Supply { Id = 3, SupplierId = 8, ProductId = 3, Quantity = 5, Price = 45000, Date = new DateTime(2025, 2, 5) });
            store.Supplies.Add(new Supply { Id = 4, SupplierId = 3, ProductId = 4, Quantity = 20, Price = 6000, Date = new DateTime(2025, 2, 6) });
            store.Supplies.Add(new Supply { Id = 5, SupplierId = 3, ProductId = 5, Quantity = 25, Price = 4000, Date = new DateTime(2025, 2, 6) });
            store.Supplies.Add(new Supply { Id = 6, SupplierId = 4, ProductId = 6, Quantity = 100, Price = 50, Date = new DateTime(2025, 2, 7) });
            store.Supplies.Add(new Supply { Id = 7, SupplierId = 4, ProductId = 7, Quantity = 80, Price = 70, Date = new DateTime(2025, 2, 7) });
            store.Supplies.Add(new Supply { Id = 8, SupplierId = 6, ProductId = 8, Quantity = 200, Price = 30, Date = new DateTime(2025, 2, 8) });
            store.Supplies.Add(new Supply { Id = 9, SupplierId = 7, ProductId = 9, Quantity = 60, Price = 120, Date = new DateTime(2025, 2, 9) });
            store.Supplies.Add(new Supply { Id = 10, SupplierId = 5, ProductId = 10, Quantity = 120, Price = 90, Date = new DateTime(2025, 2, 10) });

            // ---------------- SELLERS (ID VERSION) ----------------
            store.Sellers.Add(new Seller { Id = 1, Name = "Иван Петров", TradePointId = 1, Salary = 45000 });
            store.Sellers.Add(new Seller { Id = 2, Name = "Анна Сидорова", TradePointId = 2, Salary = 43000 });
            store.Sellers.Add(new Seller { Id = 3, Name = "Олег Кузнецов", TradePointId = 3, Salary = 42000 });
            store.Sellers.Add(new Seller { Id = 4, Name = "Мария Орлова", TradePointId = 4, Salary = 40000 });
            store.Sellers.Add(new Seller { Id = 5, Name = "Дмитрий Волков", TradePointId = 5, Salary = 38000 });
            store.Sellers.Add(new Seller { Id = 6, Name = "Елена Павлова", TradePointId = 6, Salary = 30000 });
            store.Sellers.Add(new Seller { Id = 7, Name = "Сергей Лебедев", TradePointId = 7, Salary = 30000 });
            store.Sellers.Add(new Seller { Id = 8, Name = "Ольга Федорова", TradePointId = 8, Salary = 25000 });
            store.Sellers.Add(new Seller { Id = 9, Name = "Андрей Смирнов", TradePointId = 9, Salary = 25000 });
            store.Sellers.Add(new Seller { Id = 10, Name = "Николай Иванов", TradePointId = 10, Salary = 42000 });

            // ---------------- CUSTOMERS ----------------
            store.Customers.Add(new Customer { Id = 1, Name = "Алексей" });
            store.Customers.Add(new Customer { Id = 2, Name = "Марина" });
            store.Customers.Add(new Customer { Id = 3, Name = "Виктор" });
            store.Customers.Add(new Customer { Id = 4, Name = "Татьяна" });
            store.Customers.Add(new Customer { Id = 5, Name = "Игорь" });

            // ---------------- SALES (ID VERSION) ----------------
            store.Sales.Add(new Sale { Id = 1, ProductId = 1, TradePointId = 3, SellerId = 3, CustomerId = 1, Quantity = 1, Price = 35000, Date = new DateTime(2025, 2, 11) });
            store.Sales.Add(new Sale { Id = 2, ProductId = 2, TradePointId = 3, SellerId = 3, CustomerId = 2, Quantity = 1, Price = 60000, Date = new DateTime(2025, 2, 11) });
            store.Sales.Add(new Sale { Id = 3, ProductId = 4, TradePointId = 4, SellerId = 4, CustomerId = 3, Quantity = 1, Price = 9000, Date = new DateTime(2025, 2, 12) });
            store.Sales.Add(new Sale { Id = 4, ProductId = 5, TradePointId = 4, SellerId = 4, CustomerId = 4, Quantity = 2, Price = 6000, Date = new DateTime(2025, 2, 12) });
            store.Sales.Add(new Sale { Id = 5, ProductId = 6, TradePointId = 5, SellerId = 5, CustomerId = null, Quantity = 5, Price = 60, Date = new DateTime(2025, 2, 13) });

            return store;
        }
    }
}