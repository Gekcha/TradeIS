using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TradeIS;

public class ReportEngine
{
    private readonly DataStorage _storage;

    public ReportEngine(DataStorage storage)
    {
        _storage = storage;
    }

    public DataTable GetSuppliersByProduct(
    int productId,
    int minQuantity,
    DateTime? from,
    DateTime? to)
    {
        var table = new DataTable();

        table.Columns.Add("Поставщик", typeof(string));
        table.Columns.Add("Товар", typeof(string));
        table.Columns.Add("Количество", typeof(int));

        var query = Program.Store.Supplies.AsEnumerable();

        if (from.HasValue)
            query = query.Where(x => x.Date >= from.Value);

        if (to.HasValue)
            query = query.Where(x => x.Date <= to.Value);

        query = query.Where(x => x.ProductId == productId);

        var grouped = query
            .GroupBy(x => x.SupplierId)
            .Select(g => new
            {
                SupplierId = g.Key,
                TotalQuantity = g.Sum(x => x.Quantity),
                ProductId = productId
            })
            .Where(x => x.TotalQuantity >= minQuantity);

        foreach (var g in grouped)
        {
            var supplier = Program.Store.Suppliers
                .FirstOrDefault(s => s.Id == g.SupplierId);

            var product = Program.Store.Products
                .FirstOrDefault(p => p.Id == productId);

            if (supplier == null || product == null)
                continue;

            table.Rows.Add(
                supplier.Name,
                product.Name,
                g.TotalQuantity
            );
        }

        return table;
    }

    public DataTable GetSuppliersByCategory(
    string category,
    int minQuantity,
    DateTime? from,
    DateTime? to)
    {
        var table = new DataTable();

        table.Columns.Add("Поставщик", typeof(string));
        table.Columns.Add("Категория", typeof(string));
        table.Columns.Add("Количество", typeof(int));

        var query = Program.Store.Supplies.AsEnumerable();

        if (from.HasValue)
            query = query.Where(x => x.Date >= from.Value);

        if (to.HasValue)
            query = query.Where(x => x.Date <= to.Value);

        query = query.Where(x =>
        {
            var product = Program.Store.Products
                .FirstOrDefault(p => p.Id == x.ProductId);

            return product != null && product.Category == category;
        });

        var grouped = query
            .GroupBy(x => x.SupplierId)
            .Select(g => new
            {
                SupplierId = g.Key,
                TotalQuantity = g.Sum(x => x.Quantity),
                Category = category
            })
            .Where(x => x.TotalQuantity >= minQuantity);

        foreach (var g in grouped)
        {
            var supplier = Program.Store.Suppliers
                .FirstOrDefault(s => s.Id == g.SupplierId);

            if (supplier == null)
                continue;

            table.Rows.Add(
                supplier.Name,
                category,
                g.TotalQuantity
            );
        }

        return table;
    }


    // =========================
    // 2. ПОКУПАТЕЛИ
    // =========================
    public DataTable GetCustomersByProduct(
    int productId,
    DateTime? from,
    DateTime? to,
    int minQuantity)
    {
        var table = new DataTable();

        table.Columns.Add("Покупатель");
        table.Columns.Add("Товар");
        table.Columns.Add("Количество", typeof(int));

        var sales = Program.Store.Sales.AsEnumerable();

        if (from.HasValue)
            sales = sales.Where(s => s.Date >= from.Value);

        if (to.HasValue)
            sales = sales.Where(s => s.Date <= to.Value);

        sales = sales.Where(s => s.ProductId == productId);

        var grouped = sales
            .GroupBy(s => new { s.CustomerId, s.ProductId })
            .Select(g => new
            {
                CustomerId = g.Key.CustomerId,
                ProductId = g.Key.ProductId,
                Quantity = g.Sum(x => x.Quantity)
            })
            .Where(x => x.Quantity >= minQuantity);

        foreach (var r in grouped)
        {
            var customer = Program.Store.Customers
                .FirstOrDefault(c => c.Id == r.CustomerId)?.Name ?? "N/A";

            var product = Program.Store.Products
                .FirstOrDefault(p => p.Id == r.ProductId)?.Name ?? "N/A";

            table.Rows.Add(customer, product, r.Quantity);
        }

        return table;
    }

