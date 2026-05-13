using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TradeIS.Models;

namespace TradeIS
{
    public class ReportEngine
    {
        private readonly DataStorage _storage;

        public ReportEngine(DataStorage storage)
        {
            _storage = storage;
        }

        public DataTable ToDataTable<T>(IEnumerable<T> items)
        {
            DataTable table = new DataTable();
            var props = typeof(T).GetProperties();

            foreach (var prop in props)
            {
                Type propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                table.Columns.Add(prop.Name, propType);
            }

            foreach (var item in items)
            {
                var values = new object[props.Length];
                for (int i = 0; i < props.Length; i++)
                {
                    values[i] = props[i].GetValue(item) ?? DBNull.Value;
                }
                table.Rows.Add(values);
            }
            return table;
        }

        // 1 & 9. Поставщики (товар, объем, период) + Сведения о поставках конкретного поставщика
        public DataTable GetSuppliersReport(
            string productName,
            int minQuantity,
            DateTime from,
            DateTime to,
            string supplierName = "")
        {
            var query = _storage.Suppliers

                .Join(_storage.SupplierOrders,
                    s => s.Id,
                    so => so.SupplierId,
                    (s, so) => new { s, so })

                .Join(_storage.Products,
                    x => x.so.ProductId,
                    p => p.Id,
                    (x, p) => new { x.s, x.so, p })

                .Where(x =>
                    x.so.Date >= from &&
                    x.so.Date <= to)

                .Where(x =>
                    string.IsNullOrWhiteSpace(productName) ||
                    x.p.Name.Contains(productName))

                .Where(x =>
                    string.IsNullOrWhiteSpace(supplierName) ||
                    x.s.Name == supplierName)

                // ГРУППИРОВКА ПО ПОСТАВЩИКУ И ТОВАРУ
                .GroupBy(x => new
                {
                    Поставщик = x.s.Name,
                    Товар = x.p.Name
                })

                // ФИЛЬТР ПО СУММАРНОМУ ОБЪЕМУ
                .Where(g =>
                    minQuantity <= 0 ||
                    g.Sum(x => x.so.Quantity) >= minQuantity)

                .Select(g => new
                {
                    Поставщик = g.Key.Поставщик,
                    Товар = g.Key.Товар,

                    Всего_Поставлено =
                        g.Sum(x => x.so.Quantity),

                    Общая_Сумма =
                        g.Sum(x => x.so.Quantity * x.so.Price),

                    Количество_Поставок =
                        g.Count(),

                    Первая_Поставка =
                        g.Min(x => x.so.Date),

                    Последняя_Поставка =
                        g.Max(x => x.so.Date)
                })

                .OrderByDescending(x => x.Всего_Поставлено)

                .ToList();

            return ToDataTable(query);
        }
        // 11. Рентабельность + 10. Отношение продаж к площади/прилавкам
        public DataTable GetProfitabilityReport(TradePoint tp, DateTime from, DateTime to)
        {
            if (tp == null) return new DataTable();

            double sales = _storage.Sales
                .Where(s => s.TradePoint == tp.Name && s.Date >= from && s.Date <= to)
                .Sum(s => (double)s.Price * s.Quantity);

            double salaries = _storage.Sellers
                .Where(s => s.TradePoint == tp.Name)
                .Sum(s => s.Salary);

            double costs = salaries + tp.Rent + tp.Utilities;

            var res = new[] {
                new {
                    Точка = tp.Name,
                    Тип = tp.GetPointType(),
                    Выручка = sales,
                    Расходы = costs,
                    Прибыль = sales - costs,
                    Рентабельность_Процент =
                        costs > 0
                            ? Math.Round(((sales - costs) / costs) * 100, 2)
                            : 0,                    На_кв_м = tp.Size > 0 ? Math.Round(sales / tp.Size, 2) : 0,
                    На_прилавок = tp.Counters > 0 ? Math.Round(sales / tp.Counters, 2) : 0
                }
            };
            return ToDataTable(res);
        }

