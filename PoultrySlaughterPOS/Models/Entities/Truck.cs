using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PoultrySlaughterPOS.Models.Entities
{
    /// <summary>
    /// Represents a delivery truck in the poultry slaughter system
    /// </summary>
    [Table("Trucks")]
    public class Truck
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TruckId { get; set; }

        [Required]
        [StringLength(20)]
        public string TruckNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string DriverName { get; set; } = string.Empty;

        [StringLength(15)]
        public string? DriverPhone { get; set; }

        public bool IsActive { get; set; } = true;

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? LastModifiedDate { get; set; }

        // Navigation Properties
        public virtual ICollection<TruckLoad> TruckLoads { get; set; } = new List<TruckLoad>();
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }
}