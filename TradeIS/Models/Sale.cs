using System;
using System.ComponentModel;

namespace TradeIS.Models
{
    public class Sale
    {
        [DisplayName("ID")]
        public int Id { get; set; }

        [DisplayName("Товар")]
        public string Product { get; set; }

        [DisplayName("Торговая точка")]
        public string TradePoint { get; set; }

        [DisplayName("Продавец")]
        public string Seller { get; set; }

        [DisplayName("Покупатель")]
        public string Customer { get; set; }

        [DisplayName("Количество")]
        public int Quantity { get; set; }

        [DisplayName("Цена")]
        public double Price { get; set; }

        [DisplayName("Дата продажи")]
        public DateTime Date { get; set; }
    }
}