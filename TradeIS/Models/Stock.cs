using System;
using System.Collections.Generic;
using System.Text;

namespace TradeIS.Models
{
    public class Stock
    {
        public int Id { get; set; }

        public int TradePointId { get; set; }

        public int ProductId { get; set; }

        public int Quantity { get; set; }

        public decimal Price { get; set; }
    }
}
