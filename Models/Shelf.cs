namespace pm.Models;

public class Shelf
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int SectionId { get; set; }
    
    public Section? Section { get; set; }
}
