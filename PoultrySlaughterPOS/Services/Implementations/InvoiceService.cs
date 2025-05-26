using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Models.DTOs;
using PoultrySlaughterPOS.Models.Entities;
using PoultrySlaughterPOS.Services.Interfaces;
using PoultrySlaughterPOS.Utils.Extensions;

namespace PoultrySlaughterPOS.Services.Implementations
{
    /// <summary>
    /// Advanced business service implementation for invoice management operations
    /// Implements sophisticated calculation algorithms, balance management, and transaction integrity
    /// </summary>
    public class InvoiceService : IInvoiceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<InvoiceService> _logger;

        public InvoiceService(IUnitOfWork unitOfWork, ILogger<InvoiceService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ServiceResult<Invoice>> CreateInvoiceAsync(CreateInvoiceDto dto)
        {
            try
            {
                _logger.LogInformation("Initiating invoice creation for Customer ID: {CustomerId}, Truck ID: {TruckId}",
                    dto.CustomerId, dto.TruckId);

                await _unitOfWork.BeginTransactionAsync();

                // Comprehensive validation of invoice data
                var validationResult = await ValidateInvoiceDataAsync(dto);
                if (!validationResult.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResult<Invoice>.Failure(validationResult.Errors, "Invoice validation failed");
                }

                // Retrieve and validate customer entity
                var customer = await _unitOfWork.Customers.GetByIdAsync(dto.CustomerId);
                if (customer == null || !customer.IsActive)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResult<Invoice>.Failure("Invalid or inactive customer selected", "CUSTOMER_INVALID");
                }

                // Retrieve and validate truck entity
                var truck = await _unitOfWork.Trucks.GetByIdAsync(dto.TruckId);
                if (truck == null || !truck.IsActive)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResult<Invoice>.Failure("Invalid or inactive truck selected", "TRUCK_INVALID");
                }

                // Generate unique invoice number with date-based sequencing
                var invoiceNumberResult = await GenerateInvoiceNumberAsync();
                if (!invoiceNumberResult.IsSuccess || string.IsNullOrEmpty(invoiceNumberResult.Data))
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResult<Invoice>.Failure("Failed to generate invoice number", "NUMBER_GENERATION_ERROR");
                }

                // Execute sophisticated invoice calculations
                var calculationDto = new InvoiceCalculationDto
                {
                    GrossWeight = dto.GrossWeight,
                    CagesWeight = dto.CagesWeight,
                    UnitPrice = dto.UnitPrice,
                    DiscountPercentage = dto.DiscountPercentage,
                    PreviousBalance = customer.TotalDebt
                };

                var calculationResult = await CalculateInvoiceAmountsAsync(calculationDto);
                if (!calculationResult.IsSuccess || calculationResult.Data == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResult<Invoice>.Failure("Invoice calculation failed", "CALCULATION_ERROR");
                }

                // Construct invoice entity with calculated values
                var invoice = new Invoice
                {
                    InvoiceNumber = invoiceNumberResult.Data,
                    CustomerId = dto.CustomerId,
                    TruckId = dto.TruckId,
                    InvoiceDate = dto.InvoiceDate,
                    GrossWeight = dto.GrossWeight,
                    CagesWeight = dto.CagesWeight,
                    CagesCount = dto.CagesCount,
                    NetWeight = calculationResult.Data.NetWeight,
                    UnitPrice = dto.UnitPrice,
                    TotalAmount = calculationResult.Data.TotalAmount,
                    DiscountPercentage = dto.DiscountPercentage,
                    FinalAmount = calculationResult.Data.FinalAmount,
                    PreviousBalance = customer.TotalDebt,
                    CurrentBalance = calculationResult.Data.CurrentBalance,
                    Notes = dto.Notes?.Trim(),
                    IsPaid = false,
                    CreatedDate = DateTime.Now
                };

                // Persist invoice entity
                await _unitOfWork.Invoices.AddAsync(invoice);

                // Update customer debt balance with atomic operation
                customer.TotalDebt = calculationResult.Data.CurrentBalance;
                customer.LastModifiedDate = DateTime.Now;
                _unitOfWork.Customers.Update(customer);

