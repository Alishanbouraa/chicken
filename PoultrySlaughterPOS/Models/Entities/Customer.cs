using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PoultrySlaughterPOS.Models.Entities
{
    /// <summary>
    /// Represents a customer in the poultry slaughter system
    /// </summary>
    [Table("Customers")]
    public class Customer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CustomerId { get; set; }

        [Required]
        [StringLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [StringLength(15)]
        public string? PhoneNumber { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal TotalDebt { get; set; } = 0;

        [Column(TypeName = "decimal(12,2)")]
        public decimal CreditLimit { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? LastModifiedDate { get; set; }

        // Navigation Properties
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}