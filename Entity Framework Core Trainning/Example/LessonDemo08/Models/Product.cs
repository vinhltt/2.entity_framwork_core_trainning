using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LessonDemo08.Models
{
    public class Product : IAuditableEntity
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public required string Name { get; set; }

        public string? Description { get; set; }

        [Range(0.01, 10000.00)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public bool IsAvailable { get; set; }

        public bool IsDeleted { get; set; }

        public int CategoryId { get; set; }
        public virtual Category? Category { get; set; }

        // Audit fields
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Concurrency token
        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
} 