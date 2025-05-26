using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PoultrySlaughterPOS.Models.Entities
{
    /// <summary>
    /// Represents the initial loading data for a truck before distribution
    /// </summary>
    [Table("TruckLoads")]
    public class TruckLoad
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LoadId { get; set; }

        [Required]
        public int TruckId { get; set; }

        [Required]
        public DateTime LoadDate { get; set; } = DateTime.Now;

        [Required]
        [Column(TypeName = "decimal(10,3)")]
        public decimal TotalWeight { get; set; }

        [Required]
        public int CagesCount { get; set; }

        [Column(TypeName = "decimal(10,3)")]
        public decimal CagesWeight { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Loaded"; // Loaded, InTransit, Completed

        public bool IsCompleted { get; set; } = false;

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("TruckId")]
        public virtual Truck Truck { get; set; } = null!;
    }
}