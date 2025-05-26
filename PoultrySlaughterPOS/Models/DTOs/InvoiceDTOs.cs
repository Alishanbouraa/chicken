using System.ComponentModel.DataAnnotations;

namespace PoultrySlaughterPOS.Models.DTOs
{
    /// <summary>
    /// Data transfer object for creating new invoice entries
    /// Implements comprehensive validation for sales transaction data
    /// </summary>
    public class CreateInvoiceDto
    {
        [Required(ErrorMessage = "Customer selection is required")]
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "Truck selection is required")]
        public int TruckId { get; set; }

        public DateTime InvoiceDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Gross weight is required")]
        [Range(0.001, 9999.999, ErrorMessage = "Gross weight must be between 0.001 and 9999.999 kg")]
        public decimal GrossWeight { get; set; }

        [Required(ErrorMessage = "Cages weight is required")]
        [Range(0, 999.999, ErrorMessage = "Cages weight must be between 0 and 999.999 kg")]
        public decimal CagesWeight { get; set; }

        [Required(ErrorMessage = "Cages count is required")]
        [Range(1, 999, ErrorMessage = "Cages count must be between 1 and 999")]
        public int CagesCount { get; set; }

        [Required(ErrorMessage = "Unit price is required")]
        [Range(0.01, 99999.99, ErrorMessage = "Unit price must be between 0.01 and 99999.99")]
        public decimal UnitPrice { get; set; }

        [Range(0, 100, ErrorMessage = "Discount percentage must be between 0 and 100")]
        public decimal DiscountPercentage { get; set; } = 0;

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Data transfer object for updating existing invoice entries
    /// </summary>
    public class UpdateInvoiceDto : CreateInvoiceDto
    {
        [Required]
        public int InvoiceId { get; set; }

        [Required]
        public string InvoiceNumber { get; set; } = string.Empty;
    }

    /// <summary>
    /// Data transfer object for invoice calculation operations
    /// </summary>
    public class InvoiceCalculationDto
    {
        public decimal GrossWeight { get; set; }
        public decimal CagesWeight { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal PreviousBalance { get; set; }
    }

    /// <summary>
    /// Result object containing calculated invoice amounts
    /// </summary>
    public class InvoiceCalculationResult
    {
        public decimal NetWeight { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public decimal CurrentBalance { get; set; }
    }

    /// <summary>
    /// Invoice summary for reporting purposes
    /// </summary>
    public class InvoiceSummaryDto
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public decimal NetWeight { get; set; }
        public decimal FinalAmount { get; set; }
        public bool IsPaid { get; set; }
    }

    /// <summary>
    /// Comprehensive sales report data transfer object
    /// </summary>
    public class SalesReportDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalSalesAmount { get; set; }
        public decimal TotalNetWeight { get; set; }
        public int TotalInvoicesCount { get; set; }
        public decimal AverageUnitPrice { get; set; }
        public Dictionary<int, CustomerSalesDto> CustomerSales { get; set; } = new();
        public Dictionary<int, TruckSalesDto> TruckSales { get; set; } = new();
        public List<InvoiceSummaryDto> TopInvoices { get; set; } = new();
    }

    /// <summary>
    /// Customer-specific sales summary
    /// </summary>
    public class CustomerSalesDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal TotalWeight { get; set; }
        public int InvoicesCount { get; set; }
    }

    /// <summary>
    /// Truck-specific sales summary
    /// </summary>
    public class TruckSalesDto
    {
        public int TruckId { get; set; }
        public string TruckNumber { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal TotalWeight { get; set; }
        public int InvoicesCount { get; set; }
    }
}