    public DataTable GetCustomersByCategory(
    string category,
    DateTime? from,
    DateTime? to,
    int minQuantity)
    {
        var table = new DataTable();

        table.Columns.Add("Покупатель");
        table.Columns.Add("Товар");
        table.Columns.Add("Количество", typeof(int));

        var sales = Program.Store.Sales.AsEnumerable();

        if (from.HasValue)
            sales = sales.Where(s => s.Date >= from.Value);

        if (to.HasValue)
            sales = sales.Where(s => s.Date <= to.Value);

        var productIds = Program.Store.Products
            .Where(p => p.Category == category)
            .Select(p => p.Id)
            .ToHashSet();

        sales = sales.Where(s => productIds.Contains(s.ProductId));

        var grouped = sales
            .GroupBy(s => new { s.CustomerId, s.ProductId })
            .Select(g => new
            {
                CustomerId = g.Key.CustomerId,
                ProductId = g.Key.ProductId,
                Quantity = g.Sum(x => x.Quantity)
            })
            .Where(x => x.Quantity >= minQuantity);

        foreach (var r in grouped)
        {
            var customer = Program.Store.Customers
                .FirstOrDefault(c => c.Id == r.CustomerId)?.Name ?? "N/A";

            var product = Program.Store.Products
                .FirstOrDefault(p => p.Id == r.ProductId)?.Name ?? "N/A";

            table.Rows.Add(customer, product, r.Quantity);
        }

        return table;
    }

    // =========================
    // 3. ТОВАРЫ В ТОЧКЕ
    // =========================
    public DataTable GetProductsInTradePoint(int tradePointId)
    {
        var table = new DataTable();

        table.Columns.Add("Товар");
        table.Columns.Add("Количество", typeof(int));

        var stocks = Program.Store.Stocks
            .Where(s => s.TradePointId == tradePointId); // если есть связь

        foreach (var stock in stocks)
        {
            var product = Program.Store.Products
                .FirstOrDefault(p => p.Id == stock.ProductId)?.Name ?? "N/A";

            table.Rows.Add(product, stock.Quantity);
        }

        return table;
    }
    // =========================
    // 4. ЦЕНЫ ПО ТОЧКАМ
    // =========================
    public DataTable GetProductPricesByPoints(
    int productId,
    int? tradePointId,
    string pointType)
    {
        return BuildProductPricesReport(
            productId,
            null
        );
    }

    public DataTable GetProductPricesByPointType(
    int productId,
    string type)
    {
        return BuildProductPricesReport(
            productId,
            s =>
            {
                var tp = Program.Store.TradePoints
                    .FirstOrDefault(t => t.Id == s.TradePointId);

                return tp != null && tp.GetType().Name == type;
            }
        );
    }

    public DataTable GetProductPricesByPoint(
    int productId,
    int tradePointId)
    {
        var table = new DataTable();

        table.Columns.Add("Торговая точка");
        table.Columns.Add("Количество", typeof(int));
        table.Columns.Add("Цена", typeof(double));
        table.Columns.Add("Сумма", typeof(double));
        table.Columns.Add("Дата", typeof(DateTime));

        var query = Program.Store.Sales
            .Where(s => s.ProductId == productId && s.TradePointId == tradePointId)
            .AsEnumerable();

        var tpName = Program.Store.TradePoints
            .FirstOrDefault(t => t.Id == tradePointId)?.Name ?? "N/A";

        foreach (var s in query)
        {
            table.Rows.Add(
                tpName,
                s.Quantity,
                s.Price,
                s.Price * s.Quantity,
                s.Date
            );
        }

        return table;
    }

