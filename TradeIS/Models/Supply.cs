using System;
using System.ComponentModel;

namespace TradeIS.Models
{
    public class Supply
    {
        [DisplayName("ID")]
        public int Id { get; set; }

        [DisplayName("Поставщик")]
        public string Supplier { get; set; }

        [DisplayName("Товар")]
        public string Product { get; set; }

        [DisplayName("Количество")]
        public int Quantity { get; set; }

        [DisplayName("Цена")]
        public double Price { get; set; }

        [DisplayName("Дата")]
        public DateTime Date { get; set; }
    }
}