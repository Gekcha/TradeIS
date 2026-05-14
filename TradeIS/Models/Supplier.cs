using System.ComponentModel;

public class Supplier
{
    [DisplayName("ID")]
    public int Id { get; set; }

    [DisplayName("Название")]
    public string Name { get; set; }
}