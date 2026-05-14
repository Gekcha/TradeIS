using System.ComponentModel;

namespace TradeIS.Models
{
    public class Section
    {
        [DisplayName("ID")]
        public int Id { get; set; }

        [DisplayName("Зал")]
        public int HallId { get; set; }

        [DisplayName("Название")]
        public string Name { get; set; }

        [DisplayName("Управляющий")]
        public string ManagerName { get; set; }
    }
}