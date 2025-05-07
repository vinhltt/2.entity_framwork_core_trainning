namespace Relationships.Models
{
    public class BookCategory
    {
        public int BookId { get; set; }
        public int CategoryId { get; set; }
        
        // Navigation properties
        public virtual Book? Book { get; set; }
        public virtual Category? Category { get; set; }
    }
} 