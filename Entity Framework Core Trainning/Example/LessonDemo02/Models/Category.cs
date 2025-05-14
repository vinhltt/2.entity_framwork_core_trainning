namespace LessonDemo02.Models
{
    public class Category
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
} 