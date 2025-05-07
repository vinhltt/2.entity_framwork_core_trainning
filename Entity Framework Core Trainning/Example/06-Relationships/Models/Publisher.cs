using System.Collections.Generic;

namespace Relationships.Models
{
    public class Publisher
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        
        // Navigation property
        public virtual ICollection<Book> Books { get; set; } = new List<Book>();
    }
} 