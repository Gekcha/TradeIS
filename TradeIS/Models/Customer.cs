using System.ComponentModel;

namespace TradeIS.Models
{
    public class Customer
    {
        [DisplayName("ID")]
        public int Id { get; set; }

        [DisplayName("Имя покупателя")]
        public string Name { get; set; }
    }
}