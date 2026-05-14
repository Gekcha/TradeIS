using System.ComponentModel;
using TradeIS.Models;

public class DataStorage
{
    public BindingList<TradePoint> TradePoints { get; set; } = new();
    public BindingList<Product> Products { get; set; } = new();
    public BindingList<Supplier> Suppliers { get; set; } = new();
    public BindingList<Seller> Sellers { get; set; } = new();
    public BindingList<Customer> Customers { get; set; } = new();

    public BindingList<Sale> Sales { get; set; } = new();
    public BindingList<Supply> Supplies { get; set; } = new();
    public BindingList<Request> Requests { get; set; } = new();
    public BindingList<SupplierOrder> SupplierOrders { get; set; } = new();

    public BindingList<Stock> Stocks { get; set; } = new();
    public BindingList<Transfer> Transfers { get; set; } = new();

    public IdCounters Counters { get; set; } = new();

    public void ActualizeCounters()
    {
        Counters.ProductId = GetMax(Products, x => x.Id);
        Counters.SupplierId = GetMax(Suppliers, x => x.Id);
        Counters.SellerId = GetMax(Sellers, x => x.Id);
        Counters.CustomerId = GetMax(Customers, x => x.Id);

        Counters.SaleId = GetMax(Sales, x => x.Id);
        Counters.SupplyId = GetMax(Supplies, x => x.Id);
        Counters.RequestId = GetMax(Requests, x => x.Id);
        Counters.SupplierOrderId = GetMax(SupplierOrders, x => x.Id);

        Counters.TradePointId = GetMax(TradePoints, x => x.Id);
        Counters.StockId = GetMax(Stocks, x => x.Id);
        Counters.TransferId = GetMax(Transfers, x => x.Id);
    }

    private int GetMax<T>(IEnumerable<T> list, Func<T, int> selector)
    {
        return list.Any() ? list.Max(selector) + 1 : 1;
    }
}