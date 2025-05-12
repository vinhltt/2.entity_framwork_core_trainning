namespace LessonDemo06.Models
{
    public class Book
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public DateTime PublishedDate { get; set; }
        
        // Foreign keys
        public int AuthorId { get; set; }
        public int PublisherId { get; set; }
        
        // Navigation properties
        public virtual Author? Author { get; set; }
        public virtual Publisher? Publisher { get; set; }
        public virtual ICollection<BookCategory> BookCategories { get; set; } = new List<BookCategory>();
    }
} 