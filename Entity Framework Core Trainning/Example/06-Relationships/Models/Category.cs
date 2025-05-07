using System.Collections.Generic;

namespace Relationships.Models
{
    public class Category
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        
        // Navigation property
        public virtual ICollection<BookCategory> BookCategories { get; set; } = new List<BookCategory>();
    }
} 