using System.ComponentModel;

public abstract class TradePoint
{
    [DisplayName("ID")]
    public int Id { get; set; }

    [DisplayName("Название")]
    public string Name { get; set; }

    [DisplayName("Площадь")]
    public double Size { get; set; }

    [DisplayName("Аренда")]
    public double Rent { get; set; }

    [DisplayName("Коммунальные услуги")]
    public double Utilities { get; set; }

    [DisplayName("Прилавки")]
    public int Counters { get; set; }
    public abstract string Type { get; }


    public abstract string GetPointType();
}