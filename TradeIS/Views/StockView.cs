using System;
using System.Collections.Generic;
using System.Text;

namespace TradeIS.Views
{
    public class StockView
    {
        public int Id { get; set; }

        public string TradePoint { get; set; }
        public string Product { get; set; }

        public int Quantity { get; set; }
    }
}
