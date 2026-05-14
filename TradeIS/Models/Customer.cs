using System.ComponentModel;

public class Customer
{
    [DisplayName("ID")]
    public int Id { get; set; }

    [DisplayName("Имя")]
    public string Name { get; set; }
}