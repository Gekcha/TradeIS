using System;
using System.ComponentModel;

public class Transfer
{
    [DisplayName("ID")]
    public int Id { get; set; }

    [DisplayName("Откуда")]
    public int FromTradePointId { get; set; }

    [DisplayName("Куда")]
    public int ToTradePointId { get; set; }

    [DisplayName("Товар")]
    public int ProductId { get; set; }

    [DisplayName("Количество")]
    public int Quantity { get; set; }

    [DisplayName("Дата")]
    public DateTime Date { get; set; }
}