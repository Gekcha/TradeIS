using System.ComponentModel;

namespace TradeIS.Models
{
    public class Seller
    {
        [DisplayName("ID")]
        public int Id { get; set; }

        [DisplayName("Имя продавца")]
        public string Name { get; set; }

        [DisplayName("Торговая точка")]
        public string TradePoint { get; set; }

        [DisplayName("Зарплата")]
        public double Salary { get; set; }
    }
}