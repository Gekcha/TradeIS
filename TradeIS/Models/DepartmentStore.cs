using TradeIS.Models;

public class DepartmentStore : TradePoint
{
    public List<Section> Sections { get; set; } = new();
    public List<Hall> Halls { get; set; } = new();

    public override int HallsCount => Halls.Count;
    public override string Type => "DepartmentStore";
    public override string GetPointType() => "Универмаг";
}