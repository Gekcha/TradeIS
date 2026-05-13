using System.ComponentModel;
using System.Text.Json.Serialization;

namespace TradeIS.Models
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]

    [JsonDerivedType(typeof(DepartmentStore), "department")]
    [JsonDerivedType(typeof(Shop), "shop")]
    [JsonDerivedType(typeof(Kiosk), "kiosk")]
    [JsonDerivedType(typeof(Stall), "stall")]

    public abstract class TradePoint
    {
        [DisplayName("ID")]
        public int Id { get; set; }

        [DisplayName("Название")]
        public string Name { get; set; }

        [DisplayName("Размер")]
        public double Size { get; set; }

        [DisplayName("Аренда")]
        public double Rent { get; set; }

        [DisplayName("Коммунальные услуги")]
        public double Utilities { get; set; }

        [DisplayName("Количество прилавков")]
        public int Counters { get; set; }

        public abstract string GetPointType();

        public virtual bool AllowsCustomers() => false;
    }
}