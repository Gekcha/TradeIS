using TradeIS.Models;

public class Shop : TradePoint
{
    public override string GetPointType() => "Магазин";
    public override bool AllowsCustomers() => true;
}