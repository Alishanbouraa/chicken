using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PoultrySlaughterPOS.Models.Entities
{
    /// <summary>
    /// Represents a payment transaction in the poultry slaughter system
    /// </summary>
    [Table("Payments")]
    public class Payment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PaymentId { get; set; }

        [Required]
        public int CustomerId { get; set; }

        public int? InvoiceId { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; } = DateTime.Now;

        [Required]
        [Column(TypeName = "decimal(12,2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "Cash"; // Cash, Check, Transfer

        [StringLength(100)]
        public string? ReferenceNumber { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("CustomerId")]
        public virtual Customer Customer { get; set; } = null!;

        [ForeignKey("InvoiceId")]
        public virtual Invoice? Invoice { get; set; }
    }
}