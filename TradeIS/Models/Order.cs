using System;
using System.Collections.Generic;
using System.Text;

namespace TradeIS.Models
{
    public class Order
    {
        public int Id { get; set; }

        public int SupplierId { get; set; }

        public DateTime Date { get; set; }
    }
}
