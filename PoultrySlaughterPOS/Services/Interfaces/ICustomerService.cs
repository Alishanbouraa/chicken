using PoultrySlaughterPOS.Models.Entities;
using PoultrySlaughterPOS.Models.DTOs;

namespace PoultrySlaughterPOS.Services.Interfaces
{
    /// <summary>
    /// Business service interface for customer management and debt tracking
    /// Handles customer operations, debt calculations, and payment processing
    /// </summary>
    public interface ICustomerService
    {
        Task<ServiceResult<Customer>> CreateCustomerAsync(CreateCustomerDto dto);
        Task<ServiceResult<Customer>> UpdateCustomerAsync(int customerId, UpdateCustomerDto dto);
        Task<ServiceResult<bool>> DeleteCustomerAsync(int customerId);
        Task<ServiceResult<Customer?>> GetCustomerByIdAsync(int customerId);
        Task<ServiceResult<IEnumerable<Customer>>> GetAllActiveCustomersAsync();
        Task<ServiceResult<IEnumerable<Customer>>> GetCustomersWithDebtAsync();
        Task<ServiceResult<IEnumerable<Customer>>> SearchCustomersAsync(string searchTerm);
        Task<ServiceResult<CustomerAccountSummaryDto>> GetCustomerAccountSummaryAsync(int customerId);
        Task<ServiceResult<bool>> ProcessPaymentAsync(int customerId, ProcessPaymentDto dto);
        Task<ServiceResult<decimal>> CalculateCustomerBalanceAsync(int customerId);
        Task<ServiceResult<bool>> ValidateCustomerDataAsync(CreateCustomerDto dto);
        Task<ServiceResult<DebtorsReportDto>> GenerateDebtorsReportAsync();
    }
}