        // 3. Номенклатура и ОБЪЕМ в указанной точке
        public DataTable GetProductsInPoint(string tpName)
        {
            var query = _storage.Sales

                .Where(s =>
                    s.TradePoint == tpName)

                .GroupBy(s => s.Product)

                .Select(g => new
                {
                    Товар = g.Key,

                    Общий_Объем =
                        g.Sum(x => x.Quantity),

                    Средняя_Цена =
                        Math.Round(g.Average(x => x.Price), 2),

                    Выручка =
                        g.Sum(x => x.Price * x.Quantity),

                    Количество_Продаж =
                        g.Count()
                })

                .OrderByDescending(x => x.Общий_Объем)

                .ToList();

            return ToDataTable(query);
        }
        // 2 & 13 & 14. Покупатели по товару/типу точки + Активные покупатели
        public DataTable GetCustomersByProduct(
            string productName,
            DateTime from,
            DateTime to,
            string tpType = "",
            int minVol = 0)
        {
            var query = _storage.Sales

                .Join(_storage.TradePoints,
                    s => s.TradePoint,
                    tp => tp.Name,
                    (s, tp) => new { s, tp })

                .Where(x =>
                    x.s.Date >= from &&
                    x.s.Date <= to)

                .Where(x =>
                    string.IsNullOrWhiteSpace(productName) ||
                    x.s.Product == productName)

                .Where(x =>
                    string.IsNullOrWhiteSpace(tpType) ||
                    x.tp.GetPointType() == tpType)

                // ГРУППИРОВКА ПО ПОКУПАТЕЛЮ
                .GroupBy(x => x.s.Customer ?? "Розничный")

                // ФИЛЬТР ПО ОБЩЕМУ ОБЪЕМУ
                .Where(g =>
                    minVol <= 0 ||
                    g.Sum(x => x.s.Quantity) >= minVol)

                .Select(g => new
                {
                    Покупатель = g.Key,

                    Всего_Куплено =
                        g.Sum(x => x.s.Quantity),

                    Общая_Сумма =
                        g.Sum(x => x.s.Quantity * x.s.Price),

                    Количество_Покупок =
                        g.Count(),

                    Первый_Заказ =
                        g.Min(x => x.s.Date),

                    Последний_Заказ =
                        g.Max(x => x.s.Date)
                })

                .OrderByDescending(x => x.Общая_Сумма)

                .ToList();

            return ToDataTable(query);
        }
        // 4. Цены и объемы по точкам/типам
        // 4. Объем и цены товара по торговым точкам
        public DataTable GetProductPricesByPoints(
            string prodName,
            string tpType = "",
            string tradePoint = "")
        {
            var query = _storage.Sales

                .Join(
                    _storage.TradePoints,
                    s => s.TradePoint,
                    tp => tp.Name,
                    (s, tp) => new { s, tp })

                .Where(x =>
                    string.IsNullOrWhiteSpace(prodName) ||
                    x.s.Product == prodName)

                .Where(x =>
                    string.IsNullOrWhiteSpace(tpType) ||
                    x.tp.GetPointType() == tpType)

                .Where(x =>
                    string.IsNullOrWhiteSpace(tradePoint) ||
                    x.s.TradePoint == tradePoint)

                .GroupBy(x => new
                {
                    x.s.TradePoint,
                    Тип = x.tp.GetPointType(),
                    x.s.Product
                })

                .Select(g => new
                {
                    Точка = g.Key.TradePoint,

                    Тип = g.Key.Тип,

                    Товар = g.Key.Product,

                    Общий_Объем =
                        g.Sum(x => x.s.Quantity),

                    Мин_Цена =
                        g.Min(x => x.s.Price),

                    Макс_Цена =
                        g.Max(x => x.s.Price),

                    Средняя_Цена =
                        Math.Round(g.Average(x => x.s.Price), 2),

                    Выручка =
                        g.Sum(x => x.s.Price * x.s.Quantity),

                    Количество_Продаж =
                        g.Count()
                })

                .OrderByDescending(x => x.Выручка)

                .ToList();

            return ToDataTable(query);
        }

        // 5 & 8. Продуктивность и Зарплата (все/тип)
        public DataTable GetSellersProductivity(DateTime from, DateTime to, string tpType = "")
        {
            var res = _storage.Sales
                .Join(_storage.TradePoints, s => s.TradePoint, tp => tp.Name, (s, tp) => new { s, tp })
                .Join(_storage.Sellers, x => x.s.Seller, sel => sel.Name, (x, sel) => new { x.s, x.tp, sel })
                .Where(x => x.s.Date >= from && x.s.Date <= to)
                .Where(x => string.IsNullOrEmpty(tpType) || x.tp.GetPointType() == tpType)
                .GroupBy(x => new { x.s.Seller, x.sel.Salary, x.s.TradePoint })
                .Select(g => new {
                    Продавец = g.Key.Seller,
                    Точка = g.Key.TradePoint,
                    Зарплата = g.Key.Salary,
                    Выработка_Выручка = g.Sum(x => x.s.Price * x.s.Quantity),
                    Сделок = g.Count()
                });
            return ToDataTable(res);
        }

