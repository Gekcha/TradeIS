using TradeIS.Models;

public class Shop : TradePoint
{
    public List<Hall> Halls { get; set; } = new();
    public override string Type => "Shop";
    public override string GetPointType() => "Магазин";
}