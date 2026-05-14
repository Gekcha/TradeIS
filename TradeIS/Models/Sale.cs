using System.ComponentModel;
using System;

public class Sale
{
    [DisplayName("ID")]
    public int Id { get; set; }

    [DisplayName("Товар")]
    public int ProductId { get; set; }

    [DisplayName("Точка")]
    public int TradePointId { get; set; }

    [DisplayName("Продавец")]
    public int SellerId { get; set; }

    [DisplayName("Покупатель")]
    public int? CustomerId { get; set; }

    [DisplayName("Количество")]
    public int Quantity { get; set; }

    [DisplayName("Цена")]
    public double Price { get; set; }

    [DisplayName("Дата")]
    public DateTime Date { get; set; }
}