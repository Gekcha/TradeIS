using System;
using System.Collections.Generic;
using System.Text;

namespace TradeIS.Views
{
    public class SupplierOrderView
    {
        public int Id { get; set; }

        public string Supplier { get; set; }
        public string Product { get; set; }

        public int Quantity { get; set; }
        public double Price { get; set; }
        public DateTime Date { get; set; }
    }
}