                // Commit transaction with comprehensive error handling
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Successfully created invoice {InvoiceNumber} for customer {CustomerName}, Amount: {FinalAmount:C}",
                    invoice.InvoiceNumber, customer.CustomerName, invoice.FinalAmount);

                return ServiceResult<Invoice>.Success(invoice, "Invoice created successfully");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Critical error during invoice creation for Customer ID: {CustomerId}", dto.CustomerId);
                return ServiceResult<Invoice>.Failure("A critical error occurred during invoice creation", "CREATION_ERROR", ex);
            }
        }

        public async Task<ServiceResult<InvoiceCalculationResult>> CalculateInvoiceAmountsAsync(InvoiceCalculationDto dto)
        {
            try
            {
                _logger.LogDebug("Executing invoice calculations for GrossWeight: {GrossWeight}, UnitPrice: {UnitPrice}",
                    dto.GrossWeight, dto.UnitPrice);

                // Input validation with business rule enforcement
                if (dto.GrossWeight <= 0 || dto.CagesWeight < 0 || dto.UnitPrice <= 0)
                {
                    return ServiceResult<InvoiceCalculationResult>.Failure(
                        "Invalid calculation parameters: weights and price must be positive values", "INVALID_PARAMETERS");
                }

                if (dto.CagesWeight >= dto.GrossWeight)
                {
                    return ServiceResult<InvoiceCalculationResult>.Failure(
                        "Cages weight cannot be greater than or equal to gross weight", "WEIGHT_LOGIC_ERROR");
                }

                if (dto.DiscountPercentage < 0 || dto.DiscountPercentage > 100)
                {
                    return ServiceResult<InvoiceCalculationResult>.Failure(
                        "Discount percentage must be between 0 and 100", "INVALID_DISCOUNT");
                }

                // Core calculation algorithms with precision handling
                var netWeight = dto.GrossWeight - dto.CagesWeight;
                var totalAmount = netWeight * dto.UnitPrice;
                var discountAmount = totalAmount * (dto.DiscountPercentage / 100);
                var finalAmount = totalAmount - discountAmount;
                var currentBalance = dto.PreviousBalance + finalAmount;

                // Round to appropriate decimal places for currency precision
                var result = new InvoiceCalculationResult
                {
                    NetWeight = Math.Round(netWeight, 3),
                    TotalAmount = Math.Round(totalAmount, 2),
                    DiscountAmount = Math.Round(discountAmount, 2),
                    FinalAmount = Math.Round(finalAmount, 2),
                    CurrentBalance = Math.Round(currentBalance, 2)
                };

                _logger.LogDebug("Calculation completed: NetWeight={NetWeight}, FinalAmount={FinalAmount}, CurrentBalance={CurrentBalance}",
                    result.NetWeight, result.FinalAmount, result.CurrentBalance);

                return ServiceResult<InvoiceCalculationResult>.Success(result, "Calculations completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during invoice calculations");
                return ServiceResult<InvoiceCalculationResult>.Failure("Calculation error occurred", "CALCULATION_ERROR", ex);
            }
        }

        public async Task<ServiceResult<string>> GenerateInvoiceNumberAsync()
        {
            try
            {
                var invoiceNumber = await _unitOfWork.Invoices.GenerateInvoiceNumberAsync();

                if (string.IsNullOrEmpty(invoiceNumber))
                {
                    return ServiceResult<string>.Failure("Failed to generate invoice number", "GENERATION_ERROR");
                }

                return ServiceResult<string>.Success(invoiceNumber, "Invoice number generated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating invoice number");
                return ServiceResult<string>.Failure("Invoice number generation failed", "GENERATION_ERROR", ex);
            }
        }

        public async Task<ServiceResult<bool>> ValidateInvoiceDataAsync(CreateInvoiceDto dto)
        {
            var validationErrors = new List<string>();

            try
            {
                // Entity reference validation
                if (dto.CustomerId <= 0)
                    validationErrors.Add("Valid customer selection is required");

                if (dto.TruckId <= 0)
                    validationErrors.Add("Valid truck selection is required");

                // Date validation with business rules
                if (dto.InvoiceDate.Date > DateTime.Now.Date)
                    validationErrors.Add("Invoice date cannot be in the future");

                if (dto.InvoiceDate.Date < DateTime.Now.Date.AddDays(-7))
                    validationErrors.Add("Invoice date cannot be more than 7 days in the past");

                // Weight validation with logical consistency checks
                if (dto.GrossWeight <= 0)
                    validationErrors.Add("Gross weight must be greater than zero");

                if (dto.CagesWeight < 0)
                    validationErrors.Add("Cages weight cannot be negative");

                if (dto.CagesWeight >= dto.GrossWeight)
                    validationErrors.Add("Cages weight must be less than gross weight");

                if (dto.CagesCount <= 0)
                    validationErrors.Add("Cages count must be greater than zero");

                // Price and discount validation
                if (dto.UnitPrice <= 0)
                    validationErrors.Add("Unit price must be greater than zero");

                if (dto.DiscountPercentage < 0 || dto.DiscountPercentage > 100)
                    validationErrors.Add("Discount percentage must be between 0 and 100");

                // Business logic validation
                var netWeight = dto.GrossWeight - dto.CagesWeight;
                if (netWeight <= 0)
                    validationErrors.Add("Net weight (Gross - Cages) must be greater than zero");

                // Check for reasonable weight per cage ratio
                var weightPerCage = netWeight / dto.CagesCount;
                if (weightPerCage < 0.5m || weightPerCage > 50m)
                    validationErrors.Add("Weight per cage seems unreasonable (should be between 0.5 and 50 kg)");

                if (validationErrors.Any())
                {
                    return ServiceResult<bool>.Failure(validationErrors, "Invoice validation failed");
                }

                return ServiceResult<bool>.Success(true, "Invoice validation passed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during invoice validation");
                return ServiceResult<bool>.Failure("Validation error occurred", "VALIDATION_ERROR", ex);
            }
        }

        public async Task<ServiceResult<SalesReportDto>> GenerateSalesReportAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogInformation("Generating comprehensive sales report for period: {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}",
                    startDate, endDate);

                // Retrieve invoice data with related entities
                var invoices = await _unitOfWork.Invoices.GetInvoicesByDateRangeAsync(startDate, endDate);
                var invoicesList = invoices.ToList();

                if (!invoicesList.Any())
                {
                    return ServiceResult<SalesReportDto>.Success(new SalesReportDto
                    {
                        StartDate = startDate,
                        EndDate = endDate
                    }, "No sales data found for the specified period");
                }

                // Aggregate calculations with LINQ optimization
                var totalSalesAmount = invoicesList.Sum(i => i.FinalAmount);
                var totalNetWeight = invoicesList.Sum(i => i.NetWeight);
                var totalInvoicesCount = invoicesList.Count;
                var averageUnitPrice = invoicesList.Average(i => i.UnitPrice);

                // Customer-based sales aggregation
                var customerSales = invoicesList
                    .GroupBy(i => new { i.CustomerId, i.Customer.CustomerName })
                    .ToDictionary(
                        g => g.Key.CustomerId,
                        g => new CustomerSalesDto
                        {
                            CustomerId = g.Key.CustomerId,
                            CustomerName = g.Key.CustomerName,
                            TotalAmount = g.Sum(i => i.FinalAmount),
                            TotalWeight = g.Sum(i => i.NetWeight),
                            InvoicesCount = g.Count()
                        });

                // Truck-based sales aggregation
                var truckSales = invoicesList
                    .GroupBy(i => new { i.TruckId, i.Truck.TruckNumber })
                    .ToDictionary(
                        g => g.Key.TruckId,
                        g => new TruckSalesDto
                        {
                            TruckId = g.Key.TruckId,
                            TruckNumber = g.Key.TruckNumber,
                            TotalAmount = g.Sum(i => i.FinalAmount),
                            TotalWeight = g.Sum(i => i.NetWeight),
                            InvoicesCount = g.Count()
                        });

                // Top invoices identification
                var topInvoices = invoicesList
                    .OrderByDescending(i => i.FinalAmount)
                    .Take(10)
                    .Select(i => new InvoiceSummaryDto
                    {
                        InvoiceId = i.InvoiceId,
                        InvoiceNumber = i.InvoiceNumber,
                        CustomerName = i.Customer.CustomerName,
                        InvoiceDate = i.InvoiceDate,
                        NetWeight = i.NetWeight,
                        FinalAmount = i.FinalAmount,
                        IsPaid = i.IsPaid
                    }).ToList();

                var salesReport = new SalesReportDto
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalSalesAmount = Math.Round(totalSalesAmount, 2),
                    TotalNetWeight = Math.Round(totalNetWeight, 3),
                    TotalInvoicesCount = totalInvoicesCount,
                    AverageUnitPrice = Math.Round(averageUnitPrice, 2),
                    CustomerSales = customerSales,
                    TruckSales = truckSales,
                    TopInvoices = topInvoices
                };

                _logger.LogInformation("Sales report generated successfully: {InvoiceCount} invoices, Total: {TotalAmount:C}",
                    totalInvoicesCount, totalSalesAmount);

                return ServiceResult<SalesReportDto>.Success(salesReport, "Sales report generated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating sales report for period {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}",
                    startDate, endDate);
                return ServiceResult<SalesReportDto>.Failure("Sales report generation failed", "REPORT_ERROR", ex);
            }
        }

        public async Task<ServiceResult<Invoice?>> GetInvoiceByIdAsync(int invoiceId)
        {
            try
            {
                var invoice = await _unitOfWork.Invoices.GetInvoiceWithDetailsAsync(invoiceId);
                if (invoice == null)
                {
                    return ServiceResult<Invoice?>.Failure("Invoice not found", "INVOICE_NOT_FOUND");
                }

                return ServiceResult<Invoice?>.Success(invoice, "Invoice retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoice ID: {InvoiceId}", invoiceId);
                return ServiceResult<Invoice?>.Failure("Invoice retrieval failed", "RETRIEVAL_ERROR", ex);
            }
        }

        public async Task<ServiceResult<IEnumerable<Invoice>>> GetInvoicesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var invoices = await _unitOfWork.Invoices.GetInvoicesByDateRangeAsync(startDate, endDate);
                return ServiceResult<IEnumerable<Invoice>>.Success(invoices, "Invoices retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoices for date range {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}",
                    startDate, endDate);
                return ServiceResult<IEnumerable<Invoice>>.Failure("Invoice retrieval failed", "RETRIEVAL_ERROR", ex);
            }
        }

        public async Task<ServiceResult<IEnumerable<Invoice>>> GetInvoicesByCustomerAsync(int customerId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var invoices = await _unitOfWork.Invoices.GetInvoicesByCustomerAsync(customerId, startDate, endDate);
                return ServiceResult<IEnumerable<Invoice>>.Success(invoices, "Customer invoices retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoices for Customer ID: {CustomerId}", customerId);
                return ServiceResult<IEnumerable<Invoice>>.Failure("Customer invoice retrieval failed", "RETRIEVAL_ERROR", ex);
            }
        }

        public async Task<ServiceResult<Invoice>> UpdateInvoiceAsync(int invoiceId, UpdateInvoiceDto dto)
        {
            try
            {
                _logger.LogInformation("Updating invoice ID: {InvoiceId}", invoiceId);

                await _unitOfWork.BeginTransactionAsync();

                var existingInvoice = await _unitOfWork.Invoices.GetInvoiceWithDetailsAsync(invoiceId);
                if (existingInvoice == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResult<Invoice>.Failure("Invoice not found", "INVOICE_NOT_FOUND");
                }

                // Validation of update data
                var createDto = new CreateInvoiceDto
                {
                    CustomerId = dto.CustomerId,
                    TruckId = dto.TruckId,
                    InvoiceDate = dto.InvoiceDate,
                    GrossWeight = dto.GrossWeight,
                    CagesWeight = dto.CagesWeight,
                    CagesCount = dto.CagesCount,
                    UnitPrice = dto.UnitPrice,
                    DiscountPercentage = dto.DiscountPercentage,
                    Notes = dto.Notes
                };

                var validationResult = await ValidateInvoiceDataAsync(createDto);
                if (!validationResult.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResult<Invoice>.Failure(validationResult.Errors, "Invoice validation failed");
                }

                // Recalculate customer balance impact
                var customer = existingInvoice.Customer;
                var previousBalance = customer.TotalDebt - existingInvoice.FinalAmount;

                var calculationDto = new InvoiceCalculationDto
                {
                    GrossWeight = dto.GrossWeight,
                    CagesWeight = dto.CagesWeight,
                    UnitPrice = dto.UnitPrice,
                    DiscountPercentage = dto.DiscountPercentage,
                    PreviousBalance = previousBalance
                };

                var calculationResult = await CalculateInvoiceAmountsAsync(calculationDto);
                if (!calculationResult.IsSuccess || calculationResult.Data == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResult<Invoice>.Failure("Invoice calculation failed", "CALCULATION_ERROR");
                }

                // Update invoice properties
                existingInvoice.GrossWeight = dto.GrossWeight;
                existingInvoice.CagesWeight = dto.CagesWeight;
                existingInvoice.CagesCount = dto.CagesCount;
                existingInvoice.NetWeight = calculationResult.Data.NetWeight;
                existingInvoice.UnitPrice = dto.UnitPrice;
                existingInvoice.TotalAmount = calculationResult.Data.TotalAmount;
                existingInvoice.DiscountPercentage = dto.DiscountPercentage;
                existingInvoice.FinalAmount = calculationResult.Data.FinalAmount;
                existingInvoice.CurrentBalance = calculationResult.Data.CurrentBalance;
                existingInvoice.Notes = dto.Notes?.Trim();

                // Update customer debt
                customer.TotalDebt = calculationResult.Data.CurrentBalance;
                customer.LastModifiedDate = DateTime.Now;

                _unitOfWork.Invoices.Update(existingInvoice);
                _unitOfWork.Customers.Update(customer);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Successfully updated invoice {InvoiceNumber}", existingInvoice.InvoiceNumber);
                return ServiceResult<Invoice>.Success(existingInvoice, "Invoice updated successfully");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error updating invoice ID: {InvoiceId}", invoiceId);
                return ServiceResult<Invoice>.Failure("Invoice update failed", "UPDATE_ERROR", ex);
            }
        }

        public async Task<ServiceResult<bool>> DeleteInvoiceAsync(int invoiceId)
        {
            try
            {
                _logger.LogInformation("Deleting invoice ID: {InvoiceId}", invoiceId);

                await _unitOfWork.BeginTransactionAsync();

                var invoice = await _unitOfWork.Invoices.GetInvoiceWithDetailsAsync(invoiceId);
                if (invoice == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResult<bool>.Failure("Invoice not found", "INVOICE_NOT_FOUND");
                }

                // Check for related payments
                if (invoice.Payments.Any())
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResult<bool>.Failure("Cannot delete invoice with associated payments", "HAS_PAYMENTS");
                }

                // Adjust customer balance
                var customer = invoice.Customer;
                customer.TotalDebt -= invoice.FinalAmount;
                customer.LastModifiedDate = DateTime.Now;

                _unitOfWork.Customers.Update(customer);
                _unitOfWork.Invoices.Remove(invoice);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Successfully deleted invoice {InvoiceNumber}", invoice.InvoiceNumber);
                return ServiceResult<bool>.Success(true, "Invoice deleted successfully");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error deleting invoice ID: {InvoiceId}", invoiceId);
                return ServiceResult<bool>.Failure("Invoice deletion failed", "DELETE_ERROR", ex);
            }
        }

        public async Task<ServiceResult<bool>> MarkInvoiceAsPaidAsync(int invoiceId)
        {
            try
            {
                _logger.LogInformation("Marking invoice ID {InvoiceId} as paid", invoiceId);

                var invoice = await _unitOfWork.Invoices.GetByIdAsync(invoiceId);
                if (invoice == null)
                {
                    return ServiceResult<bool>.Failure("Invoice not found", "INVOICE_NOT_FOUND");
                }

                invoice.IsPaid = true;
                _unitOfWork.Invoices.Update(invoice);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully marked invoice {InvoiceNumber} as paid", invoice.InvoiceNumber);
                return ServiceResult<bool>.Success(true, "Invoice marked as paid");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking invoice ID {InvoiceId} as paid", invoiceId);
                return ServiceResult<bool>.Failure("Failed to mark invoice as paid", "UPDATE_ERROR", ex);
            }
        }
    }
}