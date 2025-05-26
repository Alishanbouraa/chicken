using System.ComponentModel.DataAnnotations;

namespace PoultrySlaughterPOS.Models.DTOs
{
    /// <summary>
    /// Data transfer object for creating new customer entities
    /// Implements comprehensive validation for customer data integrity
    /// </summary>
    public class CreateCustomerDto
    {
        [Required(ErrorMessage = "Customer name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Customer name must be between 2 and 100 characters")]
        public string CustomerName { get; set; } = string.Empty;

        [StringLength(15, ErrorMessage = "Phone number cannot exceed 15 characters")]
        [RegularExpression(@"^[\d\s\-\+\(\)]+$", ErrorMessage = "Invalid phone number format")]
        public string? PhoneNumber { get; set; }

        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
        public string? Address { get; set; }

        [Range(0, 999999.99, ErrorMessage = "Credit limit must be between 0 and 999,999.99")]
        public decimal CreditLimit { get; set; } = 0;
    }

    /// <summary>
    /// Data transfer object for updating existing customer entities
    /// </summary>
    public class UpdateCustomerDto : CreateCustomerDto
    {
        [Required]
        public int CustomerId { get; set; }

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Data transfer object for processing customer payments
    /// Includes comprehensive validation for payment transactions
    /// </summary>
    public class ProcessPaymentDto
    {
        [Required(ErrorMessage = "Payment amount is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Payment amount must be between 0.01 and 999,999.99")]
        public decimal Amount { get; set; }

        public int? InvoiceId { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Payment method is required")]
        [StringLength(50, ErrorMessage = "Payment method cannot exceed 50 characters")]
        public string PaymentMethod { get; set; } = "Cash";

        [StringLength(100, ErrorMessage = "Reference number cannot exceed 100 characters")]
        public string? ReferenceNumber { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Comprehensive customer account summary for debt management
    /// </summary>
    public class CustomerAccountSummaryDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public decimal CurrentBalance { get; set; }
        public decimal CreditLimit { get; set; }
        public decimal AvailableCredit { get; set; }
        public DateTime LastTransactionDate { get; set; }
        public int TotalInvoicesCount { get; set; }
        public int UnpaidInvoicesCount { get; set; }
        public decimal TotalPurchases { get; set; }
        public decimal TotalPayments { get; set; }
        public List<InvoiceSummaryDto> RecentInvoices { get; set; } = new();
        public List<PaymentSummaryDto> RecentPayments { get; set; } = new();
    }

    /// <summary>
    /// Payment summary for customer account analysis
    /// </summary>
    public class PaymentSummaryDto
    {
        public int PaymentId { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? ReferenceNumber { get; set; }
        public string? Notes { get; set; }
        public int? InvoiceId { get; set; }
        public string? InvoiceNumber { get; set; }
    }

    /// <summary>
    /// Comprehensive debtors report for financial analysis
    /// </summary>
    public class DebtorsReportDto
    {
        public DateTime ReportDate { get; set; }
        public decimal TotalOutstandingDebt { get; set; }
        public int TotalDebtorsCount { get; set; }
        public decimal AverageDebtPerCustomer { get; set; }
        public List<CustomerDebtSummaryDto> CustomerDebts { get; set; } = new();
        public Dictionary<string, decimal> DebtAging { get; set; } = new();
        public List<CustomerAccountSummaryDto> TopDebtors { get; set; } = new();
    }

    /// <summary>
    /// Individual customer debt summary for reporting
    /// </summary>
    public class CustomerDebtSummaryDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalDebt { get; set; }
        public decimal CreditLimit { get; set; }
        public DateTime LastTransactionDate { get; set; }
        public int DaysSinceLastTransaction { get; set; }
        public int UnpaidInvoicesCount { get; set; }
        public decimal OldestUnpaidAmount { get; set; }
        public DateTime? OldestUnpaidDate { get; set; }
    }
}