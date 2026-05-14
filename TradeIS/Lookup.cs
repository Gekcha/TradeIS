using TradeIS;

public static class Lookup
{
    public static string ProductName(int id) =>
        Program.Store.Products.FirstOrDefault(x => x.Id == id)?.Name ?? "";

    public static string SupplierName(int id) =>
        Program.Store.Suppliers.FirstOrDefault(x => x.Id == id)?.Name ?? "";

    public static string SellerName(int id) =>
        Program.Store.Sellers.FirstOrDefault(x => x.Id == id)?.Name ?? "";

    public static string CustomerName(int? id) =>
        id == null ? "" :
        Program.Store.Customers.FirstOrDefault(x => x.Id == id)?.Name ?? "";

    public static string TradePointName(int id) =>
        Program.Store.TradePoints.FirstOrDefault(x => x.Id == id)?.Name ?? "";
}