using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdditionalFeatures.Models
{
    public class Employee
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public required string Name { get; set; }

        [Required]
        [StringLength(100)]
        public required string Position { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Salary { get; set; }

        public DateTime HireDate { get; set; }

        public bool IsActive { get; set; }

        // Temporal table columns (managed by SQL Server)
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
    }
} 