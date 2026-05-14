using System.ComponentModel;

namespace TradeIS.Models
{
    public class Hall
    {
        [DisplayName("ID")]
        public int Id { get; set; }

        [DisplayName("Точка")]
        public int TradePointId { get; set; }

        [DisplayName("Название")]
        public string Name { get; set; }

    }
}