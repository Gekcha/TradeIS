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

    // =========================
    // 1. ПОСТАВЩИКИ ТОВАРА
    // =========================
    public DataTable GetSuppliersByProduct(
    int? supplierId,
    int? productId,
    DateTime? from,
    DateTime? to)
    {
        var table = new DataTable();

        table.Columns.Add("Поставщик");
        table.Columns.Add("Товар");
        table.Columns.Add("Количество", typeof(int));
        table.Columns.Add("Цена", typeof(double));
        table.Columns.Add("Дата", typeof(DateTime));

        var query = Program.Store.Supplies.AsEnumerable();

        if (from.HasValue)
            query = query.Where(x => x.Date >= from.Value);

        if (to.HasValue)
            query = query.Where(x => x.Date <= to.Value);

        if (supplierId.HasValue)
            query = query.Where(x => x.SupplierId == supplierId.Value);

        if (productId.HasValue)
            query = query.Where(x => x.ProductId == productId.Value);

        foreach (var s in query)
        {
            var supplier = Program.Store.Suppliers
                .FirstOrDefault(x => x.Id == s.SupplierId)?.Name ?? "N/A";

            var product = Program.Store.Products
                .FirstOrDefault(x => x.Id == s.ProductId)?.Name ?? "N/A";

            table.Rows.Add(supplier, product, s.Quantity, s.Price, s.Date);
        }

        return table;
    }

    // =========================
    // 2. ПОКУПАТЕЛИ
    // =========================
    public DataTable GetCustomersByProduct(
        int productId,
        DateTime from,
        DateTime to,
        int? tradePointId = null,
        int minQty = 0)
    {
        var query = _storage.Sales
            .Where(s => s.Date >= from)
            .Where(s => s.Date <= to)
            .Where(s => !tradePointId.HasValue || s.TradePointId == tradePointId.Value)
            .Where(s => productId == 0 || s.ProductId == productId)
            .GroupBy(s => s.CustomerId)
            .Select(g => new
            {
                Покупатель = _storage.Customers.FirstOrDefault(c => c.Id == g.Key).Name,
                Покупок = g.Sum(x => x.Quantity)
            })
            .Where(x => x.Покупок >= minQty);

        return ToDataTable(query);
    }

    // =========================
    // 3. ТОВАРЫ В ТОЧКЕ
    // =========================
    public DataTable GetProductsInPoint(int tradePointId)
    {
        var query = _storage.Stocks
            .Where(s => s.TradePointId == tradePointId)
            .Join(_storage.Products, s => s.ProductId, p => p.Id, (s, p) => new
            {
                Товар = p.Name,
                Категория = p.Category,
                Количество = s.Quantity
            });

        return ToDataTable(query);
    }

    // =========================
    // 4. ЦЕНЫ ПО ТОЧКАМ
    // =========================
    public DataTable GetProductPricesByPoints(
        int productId,
        string tpType)
    {
        var query = _storage.Sales
            .Join(_storage.TradePoints, s => s.TradePointId, tp => tp.Id, (s, tp) => new { s, tp })
            .Join(_storage.Products, x => x.s.ProductId, p => p.Id, (x, p) => new { x.s, x.tp, p })
            .Where(x => productId == 0 || x.p.Id == productId)
            .Where(x => string.IsNullOrWhiteSpace(tpType) || x.tp.GetPointType() == tpType)
            .Select(x => new
            {
                Точка = x.tp.Name,
                Товар = x.p.Name,
                Цена = x.s.Price
            });

        return ToDataTable(query);
    }

    // =========================
    // 5. ВЫРАБОТКА ПРОДАВЦОВ
    // =========================
    public DataTable GetSellersProductivity(
        DateTime from,
        DateTime to,
        int? tradePointId = null)
    {
        var query = _storage.Sales
            .Where(s => s.Date >= from)
            .Where(s => s.Date <= to)
            .Where(s => !tradePointId.HasValue || s.TradePointId == tradePointId.Value)
            .GroupBy(s => s.SellerId)
            .Select(g => new
            {
                Продавец = _storage.Sellers.FirstOrDefault(x => x.Id == g.Key).Name,
                Количество = g.Sum(x => x.Quantity),
                Выручка = g.Sum(x => x.Quantity * x.Price)
            });

        return ToDataTable(query);
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
        var query = _storage.Sales
            .Where(s => s.SellerId == sellerId)
            .Where(s => s.TradePointId == tradePointId)
            .Where(s => s.Date >= from)
            .Where(s => s.Date <= to)
            .GroupBy(s => s.SellerId)
            .Select(g => new
            {
                ПродавецId = g.Key,
                Количество = g.Sum(x => x.Quantity),
                Выручка = g.Sum(x => x.Quantity * x.Price)
            });

        return ToDataTable(query);
    }

    // =========================
    // 7. ПРОДАЖИ ТОВАРА
    // =========================
    public DataTable GetProductSalesReport(
        int productId,
        DateTime from,
        DateTime to,
        int? tradePointId)
    {
        var query = _storage.Sales
            .Where(s => s.Date >= from)
            .Where(s => s.Date <= to)
            .Where(s => productId == 0 || s.ProductId == productId)
            .Where(s => !tradePointId.HasValue || s.TradePointId == tradePointId.Value)
            .Join(_storage.Products, s => s.ProductId, p => p.Id, (s, p) => new { s, p })
            .Join(_storage.TradePoints, x => x.s.TradePointId, tp => tp.Id, (x, tp) => new { x.s, x.p, tp })
            .Select(x => new
            {
                Товар = x.p.Name,
                Точка = x.tp.Name,
                Количество = x.s.Quantity,
                Выручка = x.s.Quantity * x.s.Price
            });

        return ToDataTable(query);
    }

    // =========================
    // 8. ПОСТАВКИ ПОСТАВЩИКА
    // =========================
    public DataTable GetSuppliesBySupplier(
    int? supplierId,
    DateTime? from,
    DateTime? to)
    {
        var table = new DataTable();

        table.Columns.Add("Поставщик");
        table.Columns.Add("Товар");
        table.Columns.Add("Количество", typeof(int));
        table.Columns.Add("Цена", typeof(double));
        table.Columns.Add("Дата", typeof(DateTime));

        var query = Program.Store.Supplies.AsEnumerable();

        if (from.HasValue)
            query = query.Where(x => x.Date >= from.Value);

        if (to.HasValue)
            query = query.Where(x => x.Date <= to.Value);

        if (supplierId.HasValue)
            query = query.Where(x => x.SupplierId == supplierId.Value);

        foreach (var s in query)
        {
            var supplier = Program.Store.Suppliers
                .FirstOrDefault(x => x.Id == s.SupplierId)?.Name ?? "N/A";

            var product = Program.Store.Products
                .FirstOrDefault(x => x.Id == s.ProductId)?.Name ?? "N/A";

            table.Rows.Add(supplier, product, s.Quantity, s.Price, s.Date);
        }

        return table;
    }

    // =========================
    // 9. РЕНТАБЕЛЬНОСТЬ
    // =========================
    public DataTable GetProfitabilityReport(
        int tradePointId,
        DateTime from,
        DateTime to)
    {
        var sales = _storage.Sales
            .Where(s => s.TradePointId == tradePointId)
            .Where(s => s.Date >= from)
            .Where(s => s.Date <= to);

        var revenue = sales.Sum(s => s.Quantity * s.Price);

        var salaries = _storage.Sellers
            .Where(s => s.TradePointId == tradePointId)
            .Sum(s => s.Salary);

        var tp = _storage.TradePoints.FirstOrDefault(x => x.Id == tradePointId);

        var result = new[]
        {
            new
            {
                Выручка = revenue,
                Расходы = salaries + (tp?.Rent ?? 0) + (tp?.Utilities ?? 0),
                Прибыль = revenue - (salaries + (tp?.Rent ?? 0) + (tp?.Utilities ?? 0))
            }
        };

        return ToDataTable(result);
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
}