        // 6. Выработка конкретного продавца конкретной точки
        public DataTable GetSpecificSellerProductivity(
            string sellerName,
            string tradePoint,
            DateTime from,
            DateTime to)
        {
            var query = _storage.Sales

                .Where(s =>
                    s.Seller == sellerName &&
                    s.TradePoint == tradePoint &&
                    s.Date >= from &&
                    s.Date <= to)

                .GroupBy(s => new
                {
                    s.Seller,
                    s.TradePoint
                })

                .Select(g => new
                {
                    Продавец = g.Key.Seller,

                    Точка = g.Key.TradePoint,

                    Продано_Товаров =
                        g.Sum(x => x.Quantity),

                    Общая_Выручка =
                        g.Sum(x => x.Price * x.Quantity),

                    Количество_Сделок =
                        g.Count(),

                    Средний_Чек =
                        Math.Round(
                            g.Average(x => x.Price * x.Quantity), 2),

                    Первая_Продажа =
                        g.Min(x => x.Date),

                    Последняя_Продажа =
                        g.Max(x => x.Date)
                })

                .ToList();

            return ToDataTable(query);
        }

        // 15. Товарооборот по точкам/группам (типам)
        public DataTable GetTradeTurnover(DateTime from, DateTime to, string tpType = "")
        {
            var res = _storage.Sales
                .Join(_storage.TradePoints, s => s.TradePoint, tp => tp.Name, (s, tp) => new { s, tp })
                .Where(x => x.s.Date >= from && x.s.Date <= to)
                .Where(x => string.IsNullOrEmpty(tpType) || x.tp.GetPointType() == tpType)
                .GroupBy(x => new { x.s.TradePoint, Type = x.tp.GetPointType() })
                .Select(g => new {
                    Точка = g.Key.TradePoint,
                    Тип = g.Key.Type,
                    Оборот = g.Sum(x => x.s.Price * x.s.Quantity)
                });
            return ToDataTable(res);
        }

        // Метод для Заявок
        public DataTable GetRequestsView()
        {
            var query = from r in _storage.Requests
                        join tp in _storage.TradePoints on r.TradePointId equals tp.Id
                        join p in _storage.Products on r.ProductId equals p.Id
                        select new { ID = r.Id, Торговая_Точка = tp.Name, Товар = p.Name, Количество = r.Quantity, Дата = r.Date.ToShortDateString() };
            return ToDataTable(query);
        }

        // Метод для Заказов (Пункт 12 - инфо по номеру заказа)
        public DataTable GetOrdersView()
        {
            var query = from o in _storage.SupplierOrders
                        join s in _storage.Suppliers on o.SupplierId equals s.Id
                        join p in _storage.Products on o.ProductId equals p.Id
                        select new { ID = o.Id, Поставщик = s.Name, Товар = p.Name, Кол_во = o.Quantity, Цена = o.Price, Сумма = o.Quantity * o.Price, Дата = o.Date.ToShortDateString() };
            return ToDataTable(query);
        }

        public DataTable GetProductSalesReport(string productName,
        DateTime from,
        DateTime to,
        string tpType = "",
        string tradePoint = "")
        {
            var query = _storage.Sales
                .Join(_storage.TradePoints,
                    s => s.TradePoint,
                    tp => tp.Name,
                    (s, tp) => new { s, tp })

                .Where(x => x.s.Date >= from && x.s.Date <= to)

                .Where(x =>
                    string.IsNullOrEmpty(productName) ||
                    x.s.Product == productName)

                .Where(x =>
                    string.IsNullOrEmpty(tpType) ||
                    x.tp.GetPointType() == tpType)

                .Where(x =>
                    string.IsNullOrEmpty(tradePoint) ||
                    x.s.TradePoint == tradePoint)

                .GroupBy(x => new
                {
                    x.s.Product,
                    x.s.TradePoint,
                    Type = x.tp.GetPointType()
                })

                .Select(g => new
                {
                    Товар = g.Key.Product,
                    Точка = g.Key.TradePoint,
                    Тип = g.Key.Type,
                    Продано = g.Sum(x => x.s.Quantity),
                    Выручка = g.Sum(x => x.s.Price * x.s.Quantity)
                });

            return ToDataTable(query);
        }
    }
}