    // =========================
    // 5. ВЫРАБОТКА ПРОДАВЦОВ
    // =========================
    public DataTable GetSellersProductivity(
    DateTime from,
    DateTime to)
    {
        var table = new DataTable();

        table.Columns.Add("Тип точки");
        table.Columns.Add("Количество продавцов", typeof(int));
        table.Columns.Add("Общая выручка", typeof(double));
        table.Columns.Add("Выработка на продавца", typeof(double));

        var groups = Program.Store.TradePoints
            .GroupBy(tp => tp.GetType().Name);

        foreach (var group in groups)
        {
            var tpIds = group.Select(t => t.Id).ToList();

            var sellers = Program.Store.Sellers
                .Where(s => tpIds.Contains(s.TradePointId))
                .ToList();

            int sellerCount = sellers.Count;

            var sellerIds = sellers.Select(s => s.Id).ToList();

            double revenue = Program.Store.Sales
                .Where(s =>
                    sellerIds.Contains(s.SellerId) &&
                    s.Date >= from &&
                    s.Date <= to)
                .Sum(s => s.Price * s.Quantity);

            double productivity = sellerCount > 0
                ? revenue / sellerCount
                : 0;

            table.Rows.Add(
                group.Key,
                sellerCount,
                revenue,
                productivity
            );
        }

        return table;
    }

    public DataTable GetSellersProductivityByType(
    DateTime from,
    DateTime to,
    string pointType)
    {
        var table = new DataTable();

        table.Columns.Add("Тип точки");
        table.Columns.Add("Количество продавцов", typeof(int));
        table.Columns.Add("Общая выручка", typeof(double));
        table.Columns.Add("Выработка на продавца", typeof(double));

        var tpIds = Program.Store.TradePoints
            .Where(tp => tp.GetType().Name == pointType)
            .Select(tp => tp.Id)
            .ToList();

        var sellers = Program.Store.Sellers
            .Where(s => tpIds.Contains(s.TradePointId))
            .ToList();

        int sellerCount = sellers.Count;

        var sellerIds = sellers
            .Select(s => s.Id)
            .ToList();

        double revenue = Program.Store.Sales
            .Where(s =>
                sellerIds.Contains(s.SellerId) &&
                s.Date >= from &&
                s.Date <= to)
            .Sum(s => s.Price * s.Quantity);

        double productivity = sellerCount > 0
            ? revenue / sellerCount
            : 0;

        table.Rows.Add(
            pointType,
            sellerCount,
            revenue,
            productivity
        );

        return table;
    }

    // =========================
    // 6. КОНКРЕТНЫЙ ПРОДАВЕЦ
    // =========================
    public DataTable GetSpecificSellerProductivity(
    int sellerId,
    int tradePointId,
    DateTime from,
    DateTime to)
    {
        var table = new DataTable();

        table.Columns.Add("Продавец");
        table.Columns.Add("Торговая точка");
        table.Columns.Add("Количество продаж", typeof(int));
        table.Columns.Add("Выручка", typeof(double));

        var seller = Program.Store.Sellers
            .FirstOrDefault(s => s.Id == sellerId);

        var tradePoint = Program.Store.TradePoints
            .FirstOrDefault(t => t.Id == tradePointId);

        if (seller == null || tradePoint == null)
            return table;

        var sales = Program.Store.Sales
            .Where(s =>
                s.SellerId == sellerId &&
                s.TradePointId == tradePointId &&
                s.Date >= from &&
                s.Date <= to)
            .ToList();

        int totalSales = sales.Sum(s => s.Quantity);

        double revenue = sales.Sum(
            s => s.Quantity * s.Price);

        table.Rows.Add(
            seller.Name,
            tradePoint.Name,
            totalSales,
            revenue
        );

        return table;
    }

    // =========================
    // 7. ПРОДАЖИ ТОВАРА
    // =========================
    public DataTable GetProductSalesVolume(
    int productId,
    Func<Sale, bool> pointFilter,
    DateTime from,
    DateTime to)
    {
        var table = new DataTable();

        table.Columns.Add("Торговая точка");
        table.Columns.Add("Тип точки");
        table.Columns.Add("Количество продаж", typeof(int));
        table.Columns.Add("Объём товара", typeof(int));

        var sales = Program.Store.Sales
            .Where(s =>
                s.ProductId == productId &&
                s.Date >= from &&
                s.Date <= to);

        if (pointFilter != null)
            sales = sales.Where(pointFilter);

        var grouped = sales
            .GroupBy(s => s.TradePointId);

        foreach (var g in grouped)
        {
            var tp = Program.Store.TradePoints
                .FirstOrDefault(t => t.Id == g.Key);

            if (tp == null)
                continue;

            table.Rows.Add(
                tp.Name,
                tp.GetPointType(),
                g.Count(),
                g.Sum(x => x.Quantity)
            );
        }

        return table;
    }

