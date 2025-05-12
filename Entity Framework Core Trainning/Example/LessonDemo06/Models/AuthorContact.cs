namespace LessonDemo06.Models
{
    public class AuthorContact
    {
        public int Id { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        
        // Foreign key
        public int AuthorId { get; set; }
        // Navigation property
        public virtual Author? Author { get; set; }
    }
} 