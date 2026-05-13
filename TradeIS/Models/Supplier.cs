using System.ComponentModel;

namespace TradeIS.Models
{
    public class Supplier
    {
        [DisplayName("ID")]
        public int Id { get; set; }

        [DisplayName("Название поставщика")]
        public string Name { get; set; }
    }
}