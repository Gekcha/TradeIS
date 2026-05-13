using System;
using System.ComponentModel;

namespace TradeIS.Models
{
    public class Request
    {
        [DisplayName("ID")]
        public int Id { get; set; }

        [DisplayName("Торговая точка")]
        public int TradePointId { get; set; }

        [DisplayName("Товар")]
        public int ProductId { get; set; }

        [DisplayName("Количество")]
        public int Quantity { get; set; }

        [DisplayName("Дата заявки")]
        public DateTime Date { get; set; }
    }
}