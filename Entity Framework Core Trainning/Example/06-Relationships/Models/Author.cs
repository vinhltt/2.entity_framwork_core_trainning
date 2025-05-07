using System;
using System.Collections.Generic;

namespace Relationships.Models
{
    public class Author
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string? Biography { get; set; }
        public DateTime DateOfBirth { get; set; }
        
        // Navigation properties
        public virtual ICollection<Book> Books { get; set; } = new List<Book>();
        public virtual AuthorContact? Contact { get; set; }
    }
} 