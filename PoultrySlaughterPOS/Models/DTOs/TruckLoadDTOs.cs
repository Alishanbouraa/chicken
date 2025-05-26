using System.ComponentModel.DataAnnotations;

namespace PoultrySlaughterPOS.Models.DTOs
{
    /// <summary>
    /// Data transfer object for creating new truck load entries
    /// Includes comprehensive validation attributes for data integrity
    /// </summary>
    public class CreateTruckLoadDto
    {
        [Required(ErrorMessage = "Truck selection is required")]
        public int TruckId { get; set; }

        [Required(ErrorMessage = "Load date is required")]
        public DateTime LoadDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Total weight is required")]
        [Range(0.001, 9999.999, ErrorMessage = "Total weight must be between 0.001 and 9999.999 kg")]
        public decimal TotalWeight { get; set; }

        [Required(ErrorMessage = "Cages count is required")]
        [Range(1, 999, ErrorMessage = "Cages count must be between 1 and 999")]
        public int CagesCount { get; set; }

        [Range(0, 999.999, ErrorMessage = "Cages weight must be between 0 and 999.999 kg")]
        public decimal CagesWeight { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Data transfer object for updating existing truck load entries
    /// </summary>
    public class UpdateTruckLoadDto : CreateTruckLoadDto
    {
        [Required]
        public int LoadId { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Loaded";
    }

    /// <summary>
    /// Comprehensive weight comparison report for truck load analysis
    /// </summary>
    public class WeightComparisonReportDto
    {
        public int TruckId { get; set; }
        public string TruckNumber { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public DateTime ReportDate { get; set; }
        public decimal InitialLoadWeight { get; set; }
        public decimal TotalSalesWeight { get; set; }
        public decimal WeightDifference { get; set; }
        public decimal LossPercentage { get; set; }
        public int TotalInvoicesCount { get; set; }
        public decimal TotalSalesAmount { get; set; }
        public List<InvoiceSummaryDto> InvoiceDetails { get; set; } = new();
    }

    /// <summary>
    /// Daily summary report aggregating all truck operations
    /// </summary>
    public class DailySummaryReportDto
    {
        public DateTime ReportDate { get; set; }
        public List<WeightComparisonReportDto> TruckReports { get; set; } = new();
        public decimal TotalInitialWeight { get; set; }
        public decimal TotalSalesWeight { get; set; }
        public decimal TotalWeightLoss { get; set; }
        public decimal AverageLossPercentage { get; set; }
        public decimal TotalSalesAmount { get; set; }
        public int TotalInvoicesCount { get; set; }
    }
}