    public DataTable GetProductSalesVolumeAll(
    int productId,
    DateTime from,
    DateTime to)
    {
        return GetProductSalesVolume(
            productId,
            null,
            from,
            to
        );
    }

    public DataTable GetProductSalesVolumeByType(
    int productId,
    string type,
    DateTime from,
    DateTime to)
    {
        return GetProductSalesVolume(
            productId,
            s =>
            {
                var tp = Program.Store.TradePoints
                    .FirstOrDefault(t => t.Id == s.TradePointId);

                return tp != null &&
                       tp.GetType().Name == type;
            },
            from,
            to
        );
    }

    public DataTable GetProductSalesVolumeByPoint(
    int productId,
    int tradePointId,
    DateTime from,
    DateTime to)
    {
        return GetProductSalesVolume(
            productId,
            s => s.TradePointId == tradePointId,
            from,
            to
        );
    }

    public DataTable GetSellersSalaryAll(
    DateTime from,
    DateTime to)
    {
        return BuildSellersSalaryReport(
            null,
            from,
            to
        );
    }

    public DataTable GetSellersSalaryByType(
        string type,
        DateTime from,
        DateTime to)
    {
        return BuildSellersSalaryReport(
            s =>
            {
                var tp = Program.Store.TradePoints
                    .FirstOrDefault(t => t.Id == s.TradePointId);

                return tp != null &&
                       tp.GetType().Name == type;
            },
            from,
            to
        );
    }

    public DataTable GetSellersSalaryByPoint(
        int tradePointId,
        DateTime from,
        DateTime to)
    {
        return BuildSellersSalaryReport(
            s => s.TradePointId == tradePointId,
            from,
            to
        );
    }

    private DataTable BuildSellersSalaryReport(
    Func<Seller, bool> filter,
    DateTime from,
    DateTime to)
    {
        var table = new DataTable();

        table.Columns.Add("Продавец");
        table.Columns.Add("Торговая точка");
        table.Columns.Add("Тип точки");
        table.Columns.Add("Выручка");
        table.Columns.Add("Зарплата");

        var sellers = Program.Store.Sellers.AsEnumerable();

        if (filter != null)
            sellers = sellers.Where(filter);

        foreach (var seller in sellers)
        {
            var tp = Program.Store.TradePoints
                .FirstOrDefault(t => t.Id == seller.TradePointId);

            if (tp == null)
                continue;

            var sales = Program.Store.Sales
                .Where(s =>
                    s.SellerId == seller.Id &&
                    s.Date >= from &&
                    s.Date <= to)
                .ToList();

            double revenue = sales.Sum(s =>
                s.Price * s.Quantity);

            double salary = revenue * 0.05;

            table.Rows.Add(
                seller.Name,
                tp.Name,
                tp.GetPointType(),
                revenue,
                salary
            );
        }

        return table;
    }

    // =========================
    // 8. ПОСТАВКИ ПОСТАВЩИКА
    // =========================
    public DataTable GetSupplierProductSupplies(
     int supplierId,
     int productId,
     DateTime? from,
     DateTime? to)
    {
        var table = new DataTable();

        table.Columns.Add("Поставка ID");
        table.Columns.Add("Дата");
        table.Columns.Add("Поставщик");
        table.Columns.Add("Товар");
        table.Columns.Add("Торговая точка");
        table.Columns.Add("Количество");
        table.Columns.Add("Цена");
        table.Columns.Add("Сумма");

        var supplies = Program.Store.Supplies
            .Where(s =>
                s.SupplierId == supplierId &&
                s.ProductId == productId);

        if (from.HasValue)
            supplies = supplies
                .Where(s => s.Date.Date >= from.Value.Date);

        if (to.HasValue)
            supplies = supplies
                .Where(s => s.Date.Date <= to.Value.Date);

        foreach (var s in supplies.OrderBy(s => s.Date))
        {
            var supplier = Program.Store.Suppliers
                .FirstOrDefault(x => x.Id == s.SupplierId);

            var product = Program.Store.Products
                .FirstOrDefault(x => x.Id == s.ProductId);

            var point = Program.Store.TradePoints
                .FirstOrDefault(x => x.Id == s.TradePointId);

            table.Rows.Add(
                s.Id,
                s.Date.ToShortDateString(),
                supplier?.Name ?? "",
                product?.Name ?? "",
                point?.Name ?? "",
                s.Quantity,
                s.Price,
                s.Quantity * s.Price
            );
        }

        return table;
    }

