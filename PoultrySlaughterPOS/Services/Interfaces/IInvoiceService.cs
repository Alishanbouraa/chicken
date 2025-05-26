using PoultrySlaughterPOS.Models.Entities;
using PoultrySlaughterPOS.Models.DTOs;

namespace PoultrySlaughterPOS.Services.Interfaces
{
    /// <summary>
    /// Business service interface for invoice management and sales processing
    /// Handles invoice creation, calculations, and customer balance management
    /// </summary>
    public interface IInvoiceService
    {
        Task<ServiceResult<Invoice>> CreateInvoiceAsync(CreateInvoiceDto dto);
        Task<ServiceResult<Invoice>> UpdateInvoiceAsync(int invoiceId, UpdateInvoiceDto dto);
        Task<ServiceResult<bool>> DeleteInvoiceAsync(int invoiceId);
        Task<ServiceResult<Invoice?>> GetInvoiceByIdAsync(int invoiceId);
        Task<ServiceResult<IEnumerable<Invoice>>> GetInvoicesByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<ServiceResult<IEnumerable<Invoice>>> GetInvoicesByCustomerAsync(int customerId, DateTime? startDate = null, DateTime? endDate = null);
        Task<ServiceResult<string>> GenerateInvoiceNumberAsync();
        Task<ServiceResult<InvoiceCalculationResult>> CalculateInvoiceAmountsAsync(InvoiceCalculationDto dto);
        Task<ServiceResult<bool>> ValidateInvoiceDataAsync(CreateInvoiceDto dto);
        Task<ServiceResult<SalesReportDto>> GenerateSalesReportAsync(DateTime startDate, DateTime endDate);
        Task<ServiceResult<bool>> MarkInvoiceAsPaidAsync(int invoiceId);
    }
}