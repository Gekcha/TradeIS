using System.ComponentModel;

public class Seller
{
    [DisplayName("ID")]
    public int Id { get; set; }

    [DisplayName("Имя")]
    public string Name { get; set; }

    [DisplayName("Торговая точка")]
    public int TradePointId { get; set; }

    [DisplayName("Зарплата")]
    public double Salary { get; set; }
}