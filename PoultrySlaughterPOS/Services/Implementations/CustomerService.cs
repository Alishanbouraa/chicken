using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Models.DTOs;
using PoultrySlaughterPOS.Models.Entities;
using PoultrySlaughterPOS.Services.Interfaces;

namespace PoultrySlaughterPOS.Services.Implementations
{
    /// <summary>
    /// Advanced business service implementation for customer management operations
    /// Provides sophisticated debt tracking, payment processing, and account analysis
    /// </summary>
    public class CustomerService : ICustomerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CustomerService> _logger;

        public CustomerService(IUnitOfWork unitOfWork, ILogger<CustomerService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ServiceResult<Customer>> CreateCustomerAsync(CreateCustomerDto dto)
        {
            try
            {
                _logger.LogInformation("Creating new customer: {CustomerName}", dto.CustomerName);

                // Comprehensive validation of customer data
                var validationResult = await ValidateCustomerDataAsync(dto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<Customer>.Failure(validationResult.Errors, "Customer validation failed");
                }

                // Check for duplicate customer names
                var existingCustomer = await _unitOfWork.Customers.FirstOrDefaultAsync(
                    c => c.CustomerName.ToLower() == dto.CustomerName.ToLower().Trim() && c.IsActive);

                if (existingCustomer != null)
                {
                    return ServiceResult<Customer>.Failure(
                        $"A customer with the name '{dto.CustomerName}' already exists", "DUPLICATE_CUSTOMER");
                }

                // Construct customer entity with validated data
                var customer = new Customer
                {
                    CustomerName = dto.CustomerName.Trim(),
                    PhoneNumber = dto.PhoneNumber?.Trim(),
                    Address = dto.Address?.Trim(),
                    CreditLimit = dto.CreditLimit,
                    TotalDebt = 0,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                await _unitOfWork.Customers.AddAsync(customer);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully created customer ID: {CustomerId}, Name: {CustomerName}",
                    customer.CustomerId, customer.CustomerName);

                return ServiceResult<Customer>.Success(customer, "Customer created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer: {CustomerName}", dto.CustomerName);
                return ServiceResult<Customer>.Failure("Customer creation failed", "CREATION_ERROR", ex);
            }
        }

        public async Task<ServiceResult<bool>> ProcessPaymentAsync(int customerId, ProcessPaymentDto dto)
        {
            try
            {
                _logger.LogInformation("Processing payment for Customer ID: {CustomerId}, Amount: {Amount:C}",
                    customerId, dto.Amount);

                await _unitOfWork.BeginTransactionAsync();

                // Retrieve and validate customer
                var customer = await _unitOfWork.Customers.GetByIdAsync(customerId);
                if (customer == null || !customer.IsActive)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResult<bool>.Failure("Customer not found or inactive", "CUSTOMER_NOT_FOUND");
                }

                // Validate payment amount
                if (dto.Amount <= 0)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResult<bool>.Failure("Payment amount must be greater than zero", "INVALID_AMOUNT");
                }

                if (dto.Amount > customer.TotalDebt && customer.TotalDebt > 0)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResult<bool>.Failure(
                        $"Payment amount ({dto.Amount:C}) exceeds customer debt ({customer.TotalDebt:C})", "OVERPAYMENT");
                }

                // Validate specific invoice if provided
                Invoice? targetInvoice = null;
                if (dto.InvoiceId.HasValue)
                {
                    targetInvoice = await _unitOfWork.Invoices.FirstOrDefaultAsync(
                        i => i.InvoiceId == dto.InvoiceId.Value && i.CustomerId == customerId);

                    if (targetInvoice == null)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return ServiceResult<bool>.Failure("Specified invoice not found for this customer", "INVOICE_NOT_FOUND");
                    }

