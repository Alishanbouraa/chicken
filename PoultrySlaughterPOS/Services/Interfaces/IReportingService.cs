using PoultrySlaughterPOS.Models.DTOs;

namespace PoultrySlaughterPOS.Services.Interfaces
{
    /// <summary>
    /// Advanced reporting service interface for business intelligence and data export operations
    /// Provides comprehensive report generation and data visualization capabilities
    /// </summary>
    public interface IReportingService
    {
        Task<ServiceResult<string>> ExportSalesReportToExcelAsync(SalesReportDto report, string filePath);
        Task<ServiceResult<string>> ExportCustomerListToExcelAsync(List<CustomerAccountSummaryDto> customers, string filePath);
        Task<ServiceResult<string>> ExportDailySummaryToPdfAsync(DailySummaryReportDto report, string filePath);
        Task<ServiceResult<byte[]>> GenerateInvoiceTemplateAsync(InvoiceSummaryDto invoice);
        Task<ServiceResult<bool>> EmailReportAsync(string reportPath, string recipientEmail);
    }
}