using TradeIS.Models;

public class DepartmentStore : TradePoint
{
    public List<Section> Sections { get; set; } = new();
    public override string Type => "DepartmentStore";
    public override string GetPointType() => "Универмаг";
}