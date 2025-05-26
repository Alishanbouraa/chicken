using PoultrySlaughterPOS.Models.Entities;

namespace PoultrySlaughterPOS.Services.Interfaces
{
    /// <summary>
    /// Specialized repository interface for Truck entity operations
    /// Provides truck-specific business logic and optimized queries
    /// </summary>
    public interface ITruckRepository : IGenericRepository<Truck>
    {
        Task<IEnumerable<Truck>> GetActiveTrucksAsync();
        Task<Truck?> GetTruckByNumberAsync(string truckNumber);
        Task<IEnumerable<Truck>> GetTrucksWithCurrentLoadsAsync(DateTime date);
        Task<bool> IsTruckNumberUniqueAsync(string truckNumber, int? excludeTruckId = null);
        Task<Dictionary<int, decimal>> GetTruckLoadSummaryAsync(DateTime startDate, DateTime endDate);
    }
}