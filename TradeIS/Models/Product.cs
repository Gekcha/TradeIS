using System.ComponentModel;

public class Product
{
    [DisplayName("ID")]
    public int Id { get; set; }

    [DisplayName("Наименование")]
    public string Name { get; set; }

    [DisplayName("Категория")]
    public string Category { get; set; }

    [DisplayName("Единица измерения")]
    public string Unit { get; set; }
}