using Microsoft.EntityFrameworkCore;
using PoultrySlaughterPOS.Data.Context;
using PoultrySlaughterPOS.Models.Entities;
using PoultrySlaughterPOS.Services.Interfaces;

namespace PoultrySlaughterPOS.Services.Implementations
{
    /// <summary>
    /// Specialized repository implementation for Customer entity
    /// Optimized for customer management and debt tracking operations
    /// </summary>
    public class CustomerRepository : GenericRepository<Customer>, ICustomerRepository
    {
        public CustomerRepository(PoultryDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Customer>> GetActiveCustomersAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.CustomerName)
                .ToListAsync();
        }

        public async Task<IEnumerable<Customer>> GetCustomersWithDebtAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Where(c => c.IsActive && c.TotalDebt > 0)
                .OrderByDescending(c => c.TotalDebt)
                .ToListAsync();
        }

        public async Task<Customer?> GetCustomerWithTransactionsAsync(int customerId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(c => c.Invoices.OrderByDescending(i => i.InvoiceDate))
                .Include(c => c.Payments.OrderByDescending(p => p.PaymentDate))
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);
        }

        public async Task<decimal> GetCustomerTotalDebtAsync(int customerId)
        {
            var customer = await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            return customer?.TotalDebt ?? 0;
        }

        public async Task<IEnumerable<Customer>> SearchCustomersAsync(string searchTerm)
        {
            var normalizedSearchTerm = searchTerm.ToLower().Trim();

            return await _dbSet
                .AsNoTracking()
                .Where(c => c.IsActive &&
                           (c.CustomerName.ToLower().Contains(normalizedSearchTerm) ||
                            (c.PhoneNumber != null && c.PhoneNumber.Contains(searchTerm))))
                .OrderBy(c => c.CustomerName)
                .ToListAsync();
        }

        public async Task<Dictionary<int, decimal>> GetCustomerDebtSummaryAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Where(c => c.IsActive && c.TotalDebt > 0)
                .ToDictionaryAsync(c => c.CustomerId, c => c.TotalDebt);
        }

        public async Task UpdateCustomerDebtAsync(int customerId, decimal debtAmount)
        {
            var customer = await _dbSet.FindAsync(customerId);
            if (customer != null)
            {
                customer.TotalDebt = debtAmount;
                customer.LastModifiedDate = DateTime.Now;
                _dbSet.Update(customer);
            }
        }
    }
}