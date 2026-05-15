using System;
using System.Collections.Generic;
using System.Text;

namespace TradeIS.Views
{
    public class SalesView
    {
        public int Id { get; set; }

        public string Product { get; set; }
        public string TradePoint { get; set; }
        public string Seller { get; set; }
        public string Customer { get; set; }

        public int Quantity { get; set; }
        public double Price { get; set; }
        public DateTime Date { get; set; }
    }
}
