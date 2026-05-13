using TradeIS.Models;

public class DepartmentStore : TradePoint
{
    public override string GetPointType() => "Универмаг";
    public override bool AllowsCustomers() => true;
}