using Microsoft.EntityFrameworkCore;
using PoultrySlaughterPOS.Data.Context;
using PoultrySlaughterPOS.Models.Entities;
using PoultrySlaughterPOS.Services.Interfaces;

namespace PoultrySlaughterPOS.Services.Implementations
{
    /// <summary>
    /// Specialized repository implementation for Truck entity
    /// Optimized for truck management and load tracking operations
    /// </summary>
    public class TruckRepository : GenericRepository<Truck>, ITruckRepository
    {
        public TruckRepository(PoultryDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Truck>> GetActiveTrucksAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Where(t => t.IsActive)
                .OrderBy(t => t.TruckNumber)
                .ToListAsync();
        }

        public async Task<Truck?> GetTruckByNumberAsync(string truckNumber)
        {
            return await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TruckNumber == truckNumber);
        }

        public async Task<IEnumerable<Truck>> GetTrucksWithCurrentLoadsAsync(DateTime date)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(t => t.TruckLoads.Where(tl => tl.LoadDate.Date == date.Date))
                .Where(t => t.IsActive)
                .OrderBy(t => t.TruckNumber)
                .ToListAsync();
        }

        public async Task<bool> IsTruckNumberUniqueAsync(string truckNumber, int? excludeTruckId = null)
        {
            var query = _dbSet.Where(t => t.TruckNumber == truckNumber);

            if (excludeTruckId.HasValue)
            {
                query = query.Where(t => t.TruckId != excludeTruckId.Value);
            }

            return !await query.AnyAsync();
        }

        public async Task<Dictionary<int, decimal>> GetTruckLoadSummaryAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.TruckLoads
                .AsNoTracking()
                .Where(tl => tl.LoadDate.Date >= startDate.Date && tl.LoadDate.Date <= endDate.Date)
                .GroupBy(tl => tl.TruckId)
                .Select(g => new { TruckId = g.Key, TotalWeight = g.Sum(tl => tl.TotalWeight) })
                .ToDictionaryAsync(x => x.TruckId, x => x.TotalWeight);
        }
    }
}