                    if (targetInvoice.IsPaid)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return ServiceResult<bool>.Failure("Invoice is already marked as paid", "INVOICE_ALREADY_PAID");
                    }
                }

                // Create payment entity
                var payment = new Payment
                {
                    CustomerId = customerId,
                    InvoiceId = dto.InvoiceId,
                    PaymentDate = dto.PaymentDate,
                    Amount = dto.Amount,
                    PaymentMethod = dto.PaymentMethod.Trim(),
                    ReferenceNumber = dto.ReferenceNumber?.Trim(),
                    Notes = dto.Notes?.Trim(),
                    CreatedDate = DateTime.Now
                };

                await _unitOfWork.Payments.AddAsync(payment);

                // Update customer debt balance
                customer.TotalDebt = Math.Max(0, customer.TotalDebt - dto.Amount);
                customer.LastModifiedDate = DateTime.Now;
                _unitOfWork.Customers.Update(customer);

                // Mark invoice as paid if full payment
                if (targetInvoice != null && dto.Amount >= targetInvoice.FinalAmount)
                {
                    targetInvoice.IsPaid = true;
                    _unitOfWork.Invoices.Update(targetInvoice);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Successfully processed payment ID: {PaymentId} for Customer: {CustomerName}, New Balance: {Balance:C}",
                    payment.PaymentId, customer.CustomerName, customer.TotalDebt);

                return ServiceResult<bool>.Success(true, "Payment processed successfully");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error processing payment for Customer ID: {CustomerId}", customerId);
                return ServiceResult<bool>.Failure("Payment processing failed", "PAYMENT_ERROR", ex);
            }
        }

        public async Task<ServiceResult<CustomerAccountSummaryDto>> GetCustomerAccountSummaryAsync(int customerId)
        {
            try
            {
                _logger.LogInformation("Generating account summary for Customer ID: {CustomerId}", customerId);

                // Retrieve customer with transaction history
                var customer = await _unitOfWork.Customers.GetCustomerWithTransactionsAsync(customerId);
                if (customer == null)
                {
                    return ServiceResult<CustomerAccountSummaryDto>.Failure("Customer not found", "CUSTOMER_NOT_FOUND");
                }

                // Calculate aggregated statistics
                var totalPurchases = customer.Invoices.Sum(i => i.FinalAmount);
                var totalPayments = customer.Payments.Sum(p => p.Amount);
                var unpaidInvoicesCount = customer.Invoices.Count(i => !i.IsPaid);
                var lastTransactionDate = new[] {
                    customer.Invoices.Any() ? customer.Invoices.Max(i => i.InvoiceDate) : DateTime.MinValue,
                    customer.Payments.Any() ? customer.Payments.Max(p => p.PaymentDate) : DateTime.MinValue
                }.Max();

                // Recent transaction summaries
                var recentInvoices = customer.Invoices
                    .OrderByDescending(i => i.InvoiceDate)
                    .Take(10)
                    .Select(i => new InvoiceSummaryDto
                    {
                        InvoiceId = i.InvoiceId,
                        InvoiceNumber = i.InvoiceNumber,
                        CustomerName = customer.CustomerName,
                        InvoiceDate = i.InvoiceDate,
                        NetWeight = i.NetWeight,
                        FinalAmount = i.FinalAmount,
                        IsPaid = i.IsPaid
                    }).ToList();

                var recentPayments = customer.Payments
                    .OrderByDescending(p => p.PaymentDate)
                    .Take(10)
                    .Select(p => new PaymentSummaryDto
                    {
                        PaymentId = p.PaymentId,
                        PaymentDate = p.PaymentDate,
                        Amount = p.Amount,
                        PaymentMethod = p.PaymentMethod,
                        ReferenceNumber = p.ReferenceNumber,
                        Notes = p.Notes,
                        InvoiceId = p.InvoiceId,
                        InvoiceNumber = p.Invoice?.InvoiceNumber
                    }).ToList();

                var accountSummary = new CustomerAccountSummaryDto
                {
                    CustomerId = customer.CustomerId,
                    CustomerName = customer.CustomerName,
                    PhoneNumber = customer.PhoneNumber,
                    Address = customer.Address,
                    CurrentBalance = customer.TotalDebt,
                    CreditLimit = customer.CreditLimit,
                    AvailableCredit = Math.Max(0, customer.CreditLimit - customer.TotalDebt),
                    LastTransactionDate = lastTransactionDate,
                    TotalInvoicesCount = customer.Invoices.Count,
                    UnpaidInvoicesCount = unpaidInvoicesCount,
                    TotalPurchases = totalPurchases,
                    TotalPayments = totalPayments,
                    RecentInvoices = recentInvoices,
                    RecentPayments = recentPayments
                };

                _logger.LogInformation("Generated account summary for Customer: {CustomerName}, Balance: {Balance:C}",
                    customer.CustomerName, customer.TotalDebt);

                return ServiceResult<CustomerAccountSummaryDto>.Success(accountSummary, "Account summary generated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating account summary for Customer ID: {CustomerId}", customerId);
                return ServiceResult<CustomerAccountSummaryDto>.Failure("Account summary generation failed", "SUMMARY_ERROR", ex);
            }
        }

        public async Task<ServiceResult<DebtorsReportDto>> GenerateDebtorsReportAsync()
        {
            try
            {
                _logger.LogInformation("Generating comprehensive debtors report");

                // Retrieve customers with outstanding debt
                var customersWithDebt = await _unitOfWork.Customers.GetCustomersWithDebtAsync();
                var customersList = customersWithDebt.ToList();

                if (!customersList.Any())
                {
                    return ServiceResult<DebtorsReportDto>.Success(new DebtorsReportDto
                    {
                        ReportDate = DateTime.Now
                    }, "No customers with outstanding debt found");
                }

                // Calculate summary statistics
                var totalOutstandingDebt = customersList.Sum(c => c.TotalDebt);
                var totalDebtorsCount = customersList.Count;
                var averageDebtPerCustomer = totalDebtorsCount > 0 ? totalOutstandingDebt / totalDebtorsCount : 0;

                // Generate detailed customer debt summaries
                var customerDebts = new List<CustomerDebtSummaryDto>();
                var topDebtors = new List<CustomerAccountSummaryDto>();

                foreach (var customer in customersList.OrderByDescending(c => c.TotalDebt))
                {
                    // Get customer transaction details
                    var customerWithTransactions = await _unitOfWork.Customers.GetCustomerWithTransactionsAsync(customer.CustomerId);
                    if (customerWithTransactions == null) continue;

                    var unpaidInvoices = customerWithTransactions.Invoices.Where(i => !i.IsPaid).ToList();
                    var oldestUnpaid = unpaidInvoices.OrderBy(i => i.InvoiceDate).FirstOrDefault();
                    var lastTransaction = new[] {
                        customerWithTransactions.Invoices.Any() ? customerWithTransactions.Invoices.Max(i => i.InvoiceDate) : DateTime.MinValue,
                        customerWithTransactions.Payments.Any() ? customerWithTransactions.Payments.Max(p => p.PaymentDate) : DateTime.MinValue
                    }.Max();

                    var daysSinceLastTransaction = lastTransaction != DateTime.MinValue ?
                        (DateTime.Now - lastTransaction).Days : int.MaxValue;

                    // Customer debt summary
                    var debtSummary = new CustomerDebtSummaryDto
                    {
                        CustomerId = customer.CustomerId,
                        CustomerName = customer.CustomerName,
                        TotalDebt = customer.TotalDebt,
                        CreditLimit = customer.CreditLimit,
                        LastTransactionDate = lastTransaction,
                        DaysSinceLastTransaction = daysSinceLastTransaction,
                        UnpaidInvoicesCount = unpaidInvoices.Count,
                        OldestUnpaidAmount = oldestUnpaid?.FinalAmount ?? 0,
                        OldestUnpaidDate = oldestUnpaid?.InvoiceDate
                    };

                    customerDebts.Add(debtSummary);

                    // Include in top debtors if within top 10
                    if (topDebtors.Count < 10)
                    {
                        var accountSummaryResult = await GetCustomerAccountSummaryAsync(customer.CustomerId);
                        if (accountSummaryResult.IsSuccess && accountSummaryResult.Data != null)
                        {
                            topDebtors.Add(accountSummaryResult.Data);
                        }
                    }
                }

                // Debt aging analysis
                var debtAging = new Dictionary<string, decimal>
                {
                    ["0-30 days"] = 0,
                    ["31-60 days"] = 0,
                    ["61-90 days"] = 0,
                    ["Over 90 days"] = 0
                };

                foreach (var debt in customerDebts)
                {
                    var agingKey = debt.DaysSinceLastTransaction switch
                    {
                        <= 30 => "0-30 days",
                        <= 60 => "31-60 days",
                        <= 90 => "61-90 days",
                        _ => "Over 90 days"
                    };
                    debtAging[agingKey] += debt.TotalDebt;
                }

                var debtorsReport = new DebtorsReportDto
                {
                    ReportDate = DateTime.Now,
                    TotalOutstandingDebt = Math.Round(totalOutstandingDebt, 2),
                    TotalDebtorsCount = totalDebtorsCount,
                    AverageDebtPerCustomer = Math.Round(averageDebtPerCustomer, 2),
                    CustomerDebts = customerDebts,
                    DebtAging = debtAging,
                    TopDebtors = topDebtors
                };

                _logger.LogInformation("Generated debtors report: {DebtorsCount} customers, Total Debt: {TotalDebt:C}",
                    totalDebtorsCount, totalOutstandingDebt);

                return ServiceResult<DebtorsReportDto>.Success(debtorsReport, "Debtors report generated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating debtors report");
                return ServiceResult<DebtorsReportDto>.Failure("Debtors report generation failed", "REPORT_ERROR", ex);
            }
        }

        public async Task<ServiceResult<bool>> ValidateCustomerDataAsync(CreateCustomerDto dto)
        {
            var validationErrors = new List<string>();

            try
            {
                // Name validation
                if (string.IsNullOrWhiteSpace(dto.CustomerName))
                {
                    validationErrors.Add("Customer name is required");
                }
                else if (dto.CustomerName.Trim().Length < 2)
                {
                    validationErrors.Add("Customer name must be at least 2 characters long");
                }

                // Phone number validation
                if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
                {
                    var cleanPhone = dto.PhoneNumber.Trim();
                    if (cleanPhone.Length > 15)
                    {
                        validationErrors.Add("Phone number cannot exceed 15 characters");
                    }

                    if (!System.Text.RegularExpressions.Regex.IsMatch(cleanPhone, @"^[\d\s\-\+\(\)]+$"))
                    {
                        validationErrors.Add("Phone number contains invalid characters");
                    }
                }

                // Credit limit validation
                if (dto.CreditLimit < 0)
                {
                    validationErrors.Add("Credit limit cannot be negative");
                }

                if (dto.CreditLimit > 999999.99m)
                {
                    validationErrors.Add("Credit limit cannot exceed 999,999.99");
                }

                if (validationErrors.Any())
                {
                    return ServiceResult<bool>.Failure(validationErrors, "Customer validation failed");
                }

                return ServiceResult<bool>.Success(true, "Customer validation passed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during customer validation");
                return ServiceResult<bool>.Failure("Validation error occurred", "VALIDATION_ERROR", ex);
            }
        }

        public async Task<ServiceResult<decimal>> CalculateCustomerBalanceAsync(int customerId)
        {
            try
            {
                var balance = await _unitOfWork.Customers.GetCustomerTotalDebtAsync(customerId);
                return ServiceResult<decimal>.Success(balance, "Customer balance calculated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating customer balance for ID: {CustomerId}", customerId);
                return ServiceResult<decimal>.Failure("Balance calculation failed", "CALCULATION_ERROR", ex);
            }
        }

        public async Task<ServiceResult<Customer?>> GetCustomerByIdAsync(int customerId)
        {
            try
            {
                var customer = await _unitOfWork.Customers.GetByIdAsync(customerId);
                if (customer == null)
                {
                    return ServiceResult<Customer?>.Failure("Customer not found", "CUSTOMER_NOT_FOUND");
                }

                return ServiceResult<Customer?>.Success(customer, "Customer retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer ID: {CustomerId}", customerId);
                return ServiceResult<Customer?>.Failure("Customer retrieval failed", "RETRIEVAL_ERROR", ex);
            }
        }

        public async Task<ServiceResult<IEnumerable<Customer>>> GetAllActiveCustomersAsync()
        {
            try
            {
                var customers = await _unitOfWork.Customers.GetActiveCustomersAsync();
                return ServiceResult<IEnumerable<Customer>>.Success(customers, "Active customers retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active customers");
                return ServiceResult<IEnumerable<Customer>>.Failure("Customer retrieval failed", "RETRIEVAL_ERROR", ex);
            }
        }

        public async Task<ServiceResult<IEnumerable<Customer>>> GetCustomersWithDebtAsync()
        {
            try
            {
                var customers = await _unitOfWork.Customers.GetCustomersWithDebtAsync();
                return ServiceResult<IEnumerable<Customer>>.Success(customers, "Customers with debt retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customers with debt");
                return ServiceResult<IEnumerable<Customer>>.Failure("Customer retrieval failed", "RETRIEVAL_ERROR", ex);
            }
        }

        public async Task<ServiceResult<IEnumerable<Customer>>> SearchCustomersAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return ServiceResult<IEnumerable<Customer>>.Failure("Search term cannot be empty", "INVALID_SEARCH_TERM");
                }

                var customers = await _unitOfWork.Customers.SearchCustomersAsync(searchTerm.Trim());
                return ServiceResult<IEnumerable<Customer>>.Success(customers, "Customer search completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching customers with term: {SearchTerm}", searchTerm);
                return ServiceResult<IEnumerable<Customer>>.Failure("Customer search failed", "SEARCH_ERROR", ex);
            }
        }

        public async Task<ServiceResult<Customer>> UpdateCustomerAsync(int customerId, UpdateCustomerDto dto)
        {
            try
            {
                _logger.LogInformation("Updating customer ID: {CustomerId}", customerId);

                var existingCustomer = await _unitOfWork.Customers.GetByIdAsync(customerId);
                if (existingCustomer == null)
                {
                    return ServiceResult<Customer>.Failure("Customer not found", "CUSTOMER_NOT_FOUND");
                }

                // Validate update data
                var createDto = new CreateCustomerDto
                {
                    CustomerName = dto.CustomerName,
                    PhoneNumber = dto.PhoneNumber,
                    Address = dto.Address,
                    CreditLimit = dto.CreditLimit
                };

                var validationResult = await ValidateCustomerDataAsync(createDto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<Customer>.Failure(validationResult.Errors, "Customer validation failed");
                }

                // Check for duplicate name (excluding current customer)
                var duplicateCustomer = await _unitOfWork.Customers.FirstOrDefaultAsync(
                    c => c.CustomerName.ToLower() == dto.CustomerName.ToLower().Trim() &&
                         c.IsActive && c.CustomerId != customerId);

                if (duplicateCustomer != null)
                {
                    return ServiceResult<Customer>.Failure(
                        $"Another customer with the name '{dto.CustomerName}' already exists", "DUPLICATE_CUSTOMER");
                }

                // Update customer properties
                existingCustomer.CustomerName = dto.CustomerName.Trim();
                existingCustomer.PhoneNumber = dto.PhoneNumber?.Trim();
                existingCustomer.Address = dto.Address?.Trim();
                existingCustomer.CreditLimit = dto.CreditLimit;
                existingCustomer.IsActive = dto.IsActive;
                existingCustomer.LastModifiedDate = DateTime.Now;

                _unitOfWork.Customers.Update(existingCustomer);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully updated customer ID: {CustomerId}, Name: {CustomerName}",
                    customerId, existingCustomer.CustomerName);

                return ServiceResult<Customer>.Success(existingCustomer, "Customer updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer ID: {CustomerId}", customerId);
                return ServiceResult<Customer>.Failure("Customer update failed", "UPDATE_ERROR", ex);
            }
        }

        public async Task<ServiceResult<bool>> DeleteCustomerAsync(int customerId)
        {
            try
            {
                _logger.LogInformation("Deleting customer ID: {CustomerId}", customerId);

                var customer = await _unitOfWork.Customers.GetCustomerWithTransactionsAsync(customerId);
                if (customer == null)
                {
                    return ServiceResult<bool>.Failure("Customer not found", "CUSTOMER_NOT_FOUND");
                }

                // Check for existing transactions
                if (customer.Invoices.Any() || customer.Payments.Any())
                {
                    return ServiceResult<bool>.Failure(
                        "Cannot delete customer with existing transactions. Consider deactivating instead.",
                        "HAS_TRANSACTIONS");
                }

                _unitOfWork.Customers.Remove(customer);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted customer ID: {CustomerId}, Name: {CustomerName}",
                    customerId, customer.CustomerName);

                return ServiceResult<bool>.Success(true, "Customer deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer ID: {CustomerId}", customerId);
                return ServiceResult<bool>.Failure("Customer deletion failed", "DELETE_ERROR", ex);
            }
        }
    }
}