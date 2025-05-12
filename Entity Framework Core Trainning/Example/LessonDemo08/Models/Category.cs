using System.ComponentModel.DataAnnotations;

namespace LessonDemo08.Models
{
    public class Category : IAuditableEntity
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public required string Name { get; set; }

        public string? Description { get; set; }

        public bool IsDeleted { get; set; }

        public virtual ICollection<Product> Products { get; set; } = new List<Product>();

        // Audit fields
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Concurrency token
        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
} 