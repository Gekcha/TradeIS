using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace TradeIS.Models
{
    public class SupplierOrder
    {
        [DisplayName("ID")]
        public int Id { get; set; }

        [DisplayName("Поставщик")]
        public int SupplierId { get; set; }

        [DisplayName("Товар")]
        public int ProductId { get; set; }

        [DisplayName("Количество")]
        public int Quantity { get; set; }

        [DisplayName("Цена")]
        public double Price { get; set; }

        [DisplayName("Дата")]
        public DateTime Date { get; set; }
    }
}
