using System.ComponentModel;
using System;

public class Request
{
    [DisplayName("ID")]
    public int Id { get; set; }

    [DisplayName("Точка")]
    public int TradePointId { get; set; }

    [DisplayName("Товар")]
    public int ProductId { get; set; }

    [DisplayName("Количество")]
    public int Quantity { get; set; }

    [DisplayName("Дата")]
    public DateTime Date { get; set; }
}