    // =========================
    // 9. РЕНТАБЕЛЬНОСТЬ
    // =========================
    public DataTable GetTradePointEfficiencyByArea(
    int? tradePointId,
    string type,
    DateTime from,
    DateTime to)
    {
        var table = new DataTable();

        table.Columns.Add("Точка");
        table.Columns.Add("Тип");
        table.Columns.Add("Выручка");
        table.Columns.Add("Площадь");
        table.Columns.Add("Продажи на м²");

        var tradePoints = Program.Store.TradePoints.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(type))
        {
            tradePoints = tradePoints
                .Where(t => t.GetType().Name == type);
        }

        if (tradePointId.HasValue)
        {
            tradePoints = tradePoints
                .Where(t => t.Id == tradePointId.Value);
        }

        foreach (var tp in tradePoints)
        {
            double revenue = Program.Store.Sales
                .Where(s =>
                    s.TradePointId == tp.Id &&
                    s.Date >= from &&
                    s.Date <= to)
                .Sum(s => s.Price * s.Quantity);

            double ratio = 0;

            if (tp.Size > 0)
                ratio = revenue / tp.Size;

            table.Rows.Add(
                tp.Name,
                tp.GetPointType(),
                revenue,
                tp.Size,
                Math.Round(ratio, 2)
            );
        }

