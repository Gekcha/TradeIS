using System;
using System.Collections.Generic;
using System.Text;

namespace TradeIS.Models
{
    public class Transfer
    {
        public int Id { get; set; }

        public int FromTradePointId { get; set; }

        public int ToTradePointId { get; set; }

        public int ProductId { get; set; }

        public int Quantity { get; set; }

        public DateTime Date { get; set; }
    }
}
