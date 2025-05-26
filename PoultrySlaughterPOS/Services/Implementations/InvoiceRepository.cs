using Microsoft.EntityFrameworkCore;
using PoultrySlaughterPOS.Data.Context;
using PoultrySlaughterPOS.Models.Entities;
using PoultrySlaughterPOS.Services.Interfaces;

namespace PoultrySlaughterPOS.Services.Implementations
{
    /// <summary>
    /// Specialized repository implementation for Invoice entity
    /// Optimized for invoice management and sales reporting operations
    /// </summary>
    public class InvoiceRepository : GenericRepository<Invoice>, IInvoiceRepository
    {
        public InvoiceRepository(PoultryDbContext context) : base(context)
        {
        }

        public async Task<string> GenerateInvoiceNumberAsync()
        {
            var today = DateTime.Now;
            var prefix = today.ToString("yyMMdd");

            var lastInvoiceToday = await _dbSet
                .AsNoTracking()
                .Where(i => i.InvoiceNumber.StartsWith(prefix))
                .OrderByDescending(i => i.InvoiceNumber)
                .FirstOrDefaultAsync();

            if (lastInvoiceToday == null)
            {
                return $"{prefix}001";
            }

            var lastSequence = lastInvoiceToday.InvoiceNumber.Substring(6);
            if (int.TryParse(lastSequence, out int sequence))
            {
                return $"{prefix}{(sequence + 1):D3}";
            }

            return $"{prefix}001";
        }

        public async Task<IEnumerable<Invoice>> GetInvoicesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(i => i.Customer)
                .Include(i => i.Truck)
                .Where(i => i.InvoiceDate.Date >= startDate.Date && i.InvoiceDate.Date <= endDate.Date)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Invoice>> GetInvoicesByCustomerAsync(int customerId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _dbSet
                .AsNoTracking()
                .Include(i => i.Truck)
                .Where(i => i.CustomerId == customerId);

            if (startDate.HasValue)
            {
                query = query.Where(i => i.InvoiceDate.Date >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                query = query.Where(i => i.InvoiceDate.Date <= endDate.Value.Date);
            }

            return await query
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Invoice>> GetInvoicesByTruckAsync(int truckId, DateTime date)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(i => i.Customer)
                .Where(i => i.TruckId == truckId && i.InvoiceDate.Date == date.Date)
                .OrderBy(i => i.InvoiceDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Invoice>> GetUnpaidInvoicesAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Include(i => i.Customer)
                .Include(i => i.Truck)
                .Where(i => !i.IsPaid)
                .OrderBy(i => i.InvoiceDate)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalSalesAsync(DateTime date)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(i => i.InvoiceDate.Date == date.Date)
                .SumAsync(i => i.FinalAmount);
        }

        public async Task<decimal> GetTotalSalesAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(i => i.InvoiceDate.Date >= startDate.Date && i.InvoiceDate.Date <= endDate.Date)
                .SumAsync(i => i.FinalAmount);
        }

        public async Task<Dictionary<int, decimal>> GetSalesByTruckAsync(DateTime date)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(i => i.InvoiceDate.Date == date.Date)
                .GroupBy(i => i.TruckId)
                .Select(g => new { TruckId = g.Key, TotalSales = g.Sum(i => i.FinalAmount) })
                .ToDictionaryAsync(x => x.TruckId, x => x.TotalSales);
        }

        public async Task<Invoice?> GetInvoiceWithDetailsAsync(int invoiceId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(i => i.Customer)
                .Include(i => i.Truck)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);
        }
    }
}