        return table;
    }

    public DataTable GetTradePointEfficiencyByHalls(
     int? tradePointId,
     string type,
     DateTime from,
     DateTime to)
    {
        var table = new DataTable();

        table.Columns.Add("Точка");
        table.Columns.Add("Тип");
        table.Columns.Add("Выручка");
        table.Columns.Add("Залы");
        table.Columns.Add("Продажи на зал");

        var tradePoints = Program.Store.TradePoints.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(type))
        {
            tradePoints = tradePoints
                .Where(t => t.GetType().Name == type);
        }

        if (tradePointId.HasValue)
        {
            tradePoints = tradePoints
                .Where(t => t.Id == tradePointId.Value);
        }

        foreach (var tp in tradePoints)
        {
            int halls = 0;

            if (tp is Shop shop)
                halls = shop.Halls.Count;
            if (tp is DepartmentStore dp)
                halls = dp.Halls.Count;
            if (tp is Kiosk kiosk)
                halls = 1;
            if (tp is Stall stall)
                halls = 1;

            double revenue = Program.Store.Sales
                .Where(s =>
                    s.TradePointId == tp.Id &&
                    s.Date >= from &&
                    s.Date <= to)
                .Sum(s => s.Price * s.Quantity);

            double ratio = 0;

            if (halls > 0)
                ratio = revenue / halls;

            table.Rows.Add(
                tp.Name,
                tp.GetPointType(),
                revenue,
                halls,
                Math.Round(ratio, 2)
            );
        }

        return table;
    }

    public DataTable GetTradePointEfficiencyByCounters(
    int? tradePointId,
    string type,
    DateTime from,
    DateTime to)
    {
        var table = new DataTable();

        table.Columns.Add("Точка");
        table.Columns.Add("Тип");
        table.Columns.Add("Выручка");
        table.Columns.Add("Прилавки");
        table.Columns.Add("Продажи на прилавок");

        var tradePoints = Program.Store.TradePoints.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(type))
        {
            tradePoints = tradePoints
                .Where(t => t.GetType().Name == type);
        }

        if (tradePointId.HasValue)
        {
            tradePoints = tradePoints
                .Where(t => t.Id == tradePointId.Value);
        }

        foreach (var tp in tradePoints)
        {
            double revenue = Program.Store.Sales
                .Where(s =>
                    s.TradePointId == tp.Id &&
                    s.Date >= from &&
                    s.Date <= to)
                .Sum(s => s.Price * s.Quantity);

            double ratio = 0;

            if (tp.Counters > 0)
                ratio = revenue / tp.Counters;

            table.Rows.Add(
                tp.Name,
                tp.GetPointType(),
                revenue,
                tp.Counters,
                Math.Round(ratio, 2)
            );
        }

        return table;
    }

    // =========================
    // 10. ТОВАРООБОРОТ
    // =========================
    public DataTable GetTradeTurnover(
        DateTime from,
        DateTime to,
        int? tradePointId = null)
    {
        var query = _storage.Sales
            .Where(s => s.Date >= from)
            .Where(s => s.Date <= to)
            .Where(s => !tradePointId.HasValue || s.TradePointId == tradePointId.Value)
            .Join(_storage.TradePoints, s => s.TradePointId, tp => tp.Id, (s, tp) => new { s, tp })
            .GroupBy(x => new { x.tp.Id, x.tp.Name })
            .Select(g => new
            {
                Точка = g.Key.Name,
                Оборот = g.Sum(x => x.s.Quantity * x.s.Price)
            });

        return ToDataTable(query);
    }

    // =========================
    // 11. Рентабельность ТТ
    // =========================
    public DataTable GetTradePointProfitability(
    int? tradePointId,
    DateTime from,
    DateTime to)
    {
        var table = new DataTable();

        table.Columns.Add("Точка");
        table.Columns.Add("Тип");
        table.Columns.Add("Выручка");
        table.Columns.Add("Зарплаты");
        table.Columns.Add("Аренда");
        table.Columns.Add("Коммунальные");
        table.Columns.Add("Расходы");
        table.Columns.Add("Рентабельность");

        var tradePoints = Program.Store.TradePoints.AsEnumerable();

        if (tradePointId.HasValue)
        {
            tradePoints = tradePoints
                .Where(t => t.Id == tradePointId.Value);
        }

        foreach (var tp in tradePoints)
        {
            double revenue = Program.Store.Sales
                .Where(s =>
                    s.TradePointId == tp.Id &&
                    s.Date >= from &&
                    s.Date <= to)
                .Sum(s => s.Price * s.Quantity);

            double salaries = Program.Store.Sellers
                .Where(s => s.TradePointId == tp.Id)
                .Sum(s => s.Salary);

            double expenses =
                salaries +
                tp.Rent +
                tp.Utilities;

            double profitability = 0;

            if (expenses > 0)
            {
                profitability = revenue / expenses;
            }

            table.Rows.Add(
                tp.Name,
                tp.GetPointType(),
                Math.Round(revenue, 2),
                Math.Round(salaries, 2),
                Math.Round(tp.Rent, 2),
                Math.Round(tp.Utilities, 2),
                Math.Round(expenses, 2),
                Math.Round(profitability, 2)
            );
        }

        return table;
    }

    // =========================
    // 11. Поставка по номеру заказа
    // =========================
    public DataTable GetSupplyByOrderNumber(int supplyId)
    {
        var table = new DataTable();

        table.Columns.Add("Номер");
        table.Columns.Add("Торговая точка");
        table.Columns.Add("Поставщик");
        table.Columns.Add("Товар");
        table.Columns.Add("Количество");
        table.Columns.Add("Цена");
        table.Columns.Add("Дата");

        var supply = Program.Store.Supplies
            .FirstOrDefault(s => s.Id == supplyId);

        if (supply == null)
            return table;

        var tradePoint = Program.Store.TradePoints
            .FirstOrDefault(t => t.Id == supply.TradePointId);

        var supplier = Program.Store.Suppliers
            .FirstOrDefault(s => s.Id == supply.SupplierId);

        var product = Program.Store.Products
            .FirstOrDefault(p => p.Id == supply.ProductId);

        table.Rows.Add(
            supply.Id,
            tradePoint?.Name,
            supplier?.Name,
            product?.Name,
            supply.Quantity,
            supply.Price,
            supply.Date.ToShortDateString()
        );

        return table;
    }

    public DataTable GetBuyersByProductAll(
    int productId,
    DateTime from,
    DateTime to)
    {
        var table = CreateBuyersTable();

        var sales = Program.Store.Sales
            .Where(s =>
                s.ProductId == productId &&
                s.Date >= from &&
                s.Date <= to);

        FillBuyersTable(table, sales);

        return table;
    }

    public DataTable GetBuyersByProductType(
    int productId,
    string type,
    DateTime from,
    DateTime to)
    {
        var table = CreateBuyersTable();

        var tradePointIds = Program.Store.TradePoints
            .Where(t => t.GetType().Name == type)
            .Select(t => t.Id)
            .ToList();

        var sales = Program.Store.Sales
            .Where(s =>
                s.ProductId == productId &&
                tradePointIds.Contains(s.TradePointId) &&
                s.Date >= from &&
                s.Date <= to);

        FillBuyersTable(table, sales);

        return table;
    }

    public DataTable GetBuyersByProductPoint(
    int productId,
    int tradePointId,
    DateTime from,
    DateTime to)
    {
        var table = CreateBuyersTable();

        var sales = Program.Store.Sales
            .Where(s =>
                s.ProductId == productId &&
                s.TradePointId == tradePointId &&
                s.Date >= from &&
                s.Date <= to);

        FillBuyersTable(table, sales);

        return table;
    }

    private DataTable CreateBuyersTable()
    {
        var table = new DataTable();

        table.Columns.Add("Покупатель");
        table.Columns.Add("Товар");
        table.Columns.Add("Точка");
        table.Columns.Add("Тип");
        table.Columns.Add("Количество");
        table.Columns.Add("Сумма");
        table.Columns.Add("Дата");

        return table;
    }

    private void FillBuyersTable(
    DataTable table,
    IEnumerable<Sale> sales)
    {
        foreach (var sale in sales)
        {
            var customer = Program.Store.Customers
                .FirstOrDefault(c => c.Id == sale.CustomerId);

            var product = Program.Store.Products
                .FirstOrDefault(p => p.Id == sale.ProductId);

            var tradePoint = Program.Store.TradePoints
                .FirstOrDefault(t => t.Id == sale.TradePointId);

            table.Rows.Add(
                customer?.Name ?? "Не указан",
                product?.Name,
                tradePoint?.Name,
                tradePoint?.GetPointType(),
                sale.Quantity,
                sale.Quantity * sale.Price,
                sale.Date.ToShortDateString()
            );
        }
    }

    public DataTable GetMostActiveCustomersAll(
    DateTime from,
    DateTime to)
    {
        var sales = Program.Store.Sales
            .Where(s =>
                s.CustomerId.HasValue &&
                s.Date >= from &&
                s.Date <= to);

        return BuildActiveCustomersTable(sales);
    }

    public DataTable GetMostActiveCustomersByType(
    string type,
    DateTime from,
    DateTime to)
    {
        var tradePointIds = Program.Store.TradePoints
            .Where(t => t.GetType().Name == type)
            .Select(t => t.Id)
            .ToList();

        var sales = Program.Store.Sales
            .Where(s =>
                s.CustomerId.HasValue &&
                tradePointIds.Contains(s.TradePointId) &&
                s.Date >= from &&
                s.Date <= to);

        return BuildActiveCustomersTable(sales);
    }

    public DataTable GetMostActiveCustomersByPoint(
    int tradePointId,
    DateTime from,
    DateTime to)
    {
        var sales = Program.Store.Sales
            .Where(s =>
                s.CustomerId.HasValue &&
                s.TradePointId == tradePointId &&
                s.Date >= from &&
                s.Date <= to);

        return BuildActiveCustomersTable(sales);
    }

    private DataTable BuildActiveCustomersTable(
    IEnumerable<Sale> sales)
    {
        var table = new DataTable();

        table.Columns.Add("Покупатель");
        table.Columns.Add("Количество покупок");
        table.Columns.Add("Общая сумма");

        var grouped = sales
            .GroupBy(s => s.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Purchases = g.Count(),
                Total = g.Sum(x => x.Price * x.Quantity)
            })
            .OrderByDescending(x => x.Total);

        foreach (var item in grouped)
        {
            var customer = Program.Store.Customers
                .FirstOrDefault(c => c.Id == item.CustomerId);

            table.Rows.Add(
                customer?.Name,
                item.Purchases,
                Math.Round(item.Total, 2)
            );
        }

        return table;
    }

    public DataTable GetTradePointTurnover(
    int tradePointId,
    DateTime from,
    DateTime to)
    {
        var table = new DataTable();

        table.Columns.Add("Точка");
        table.Columns.Add("Тип");
        table.Columns.Add("Товарооборот");

        var tp = Program.Store.TradePoints
            .FirstOrDefault(t => t.Id == tradePointId);

        if (tp == null)
            return table;

        double turnover = Program.Store.Sales
            .Where(s =>
                s.TradePointId == tradePointId &&
                s.Date >= from &&
                s.Date <= to)
            .Sum(s => s.Price * s.Quantity);

        table.Rows.Add(
            tp.Name,
            tp.GetPointType(),
            Math.Round(turnover, 2)
        );

        return table;
    }

    public DataTable GetTradePointGroupTurnover(
    string type,
    DateTime from,
    DateTime to)
    {
        var table = new DataTable();

        table.Columns.Add("Тип");
        table.Columns.Add("Количество точек");
        table.Columns.Add("Товарооборот");

        var points = Program.Store.TradePoints
            .Where(t => t.GetType().Name == type)
            .ToList();

        var ids = points.Select(t => t.Id).ToList();

        double turnover = Program.Store.Sales
            .Where(s =>
                ids.Contains(s.TradePointId) &&
                s.Date >= from &&
                s.Date <= to)
            .Sum(s => s.Price * s.Quantity);

        string typeRu = points.FirstOrDefault()?.GetPointType()
            ?? type;

        table.Rows.Add(
            typeRu,
            points.Count,
            Math.Round(turnover, 2)
        );

        return table;
    }
    // =========================
    // helper
    // =========================
    private DataTable ToDataTable<T>(IEnumerable<T> data)
    {
        var dt = new DataTable();
        var props = typeof(T).GetProperties();

        foreach (var p in props)
            dt.Columns.Add(p.Name, Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType);

        foreach (var item in data)
            dt.Rows.Add(props.Select(p => p.GetValue(item)).ToArray());

        return dt;
    }

    public DataTable GetSalesView()
    {
        var data = Program.Store.Sales.Select(s => new
        {
            s.Id,
            Product = Lookup.ProductName(s.ProductId),
            TradePoint = Lookup.TradePointName(s.TradePointId),
            Seller = Lookup.SellerName(s.SellerId),
            Customer = Lookup.CustomerName(s.CustomerId),
            s.Quantity,
            s.Price,
            s.Date
        });

        return ToDataTable(data);
    }

    public DataTable GetSuppliesView()
    {
        var data = Program.Store.Supplies.Select(s => new
        {
            s.Id,
            Supplier = Lookup.SupplierName(s.SupplierId),
            Product = Lookup.ProductName(s.ProductId),
            s.Quantity,
            s.Price,
            s.Date
        });

        return ToDataTable(data);
    }

    public DataTable GetRequestsView()
    {
        var data = Program.Store.Requests.Select(r => new
        {
            r.Id,
            TradePoint = Lookup.TradePointName(r.TradePointId),
            Product = Lookup.ProductName(r.ProductId),
            r.Quantity,
            r.Date
        });

        return ToDataTable(data);
    }

    public DataTable GetOrdersView()
    {
        var data = Program.Store.SupplierOrders.Select(o => new
        {
            o.Id,
            Supplier = Lookup.SupplierName(o.SupplierId),
            Product = Lookup.ProductName(o.ProductId),
            o.Quantity,
            o.Price,
            o.Date
        });

        return ToDataTable(data);
    }

    private DataTable BuildProductPricesReport(
    int productId,
    Func<Sale, bool> filter)
    {
        var table = new DataTable();

        table.Columns.Add("Торговая точка");
        table.Columns.Add("Количество", typeof(int));
        table.Columns.Add("Цена", typeof(double));
        table.Columns.Add("Сумма", typeof(double));
        table.Columns.Add("Дата", typeof(DateTime));

        var query = Program.Store.Sales
            .Where(s => s.ProductId == productId)
            .AsEnumerable();

        if (filter != null)
            query = query.Where(filter);

        foreach (var s in query)
        {
            var tpName = Program.Store.TradePoints
                .FirstOrDefault(t => t.Id == s.TradePointId)?.Name ?? "N/A";

            table.Rows.Add(
                tpName,
                s.Quantity,
                s.Price,
                s.Price * s.Quantity,
                s.Date
            );
        }

        return table;
    }

}