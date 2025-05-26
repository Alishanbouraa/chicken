using Microsoft.EntityFrameworkCore.Storage;
using PoultrySlaughterPOS.Data.Context;
using PoultrySlaughterPOS.Models.Entities;
using PoultrySlaughterPOS.Services.Interfaces;

namespace PoultrySlaughterPOS.Services.Implementations
{
    /// <summary>
    /// Unit of Work pattern implementation for coordinating repository operations
    /// Provides transaction management and ensures data consistency
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly PoultryDbContext _context;
        private IDbContextTransaction? _transaction;
        private bool _disposed = false;

        // Repository instances
        private ITruckRepository? _trucks;
        private IGenericRepository<TruckLoad>? _truckLoads;
        private ICustomerRepository? _customers;
        private IInvoiceRepository? _invoices;
        private IGenericRepository<Payment>? _payments;

        public UnitOfWork(PoultryDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public ITruckRepository Trucks
        {
            get { return _trucks ??= new TruckRepository(_context); }
        }

        public IGenericRepository<TruckLoad> TruckLoads
        {
            get { return _truckLoads ??= new GenericRepository<TruckLoad>(_context); }
        }

        public ICustomerRepository Customers
        {
            get { return _customers ??= new CustomerRepository(_context); }
        }

        public IInvoiceRepository Invoices
        {
            get { return _invoices ??= new InvoiceRepository(_context); }
        }

        public IGenericRepository<Payment> Payments
        {
            get { return _payments ??= new GenericRepository<Payment>(_context); }
        }

        public async Task<int> SaveChangesAsync()
        {
            try
            {
                return await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                if (_transaction != null)
                {
                    await RollbackTransactionAsync();
                }
                throw;
            }
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception)
            {
                if (_transaction != null)
                {
                    await RollbackTransactionAsync();
                }
                throw;
            }
        }

        public async Task BeginTransactionAsync()
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException("Transaction already started");
            }

            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No transaction started");
            }

            try
            {
                await _transaction.CommitAsync();
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No transaction started");
            }

            try
            {
                await _transaction.RollbackAsync();
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _transaction?.Dispose();
                _context.Dispose();
            }
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}