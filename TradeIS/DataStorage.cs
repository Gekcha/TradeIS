using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TradeIS.Models;

namespace TradeIS
{
    public class DataStorage
    {
        // Списки данных
        public BindingList<TradePoint> TradePoints { get; set; } = new BindingList<TradePoint>();
        public BindingList<Product> Products { get; set; } = new BindingList<Product>();
        public BindingList<Supplier> Suppliers { get; set; } = new BindingList<Supplier>();
        public BindingList<Seller> Sellers { get; set; } = new BindingList<Seller>();
        public BindingList<Customer> Customers { get; set; } = new BindingList<Customer>();
        public BindingList<Sale> Sales { get; set; } = new BindingList<Sale>();
        public BindingList<Supply> Supplies { get; set; } = new BindingList<Supply>();
        public BindingList<Request> Requests { get; set; } = new BindingList<Request>();
        public BindingList<SupplierOrder> SupplierOrders { get; set; } = new BindingList<SupplierOrder>();

        // Объект со счетчиками
        public IdCounters Counters { get; set; } = new IdCounters();

        /// <summary>
        /// Метод для обновления счетчиков ID на основе существующих данных.
        /// Вызывается сразу после загрузки из файла.
        /// </summary>
        public void ActualizeCounters()
        {
            if (Counters == null) Counters = new IdCounters();

            Counters.TradePointId = GetNextId(TradePoints.Select(x => x.Id));
            Counters.ProductId = GetNextId(Products.Select(x => x.Id));
            Counters.SupplierId = GetNextId(Suppliers.Select(x => x.Id));
            Counters.SellerId = GetNextId(Sellers.Select(x => x.Id));
            Counters.CustomerId = GetNextId(Customers.Select(x => x.Id));
            Counters.SaleId = GetNextId(Sales.Select(x => x.Id));
            Counters.SupplyId = GetNextId(Supplies.Select(x => x.Id));
            Counters.RequestId = GetNextId(Requests.Select(x => x.Id));
            Counters.OrderId = GetNextId(SupplierOrders.Select(x => x.Id));
        }

        private int GetNextId(IEnumerable<int> ids)
        {
            // Если в списке есть элементы, берем Max + 1, иначе начинаем с 1
            return ids != null && ids.Any() ? ids.Max() + 1 : 1;
        }
    }

    public class IdCounters
    {
        public int TradePointId { get; set; } = 1;
        public int ProductId { get; set; } = 1;
        public int SupplierId { get; set; } = 1;
        public int SellerId { get; set; } = 1;
        public int CustomerId { get; set; } = 1;
        public int SaleId { get; set; } = 1;
        public int SupplyId { get; set; } = 1;
        public int RequestId { get; set; } = 1;
        public int OrderId { get; set; } = 1;
    }
}