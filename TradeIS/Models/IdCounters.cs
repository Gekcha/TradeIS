using System;
using System.Collections.Generic;
using System.Text;

namespace TradeIS.Models
{
    public class IdCounters
    {
        public int ProductId { get; set; }
        public int SupplierId { get; set; }
        public int SellerId { get; set; }
        public int CustomerId { get; set; }

        public int SaleId { get; set; }
        public int SupplyId { get; set; }
        public int RequestId { get; set; }

        public int SupplierOrderId { get; set; }
        public int OrderId { get; set; }

        public int TradePointId { get; set; }
        public int StockId { get; set; }
        public int TransferId { get; set; }
    }
}
