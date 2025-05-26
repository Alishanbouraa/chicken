using PoultrySlaughterPOS.Models.Entities;

namespace PoultrySlaughterPOS.Services.Interfaces
{
    /// <summary>
    /// Specialized repository interface for Customer entity operations
    /// Provides customer-specific queries and debt management functionality
    /// </summary>
    public interface ICustomerRepository : IGenericRepository<Customer>
    {
        Task<IEnumerable<Customer>> GetActiveCustomersAsync();
        Task<IEnumerable<Customer>> GetCustomersWithDebtAsync();
        Task<Customer?> GetCustomerWithTransactionsAsync(int customerId);
        Task<decimal> GetCustomerTotalDebtAsync(int customerId);
        Task<IEnumerable<Customer>> SearchCustomersAsync(string searchTerm);
        Task<Dictionary<int, decimal>> GetCustomerDebtSummaryAsync();
        Task UpdateCustomerDebtAsync(int customerId, decimal debtAmount);
    }
}