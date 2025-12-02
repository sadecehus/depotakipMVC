using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
 
namespace pm.Models
{
    public class Product
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string ProductCode { get; set; }
        
        public int SectionId { get; set; }
        public int ShelfId { get; set; }
        
        public int Stock { get; set; }
        public int MinimumStock { get; set; }
        
        public decimal? Price { get; set; }
        public string? Description { get; set; }
        
        public Section? Section { get; set; }
        public Shelf? Shelf { get; set; }
    }
}