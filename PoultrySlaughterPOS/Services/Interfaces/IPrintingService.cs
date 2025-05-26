using PoultrySlaughterPOS.Models.DTOs;
using PoultrySlaughterPOS.Models.Entities;

namespace PoultrySlaughterPOS.Services.Interfaces
{
    /// <summary>
    /// Advanced printing service interface for document generation and printing operations
    /// Provides comprehensive invoice printing and report generation capabilities
    /// </summary>
    public interface IPrintingService
    {
        Task<ServiceResult<bool>> PrintInvoiceAsync(Invoice invoice);
        Task<ServiceResult<bool>> PrintReceiptAsync(Payment payment);
        Task<ServiceResult<bool>> PrintReportAsync(DailySummaryReportDto report);
        Task<ServiceResult<bool>> PrintCustomerStatementAsync(CustomerAccountSummaryDto statement);
        Task<ServiceResult<string>> GenerateInvoicePdfAsync(Invoice invoice);
        Task<ServiceResult<bool>> ConfigurePrinterAsync(string printerName);
        Task<ServiceResult<List<string>>> GetAvailablePrintersAsync();
    }
}