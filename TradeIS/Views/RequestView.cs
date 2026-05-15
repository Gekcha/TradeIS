using System;
using System.Collections.Generic;
using System.Text;

namespace TradeIS.Views
{
    public class RequestView
    {
        public int Id { get; set; }

        public string TradePoint { get; set; }
        public string Product { get; set; }

        public int Quantity { get; set; }
        public DateTime Date { get; set; }
    }
}
