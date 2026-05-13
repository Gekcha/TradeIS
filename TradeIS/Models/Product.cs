using System.ComponentModel;

namespace TradeIS.Models
{
    public class Product
    {
        [DisplayName("ID")]
        public int Id { get; set; }

        [DisplayName("Название товара")]
        public string Name { get; set; }
    }
}