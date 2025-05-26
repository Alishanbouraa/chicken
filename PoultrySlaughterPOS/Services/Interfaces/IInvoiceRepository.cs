using PoultrySlaughterPOS.Models.Entities;

namespace PoultrySlaughterPOS.Services.Interfaces
{
    /// <summary>
    /// Specialized repository interface for Invoice entity operations
    /// Provides invoice-specific queries and reporting functionality
    /// </summary>
    public interface IInvoiceRepository : IGenericRepository<Invoice>
    {
        Task<string> GenerateInvoiceNumberAsync();
        Task<IEnumerable<Invoice>> GetInvoicesByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<Invoice>> GetInvoicesByCustomerAsync(int customerId, DateTime? startDate = null, DateTime? endDate = null);
        Task<IEnumerable<Invoice>> GetInvoicesByTruckAsync(int truckId, DateTime date);
        Task<IEnumerable<Invoice>> GetUnpaidInvoicesAsync();
        Task<decimal> GetTotalSalesAsync(DateTime date);
        Task<decimal> GetTotalSalesAsync(DateTime startDate, DateTime endDate);
        Task<Dictionary<int, decimal>> GetSalesByTruckAsync(DateTime date);
        Task<Invoice?> GetInvoiceWithDetailsAsync(int invoiceId);
    }
}