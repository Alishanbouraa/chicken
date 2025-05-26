using PoultrySlaughterPOS.Models.Entities;

namespace PoultrySlaughterPOS.Services.Interfaces
{
    /// <summary>
    /// Unit of Work pattern interface for coordinating repository operations
    /// Ensures transaction consistency across multiple entity operations
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        ITruckRepository Trucks { get; }
        IGenericRepository<TruckLoad> TruckLoads { get; }
        ICustomerRepository Customers { get; }
        IInvoiceRepository Invoices { get; }
        IGenericRepository<Payment> Payments { get; }

        Task<int> SaveChangesAsync();
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}